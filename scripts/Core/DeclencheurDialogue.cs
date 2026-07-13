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
	private BulleDialogue _bulle;
	private bool _joueurProche;
	private bool _enDialogue;
	private int _ligne;

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

		if (!_parlant.PeutParler() || _parlant.Dialogue.Count == 0)
			return;

		if (_parlant.DeclencheAuPassage)
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
		if (!_joueurProche || _parlant == null || !Input.IsActionJustPressed("action"))
			return;

		if (_enDialogue)
			LigneSuivante();
		else if (!_parlant.DeclencheAuPassage && _parlant.PeutParler() && _parlant.Dialogue.Count > 0)
			DemarrerDialogue();
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
		_enDialogue = true;
		_ligne = 0;
		GameState.Instance.DialogueDisponible = true;
		_parlant.SurDebutDialogue();
		_bulle.Position = ToLocal(_parlant.PointBulle);
		_bulle.AfficherDialogue(_parlant.Dialogue[_ligne]);
	}

	private void LigneSuivante()
	{
		_ligne++;
		if (_ligne >= _parlant.Dialogue.Count)
		{
			TerminerDialogue(sortie: false);
			return;
		}
		_bulle.AfficherDialogue(_parlant.Dialogue[_ligne]);
	}

	private void TerminerDialogue(bool sortie)
	{
		bool etaitEnDialogue = _enDialogue;
		_enDialogue = false;

		if (etaitEnDialogue)
			_parlant.SurFinDialogue();

		// Fin « normale » avec le joueur encore proche (mode touche) et dialogue encore
		// autorisé : on revient au rappel de touche pour pouvoir reparler.
		if (!sortie && _joueurProche && !_parlant.DeclencheAuPassage
			&& _parlant.PeutParler() && _parlant.Dialogue.Count > 0)
		{
			AfficherRappel();
			return;
		}

		_bulle?.Cacher();
		GameState.Instance.DialogueDisponible = false;
	}
}
