using Godot;

// Bloc de grotte solide et VISIBLE, réutilisable et redimensionnable : la texture
// de roche (grotte_base, 128x128) est répétée en tuiles pour couvrir Taille, avec
// une collision pleine (4 côtés) de mêmes dimensions. Sert de sol, plafond ou paroi
// de caverne dans le labyrinthe - remplace les StaticBody2D+CollisionShape2D
// invisibles posés à la main (règle : tout élément de niveau est un .tscn réutilisable).
public partial class BlocGrotte : StaticBody2D
{
	[Export] public Vector2 Taille = new(128, 128);

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
		sprite.RegionEnabled = true;
		sprite.RegionRect = new Rect2(Vector2.Zero, Taille);

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = Taille };
	}
}
