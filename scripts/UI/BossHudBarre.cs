using Godot;

// Barre de vie du boss : un simple rectangle qui se réduit, pas de nouvel asset.
// Masquée par défaut ; c'est la ZoneBoss qui la lie au boss spawné (Lier) puis la
// révèle (Afficher) quand le joueur entre dans l'arène.
public partial class BossHudBarre : CanvasLayer
{
	private ColorRect _fond;
	private ColorRect _remplissage;

	public override void _Ready()
	{
		Visible = false;
		_fond = GetNode<ColorRect>("Fond");
		_remplissage = GetNode<ColorRect>("Fond/Remplissage");
	}

	// Lie la barre à un boss (spawné par la ZoneBoss) et l'initialise à ses PV.
	public void Lier(Boss boss)
	{
		boss.PvChanges += OnPvChanges;
		OnPvChanges(boss.Pv, boss.PvMax);
	}

	public void Afficher() => Visible = true;

	public void Masquer() => Visible = false;

	private void OnPvChanges(int pv, int pvMax)
	{
		float ratio = pvMax > 0 ? (float)pv / pvMax : 0f;
		_remplissage.Size = new Vector2(_fond.Size.X * ratio, _remplissage.Size.Y);
	}
}
