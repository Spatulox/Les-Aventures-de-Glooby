using Godot;
using System.Collections.Generic;

// Segment de sol de banquise : pièce posable côte à côte pour former un sol
// continu de n'importe quelle longueur. Les 3 centres partagent les mêmes
// bords (greffés depuis le segment A, auto-tuilable) donc sont
// interchangeables ; les embouts portent la cassure de glace et les
// stalactites décoratives (hors collision). Le sprite est décalé par type
// pour que la surface de neige soit toujours à y = -50 en local, quelle que
// soit la hauteur du canevas.
public partial class SolBanquise : StaticBody2D
{
	public enum TypeSegment { CentreA, CentreB, CentreC, EmboutGauche, EmboutDroit }

	[Export] public TypeSegment Type = TypeSegment.CentreA;

	// Largeur d'un emplacement dans une ligne (172px natif x2).
	public const float LargeurSegment = 344f;

	private record Config(string Texture, Vector2 DecalageSprite, Vector2 TailleCollision, Vector2 PositionCollision);

	private static readonly Dictionary<TypeSegment, Config> Configs = new()
	{
		[TypeSegment.CentreA] = new("res://assets/decors/sol/sol_centre_a.png", Vector2.Zero, new Vector2(344, 112), new Vector2(0, 6)),
		[TypeSegment.CentreB] = new("res://assets/decors/sol/sol_centre_b.png", Vector2.Zero, new Vector2(344, 112), new Vector2(0, 6)),
		[TypeSegment.CentreC] = new("res://assets/decors/sol/sol_centre_c.png", Vector2.Zero, new Vector2(344, 112), new Vector2(0, 6)),
		[TypeSegment.EmboutGauche] = new("res://assets/decors/sol/sol_embout_gauche.png", new Vector2(0, 40), new Vector2(300, 112), new Vector2(22, 6)),
		[TypeSegment.EmboutDroit] = new("res://assets/decors/sol/sol_embout_droit.png", new Vector2(0, 40), new Vector2(300, 112), new Vector2(-22, 6)),
	};

	public override void _Ready()
	{
		var config = Configs[Type];

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);
		sprite.Position = config.DecalageSprite;

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new RectangleShape2D { Size = config.TailleCollision };
		collision.Position = config.PositionCollision;
	}
}
