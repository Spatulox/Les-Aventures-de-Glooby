using Godot;
using static Constantes;

// La Crevasse : descente verticale étroite, paliers en zigzag, bascule
// progressive du tileset banquise vers grotte.
public static class SalleCrevasse
{
	private static readonly TerrainPeintre.Segment[] Paliers =
	{
		new(0, 3, 2, "banquise_plein", 1),
		new(4, 7, 6, "banquise_plein", 1),
		new(0, 3, 10, "banquise_glissant", 1),
		new(4, 7, 14, "grotte_plein", 1),
		new(0, 3, 18, "grotte_plein", 1),
		new(0, 7, 22, "grotte_plein", 2),
	};

	public const int Largeur = 8;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		TerrainPeintre.PeindreSegments(couche, tileSet, Paliers, decalage);

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(6 * TailleTuile, 12 * TailleTuile) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_gros.png", new Vector2(2 * TailleTuile, 20 * TailleTuile) + dec);
	}
}
