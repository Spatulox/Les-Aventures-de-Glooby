using Godot;

// Campement de pêche : point de sauvegarde visuel et fonctionnel (pas de pêche jouable).
// Un seul campement actif à la fois ; les autres basculent en inactif via GameState.
public partial class Checkpoint : DeclencheurZone
{
	[Export] public string IdCheckpoint = "";

	private Sprite2D _spriteInactif;
	private Sprite2D _spriteActif;

	protected override bool PreparerDeclencheur()
	{
		_spriteInactif = GetNode<Sprite2D>("TrouInactif");
		_spriteActif = GetNode<Sprite2D>("TrouActif");

		if (string.IsNullOrEmpty(IdCheckpoint))
			IdCheckpoint = GetPath().ToString();

		AfficherEtat(GameState.Instance.CheckpointIdActif == IdCheckpoint);
		GameState.Instance.CheckpointActif += OnCheckpointActif;
		return true;
	}

	private void OnCheckpointActif(string idCheckpoint)
	{
		AfficherEtat(idCheckpoint == IdCheckpoint);
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		if (GameState.Instance.CheckpointIdActif == IdCheckpoint)
			return;

		GameState.Instance.ActiverCheckpoint(IdCheckpoint, GlobalPosition + new Vector2(-20, 0));
	}

	private void AfficherEtat(bool actif)
	{
		_spriteInactif.Visible = !actif;
		_spriteActif.Visible = actif;
	}
}
