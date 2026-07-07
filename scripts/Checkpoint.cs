using Godot;

// Campement de pêche : point de sauvegarde visuel et fonctionnel (pas de pêche jouable).
// Un seul campement actif à la fois ; les autres basculent en inactif via GameState.
public partial class Checkpoint : Area2D
{
	[Export] public string IdCheckpoint = "";

	private Sprite2D _spriteInactif;
	private Sprite2D _spriteActif;

	public override void _Ready()
	{
		_spriteInactif = GetNode<Sprite2D>("TrouInactif");
		_spriteActif = GetNode<Sprite2D>("TrouActif");

		if (string.IsNullOrEmpty(IdCheckpoint))
			IdCheckpoint = GetPath().ToString();

		AfficherEtat(GameState.Instance.CheckpointIdActif == IdCheckpoint);
		GameState.Instance.CheckpointActif += OnCheckpointActif;
		BodyEntered += OnBodyEntered;
	}

	private void OnCheckpointActif(string idCheckpoint)
	{
		AfficherEtat(idCheckpoint == IdCheckpoint);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (GameState.Instance.CheckpointIdActif == IdCheckpoint)
			return;
		if (body is not Player)
			return;

		GameState.Instance.ActiverCheckpoint(IdCheckpoint, GetTree().CurrentScene.SceneFilePath, GlobalPosition + new Vector2(-20, 0));
	}

	private void AfficherEtat(bool actif)
	{
		_spriteInactif.Visible = !actif;
		_spriteActif.Visible = actif;
	}
}
