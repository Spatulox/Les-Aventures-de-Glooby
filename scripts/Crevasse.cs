using Godot;

// La Crevasse : descente verticale étroite entre la banquise et la grotte.
// Paliers en zigzag, tileset qui bascule de banquise à grotte à mi-hauteur.
public partial class Crevasse : Node2D
{
	private const int TailleTuile = 32;

	private record Palier(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Palier[] Paliers =
	{
		new(0, 3, 2, "banquise_plein", 1),
		new(4, 7, 6, "banquise_plein", 1),
		new(0, 3, 10, "banquise_glissant", 1),
		new(4, 7, 14, "grotte_plein", 1),
		new(0, 3, 18, "grotte_plein", 1),
		new(0, 7, 22, "grotte_plein", 2), // fond, transition vers le Carrefour
	};

	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		var tileSet = TileSetFabrique.CreerMonde();
		couche.TileSet = tileSet;
		couche.AddToGroup("sol");

		foreach (var palier in Paliers)
		{
			int sourceId = (int)tileSet.GetMeta(palier.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, palier.ColDebut, palier.ColFin, palier.Rangee, palier.Profondeur);
		}

		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(6 * TailleTuile, 12 * TailleTuile));
		AjouterDecor("res://assets/props/cristal_gros.png", new Vector2(2 * TailleTuile, 20 * TailleTuile));

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = 8 * TailleTuile;
		camera.LimitBottom = 24 * TailleTuile;
	}

	private void AjouterDecor(string chemin, Vector2 position)
	{
		var sprite = new Sprite2D
		{
			Texture = GD.Load<Texture2D>(chemin),
			Position = position,
			ZIndex = -1
		};
		AddChild(sprite);
	}
}
