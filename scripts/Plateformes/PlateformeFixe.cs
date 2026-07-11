using Godot;
using System.Collections.Generic;

// Plateforme fixe, un seul objet peint (pas un tileset) : la neige du dessus
// porte la collision, les stalactites en dessous sont purement décoratives.
// 3 tailles disponibles, chacune avec sa propre silhouette générée (pas un
// étirement de la même image).
public partial class PlateformeFixe : StaticBody2D
{
	public enum TaillePlateforme { Petite, Moyenne, Grande }

	[Export] public TaillePlateforme Taille = TaillePlateforme.Petite;

	private record Config(string Texture, Vector2 TailleCollision, Vector2 PositionCollision);

	// Sprites générés en résolution native puis affichés à l'échelle x2 (comme
	// les couches de fond) : ce sont de gros objets de gameplay, pas des
	// petits props décoratifs affichés en 1:1. Zone de collision calculée sur
	// le sommet de neige uniquement (mesurée sur chaque sprite).
	private static readonly Dictionary<TaillePlateforme, Config> Configs = new()
	{
		[TaillePlateforme.Petite] = new("res://assets/plateformes/fixe_petite.png", new Vector2(266, 52), new Vector2(-4, -62)),
		[TaillePlateforme.Moyenne] = new("res://assets/plateformes/fixe_moyenne.png", new Vector2(468, 36), new Vector2(-1, -44)),
		[TaillePlateforme.Grande] = new("res://assets/plateformes/fixe_grande.png", new Vector2(652, 38), new Vector2(-2, -45)),
	};

	public override void _Ready()
	{
		var config = Configs[Taille];

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = config.TailleCollision };
		collision.Position = config.PositionCollision;
	}
}
