using Godot;

// Salle "Le Départ" (banquise) : igloo, campements, poissons, jusqu'à l'entrée
// de la grotte. Construite à un décalage (en tuiles) dans le monde continu.
public static class SalleDepart
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
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
		foreach (var segment in Segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, segment.ColDebut + decalage.X, segment.ColFin + decalage.X, segment.Rangee + decalage.Y, segment.Profondeur);
		}

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

		var positionsPoissons = new[]
		{
			new Vector2(320, 220), new Vector2(880, 160), new Vector2(1450, 220), new Vector2(2016, 220),
		};
		for (int i = 0; i < positionsPoissons.Length; i++)
		{
			int index = i;
			Outils.Instancier(racine, "res://scenes/poisson.tscn", positionsPoissons[i] + dec,
				n => n.Set("IdPoisson", $"ecran01_poisson_{index + 1}"));
		}
	}

	private static void PlacerFondAurore(Node2D racine, Node2D parallaxe, Vector2 dec)
	{
		const float largeurPanneau = 720f;
		int largeurNiveau = (Segments[^1].ColFin + 1) * TailleTuile;
		int nombrePanneaux = Mathf.CeilToInt(largeurNiveau / largeurPanneau) + 1;

		for (int i = 0; i < nombrePanneaux; i++)
		{
			var panneau = new Sprite2D
			{
				Texture = GD.Load<Texture2D>(VariantesAurore[i % VariantesAurore.Length]),
				Scale = new Vector2(2f, 2f),
				Position = new Vector2(i * largeurPanneau + largeurPanneau / 2f, 180) + dec,
			};
			parallaxe.AddChild(panneau);
			panneau.Owner = racine;
		}
	}
}
