using Godot;

// Ligne de sol de grotte : instancie automatiquement embout gauche +
// N segments centraux + embout droit, posés bout à bout (les embouts grotte
// sont plus étroits que les centres, donc la progression suit la largeur réelle
// de chaque pièce). Poser le nœud à l'endroit voulu et régler NombreSegments
// dans l'inspecteur ; la surface de marche est à y = -84 en local.
// Le visuel et la collision sont figés dans les scènes SolGrotteXxx.tscn,
// donc justes dans l'éditeur comme au runtime.
public partial class SolGrotteLigne : Node2D
{
	[Export] public int NombreSegments = 3;
	[Export] public bool AvecEmbouts = true;

	private const string SceneCentre = "res://scenes/sol/SolGrotte.tscn";
	private const string SceneEmboutGauche = "res://scenes/sol/SolGrotteEmboutGauche.tscn";
	private const string SceneEmboutDroit = "res://scenes/sol/SolGrotteEmboutDroit.tscn";

	public override void _Ready()
	{
		float x = 0f;

		if (AvecEmbouts)
			x += AjouterSegment(SceneEmboutGauche, x, SolGrotte.LargeurEmboutGauche);

		for (int i = 0; i < NombreSegments; i++)
			x += AjouterSegment(SceneCentre, x, SolGrotte.LargeurCentre);

		if (AvecEmbouts)
			AjouterSegment(SceneEmboutDroit, x, SolGrotte.LargeurEmboutDroit);
	}

	// Place la pièce par son bord gauche (les sprites sont centrés) et renvoie sa largeur.
	private float AjouterSegment(string scene, float bordGauche, float largeur)
	{
		var segment = GD.Load<PackedScene>(scene).Instantiate<SolGrotte>();
		segment.Position = new Vector2(bordGauche + largeur / 2f, 0);
		AddChild(segment);
		return largeur;
	}
}
