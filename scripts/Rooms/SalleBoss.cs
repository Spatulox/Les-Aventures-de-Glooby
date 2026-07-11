using Godot;
using static Constantes;

// Arène du Boss Cerf : grande salle plate (~3 écrans de large), fond
// cathédrale répété, plafond garni de stalactites, deux plateformes latérales.
public static class SalleBoss
{
	public const int Largeur = 90;

	public static void Construire(TileMapLayer couche, TileSet tileSet, Node2D racine, Vector2I decalage)
	{
		int sourceId = (int)tileSet.GetMeta("grotte_plein");
		TerrainPeintre.PeindreBandeSol(couche, sourceId, decalage.X, Largeur - 1 + decalage.X, 8 + decalage.Y, 3);
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 13 + decalage.X, 16 + decalage.X, 5 + decalage.Y, 1);
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 73 + decalage.X, 76 + decalage.X, 5 + decalage.Y, 1);

		var dec = new Vector2(decalage.X * TailleTuile, decalage.Y * TailleTuile);

		string[] fond = { "res://assets/backgrounds/grotte_cathedrale.png" };
		int nombrePanneaux = Mathf.CeilToInt(Largeur * TailleTuile / 720f) + 1;
		Outils.PlacerFondRepete(racine, fond, nombrePanneaux, 720f, 260f, 2f, -3, dec, racine);

		var boss = GD.Load<PackedScene>("res://scenes/boss_cerf.tscn").Instantiate<BossCerf>();
		Outils.Attacher(racine, boss);
		boss.Position = new Vector2(2240, 250) + dec;
		boss.LimiteGauche = 6 * TailleTuile + dec.X;
		boss.LimiteDroite = (Largeur - 6) * TailleTuile + dec.X;
		boss.Vaincu += () =>
		{
			var minuteur = racine.GetTree().CreateTimer(2.5);
			minuteur.Timeout += () => racine.GetTree().ChangeSceneToFile("res://scenes/ecran_fin.tscn");
		};

		int[] colonnesStalactites = { 22, 30, 38, 46, 54, 62 };
		foreach (var colonne in colonnesStalactites)
		{
			var stalactite = Outils.Instancier(racine, "res://scenes/stalactite_piege.tscn", new Vector2(colonne * TailleTuile, 80) + dec);
			stalactite.AddToGroup("stalactites_boss");
		}

		var barre = GD.Load<PackedScene>("res://scenes/boss_hud_barre.tscn").Instantiate<BossHudBarre>();
		barre.CheminBoss = "../BossCerf";
		Outils.Attacher(racine, barre);
	}
}
