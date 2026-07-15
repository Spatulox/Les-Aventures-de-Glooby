using Godot;
using System.Collections.Generic;

// Petit morceau de banquise flottant servant de plateforme traversable
// (one-way) : plaque de glace, bloc empilable ou congère. Le joueur saute
// dessus par en dessous et peut retomber au travers (bas+saut), comme la
// PlateformeUnidirectionnelle mais avec un visuel de banquise. Chaque type
// mappe sa texture (assets/decors/banquise/elements) et sa boîte de
// collision ; la surface de marche est calée sur le haut du sprite. Sprite
// et collision sont appliqués au runtime pour rester paramétrables par Type.
public partial class PlateformeBanquise : StaticBody2D
{
	public enum TypeElement { Plaque, Bloc, Congere }

	[Export] public TypeElement Type = TypeElement.Plaque;

	private record Config(string Texture, Vector2 TailleCollision, Vector2 PositionCollision);

	// Tailles natives : plaque 56x32, bloc 40x32, congère 48x32 (x2 à l'écran).
	// La collision couvre la moitié haute (la glace visible) : son bord
	// supérieur est la surface de marche.
	private static readonly Dictionary<TypeElement, Config> Configs = new()
	{
		[TypeElement.Plaque] = new("res://assets/decors/banquise/elements/plaque_flottante.png", new Vector2(96, 28), new Vector2(0, -4)),
		[TypeElement.Bloc] = new("res://assets/decors/banquise/elements/bloc_empilable.png", new Vector2(68, 44), new Vector2(0, -8)),
		[TypeElement.Congere] = new("res://assets/decors/banquise/elements/congere.png", new Vector2(80, 32), new Vector2(0, -2)),
	};

	public override void _Ready()
	{
		CollisionLayer = Constantes.LayerPlateformesTraversables;
		CollisionMask = 0;

		var config = Configs[Type];

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = config.TailleCollision };
		collision.Position = config.PositionCollision;
		collision.OneWayCollision = true;
	}
}
