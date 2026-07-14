using Godot;
using System.Collections.Generic;

// Sapin de Noël décoré, prop de décor pur (pas de collision) : grand format
// pour servir de point de repère, petit pour le dressing des salles.
public partial class SapinNoel : Node2D
{
	public enum TailleSapin { Grand, Petit }

	[Export] public TailleSapin Taille = TailleSapin.Petit;

	private static readonly Dictionary<TailleSapin, string> Textures = new()
	{
		[TailleSapin.Grand] = "res://assets/props/noel/sapin_grand.png",
		[TailleSapin.Petit] = "res://assets/props/noel/sapin_petit.png",
	};

	public override void _Ready()
	{
		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(Textures[Taille]);
	}
}
