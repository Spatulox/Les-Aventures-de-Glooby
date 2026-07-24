using Godot;

// Campement de pêche : point de sauvegarde visuel et fonctionnel (pas de pêche jouable).
// Un seul campement actif à la fois ; les autres basculent en inactif via GameState.
// La détection N'est PAS par BodyEntered (bord) mais par SONDAGE de position chaque
// frame (comme CameraZone) : ça re-déclenche à chaque passage - campement déjà actif
// compris - et reste robuste aux téléportations (respawn qui pose le joueur sur le
// campement). L'hystérésis (_joueurDansZone) garantit une seule sauvegarde par passage.
public partial class Checkpoint : DeclencheurZone
{
	[Export] public string IdCheckpoint = "";

	private Sprite2D _spriteInactif;
	private Sprite2D _spriteActif;

	private Player _joueur;
	private bool _joueurDansZone;

	protected override bool PreparerDeclencheur()
	{
		_spriteInactif = GetNode<Sprite2D>("TrouInactif");
		_spriteActif = GetNode<Sprite2D>("TrouActif");

		if (string.IsNullOrEmpty(IdCheckpoint))
			IdCheckpoint = GetPath().ToString();

		AfficherEtat(GameState.Instance.CheckpointIdActif == IdCheckpoint);
		GameState.Instance.CheckpointActif += OnCheckpointActif;

		// Détection par sondage (Contient) dans _PhysicsProcess : on ne câble PAS
		// BodyEntered (retour false = pas de branchement du signal côté parent).
		return false;
	}

	// Sonde la position du joueur chaque frame. Front montant (entrée dans la zone)
	// uniquement -> sauvegarde ; _joueurDansZone ré-arme à la sortie, sans spam disque
	// tant que le joueur reste dessus.
	public override void _PhysicsProcess(double delta)
	{
		_joueur ??= GetTree().GetFirstNodeInGroup("joueur") as Player;
		if (_joueur == null)
			return;

		bool dedans = Contient(_joueur.GlobalPosition);
		if (dedans && !_joueurDansZone)
			DeclencherSauvegarde();
		_joueurDansZone = dedans;
	}

	// Active le campement s'il ne l'est pas déjà, puis écrit la progression courante :
	// à chaque passage, même campement déjà actif (capture poissons consommés, murs
	// fondus, pouvoir, position).
	private void DeclencherSauvegarde()
	{
		if (GameState.Instance.CheckpointIdActif != IdCheckpoint && !GameState.Instance.ModeDebug)
			GameState.Instance.ActiverCheckpoint(IdCheckpoint, GlobalPosition + new Vector2(-20, 0));

		GameState.Instance.Sauvegarder();
	}

	private void OnCheckpointActif(string idCheckpoint)
	{
		AfficherEtat(idCheckpoint == IdCheckpoint);
	}

	private void AfficherEtat(bool actif)
	{
		_spriteInactif.Visible = !actif;
		_spriteActif.Visible = actif;
	}
}
