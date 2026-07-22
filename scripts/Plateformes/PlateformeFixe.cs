using Godot;
using System.Collections.Generic;

// Plateforme fixe, un seul objet peint (pas un tileset) : la neige du dessus
// porte la collision, les stalactites en dessous sont purement décoratives.
// 3 tailles disponibles, chacune avec sa propre silhouette générée (pas un
// étirement de la même image).
//
// Script [Tool] : sprite + collision sont (re)construits depuis Configs[Taille]
// À LA FOIS dans l'éditeur et en jeu. Changer « Taille » dans l'inspecteur met à
// jour la texture ET la hitbox en direct dans Godot — l'éditeur montre donc
// exactement ce qu'on aura en jeu (fini le décalage éditeur ≠ runtime).
[Tool]
public partial class PlateformeFixe : StaticBody2D
{
	public enum TaillePlateforme { Petite, Moyenne, Grande }

	private TaillePlateforme _taille = TaillePlateforme.Petite;
	[Export] public TaillePlateforme Taille
	{
		get => _taille;
		set { _taille = value; Appliquer(); }
	}

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

	public override void _Ready() => Appliquer();

	// Applique la texture + la collision de la taille courante. Réutilisée par le
	// setter (édition live dans l'éditeur) et par _Ready (chargement en jeu comme
	// en éditeur). Les enfants peuvent ne pas encore exister quand le setter tombe
	// pendant le chargement de la scène : dans ce cas on ne fait rien, _Ready rejouera.
	private void Appliquer()
	{
		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (sprite == null || collision == null)
			return;

		var config = Configs[_taille];
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);
		collision.Shape = new RectangleShape2D { Size = config.TailleCollision };
		collision.Position = config.PositionCollision;
	}
}
