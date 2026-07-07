using Godot;

// Construit par code les TileSet à partir des feuilles PixelLab (grille 4x4 de tuiles Wang 32x32).
// Évite d'écrire à la main une longue resource .tres pour chaque variante.
public static class TileSetFabrique
{
	public const string DonneeIsIce = "is_ice";
	public const string DonneeIsFragile = "is_fragile";

	// Repérées dans les métadonnées PixelLab : tuile pleine (4 coins "upper") et
	// tuile de surface (coins du haut "lower", coins du bas "upper" = neige sur glace pleine).
	private static readonly Vector2I CoordsPleine = new Vector2I(0, 3);
	private static readonly Vector2I CoordsSurface = new Vector2I(1, 2);

	public static TileSet CreerBanquise()
	{
		var tileSet = CreerTileSetVide();
		int sourcePlein = AjouterSource(tileSet, "res://assets/tiles/banquise_base.png", false, false);
		int sourceGlissant = AjouterSource(tileSet, "res://assets/tiles/banquise_glissante.png", true, false);
		int sourceFragile = AjouterSource(tileSet, "res://assets/tiles/banquise_fragile.png", false, true);
		tileSet.SetMeta("source_plein", sourcePlein);
		tileSet.SetMeta("source_glissant", sourceGlissant);
		tileSet.SetMeta("source_fragile", sourceFragile);
		return tileSet;
	}

	public static TileSet CreerGrotte()
	{
		var tileSet = CreerTileSetVide();
		int sourcePlein = AjouterSource(tileSet, "res://assets/tiles/grotte_base.png", false, false);
		tileSet.SetMeta("source_plein", sourcePlein);
		return tileSet;
	}

	// TileSet unique regroupant toutes les sources (banquise + grotte), nécessaire
	// dès qu'un même niveau/TileMapLayer doit mélanger plusieurs tuilesets.
	public static TileSet CreerMonde()
	{
		var tileSet = CreerTileSetVide();
		tileSet.SetMeta("banquise_plein", AjouterSource(tileSet, "res://assets/tiles/banquise_base.png", false, false));
		tileSet.SetMeta("banquise_glissant", AjouterSource(tileSet, "res://assets/tiles/banquise_glissante.png", true, false));
		tileSet.SetMeta("banquise_fragile", AjouterSource(tileSet, "res://assets/tiles/banquise_fragile.png", false, true));
		tileSet.SetMeta("grotte_plein", AjouterSource(tileSet, "res://assets/tiles/grotte_base.png", false, false));
		return tileSet;
	}

	public static Vector2I CoordsAtlasPleine => CoordsPleine;
	public static Vector2I CoordsAtlasSurface => CoordsSurface;

	private static TileSet CreerTileSetVide()
	{
		var tileSet = new TileSet();
		tileSet.TileSize = new Vector2I(32, 32);

		tileSet.AddCustomDataLayer();
		tileSet.SetCustomDataLayerName(0, DonneeIsIce);
		tileSet.SetCustomDataLayerType(0, Variant.Type.Bool);

		tileSet.AddCustomDataLayer();
		tileSet.SetCustomDataLayerName(1, DonneeIsFragile);
		tileSet.SetCustomDataLayerType(1, Variant.Type.Bool);

		tileSet.AddPhysicsLayer();
		tileSet.SetPhysicsLayerCollisionLayer(0, 1);
		tileSet.SetPhysicsLayerCollisionMask(0, 1);

		return tileSet;
	}

	private static readonly Vector2[] PolygoneTuilePleine =
	{
		new Vector2(-16, -16),
		new Vector2(16, -16),
		new Vector2(16, 16),
		new Vector2(-16, 16),
	};

	private static int AjouterSource(TileSet tileSet, string chemin, bool isIce, bool isFragile)
	{
		var texture = GD.Load<Texture2D>(chemin);
		var source = new TileSetAtlasSource
		{
			Texture = texture,
			TextureRegionSize = new Vector2I(32, 32)
		};

		// La source doit être attachée au TileSet AVANT de créer les tuiles,
		// sinon TileData ne connaît pas encore les couches de données personnalisées.
		int sourceId = tileSet.AddSource(source);

		for (int y = 0; y < 4; y++)
		{
			for (int x = 0; x < 4; x++)
			{
				var coords = new Vector2I(x, y);
				source.CreateTile(coords);
				var donnees = source.GetTileData(coords, 0);
				donnees.SetCustomData(DonneeIsIce, isIce);
				donnees.SetCustomData(DonneeIsFragile, isFragile);
				donnees.AddCollisionPolygon(0);
				donnees.SetCollisionPolygonPoints(0, 0, PolygoneTuilePleine);
			}
		}

		return sourceId;
	}
}
