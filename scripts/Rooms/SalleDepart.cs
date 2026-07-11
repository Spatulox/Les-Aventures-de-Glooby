using Godot;
using static Constantes;

// Salle "Le Départ" (banquise) : igloo, campements, jusqu'à l'entrée
// de la grotte. Construite à un décalage (en tuiles) dans le monde continu.
public static class SalleDepart
{
	private static readonly TerrainPeintre.Segment[] Segments =
	{
		new(0, 14, 8, "banquise_plein", 3),
		new(18, 24, 8, "banquise_plein", 3),
		new(25, 27, 6, "banquise_plein", 1),
		new(31, 33, 6, "banquise_plein", 1),
		new(34, 36, 6, "banquise_glissant", 1),
		new(40, 50, 8, "banquise_plein", 3),
		new(44, 46, 6, "banquise_plein", 1),
		new(51, 53, 8, "banquise_fragile", 3),
		new(54, 56, 8, "banquise_plein", 3),
		new(60, 68, 8, "banquise_plein", 3),
		new(69, 85, 8, "grotte_plein", 4),
	};

	private static readonly string[] VariantesAurore =
	{
		"res://assets/backgrounds/fond_aurore_banquise.png",
		"res://assets/backgrounds/fond_aurore_banquise_b.png",
		"res://assets/backgrounds/fond_aurore_banquise_c.png",
	};

	public const int Largeur = 86;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Node2D parallaxe, Vector2I decalage)
	{
		TerrainPeintre.PeindreSegments(couche, tileSet, Segments, decalage);

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		PlacerFondAurore(racine, parallaxe, dec);

		var fondGrotte = new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://assets/backgrounds/fond_grotte.png"),
			Scale = new Vector2(2f, 2f),
			ZIndex = -2,
			Position = new Vector2(69 * TailleTuile + 8 * TailleTuile, 180) + dec,
		};
		parallaxe.AddChild(fondGrotte);
		fondGrotte.Owner = racine;

		Outils.AjouterDecor(racine, "res://assets/props/rocher_glace.png", new Vector2(20 * TailleTuile, 8 * TailleTuile - 24) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(26 * TailleTuile, 6 * TailleTuile - 20) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(32 * TailleTuile, 6 * TailleTuile - 20) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_gros.png", new Vector2(66 * TailleTuile, 8 * TailleTuile - 28) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/stalactite.png", new Vector2(74 * TailleTuile, 40) + dec);

		Outils.Instancier(racine, "res://scenes/igloo.tscn", new Vector2(110, 220) + dec);

		Outils.Instancier(racine, "res://scenes/checkpoint_peche.tscn", new Vector2(210, 242) + dec,
			n => n.Set("IdCheckpoint", "ecran01_campement"));

		Outils.Instancier(racine, "res://scenes/checkpoint_peche.tscn", new Vector2(1344, 242) + dec,
			n => n.Set("IdCheckpoint", "ecran01_campement_2"));
	}

	private static void PlacerFondAurore(Node2D racine, Node2D parallaxe, Vector2 dec)
	{
		const float largeurPanneau = 720f;
		int largeurNiveau = (Segments[^1].ColFin + 1) * TailleTuile;
		int nombrePanneaux = Mathf.CeilToInt(largeurNiveau / largeurPanneau) + 1;

		Outils.PlacerFondRepete(parallaxe, VariantesAurore, nombrePanneaux, largeurPanneau, 180f, 2f, 0, dec, racine);
	}
}
