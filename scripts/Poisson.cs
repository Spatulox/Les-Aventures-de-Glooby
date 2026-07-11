using Godot;

// Poisson ramassable : monnaie + consommable de soin (mangé via GameState.ManagerPoisson).
// Disparaît définitivement une fois ramassé (état persistant dans GameState).
public partial class Poisson : ElementRamassable
{
	[Export] public string IdPoisson = "";

	protected override void Initialiser()
	{
		if (string.IsNullOrEmpty(IdPoisson))
			IdPoisson = GetPath().ToString();
	}

	protected override bool EstDejaConsomme() => GameState.Instance.EstPoissonRamasse(IdPoisson);

	protected override void Ramasser() => GameState.Instance.RamasserPoisson(IdPoisson);
}
