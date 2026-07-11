using Godot;

// Barre de vie du boss : un simple rectangle qui se réduit, pas de nouvel asset.
public partial class BossHudBarre : CanvasLayer
{
	[Export] public NodePath CheminBoss;

	private ColorRect _fond;
	private ColorRect _remplissage;
	private BossCerf _boss;

	public override void _Ready()
	{
		_fond = GetNode<ColorRect>("Fond");
		_remplissage = GetNode<ColorRect>("Fond/Remplissage");
		_boss = GetNode<BossCerf>(CheminBoss);
		_boss.PvChanges += OnPvChanges;
		OnPvChanges(_boss.Pv, _boss.PvMax);
	}

	private void OnPvChanges(int pv, int pvMax)
	{
		float ratio = pvMax > 0 ? (float)pv / pvMax : 0f;
		_remplissage.Size = new Vector2(_fond.Size.X * ratio, _remplissage.Size.Y);
	}
}
