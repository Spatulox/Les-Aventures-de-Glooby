using Godot;

// Sonde de test pour scenes/decors/usine/TestPenteUsine.tscn : maintient
// « move_right » pour faire monter le pingouin le long de la pente en bois et
// imprime sa position + son état « au sol » à intervalles réguliers. Sert à
// valider en headless que le raccord sol plat → pente → sol plat se franchit
// sans blocage ni perte de contact (pas de marche invisible).
public partial class TestPenteUsineDebug : Node
{
	private CharacterBody2D _joueur;
	private float _temps;
	private float _yMin = float.MaxValue;

	public override void _Ready()
	{
		_joueur = GetTree().GetFirstNodeInGroup("joueur") as CharacterBody2D;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_joueur == null)
			return;

		_temps += (float)delta;

		// Laisse 0,5 s pour retomber au sol, puis avance vers la droite.
		if (_temps > 0.5f)
			Input.ActionPress("move_right");

		_yMin = Mathf.Min(_yMin, _joueur.GlobalPosition.Y);

		if (Mathf.PosMod(_temps, 0.5f) < (float)delta)
			GD.Print($"[TestPente] t={_temps:F1} x={_joueur.GlobalPosition.X:F0} " +
				$"y={_joueur.GlobalPosition.Y:F0} yMin={_yMin:F0} auSol={_joueur.IsOnFloor()}");
	}
}
