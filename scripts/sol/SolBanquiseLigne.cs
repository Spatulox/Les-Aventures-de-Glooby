using Godot;

// Ligne de sol de banquise : instancie automatiquement embout gauche +
// N segments centraux + embout droit. Poser le nœud à l'endroit voulu et régler NombreSegments
// dans l’inspecteur ; la surface de marche est à y = -29 en local.
// Les centres partagent une seule scène (SolBanquise.tscn) ; les embouts ont
// la leur. Le visuel et la collision sont figés dans ces scènes, donc justes
// dans l'éditeur comme au runtime.
public partial class SolBanquiseLigne : Node2D
{
	[Export] public int NombreSegments = 3;
	[Export] public bool AvecEmbouts = true;

	private const string SceneCentre = "res://scenes/sol/SolBanquise.tscn";
	private const string SceneEmboutGauche = "res://scenes/sol/SolBanquiseEmboutGauche.tscn";
	private const string SceneEmboutDroit = "res://scenes/sol/SolBanquiseEmboutDroit.tscn";

	public override void _Ready()
	{
		float x = 0f;

		if (AvecEmbouts)
		{
			AjouterSegment(SceneEmboutGauche, x);
			x += SolBanquise.LargeurSegment;
		}

		for (int i = 0; i < NombreSegments; i++)
		{
			AjouterSegment(SceneCentre, x);
			x += SolBanquise.LargeurSegment;
		}

		if (AvecEmbouts)
			AjouterSegment(SceneEmboutDroit, x);
	}

	private void AjouterSegment(string scene, float x)
	{
		var segment = GD.Load<PackedScene>(scene).Instantiate<SolBanquise>();
		segment.Position = new Vector2(x, 0);
		AddChild(segment);
	}
}
