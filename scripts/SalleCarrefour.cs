using Godot;

// Le Carrefour de Glace : salle-hub à trois embranchements. Le mur de glace
// fondable est visible dès l'entrée (verrou mémoire du metroidvania).
public static class SalleCarrefour
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
	{
		new(4, 14, 10, "grotte_plein", 3),
		new(18, 28, 10, "grotte_plein", 3),
		new(12, 20, 16, "grotte_plein", 2),
		new(15, 17, 16, "banquise_glissant", 2),
		new(22, 23, 8, "grotte_plein", 1),
		new(24, 25, 6, "grotte_plein", 1),
		new(22, 23, 4, "grotte_plein", 1),
	};

	public const int Largeur = 30;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		foreach (var segment in Segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, segment.ColDebut + decalage.X, segment.ColFin + decalage.X, segment.Rangee + decalage.Y, segment.Profondeur);
		}

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		var fond = new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://assets/backgrounds/grotte_cathedrale.png"),
			Scale = new Vector2(2.7f, 2.7f),
			ZIndex = -3,
			Position = new Vector2(480, 320) + dec,
		};
		racine.AddChild(fond);

		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(16 * TailleTuile, 9 * TailleTuile - 20) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(23 * TailleTuile, 9 * TailleTuile - 20) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_gros.png", new Vector2(27 * TailleTuile, 9 * TailleTuile - 28) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/stalactite.png", new Vector2(16 * TailleTuile, 110) + dec);

		Outils.Instancier(racine, "res://scenes/mur_fondable.tscn", new Vector2(112, 272) + dec,
			n => n.Set("IdMur", "carrefour_mur_raccourci"));
	}
}
