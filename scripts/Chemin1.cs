using Godot;

// Chemin 1 : corridor principal, praticable immédiatement, mène à l'arène du Boss Cerf.
public partial class Chemin1 : Node2D
{
	private const int TailleTuile = 32;

	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		var tileSet = TileSetFabrique.CreerMonde();
		couche.TileSet = tileSet;
		couche.AddToGroup("sol");

		int sourceId = (int)tileSet.GetMeta("grotte_plein");
		TerrainPeintre.PeindreBandeSol(couche, sourceId, 0, 26, 8, 3);

		AjouterDecor("res://assets/props/cristal_gros.png", new Vector2(8 * TailleTuile, 8 * TailleTuile - 28));
		AjouterDecor("res://assets/props/rocher_glace.png", new Vector2(16 * TailleTuile, 8 * TailleTuile - 24));
		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(22 * TailleTuile, 8 * TailleTuile - 20));

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = 27 * TailleTuile;
		camera.LimitBottom = 400;
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
