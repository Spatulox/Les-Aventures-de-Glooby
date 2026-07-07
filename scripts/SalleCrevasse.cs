using Godot;

// La Crevasse : descente verticale étroite, paliers en zigzag, bascule
// progressive du tileset banquise vers grotte.
public static class SalleCrevasse
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
		new(0, 7, 22, "grotte_plein", 2),
	};

	public const int Largeur = 8;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		foreach (var palier in Paliers)
		{
			int sourceId = (int)tileSet.GetMeta(palier.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, palier.ColDebut + decalage.X, palier.ColFin + decalage.X, palier.Rangee + decalage.Y, palier.Profondeur);
		}

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(6 * TailleTuile, 12 * TailleTuile) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_gros.png", new Vector2(2 * TailleTuile, 20 * TailleTuile) + dec);
	}
}
