using Godot;

// Le Carrefour de Glace : salle-hub à trois embranchements (Chemin 1 à droite,
// Chemin 2 en bas, Chemin 3 en haut), avec le mur de glace fondable visible
// dès l'entrée (verrou mémoire du metroidvania - la suite se construit plus tard).
public partial class EcranCarrefour : Node2D
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
	{
		new(4, 14, 10, "grotte_plein", 3),
		// trou 15-17 : descente vers le Chemin 2 (pouvoir de chaleur)
		new(18, 28, 10, "grotte_plein", 3),

		new(12, 20, 16, "grotte_plein", 2), // fond du Chemin 2
		new(15, 17, 16, "banquise_glissant", 2), // patch de glace glissante

		new(22, 23, 8, "grotte_plein", 1), // Chemin 3 : palier 1
		new(24, 25, 6, "grotte_plein", 1), // Chemin 3 : palier 2
		new(22, 23, 4, "grotte_plein", 1), // Chemin 3 : palier 3 (accès difficile, suite à venir)
	};

	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		var tileSet = TileSetFabrique.CreerMonde();
		couche.TileSet = tileSet;
		couche.AddToGroup("sol");

		foreach (var segment in Segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, segment.ColDebut, segment.ColFin, segment.Rangee, segment.Profondeur);
		}

		PlacerDecors();

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = 30 * TailleTuile;
		camera.LimitBottom = 20 * TailleTuile;
	}

	private void PlacerDecors()
	{
		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(16 * TailleTuile, 9 * TailleTuile - 20));
		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(23 * TailleTuile, 9 * TailleTuile - 20));
		AjouterDecor("res://assets/props/cristal_gros.png", new Vector2(27 * TailleTuile, 9 * TailleTuile - 28));
		AjouterDecor("res://assets/props/stalactite.png", new Vector2(16 * TailleTuile, 110));
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
