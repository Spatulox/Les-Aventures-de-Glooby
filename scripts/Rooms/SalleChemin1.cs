using Godot;
using static Constantes;

// Chemin 1 : corridor principal, praticable immédiatement, mène à l'arène du
// Boss Cerf. Checkpoint juste avant l'arène (mort = re-tentative immédiate).
public static class SalleChemin1
{
	public const int Largeur = 27;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		int sourceId = (int)tileSet.GetMeta("grotte_plein");
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 0 + decalage.X, 26 + decalage.X, 8 + decalage.Y, 3);

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		Outils.AjouterDecor(racine, "res://assets/props/cristal_gros.png", new Vector2(8 * TailleTuile, 8 * TailleTuile - 28) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/rocher_glace.png", new Vector2(16 * TailleTuile, 8 * TailleTuile - 24) + dec);
		Outils.AjouterDecor(racine, "res://assets/props/cristal_petit.png", new Vector2(22 * TailleTuile, 8 * TailleTuile - 20) + dec);

		Outils.Instancier(racine, "res://scenes/checkpoint_peche.tscn", new Vector2(700, 242) + dec,
			n => n.Set("IdCheckpoint", "chemin1_campement_avant_boss"));
	}
}
