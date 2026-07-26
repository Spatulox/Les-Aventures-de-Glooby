using Godot;

// Sonde de test pour scenes/plateformes/usine/TestPlateformesBois.tscn : suit une
// plateforme mobile et le joueur posé dessus, et imprime l'écart vertical entre
// les deux. Si le joueur est bien entraîné (AnimatableBody2D + SyncToPhysics),
// cet écart reste constant pendant que la plateforme monte et descend.
public partial class TestPlateformeBoisDebug : Node
{
	[Export] public NodePath PlateformeASuivre;

	private CharacterBody2D _joueur;
	private Node2D _plateforme;
	private float _temps;

	public override void _Ready()
	{
		_joueur = GetTree().GetFirstNodeInGroup("joueur") as CharacterBody2D;
		if (PlateformeASuivre != null && !PlateformeASuivre.IsEmpty)
			_plateforme = GetNode<Node2D>(PlateformeASuivre);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_joueur == null || _plateforme == null)
			return;

		_temps += (float)delta;

		if (Mathf.PosMod(_temps, 0.4f) < (float)delta)
		{
			float ecart = _joueur.GlobalPosition.Y - _plateforme.GlobalPosition.Y;
			GD.Print($"[TestPlatBois] t={_temps:F1} plateformeY={_plateforme.GlobalPosition.Y:F0} " +
				$"joueurY={_joueur.GlobalPosition.Y:F0} ecart={ecart:F1} auSol={_joueur.IsOnFloor()}");
		}
	}
}
