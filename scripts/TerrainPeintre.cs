using Godot;

// Aide à peindre des bandes de sol rectangulaires sur un TileMapLayer,
// réutilisable pour tous les écrans (surface en haut, remplissage plein en dessous).
public static class TerrainPeintre
{
	public static void PeindreBandeSol(TileMapLayer couche, int sourceId, int colonneDebut, int colonneFin, int rangeeSurface, int rangeesRemplissage)
	{
		for (int x = colonneDebut; x <= colonneFin; x++)
		{
			couche.SetCell(new Vector2I(x, rangeeSurface), sourceId, TileSetFabrique.CoordsAtlasSurface);
			for (int y = 1; y <= rangeesRemplissage; y++)
				couche.SetCell(new Vector2I(x, rangeeSurface + y), sourceId, TileSetFabrique.CoordsAtlasPleine);
		}
	}
}
