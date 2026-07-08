using Godot;

// Frontière de région (à poser dans un passage étroit) : au passage du
// joueur, bascule le fond actif via BackgroundManager (fondu croisé, pas
// de coupure brute).
public partial class RegionTrigger : Area2D
{
	[Export] public string NomRegion = "";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player || string.IsNullOrEmpty(NomRegion))
			return;

		BackgroundManager.Instance?.AfficherRegion(NomRegion);
	}
}
