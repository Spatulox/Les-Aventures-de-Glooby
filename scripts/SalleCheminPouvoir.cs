using Godot;
using static Constantes;

// Chemin du Pouvoir : défi (stalactite-piège + couloir), escalade en escalier,
// salle de récompense avec le pickup et un mur fondable bloquant un raccourci
// visible dès l'entrée - inutilisable au premier passage.
public static class SalleCheminPouvoir
{
	private static readonly TerrainPeintre.Segment[] Segments =
	{
		new(0, 1, 2, "grotte_plein", 1),
		new(3, 4, 2, "grotte_plein", 1),
		new(0, 16, 8, "grotte_plein", 2),
		new(17, 18, 6, "grotte_plein", 0),
		new(19, 20, 4, "grotte_plein", 0),
		new(19, 24, 2, "grotte_plein", 0),
	};

	public const int Largeur = 25;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		TerrainPeintre.PeindreSegments(couche, tileSet, Segments, decalage);

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(20 * TailleTuile, 4 * TailleTuile - 20) + dec);

		Outils.Instancier(racine, "res://scenes/mur_fondable.tscn", new Vector2(96, 32) + dec,
			n => n.Set("IdMur", "chemin_pouvoir_raccourci"));

		Outils.Instancier(racine, "res://scenes/stalactite_piege.tscn", new Vector2(192, 100) + dec);
		Outils.Instancier(racine, "res://scenes/pouvoir_chaleur_pickup.tscn", new Vector2(672, 40) + dec);
	}
}
