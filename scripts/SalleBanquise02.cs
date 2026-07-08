using Godot;

// Écran 02 : banquise plus difficile (trou plus long, pont fragile avec
// filet de sécurité, patch glissant).
public static class SalleBanquise02
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
	{
		new(0, 8, 8, "banquise_plein", 3),
		new(13, 19, 8, "banquise_plein", 3),
		new(20, 24, 8, "banquise_fragile", 3),
		new(18, 26, 14, "banquise_plein", 1),
		new(25, 31, 8, "banquise_plein", 3),
		new(27, 29, 8, "banquise_glissant", 3),
		new(35, 40, 8, "grotte_plein", 4),
	};

	public const int Largeur = 41;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Node2D parallaxe, Vector2I decalage)
	{
		foreach (var segment in Segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, segment.ColDebut + decalage.X, segment.ColFin + decalage.X, segment.Rangee + decalage.Y, segment.Profondeur);
		}

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(21 * TailleTuile, 8 * TailleTuile - 20) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/rocher_glace.png", new Vector2(30 * TailleTuile, 8 * TailleTuile - 24) + dec);

		string[] variantes =
		{
			"res://assets/backgrounds/fond_aurore_banquise_b.png",
			"res://assets/backgrounds/fond_aurore_banquise_c.png",
		};
		for (int i = 0; i < 2; i++)
		{
			var panneau = new Sprite2D
			{
				Texture = GD.Load<Texture2D>(variantes[i]),
				Scale = new Vector2(2f, 2f),
				Position = new Vector2(i * 720f + 360f, 180) + dec,
			};
			parallaxe.AddChild(panneau);
			panneau.Owner = racine;
		}

		Outils.Instancier(racine, "res://scenes/checkpoint_peche.tscn", new Vector2(80, 242) + dec,
			n => n.Set("IdCheckpoint", "ecran02_campement"));
	}
}
