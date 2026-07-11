using Godot;

// Sonde de test pour scenes/test/TestPlateformes.tscn : imprime la position
// et l'état "au sol" du joueur à intervalles réguliers, et simule la
// combinaison bas+saut à un instant donné pour valider en headless (sans
// rendu) le passage à travers une plateforme traversable.
public partial class TestPlateformesDebug : Node
{
	[Export] public float InstantTraversee = -1f;
	[Export] public NodePath PlateformeASuivre;
	[Export] public float VitesseInitialeX = 0f;

	private CharacterBody2D _joueur;
	private Node2D _plateformeSuivie;
	private bool _vitesseAppliquee;
	private float _temps;
	private bool _traverseeDeclenchee;
	private bool _basRelache;

	public override void _Ready()
	{
		_joueur = GetTree().GetFirstNodeInGroup("joueur") as CharacterBody2D;
		if (PlateformeASuivre != null && !PlateformeASuivre.IsEmpty)
			_plateformeSuivie = GetNode<Node2D>(PlateformeASuivre);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_joueur == null)
			return;

		_temps += (float)delta;

		if (!_vitesseAppliquee && VitesseInitialeX != 0f && _joueur.IsOnFloor())
		{
			_vitesseAppliquee = true;
			_joueur.Velocity = new Vector2(VitesseInitialeX, _joueur.Velocity.Y);
		}

		if (InstantTraversee >= 0f && !_traverseeDeclenchee && _temps >= InstantTraversee)
		{
			_traverseeDeclenchee = true;
			Input.ActionPress("bas");
			Input.ActionPress("jump");
			GD.Print("[TestPlateformes] déclenchement bas+saut");
		}
		else if (_traverseeDeclenchee && !_basRelache && _temps >= InstantTraversee + 0.1f)
		{
			_basRelache = true;
			Input.ActionRelease("jump");
			Input.ActionRelease("bas");
		}

		if (Mathf.PosMod(_temps, 0.5f) < (float)delta)
		{
			var texte = $"[TestPlateformes] t={_temps:F1} x={_joueur.GlobalPosition.X:F0} y={_joueur.GlobalPosition.Y:F0} auSol={_joueur.IsOnFloor()}";
			if (_plateformeSuivie != null)
				texte += $" plateformeX={_plateformeSuivie.GlobalPosition.X:F0}";
			if (_plateformeSuivie is PlateformeFragile fragile)
				texte += $" effondree={fragile.EstEffondree}";
			GD.Print(texte);
		}
	}
}
