using System;
using System.Collections.Generic;
using System.Text;
using Godot;

// Moteur de dialogue réutilisable posé sur un PNJ/panneau qui implémente Talkative
// (par composition : ajouté en enfant du nœud « parlant », ou ciblé via Cible). Il gère
// la détection de proximité du joueur, l'affichage de la bulle « banquise » au-dessus du
// model, le rappel de touche par défaut, et le défilement des lignes à l'appui de l'action.
// Deux déclenchements (selon Talkative.DeclencheAuPassage) : automatique au passage, ou
// sur touche quand le joueur est proche. Aucune logique propre au PNJ ne vit ici.
//
// Il connaît trois capacités OPTIONNELLES, détectées par simple cast (un Talkative qui
// n'en porte aucune garde le comportement de base) :
//   - TalkativeAutomatique : la bulle défile toute seule sur minuteur ;
//   - OllamaTalkative      : la réplique est générée par le LLM local, en streaming ;
//   - TalkativeAChoix      : le joueur RÉPOND en choisissant sa réplique dans une liste,
//     et la conversation parcourt un arbre NoeudDialogue/ChoixDialogue (.tres).
// Les deux dernières se combinent : un choix sans Reponse écrite est envoyé au LLM comme
// invite, donc le PNJ improvise sa réaction à ce que le joueur vient de dire.
public partial class DeclencheurDialogue : DeclencheurZone
{
	// Nœud parlant ciblé ; par défaut le parent s'il implémente Talkative.
	[Export] public NodePath Cible;

	// Libellé de touche affiché dans le rappel (adapté à la reliure de "action").
	[Export] public string LibelleTouche = "Espace";

	// Ancrage de la bulle des réponses, relatif au JOUEUR (c'est lui qui parle).
	[Export] public Vector2 AncrageChoix = new(0f, -40f);

	private Talkative _parlant;
	// Non nul si la cible défile toute seule (bavardage d'ambiance) : voir TalkativeAutomatique.
	private TalkativeAutomatique _auto;
	// Non nul si la cible génère sa réplique via le LLM local : voir OllamaTalkative.
	private OllamaTalkative _dyn;
	// Non nul si la cible propose des réponses au joueur : voir TalkativeAChoix.
	private TalkativeAChoix _aChoix;
	private BulleDialogue _bulle;
	// 2e bulle, posée sur le joueur : la liste de ses réponses possibles.
	private BulleDialogue _bulleChoix;
	private Player _joueur;
	private bool _joueurProche;
	private bool _enDialogue;
	// Génération LLM en cours (streaming). Le dialogue dynamique SANS choix est piloté par
	// la proximité, jamais par la touche : il démarre à l'approche et se ferme à la sortie.
	private bool _enFlux;
	private int _ligne;
	private float _minuteurAuto;
	private readonly RandomNumberGenerator _rng = new();

	// ---- Dialogue à choix ----
	private NoeudDialogue _noeud;                  // étape courante de l'arbre
	private List<ChoixDialogue> _options = new();  // réponses réellement proposées
	private int _selection;
	private bool _enChoix;                         // la liste est affichée (joueur figé)
	// Lignes en cours de défilement, et ce qu'il faut faire quand elles sont épuisées.
	// Suite nulle = dialogue classique (on referme) ; non nulle = on avance dans l'arbre.
	private IReadOnlyList<string> _lignes = Array.Empty<string>();
	private Action _apresLignes;
	private Action _apresFlux;
	// Texte écrit à jouer si la génération en cours échoue (le « dur » n'est plus la
	// réplique par défaut mais le filet : voir DemarrerNoeud).
	private string[] _repliFlux;
	private string _dernierFlux = "";
	// Mémoire courte de l'échange, réinjectée dans le contexte du LLM pour qu'il ne
	// réponde pas hors sol au tour suivant. Bornée : les petits modèles saturent vite.
	private readonly List<string> _historique = new();
	private const int TaillHistorique = 4;

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
		_aChoix = _parlant as TalkativeAChoix;      // réponses du joueur si la cible l'implémente
		_rng.Randomize();

		_bulle = new BulleDialogue();
		AddChild(_bulle);
		_bulle.Position = ToLocal(_parlant.PointBulle);

		_bulleChoix = new BulleDialogue();
		AddChild(_bulleChoix);

		BodyExited += OnBodyExited;   // la sortie n'est pas couverte par DeclencheurZone
		return true;
	}

	private Talkative ResoudreParlant()
	{
		if (Cible != null && !Cible.IsEmpty)
			return GetNodeOrNull(Cible) as Talkative;
		return GetParent() as Talkative;
	}

	// La cible propose-t-elle un arbre de réponses ? (capacité portée par la classe,
	// mais c'est l'instance qui décide, en renseignant ou non sa Conversation).
	private bool ConversationAChoix => _aChoix?.Conversation != null;

	// Entrée du joueur (hook DeclencheurZone) : dialogue au passage, ou rappel de
	// touche en mode interaction. _parlant est garanti non nul ici (voir Preparer).
	protected override void SurEntreeJoueur(Player joueur)
	{
		_joueur = joueur;
		_joueurProche = true;

		if (!PeutDialoguer())
			return;

		// Une conversation à choix ne démarre JAMAIS toute seule : elle fige le joueur
		// et se pilote au clavier, donc elle attend un appui volontaire — même sur un
		// PNJ qui, sans arbre, aurait bavardé au passage.
		if (ConversationAChoix)
		{
			AfficherRappel();
			return;
		}

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

		// Liste de réponses affichée : le joueur est figé, ses touches naviguent.
		if (_enChoix)
		{
			GererNavigationChoix();
			return;
		}

		// Flux LLM (dialogue dynamique) : entièrement piloté par la proximité, jamais par la
		// touche. La bulle grandit à mesure que le texte arrive et la fermeture se fait à la
		// sortie de zone (OnBodyExited) — Espace n'y touche pas et garde son rôle de saut.
		if (_enFlux)
		{
			// Exception : dans une conversation à choix, le joueur est figé et attend la
			// suite — « action » coupe la génération pour reprendre la main (c'est aussi
			// le filet de sécurité si le modèle traîne).
			if (_apresFlux != null && Input.IsActionJustPressed("action"))
				InterrompreFlux();
			return;
		}

		// Défilement automatique : la bulle avance sur minuteur, sans appui de touche.
		if (_enDialogue && _auto != null && _apresLignes == null)
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
		// donc pas aussi déclencher ni faire avancer le dialogue. Dans une conversation à
		// choix la question ne se pose pas : le joueur y est figé.
		if (!GameState.Instance.DialogueModal
			&& !Mathf.IsZeroApprox(Input.GetAxis("move_left", "move_right")))
			return;

		if (_enDialogue)
			LigneSuivante();
		// Une conversation à choix démarre TOUJOURS à la touche, même sur un PNJ réglé
		// AuPassage : c'est SurEntreeJoueur qui l'a renvoyé vers le rappel de touche,
		// il faut donc l'accepter ici — sinon le rappel s'affiche sans jamais rien ouvrir.
		else if ((ConversationAChoix || !_parlant.DeclencheAuPassage) && PeutDialoguer())
			DemarrerDialogue();
	}

	// Le PNJ a-t-il quelque chose à dire ? Une réplique statique (Lignes), un dialogue
	// dynamique actif (le LLM fournira le texte) OU un arbre de réponses. Un PNJ IA sans
	// Lignes reste donc parlant.
	private bool PeutDialoguer()
	{
		if (!_parlant.PeutParler())
			return false;
		return _parlant.Dialogue.Count > 0 || ConversationAChoix || (_dyn?.DialogueDynamiqueActif ?? false);
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
		// Branche « arbre de réponses » prioritaire : elle absorbe les deux autres modes
		// (une étape sans réplique écrite passe par le LLM, comme avant).
		if (ConversationAChoix)
		{
			_historique.Clear();
			EntrerEnModal();
			DemarrerNoeud(_aChoix.Conversation);
			return;
		}

		// Réplique générée en streaming (court-circuite le mode auto et le défilement
		// statique). Si le dynamique n'est pas actif, chemin statique.
		if (_dyn != null && _dyn.DialogueDynamiqueActif)
		{
			DemarrerFlux(_dyn.Invite, null, null);
			return;
		}

		DemarrerDialogueStatique();
	}

	// Ouvre la conversation une seule fois (le PNJ s'immobilise), quel que soit le nombre
	// d'étapes jouées ensuite : dans un arbre, DemarrerNoeud est rappelé à chaque étape.
	private void OuvrirConversation()
	{
		if (_enDialogue)
			return;
		_enDialogue = true;
		_parlant.SurDebutDialogue();
	}

	// Passe la main au dialogue : le joueur ne joue plus (voir GameState.DialogueModal),
	// ses touches naviguent dans les réponses et valident.
	private void EntrerEnModal()
	{
		OuvrirConversation();
		GameState.Instance.DialogueModal = true;
		GameState.Instance.DialogueDisponible = false;
	}

	// ---- Parcours de l'arbre ----

	// Joue une étape : ce que dit le PNJ, puis la liste des réponses. Nœud nul = fin.
	//
	// L'IA passe DEVANT le texte écrit, comme partout ailleurs dans le jeu : les
	// `Repliques` ne sont pas la réplique finale, elles sont l'INTENTION que le modèle
	// doit faire passer avec ses mots (et le repli exact si Ollama est coupé ou échoue).
	// Sans elles, le PNJ improvise librement à partir de son seul contexte.
	private void DemarrerNoeud(NoeudDialogue noeud)
	{
		_noeud = noeud;
		if (noeud == null)
		{
			TerminerDialogue(sortie: false);
			return;
		}

		if (_dyn != null && _dyn.DialogueDynamiqueActif)
		{
			DemarrerFlux(InviteAvecIntention(null, noeud.Repliques), noeud.Repliques, AfficherChoix);
			return;
		}

		if (noeud.Repliques.Length > 0)
		{
			JouerLignes(noeud.Repliques, AfficherChoix);
			return;
		}

		AfficherChoix();
	}

	// Construit l'invite d'une étape. `ditParJoueur` = la réplique que Glooby vient de
	// choisir (nulle à l'ouverture d'un nœud) ; `intention` = le texte écrit à faire
	// passer (vide = le PNJ improvise, on retombe sur l'invite générique du PNJ).
	private string InviteAvecIntention(string ditParJoueur, string[] intention)
	{
		var sb = new StringBuilder();
		if (!string.IsNullOrEmpty(ditParJoueur))
			sb.Append($"Glooby vient de te dire : « {ditParJoueur} ». ");

		if (intention != null && intention.Length > 0)
			sb.Append($"Fais passer cette idée avec TES mots, sans la répéter mot pour mot : « {string.Join(" ", intention)} »");
		else if (string.IsNullOrEmpty(ditParJoueur))
			sb.Append(_dyn.Invite);
		else
			sb.Append("Réagis à ce qu'il dit.");

		return sb.ToString();
	}

	// Fait défiler des lignes à la touche, puis exécute `apres` (l'étape suivante de
	// l'arbre). Réutilisé pour les répliques d'un nœud comme pour la réponse à un choix.
	private void JouerLignes(IReadOnlyList<string> lignes, Action apres)
	{
		_lignes = lignes;
		_apresLignes = apres;
		_ligne = 0;
		AfficherLigneCourante();
	}

	private void AfficherLigneCourante()
	{
		_bulle.Position = ToLocal(_parlant.PointBulle);
		_bulle.AfficherDialogue(_lignes[_ligne]);
		MemoriserHistorique($"Tu as dit : « {_lignes[_ligne]} »");
	}

	// Affiche les réponses proposables. Filet de sécurité : un nœud dont tous les choix
	// sont épuisés referme la conversation au lieu de laisser le joueur figé sans issue.
	private void AfficherChoix()
	{
		_options = _noeud.ChoixDisponibles();
		if (_options.Count == 0)
		{
			TerminerDialogue(sortie: false);
			return;
		}

		_enChoix = true;
		_selection = 0;
		EntrerEnModal();
		_bulleChoix.Position = ToLocal(_joueur != null ? _joueur.GlobalPosition + AncrageChoix : _parlant.PointBulle);
		_bulleChoix.AfficherChoix(TextesChoix(), _selection);
	}

	private List<string> TextesChoix()
	{
		var textes = new List<string>();
		foreach (var choix in _options)
			textes.Add(choix.Texte);
		return textes;
	}

	// Haut/bas font tourner la sélection (avec bouclage), "action" valide.
	private void GererNavigationChoix()
	{
		int pas = 0;
		if (Input.IsActionJustPressed("bas"))
			pas = 1;
		else if (Input.IsActionJustPressed("haut"))
			pas = -1;

		if (pas != 0)
		{
			_selection = (_selection + pas + _options.Count) % _options.Count;
			_bulleChoix.AfficherChoix(TextesChoix(), _selection);
		}

		if (Input.IsActionJustPressed("action"))
			ValiderChoix(_options[_selection]);
	}

	// Le joueur a tranché : on mémorise, on prévient le PNJ (qui peut agir sur le jeu),
	// puis on enchaîne sur la réponse — écrite si elle existe, générée par le LLM sinon.
	private void ValiderChoix(ChoixDialogue choix)
	{
		_enChoix = false;
		_bulleChoix.Cacher();
		MemoriserHistorique($"Glooby t'a répondu : « {choix.Texte} »");

		if (choix.CoutEffectif > 0)
			GameState.Instance.DepenserPoissons(choix.CoutEffectif);
		if (!string.IsNullOrEmpty(choix.IdMemoire))
			GameState.Instance.MarquerConsomme(choix.IdMemoire);
		_aChoix.SurChoixRetenu(choix);

		var suite = choix.Suite;

		// Même règle qu'à l'ouverture d'un nœud : l'IA répond en s'appuyant sur la
		// `Reponse` écrite comme intention, et ce texte sert de repli exact sans IA.
		if (_dyn != null && _dyn.DialogueDynamiqueActif)
		{
			DemarrerFlux(InviteAvecIntention(choix.Texte, choix.Reponse), choix.Reponse,
				() => DemarrerNoeud(suite));
			return;
		}

		if (choix.Reponse.Length > 0)
		{
			JouerLignes(choix.Reponse, () => DemarrerNoeud(suite));
			return;
		}

		DemarrerNoeud(suite);
	}

	// ---- Génération LLM ----

	// Lance la génération : bulle « … » immédiate, puis rendu incrémental token par token.
	// `apres` nul = PNJ IA classique (déclenché à l'approche, fermé à la sortie de zone,
	// la touche de saut n'est PAS détournée) ; non nul = étape d'un arbre, on enchaîne.
	// `repli` = le texte écrit à jouer tel quel si la génération échoue.
	private void DemarrerFlux(string invite, string[] repli, Action apres)
	{
		OuvrirConversation();
		_enFlux = true;
		_apresFlux = apres;
		_repliFlux = repli;
		_dernierFlux = "";
		_bulle.Position = ToLocal(_parlant.PointBulle);

		_bulle.AfficherDialogue("…"); // retour visuel avant le 1er token

		var svc = OllamaService.Instance;
		svc.GenererFlux(
			svc.ConstruireContexte(ContexteAvecHistorique()),
			invite,
			_dyn.MotMoyenParReponse,
			surChunk: texte =>
			{
				if (!_enFlux)
					return;
				_dernierFlux = texte;
				_bulle.MettreAJourFlux(texte);
			},
			surFin: TerminerFlux,
			surErreur: EchecFlux);
	}

	// Fin de génération. Sans suite prévue on RESTE en flux : la bulle garde sa réplique
	// jusqu'à ce que le joueur s'éloigne (comportement d'origine du PNJ IA).
	private void TerminerFlux()
	{
		if (!_enFlux || _apresFlux == null)
			return;
		SortirDuFlux();
	}

	// Le joueur coupe une réplique générée (touche action pendant un arbre).
	private void InterrompreFlux()
	{
		OllamaService.Instance?.AnnulerGeneration();
		SortirDuFlux();
	}

	// Échec de génération. Dans un arbre, SortirDuFlux joue le texte écrit ; sur un PNJ
	// IA sans arbre, on bascule sur les Lignes statiques (comportement d'origine).
	private void EchecFlux()
	{
		if (!_enFlux)
			return;

		if (_apresFlux != null)
		{
			SortirDuFlux();
			return;
		}

		ReplierSurStatique();
	}

	// Sortie unique du streaming, quelle qu'en soit la raison (fin, coupure, échec).
	// Règle : si RIEN n'a été généré — modèle muet, coupure avant le 1er token, erreur —
	// le texte écrit de l'étape reprend son rôle de repli et s'affiche tel quel. Sinon
	// on garde la réplique générée et on enchaîne.
	private void SortirDuFlux()
	{
		_enFlux = false;
		var suite = _apresFlux;
		var repli = _repliFlux;
		_apresFlux = null;
		_repliFlux = null;

		if (string.IsNullOrWhiteSpace(_dernierFlux))
		{
			if (repli is { Length: > 0 })
				JouerLignes(repli, suite);
			else
				suite?.Invoke();
			return;
		}

		MemoriserHistorique($"Tu as dit : « {_dernierFlux} »");
		suite?.Invoke();
	}

	// Le contexte du PNJ, augmenté des dernières répliques échangées : sans ça, le modèle
	// répond au choix du joueur sans se souvenir de ce qu'il vient lui-même de dire.
	private string ContexteAvecHistorique()
	{
		if (_historique.Count == 0)
			return _dyn.Contexte;
		return $"{_dyn.Contexte}\nCe qui vient d'être dit : {string.Join(" ", _historique)}";
	}

	private void MemoriserHistorique(string ligne)
	{
		_historique.Add(ligne);
		if (_historique.Count > TaillHistorique)
			_historique.RemoveAt(0);
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
		OuvrirConversation();
		_lignes = _parlant.Dialogue;
		_apresLignes = null;
		_ligne = _parlant.Aleatoire ? _rng.RandiRange(0, _lignes.Count - 1) : 0;
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

		_bulle.AfficherDialogue(_lignes[_ligne]);
	}

	private void LigneSuivante()
	{
		// Arbre de dialogue : les lignes défilent dans l'ordre, puis on passe à la suite
		// prévue (ouvrir les réponses, enchaîner sur le nœud suivant...).
		if (_apresLignes != null)
		{
			_ligne++;
			if (_ligne >= _lignes.Count)
			{
				var suite = _apresLignes;
				_apresLignes = null;
				suite();
				return;
			}
			AfficherLigneCourante();
			return;
		}

		// Mode aléatoire manuel : une seule réplique, puis fin.
		if (_parlant.Aleatoire && _auto == null)
		{
			TerminerDialogue(sortie: false);
			return;
		}

		// Choix de la ligne suivante : nouvelle réplique au hasard, ou ligne d'après.
		if (_parlant.Aleatoire)
		{
			_ligne = _rng.RandiRange(0, _lignes.Count - 1);
		}
		else
		{
			_ligne++;
			if (_ligne >= _lignes.Count)
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
		_bulle.AfficherDialogue(_lignes[_ligne]);
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

		// Rendre la main au joueur, quel que soit le chemin de sortie (choix terminal,
		// nœud sans issue, sortie de zone) : un modal laissé armé le figerait pour de bon.
		_enChoix = false;
		_apresLignes = null;
		_apresFlux = null;
		_repliFlux = null;
		_noeud = null;
		GameState.Instance.DialogueModal = false;
		_bulleChoix?.Cacher();

		if (etaitEnDialogue)
			_parlant.SurFinDialogue();

		// Fin « normale » avec le joueur encore proche : on revient au rappel de touche pour
		// pouvoir reparler. Vrai pour le dialogue statique manuel et pour une conversation à
		// choix (qui se relance toujours à la touche) ; exclu pour le dynamique sans arbre,
		// qui ne doit jamais détourner Espace (il repart seul à la prochaine approche).
		bool relanceParTouche = ConversationAChoix
			|| (!_parlant.DeclencheAuPassage && !(_dyn?.DialogueDynamiqueActif ?? false));
		if (!sortie && _joueurProche && relanceParTouche && PeutDialoguer())
		{
			AfficherRappel();
			return;
		}

		_auto?.Cacher();
		_bulle?.Cacher();
		GameState.Instance.DialogueDisponible = false;
	}
}
