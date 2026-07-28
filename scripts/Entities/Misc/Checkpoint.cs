using Godot;

// Campement de pêche : point de sauvegarde visuel et fonctionnel (pas de pêche jouable).
// Un seul campement actif à la fois ; les autres basculent en inactif via GameState.
// La détection N'est PAS par BodyEntered (bord) mais par SONDAGE de position chaque
// frame (comme CameraZone) : ça re-déclenche à chaque passage - campement déjà actif
// compris - et reste robuste aux téléportations (respawn qui pose le joueur sur le
// campement). L'hystérésis (_joueurDansZone) garantit une seule sauvegarde par passage.
// Toute la logique ci-dessous est indépendante du visuel : une variante de campement
// (cadeau mécanique de l'usine, cf. CheckpointCadeau) n'a qu'à redéfinir PreparerVisuel
// et AfficherEtat.
public partial class Checkpoint : DeclencheurZone
{
	[Export] public string IdCheckpoint = "";

	// Décalage du point de réapparition par rapport au campement : on ne fait pas
	// réapparaître le joueur DANS le décor. Exporté car il dépend de la largeur du
	// visuel (trou de pêche 48 px de large, cadeau bien plus).
	[Export] public Vector2 OffsetRespawn = new(-20, 0);

	private Sprite2D _spriteInactif;
	private Sprite2D _spriteActif;

	private Player _joueur;
	private bool _joueurDansZone;

	protected override bool PreparerDeclencheur()
	{
		PreparerVisuel();

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
			GameState.Instance.ActiverCheckpoint(IdCheckpoint, GlobalPosition + OffsetRespawn);

		GameState.Instance.Sauvegarder();
	}

	// Débranchement obligatoire : un délégué C# ne se défait pas seul à la libération du
	// nœud abonné (même piège que Player._ExitTree et ZoneBoss._ExitTree). Les campements
	// de la scène précédente restaient branchés sur CheckpointActif ; à l'activation
	// suivante leur handler levait ObjectDisposedException sur des Sprite2D libérés, ce
	// qui AVORTAIT la diffusion du signal — les campements vivants, abonnés après eux, ne
	// le recevaient jamais et ne s'allumaient plus. Se voyait dès la 2e partie d'une même
	// session (debug -> menu -> nouvelle partie).
	public override void _ExitTree()
	{
		if (GameState.Instance != null)
			GameState.Instance.CheckpointActif -= OnCheckpointActif;
	}

	private void OnCheckpointActif(string idCheckpoint)
	{
		AfficherEtat(idCheckpoint == IdCheckpoint);
	}

	// Récupération des nœuds du visuel, avant tout affichage. Redéfinie par les variantes
	// dont le skin n'est pas la paire de Sprite2D du trou de pêche.
	protected virtual void PreparerVisuel()
	{
		_spriteInactif = GetNode<Sprite2D>("TrouInactif");
		_spriteActif = GetNode<Sprite2D>("TrouActif");
	}

	// Reflète l'état du campement. Appelée une première fois au _Ready (restauration de la
	// sauvegarde) puis à chaque diffusion de CheckpointActif.
	protected virtual void AfficherEtat(bool actif)
	{
		_spriteInactif.Visible = !actif;
		_spriteActif.Visible = actif;
	}
}
