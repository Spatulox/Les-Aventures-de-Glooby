using Godot;
using System.Collections.Generic;

// Petit morceau de banquise servant de sol surélevé : plaque de glace, bloc
// empilable ou congère. C'est un sol *plein*, pas une plateforme traversable :
// il reste sur le layer de collision par défaut (layer 1), donc bas+saut ne
// permet pas de tomber au travers — ces morceaux surplombent souvent un trou
// mortel, où une traversée serait une mort accidentelle. Chaque type mappe sa
// texture (assets/decors/banquise/elements) et sa boîte de collision ; la
// surface de marche est calée sur le haut du sprite. Sprite et collision sont
// appliqués au runtime pour rester paramétrables par Type.
// Le .tscn embarque le visuel du type par défaut (Plaque) pour que la pièce
// soit VISIBLE dans l'éditeur ; _Ready le ré-applique ensuite depuis Type.
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
		[TypeElement.Plaque] = new("res://assets/sol/elements/plaque_flottante.png", new Vector2(96, 28), new Vector2(0, -4)),
		[TypeElement.Bloc] = new("res://assets/sol/elements/bloc_empilable.png", new Vector2(68, 44), new Vector2(0, -8)),
		[TypeElement.Congere] = new("res://assets/sol/elements/congere.png", new Vector2(80, 32), new Vector2(0, -2)),
	};

	public override void _Ready()
	{
		// Layer 1 (terrain, vu par le joueur) + layer sol des PNJ. Voir Constantes.
		CollisionLayer |= Constantes.LayerSolPnj;

		var config = Configs[Type];

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = config.TailleCollision };
		collision.Position = config.PositionCollision;
	}
}
