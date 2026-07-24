using Godot;

// Moteur de dialogue réutilisable posé sur un PNJ/panneau qui implémente Talkative
// (par composition : ajouté en enfant du nœud « parlant », ou ciblé via Cible). Il gère
// la détection de proximité du joueur, l'affichage de la bulle « banquise » au-dessus du
// model, le rappel de touche par défaut, et le défilement des lignes à l'appui de l'action.
// Deux déclenchements (selon Talkative.DeclencheAuPassage) : automatique au passage, ou
// sur touche quand le joueur est proche. Aucune logique propre au PNJ ne vit ici.
public partial class DeclencheurDialogue : DeclencheurZone
{
	// Nœud parlant ciblé ; par défaut le parent s'il implémente Talkative.
	[Export] public NodePath Cible;

	// Libellé de touche affiché dans le rappel (adapté à la reliure de "action").
	[Export] public string LibelleTouche = "Espace";

	private Talkative _parlant;
	// Non nul si la cible défile toute seule (bavardage d'ambiance) : voir TalkativeAutomatique.
	private TalkativeAutomatique _auto;
	// Non nul si la cible génère sa réplique via le LLM local : voir OllamaTalkative.
	private OllamaTalkative _dyn;
	private BulleDialogue _bulle;
	private bool _joueurProche;
	private bool _enDialogue;
	// Génération LLM en cours (streaming). Le dialogue dynamique est piloté par la proximité,
	// jamais par la touche : il démarre à l'approche et se ferme à la sortie de zone.
	private bool _enFlux;
	private int _ligne;
	private float _minuteurAuto;
	private readonly RandomNumberGenerator _rng = new();

	// Hook DeclencheurZone (avant le branchement de l'entrée) : résout le Talkative
	// et prépare la bulle. Retourne false — donc le déclencheur reste inerte — si
	// rien de parlant n'est trouvé, ce qui évite un NullReference à l'entrée.
	protected override bool PreparerDeclencheur()
	{
		_parlant = ResoudreParlant();
		if (_parlant == null)
		{
			GD.PushWarning($"DeclencheurDialogue ({GetPath()}) : aucun Talkative trouvé (parent ou Cible).");
			return false;
		}

		_auto = _parlant as TalkativeAutomatique;   // défilement automatique si la cible l'implémente
		_dyn = _parlant as OllamaTalkative;         // génération LLM si la cible l'implémente
		_rng.Randomize();

		_bulle = new BulleDialogue();
		AddChild(_bulle);
		_bulle.Position = ToLocal(_parlant.PointBulle);

		BodyExited += OnBodyExited;   // la sortie n'est pas couverte par DeclencheurZone
		return true;
	}

	private Talkative ResoudreParlant()
	{
		if (Cible != null && !Cible.IsEmpty)
			return GetNodeOrNull(Cible) as Talkative;
		return GetParent() as Talkative;
	}

	// Entrée du joueur (hook DeclencheurZone) : dialogue au passage, ou rappel de
	// touche en mode interaction. _parlant est garanti non nul ici (voir Preparer).
	protected override void SurEntreeJoueur(Player joueur)
	{
		_joueurProche = true;

		if (!PeutDialoguer())
			return;

		// Démarrent tout seuls à l'approche : le bavard automatique (défilement au minuteur)
		// ET le PNJ à dialogue dynamique actif (génération LLM) — dans les deux cas la touche
		// n'est pas détournée, Espace reste le saut. Le statique manuel, lui, passe par le
		// rappel de touche et attend l'appui.
		if (_parlant.DeclencheAuPassage || _auto != null || (_dyn?.DialogueDynamiqueActif ?? false))
			DemarrerDialogue();
		else
			AfficherRappel();
	}

	private void OnBodyExited(Node2D corps)
	{
		if (corps is not Player)
			return;
		_joueurProche = false;
		TerminerDialogue(sortie: true);
	}

	public override void _Process(double delta)
	{
		if (!_joueurProche || _parlant == null)
			return;

		// Flux LLM (dialogue dynamique) : entièrement piloté par la proximité, jamais par la
		// touche. La bulle grandit à mesure que le texte arrive et la fermeture se fait à la
		// sortie de zone (OnBodyExited) — Espace n'y touche pas et garde son rôle de saut.
		if (_enFlux)
			return;

		// Défilement automatique : la bulle avance sur minuteur, sans appui de touche.
		if (_enDialogue && _auto != null)
		{
			_minuteurAuto -= (float)delta;
			if (_minuteurAuto <= 0f)
			{
				_minuteurAuto = _auto.IntervalleAuto;
				LigneSuivante();
			}
			return;
		}

		if (!Input.IsActionJustPressed("action"))
			return;

		// Espace ne pilote le dialogue (démarrage / ligne suivante) que si le joueur est
		// immobile : s'il se déplace, la même touche sert à sauter (voir Player) et ne doit
		// donc pas aussi déclencher ni faire avancer le dialogue.
		if (!Mathf.IsZeroApprox(Input.GetAxis("move_left", "move_right")))
			return;

		if (_enDialogue)
			LigneSuivante();
		else if (!_parlant.DeclencheAuPassage && PeutDialoguer())
			DemarrerDialogue();
	}

	// Le PNJ a-t-il quelque chose à dire ? Une réplique statique (Lignes) OU un dialogue
	// dynamique actif (le LLM fournira le texte). Un PNJ IA sans Lignes reste donc parlant.
	private bool PeutDialoguer()
	{
		if (!_parlant.PeutParler())
			return false;
		return _parlant.Dialogue.Count > 0 || (_dyn?.DialogueDynamiqueActif ?? false);
	}

	private void AfficherRappel()
	{
		// À portée : la touche de saut (Espace) parle au lieu de faire sauter.
		GameState.Instance.DialogueDisponible = true;
		_bulle.Position = ToLocal(_parlant.PointBulle);
		_bulle.AfficherRappel(LibelleTouche);
	}

	private void DemarrerDialogue()
	{
		// Branche LLM prioritaire : réplique générée en streaming (court-circuite le mode
		// auto et le défilement statique). Si le dynamique n'est pas actif, chemin statique.
		if (_dyn != null && _dyn.DialogueDynamiqueActif)
		{
			DemarrerFlux();
			return;
		}

		DemarrerDialogueStatique();
	}

	// Lance la génération LLM : bulle « … » immédiate, puis rendu incrémental token par token.
	// Déclenché automatiquement à l'approche et fermé à la sortie de zone : la touche de saut
	// n'est PAS détournée (on ne pose pas DialogueDisponible), Espace reste le saut.
	private void DemarrerFlux()
	{
		_enDialogue = true;
		_enFlux = true;
		_parlant.SurDebutDialogue();
		_bulle.Position = ToLocal(_parlant.PointBulle);

		_bulle.AfficherDialogue("…"); // retour visuel avant le 1er token

		var svc = OllamaService.Instance;
		svc.GenererFlux(
			svc.ConstruireContexte(_dyn.Contexte),
			_dyn.Invite,
			_dyn.MotMoyenParReponse,
			surChunk: texte => { if (_enFlux) _bulle.MettreAJourFlux(texte); },
			surFin: null,
			surErreur: ReplierSurStatique);
	}

	// Échec de génération : bascule sur le chemin statique (Lignes) sans repasser par la
	// branche dynamique (qui relancerait un flux). Si le PNJ n'a aucune Ligne, on ferme.
	private void ReplierSurStatique()
	{
		_enFlux = false;
		if (_parlant.Dialogue.Count == 0)
		{
			TerminerDialogue(sortie: false);
			return;
		}
		DemarrerDialogueStatique();
	}

	private void DemarrerDialogueStatique()
	{
		_enDialogue = true;
		_ligne = _parlant.Aleatoire ? _rng.RandiRange(0, _parlant.Dialogue.Count - 1) : 0;
		_parlant.SurDebutDialogue();
		_bulle.Position = ToLocal(_parlant.PointBulle);

		if (_auto != null)
		{
			// Bavardage automatique : minuteur armé, pas de détournement de la touche de saut.
			_minuteurAuto = _auto.IntervalleAuto;
			_auto.Incrementer();
		}
		else
		{
			GameState.Instance.DialogueDisponible = true;
		}

		_bulle.AfficherDialogue(_parlant.Dialogue[_ligne]);
	}

	private void LigneSuivante()
	{
		// Mode aléatoire manuel : une seule réplique, puis fin.
		if (_parlant.Aleatoire && _auto == null)
		{
			TerminerDialogue(sortie: false);
			return;
		}

		// Choix de la ligne suivante : nouvelle réplique au hasard, ou ligne d'après.
		if (_parlant.Aleatoire)
		{
			_ligne = _rng.RandiRange(0, _parlant.Dialogue.Count - 1);
		}
		else
		{
			_ligne++;
			if (_ligne >= _parlant.Dialogue.Count)
			{
				if (_auto == null)
				{
					TerminerDialogue(sortie: false);
					return;
				}
				_ligne = 0;   // bavardage automatique : on boucle tant que le joueur est là
			}
		}

		_auto?.Incrementer();
		_bulle.AfficherDialogue(_parlant.Dialogue[_ligne]);
	}

	private void TerminerDialogue(bool sortie)
	{
		// Flux LLM : couper la génération en cours avant de fermer.
		if (_enFlux)
		{
			OllamaService.Instance?.AnnulerGeneration();
			_enFlux = false;
		}

		bool etaitEnDialogue = _enDialogue;
		_enDialogue = false;

		if (etaitEnDialogue)
			_parlant.SurFinDialogue();

		// Fin « normale » du dialogue statique manuel avec le joueur encore proche : on revient
		// au rappel de touche pour pouvoir reparler. Exclu pour le dynamique actif, qui ne doit
		// jamais détourner Espace (il se relance tout seul à la prochaine approche).
		if (!sortie && _joueurProche && !_parlant.DeclencheAuPassage
			&& !(_dyn?.DialogueDynamiqueActif ?? false) && PeutDialoguer())
		{
			AfficherRappel();
			return;
		}

		_auto?.Cacher();
		_bulle?.Cacher();
		GameState.Instance.DialogueDisponible = false;
	}
}
