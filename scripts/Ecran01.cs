using Godot;

// Écran 01 "Le Départ" : parcours complet depuis l'igloo jusqu'à l'entrée de la grotte.
// Le sol est peint par code à partir de tronçons (palier + trous à sauter), pour éviter
// d'écrire des dizaines de cellules à la main dans le .tscn.
public partial class Ecran01 : Node2D
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	// Rangée 8 = sol principal, rangée 6 = palier surélevé de 2 tuiles (~64px, sauté franchissable).
	private static readonly Segment[] Segments =
	{
		new(0, 14, 8, "banquise_plein", 3),
		// trou 15-17
		new(18, 24, 8, "banquise_plein", 3),
		new(25, 27, 6, "banquise_plein", 1),
		// trou 28-30
		new(31, 33, 6, "banquise_plein", 1),
		new(34, 36, 6, "banquise_glissant", 1),
		// trou 37-39 (redescend vers la rangée principale)
		new(40, 50, 8, "banquise_plein", 3),
		new(44, 46, 6, "banquise_plein", 1), // plateforme bonus flottante
		new(51, 53, 8, "banquise_fragile", 3),
		new(54, 56, 8, "banquise_plein", 3),
		// trou 57-59
		new(60, 68, 8, "banquise_plein", 3),
		new(69, 85, 8, "grotte_plein", 4), // entrée de la grotte
	};

	// 3 variantes différentes (même palette, composition différente) pour éviter
	// l'effet "copier-coller" d'un même fond répété via Parallax2D.
	private static readonly string[] VariantesAurore =
	{
		"res://assets/backgrounds/fond_aurore_banquise.png",
		"res://assets/backgrounds/fond_aurore_banquise_b.png",
		"res://assets/backgrounds/fond_aurore_banquise_c.png",
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

		int largeurNiveau = (Segments[^1].ColFin + 1) * TailleTuile;

		PlacerFondAurore(largeurNiveau);
		PlacerFondGrotte();
		PlacerDecors();

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = largeurNiveau;
		camera.LimitBottom = 400;
	}

	private void PlacerFondAurore(int largeurNiveau)
	{
		var parallaxe = GetNode<Node2D>("FondParallaxe");
		const float largeurPanneau = 720f;
		int nombrePanneaux = Mathf.CeilToInt(largeurNiveau / largeurPanneau) + 1;

		for (int i = 0; i < nombrePanneaux; i++)
		{
			var panneau = new Sprite2D
			{
				Texture = GD.Load<Texture2D>(VariantesAurore[i % VariantesAurore.Length]),
				Scale = new Vector2(2f, 2f),
				Position = new Vector2(i * largeurPanneau + largeurPanneau / 2f, 180)
			};
			parallaxe.AddChild(panneau);
		}
	}

	private void PlacerFondGrotte()
	{
		var fond = new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://assets/backgrounds/fond_grotte.png"),
			Scale = new Vector2(2f, 2f),
			ZIndex = -2
		};
		AddChild(fond);
		fond.Position = new Vector2(69 * TailleTuile + 8 * TailleTuile, 180);
	}

	private void PlacerDecors()
	{
		AjouterDecor("res://assets/props/rocher_glace.png", new Vector2(20 * TailleTuile, 8 * TailleTuile - 24));
		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(26 * TailleTuile, 6 * TailleTuile - 20));
		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(32 * TailleTuile, 6 * TailleTuile - 20));
		AjouterDecor("res://assets/props/cristal_gros.png", new Vector2(66 * TailleTuile, 8 * TailleTuile - 28));
		AjouterDecor("res://assets/props/stalactite.png", new Vector2(74 * TailleTuile, 40));
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
