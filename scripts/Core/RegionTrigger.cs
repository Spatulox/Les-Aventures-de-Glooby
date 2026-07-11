using Godot;

// Frontière de région (à poser dans un passage étroit) : au passage du
// joueur, bascule le fond actif via BackgroundManager (fondu croisé, pas
// de coupure brute).
public partial class RegionTrigger : DeclencheurZone
{
	[Export] public string NomRegion = "";

	protected override void SurEntreeJoueur(Player joueur)
	{
		if (string.IsNullOrEmpty(NomRegion))
			return;

		BackgroundManager.Instance?.AfficherRegion(NomRegion);
	}
}
