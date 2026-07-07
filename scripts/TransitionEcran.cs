using Godot;

// Zone de sortie d'écran : place le point d'arrivée du joueur dans GameState
// puis charge la scène suivante.
public partial class TransitionEcran : Area2D
{
	[Export] public string CheminScene = "";
	[Export] public Vector2 PositionEntree = Vector2.Zero;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player || string.IsNullOrEmpty(CheminScene))
			return;

		GameState.Instance.PositionEntreeSuivante = PositionEntree;
		// Différé : changer de scène en plein callback physique (body_entered)
		// fait planter Godot ("Removing a CollisionObject during a physics callback").
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, CheminScene);
	}
}
