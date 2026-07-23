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
	// Génération LLM en cours (streaming) ; _fluxPret = génération terminée (l'action ferme alors).
	private bool _enFlux;
	private bool _fluxPret;
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

		// Un bavard automatique démarre toujours au passage (son défilement ne dépend
		// pas de la touche) ; sinon on suit le mode déclaré par le Talkative.
		if (_parlant.DeclencheAuPassage || _auto != null)
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

		// Flux LLM : on attend la fin de génération (l'action ne fait rien pendant le
		// streaming), puis un appui « action » ferme la bulle.
		if (_enFlux)
		{
			if (_fluxPret && Input.IsActionJustPressed("action"))
				TerminerDialogue(sortie: false);
			return;
		}

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

	// Lance la génération LLM : bulle « … » immédiate, puis rendu incrémental token par
	// token. Traité comme un dialogue manuel (la touche de saut parle, pas de minuteur auto).
	private void DemarrerFlux()
	{
		_enDialogue = true;
		_enFlux = true;
		_fluxPret = false;
		_parlant.SurDebutDialogue();
		_bulle.Position = ToLocal(_parlant.PointBulle);
		GameState.Instance.DialogueDisponible = true;

		_bulle.AfficherDialogue("…"); // retour visuel avant le 1er token

		var svc = OllamaService.Instance;
		svc.GenererFlux(
			svc.ConstruireContexte(_dyn.Contexte),
			_dyn.Invite,
			surChunk: texte => { if (_enFlux) _bulle.MettreAJourFlux(texte); },
			surFin: () => _fluxPret = true,
			surErreur: ReplierSurStatique);
	}

	// Échec de génération : bascule sur le chemin statique (Lignes) sans repasser par la
	// branche dynamique (qui relancerait un flux). Si le PNJ n'a aucune Ligne, on ferme.
	private void ReplierSurStatique()
	{
		_enFlux = false;
		_fluxPret = false;
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
			_fluxPret = false;
		}

		bool etaitEnDialogue = _enDialogue;
		_enDialogue = false;

		if (etaitEnDialogue)
			_parlant.SurFinDialogue();

		// Fin « normale » avec le joueur encore proche (mode touche) et dialogue encore
		// autorisé : on revient au rappel de touche pour pouvoir reparler.
		if (!sortie && _joueurProche && !_parlant.DeclencheAuPassage && PeutDialoguer())
		{
			AfficherRappel();
			return;
		}

		_auto?.Cacher();
		_bulle?.Cacher();
		GameState.Instance.DialogueDisponible = false;
	}
}
