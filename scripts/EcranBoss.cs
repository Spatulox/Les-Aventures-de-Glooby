using Godot;

// Arène du Boss Cerf : grande salle plate (~3 écrans de large), fond
// cathédrale répété, plafond garni de stalactites, deux plateformes latérales.
public partial class EcranBoss : Node2D
{
	private const int TailleTuile = 32;
	private const int LargeurNiveau = 90;

	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		var tileSet = TileSetFabrique.CreerMonde();
		couche.TileSet = tileSet;
		couche.AddToGroup("sol");

		int sourceId = (int)tileSet.GetMeta("grotte_plein");
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 0, LargeurNiveau - 1, 8, 3);
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 13, 16, 5, 1); // plateforme latérale gauche
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 73, 76, 5, 1); // plateforme latérale droite

		PlacerFond();

		var boss = GetNode<BossCerf>("BossCerf");
		boss.LimiteGauche = 6 * TailleTuile;
		boss.LimiteDroite = (LargeurNiveau - 6) * TailleTuile;
		boss.Vaincu += OnBossVaincu;

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = LargeurNiveau * TailleTuile;
		camera.LimitBottom = 400;
	}

	private void PlacerFond()
	{
		var texture = GD.Load<Texture2D>("res://assets/backgrounds/grotte_cathedrale.png");
		int nombrePanneaux = Mathf.CeilToInt(LargeurNiveau * TailleTuile / 720f) + 1;
		for (int i = 0; i < nombrePanneaux; i++)
		{
			var panneau = new Sprite2D
			{
				Texture = texture,
				Scale = new Vector2(2f, 2f),
				Position = new Vector2(i * 720f + 360f, 260),
				ZIndex = -3
			};
			AddChild(panneau);
		}
	}

	private void OnBossVaincu()
	{
		var minuteur = GetTree().CreateTimer(2.5);
		minuteur.Timeout += () => GetTree().ChangeSceneToFile("res://scenes/ecran_fin.tscn");
	}
}
