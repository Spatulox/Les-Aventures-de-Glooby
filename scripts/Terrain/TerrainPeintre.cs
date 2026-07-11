using Godot;

// Aide à peindre le terrain d'une salle sur un TileMapLayer partagé,
// réutilisable pour tous les écrans (surface en haut, remplissage plein en dessous).
public static class TerrainPeintre
{
	// Un segment de terrain décrit une bande rectangulaire : de la colonne
	// ColDebut à ColFin, avec sa rangée de surface, la source de tuile (clé de
	// métadonnée du TileSet) et le nombre de rangées de remplissage sous la surface.
	public record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	// Peint tous les segments d'une salle à son décalage (en tuiles) dans le
	// monde continu. Remplace la boucle GetMeta+PeindreBandeSol dupliquée dans
	// chaque salle par un seul appel.
	public static void PeindreSegments(TileMapLayer couche, TileSet tileSet, Segment[] segments, Vector2I decalage)
	{
		foreach (var segment in segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			PeindreBandeSol(couche, sourceId, segment.ColDebut + decalage.X, segment.ColFin + decalage.X, segment.Rangee + decalage.Y, segment.Profondeur);
		}
	}

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
