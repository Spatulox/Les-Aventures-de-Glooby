using Godot;

// Porte en bois de l'usine (battant à guillotine) : le joueur la manœuvre lui-même avec
// sa touche d'action (Espace / Entrée) quand il est à portée. Fermée, elle barre le passage
// (StaticBody2D) ; ouverte, le battant remonte hors du cadre et la collision se désactive.
//
// Le rappel de touche réutilise la BulleDialogue des PNJ, et la capture d'Espace passe par
// GameState.InteractionDisponible — le même mécanisme que les dialogues, donc le joueur
// immobile ouvre la porte au lieu de sauter, et un joueur qui marche saute normalement.
//
// Découpage des nœuds imposé par le SKEW : la physique 2D de Godot ne gère pas une
// transformation cisaillée, donc le skew se pose sur le nœud "Visuel" (purement graphique)
// et JAMAIS sur la racine — les collisions, elles, restent d'aplomb.
//
// PROFONDEUR : le cadre est coupé en deux dans la scène — pas en deux PNG, mais deux Sprite2D
// affichant chacun une moitié du MÊME fichier via region_rect. Le joueur (z_index = 1) passe
// DEVANT le montant gauche (z −1, avec le battant) et DERRIÈRE le montant droit (z 3), ce qui
// donne la profondeur d'une porte franchie en biais. Déplacer la ligne de coupe = bouger les
// deux region_rect (et l'offset de la moitié, qui vaut ± largeur_region / 2).
//
// Anti-piège : la porte refuse de se refermer tant que le joueur est dans l'embrasure
// (ZoneBattant), et si jamais il s'y glisse pendant la fermeture, elle se rouvre au lieu
// de l'emmurer dans un StaticBody2D.
public partial class PorteBois : StaticBody2D
{
	[Export] public bool OuverteAuDepart;

	// Libellé de touche affiché dans le rappel, comme DeclencheurDialogue.
	[Export] public string LibelleTouche = "Espace";

	// Ancrage du rappel de touche, relatif à la porte (au-dessus du cadre).
	[Export] public Vector2 AncrageBulle = new(0, -120);

	private AnimatedSprite2D _battant;
	private CollisionShape2D _blocage;
	private Area2D _zoneInteraction;
	private Area2D _zoneBattant;
	private BulleDialogue _bulle;

	private bool _ouverte;
	private bool _joueurAPortee;
	private bool _rappelAffiche;

	public override void _Ready()
	{
		_battant = GetNode<AnimatedSprite2D>("Visuel/Battant");
		_blocage = GetNode<CollisionShape2D>("Blocage");
		_zoneInteraction = GetNode<Area2D>("ZoneInteraction");
		_zoneBattant = GetNode<Area2D>("ZoneBattant");

		_battant.AnimationFinished += SurAnimationFinie;
		_zoneInteraction.BodyEntered += SurEntree;
		_zoneInteraction.BodyExited += SurSortie;

		_bulle = new BulleDialogue();
		AddChild(_bulle);
		_bulle.Position = AncrageBulle;

		_ouverte = OuverteAuDepart;
		_battant.Play(_ouverte ? "ouvert" : "ferme");
		_blocage.Disabled = _ouverte;

		SetProcess(false);   // ne sonde la touche que quand le joueur est à portée
	}

	// Le rappel de touche ne s'affiche que quand Espace ferait bien manœuvrer la porte :
	// en marche, la touche retourne au saut (cf. Player), donc le rappel disparaît.
	public override void _Process(double delta)
	{
		bool bougeHorizon = !Mathf.IsZeroApprox(Input.GetAxis("move_left", "move_right"));
		if (bougeHorizon || GameState.Instance.DialogueModal)
		{
			AfficherRappel(false);
			return;
		}

		AfficherRappel(true);
		if (Input.IsActionJustPressed("action"))
			Basculer();
	}

	// La bulle remesure son texte à chaque composition : on ne la recompose donc qu'aux
	// changements d'état, pas à chaque frame.
	private void AfficherRappel(bool visible)
	{
		if (visible == _rappelAffiche)
			return;
		_rappelAffiche = visible;

		if (visible)
			_bulle.AfficherRappel(LibelleTouche);
		else
			_bulle.Cacher();
	}

	// Ouvre ou ferme. La fermeture est refusée tant que quelqu'un occupe l'embrasure.
	private void Basculer()
	{
		if (_ouverte && EmbrasureOccupee())
			return;

		_ouverte = !_ouverte;

		// À l'ouverture on libère le passage tout de suite (le battant met le temps de
		// l'animation à remonter, autant ne pas bloquer le joueur pendant). À la fermeture
		// le blocage n'est réarmé qu'une fois le battant posé, dans SurAnimationFinie.
		if (_ouverte)
			_blocage.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		_battant.Play(_ouverte ? "ouverture" : "fermeture");
	}

	private void SurAnimationFinie()
	{
		if (_ouverte || _battant.Animation != "fermeture")
			return;

		// Le joueur s'est glissé sous le battant pendant sa descente : on rouvre plutôt que
		// de refermer la collision autour de lui.
		if (EmbrasureOccupee())
		{
			_ouverte = true;
			_battant.Play("ouverture");
			return;
		}

		_blocage.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
	}

	private bool EmbrasureOccupee()
	{
		foreach (var corps in _zoneBattant.GetOverlappingBodies())
		{
			if (corps is Player)
				return true;
		}
		return false;
	}

	private void SurEntree(Node2D corps)
	{
		if (corps is not Player)
			return;
		_joueurAPortee = true;
		GameState.Instance.InteractionDisponible = true;
		SetProcess(true);
	}

	private void SurSortie(Node2D corps)
	{
		if (corps is not Player)
			return;
		_joueurAPortee = false;
		GameState.Instance.InteractionDisponible = false;
		AfficherRappel(false);
		SetProcess(false);
	}

	// Même précaution que ZoneBoss : une porte libérée alors que le joueur était à portée
	// laisserait InteractionDisponible à vrai, et le joueur ne pourrait plus sauter à l'arrêt.
	public override void _ExitTree()
	{
		if (_joueurAPortee && GameState.Instance != null)
			GameState.Instance.InteractionDisponible = false;
	}
}
