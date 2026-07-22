using Godot;

// Colonne de mur de grotte : empile automatiquement le haut + N centres + le bas
// bout à bout verticalement (l'origine (0,0) du nœud est le sommet de la paroi).
// Poser le nœud en haut de la paroi voulue et régler NombreSegments dans
// l'inspecteur. Le visuel et la collision sont figés dans les scènes
// MurGrotteXxx.tscn, donc justes dans l'éditeur comme au runtime.
public partial class MurGrotteColonne : Node2D
{
	[Export] public int NombreSegments = 3;
	[Export] public bool AvecCoiffes = true;

	private const string SceneHaut = "res://scenes/mur/MurGrotteHaut.tscn";
	private const string SceneCentre = "res://scenes/mur/MurGrotteCentre.tscn";
	private const string SceneBas = "res://scenes/mur/MurGrotteBas.tscn";

	public override void _Ready()
	{
		float y = 0f;

		if (AvecCoiffes)
			y += AjouterSegment(SceneHaut, y, MurGrotte.HauteurHaut);

		for (int i = 0; i < NombreSegments; i++)
			y += AjouterSegment(SceneCentre, y, MurGrotte.HauteurCentre);

		if (AvecCoiffes)
			AjouterSegment(SceneBas, y, MurGrotte.HauteurBas);
	}

	// Place la pièce par son bord haut (les sprites sont centrés) et renvoie sa hauteur.
	private float AjouterSegment(string scene, float bordHaut, float hauteur)
	{
		var segment = GD.Load<PackedScene>(scene).Instantiate<MurGrotte>();
		segment.Position = new Vector2(0, bordHaut + hauteur / 2f);
		AddChild(segment);
		return hauteur;
	}
}
