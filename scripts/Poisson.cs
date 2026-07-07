using Godot;

// Poisson ramassable : monnaie + consommable de soin (mangé via GameState.ManagerPoisson).
// Disparaît définitivement une fois ramassé (état persistant dans GameState).
public partial class Poisson : Area2D
{
	[Export] public string IdPoisson = "";

	public override void _Ready()
	{
		if (string.IsNullOrEmpty(IdPoisson))
			IdPoisson = GetPath().ToString();

		if (GameState.Instance.EstPoissonRamasse(IdPoisson))
		{
			QueueFree();
			return;
		}

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
			return;

		GameState.Instance.RamasserPoisson(IdPoisson);
		QueueFree();
	}
}
