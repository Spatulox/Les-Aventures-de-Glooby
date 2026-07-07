using Godot;

// Écran 02 : banquise, difficulté progressive (trous plus longs, pont de glace
// fragile au-dessus d'un vrai vide avec filet de sécurité, patch glissant).
public partial class EcranBanquise02 : Node2D
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
	{
		new(0, 8, 8, "banquise_plein", 3),
		// trou 9-12 (4 tuiles, plus long qu'à l'écran 01)
		new(13, 19, 8, "banquise_plein", 3),
		new(20, 24, 8, "banquise_fragile", 3),
		new(18, 26, 14, "banquise_plein", 1), // filet de sécurité sous le pont fragile
		new(25, 31, 8, "banquise_plein", 3),
		new(27, 29, 8, "banquise_glissant", 3),
		// trou 32-34
		new(35, 40, 8, "grotte_plein", 4), // transition vers la Crevasse
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

		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(21 * TailleTuile, 8 * TailleTuile - 20));
		AjouterDecor("res://assets/props/rocher_glace.png", new Vector2(30 * TailleTuile, 8 * TailleTuile - 24));
		PlacerFond();

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = (Segments[^1].ColFin + 1) * TailleTuile;
		camera.LimitBottom = 500;
	}

	private void PlacerFond()
	{
		var parallaxe = GetNode<Node2D>("FondParallaxe");
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
				Position = new Vector2(i * 720f + 360f, 180)
			};
			parallaxe.AddChild(panneau);
		}
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
