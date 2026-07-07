using Godot;

// Écran 01 "Le Départ" : construit le TileSet banquise par code et peint le sol.
public partial class Ecran01 : Node2D
{
	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		couche.TileSet = TileSetFabrique.CreerBanquise();
		couche.AddToGroup("sol");

		int sourcePlein = (int)couche.TileSet.GetMeta("source_plein");
		int sourceGlissant = (int)couche.TileSet.GetMeta("source_glissant");

		const int rangeeSurface = 8;
		const int rangeesRemplissage = 3;

		TerrainPeintre.PeindreBandeSol(couche, sourcePlein, 0, 13, rangeeSurface, rangeesRemplissage);
		TerrainPeintre.PeindreBandeSol(couche, sourceGlissant, 14, 16, rangeeSurface, rangeesRemplissage);
		TerrainPeintre.PeindreBandeSol(couche, sourcePlein, 17, 19, rangeeSurface, rangeesRemplissage);
	}
}
