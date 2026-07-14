using Godot;
using System.Collections.Generic;

// Établi en bois encombré, prop purement décoratif de l'usine : deux
// variantes d'encombrement (outils/jouet en cours vs débordant de pots,
// robot et cadeaux) pour habiller sans effet de série.
public partial class BancTravail : Node2D
{
	public enum VarianteBanc { Simple, Encombre }

	[Export] public VarianteBanc Variante = VarianteBanc.Simple;

	private static readonly Dictionary<VarianteBanc, string> Textures = new()
	{
		[VarianteBanc.Simple] = "res://assets/props/noel/banc_travail_a.png",
		[VarianteBanc.Encombre] = "res://assets/props/noel/banc_travail_b.png",
	};

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(Textures[Variante]);
	}
}
