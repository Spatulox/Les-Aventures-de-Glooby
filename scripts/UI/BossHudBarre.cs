using Godot;

// Barre de vie du boss : cadre ornementé (contour) + remplissage rouge piloté par
// un TextureProgressBar, avec le nom du boss. Masquée par défaut ; c'est la ZoneBoss
// qui la lie au boss spawné (Lier) puis la révèle (Afficher) à l'entrée dans l'arène.
public partial class BossHudBarre : CanvasLayer
{
	[Export] public string NomBoss = "Boss";

	private TextureProgressBar _barre;
	private Label _nom;

	public override void _Ready()
	{
		Visible = false;
		_barre = GetNode<TextureProgressBar>("Barre");
		_nom = GetNode<Label>("NomBoss");
		_nom.Text = NomBoss;
	}

	// Lie la barre à un boss (spawné par la ZoneBoss) et l'initialise à ses PV.
	public void Lier(Boss boss)
	{
		boss.PvChanges += OnPvChanges;
		OnPvChanges(boss.Pv, boss.PvMax);
	}

	// Permet à la ZoneBoss de fixer le nom affiché avant de révéler la barre.
	public void DefinirNom(string nom)
	{
		NomBoss = nom;
		if (_nom != null)
			_nom.Text = nom;
	}

	public void Afficher() => Visible = true;

	public void Masquer() => Visible = false;

	private void OnPvChanges(int pv, int pvMax)
	{
		_barre.MaxValue = pvMax > 0 ? pvMax : 1;
		_barre.Value = pv;
	}
}
