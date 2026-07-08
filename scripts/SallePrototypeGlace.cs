using Godot;

// Petite zone bonus reconstruite depuis un croquis fait avec l'éditeur visuel
// pixellab.ai/maps (mur à échancrure, plateforme flottante, structure en L,
// bande de sol en glace fissurée) - greffée au bout du Chemin 3, qui n'était
// jusqu'ici qu'une impasse vide.
public static class SallePrototypeGlace
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
	{
		// Mur à échancrure (bonus discret en arrière, à gauche de l'arrivée).
		new(0, 5, 2, "banquise_neige_pastel", 2),
		new(0, 1, 1, "banquise_neige_pastel", 0),
		new(0, 3, 5, "banquise_neige_pastel", 0),
		new(5, 5, 5, "banquise_neige_pastel", 0),
		new(3, 5, 0, "banquise_neige_pastel", 0), // plateforme flottante au-dessus du mur

		// Bande de sol en glace fissurée (fragile - traverser sans s'arrêter).
		new(7, 21, 6, "banquise_fissuree", 0),

		// Structure en L et blocs flottants, au-dessus de la bande de sol.
		new(14, 14, 1, "banquise_fissuree", 2),
		new(14, 15, 3, "banquise_fissuree", 0),
		new(19, 19, 2, "banquise_fissuree", 1),
		new(20, 20, 0, "banquise_fissuree", 0),
	};

	public const int Largeur = 22;
	public const int Hauteur = 7;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		foreach (var segment in Segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, segment.ColDebut + decalage.X, segment.ColFin + decalage.X, segment.Rangee + decalage.Y, segment.Profondeur);
		}

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);
		Outils.AjouterDecor(racine, "res://assets/props/grotte/fleur_givre.png", new Vector2(4 * TailleTuile, 0 * TailleTuile - 10) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(17 * TailleTuile, 1 * TailleTuile - 16) + dec);
	}
}
