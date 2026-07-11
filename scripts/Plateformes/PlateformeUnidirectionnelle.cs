using Godot;

// Plateforme traversable (one-way) : solide de dessus, mais le joueur peut
// tomber au travers en appuyant sur bas+saut (voir Player.GererTraverseePlateforme).
// Sur un layer physique dédié (Constantes.LayerPlateformesTraversables) plutôt
// que le layer 1 du terrain normal, pour que ce retrait temporaire du masque
// du joueur ne touche jamais les collisions normales.
public partial class PlateformeUnidirectionnelle : StaticBody2D
{
	public override void _Ready()
	{
		CollisionLayer = Constantes.LayerPlateformesTraversables;
		CollisionMask = 0;

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.OneWayCollision = true;
	}
}
