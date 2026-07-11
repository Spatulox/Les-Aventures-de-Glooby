using Godot;
using static Constantes;

// Écran 02 : banquise plus difficile (trou plus long, pont fragile avec
// filet de sécurité, patch glissant).
public static class SalleBanquise02
{
	private static readonly TerrainPeintre.Segment[] Segments =
	{
		new(0, 8, 8, "banquise_plein", 3),
		new(13, 19, 8, "banquise_plein", 3),
		new(20, 24, 8, "banquise_fragile", 3),
		new(18, 26, 14, "banquise_plein", 1),
		new(25, 31, 8, "banquise_plein", 3),
		new(27, 29, 8, "banquise_glissant", 3),
		new(35, 40, 8, "grotte_plein", 4),
	};

	private static readonly string[] VariantesAurore =
	{
		"res://assets/backgrounds/fond_aurore_banquise_b.png",
		"res://assets/backgrounds/fond_aurore_banquise_c.png",
	};

	public const int Largeur = 41;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Node2D parallaxe, Vector2I decalage)
	{
		TerrainPeintre.PeindreSegments(couche, tileSet, Segments, decalage);

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(21 * TailleTuile, 8 * TailleTuile - 20) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/rocher_glace.png", new Vector2(30 * TailleTuile, 8 * TailleTuile - 24) + dec);

		Outils.PlacerFondRepete(parallaxe, VariantesAurore, 2, 720f, 180f, 2f, 0, dec, racine);

		Outils.Instancier(racine, "res://scenes/checkpoint_peche.tscn", new Vector2(80, 242) + dec,
			n => n.Set("IdCheckpoint", "ecran02_campement"));
	}
}
