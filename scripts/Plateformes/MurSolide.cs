using Godot;

// Mur plein, solide et VISIBLE, réutilisable et redimensionnable : une texture au
// choix (par défaut une pile de glace) est répétée en tuiles pour couvrir Taille,
// avec une collision pleine (4 côtés) de mêmes dimensions. Sert de paroi verticale
// (murs du labyrinthe) ou de tout obstacle solide ; contrairement aux
// PlateformeUnidirectionnelle traversables, il bloque le joueur sur ses 4 côtés.
// La texture est exportée pour réutiliser le même mur dans d'autres biomes
// (roche, glace, usine...). Élément de niveau = .tscn réutilisable et visible.
public partial class MurSolide : StaticBody2D
{
	[Export] public Vector2 Taille = new(48, 540);

	// Laisser vide pour garder la texture par défaut de la scène (pile de glace).
	[Export] public Texture2D Texture;

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");
		if (Texture != null)
			sprite.Texture = Texture;
		sprite.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
		sprite.RegionEnabled = true;
		sprite.RegionRect = new Rect2(Vector2.Zero, Taille);

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = Taille };
	}
}
