using Godot;

// Base commune aux boules de neige (celle du joueur et celle du bonhomme de neige) :
// un Projectile animé qui roule en vol (« vol ») puis joue le même éclat de neige
// (« impact » = animation de « mort ») avant de disparaître. Les sous-classes ne
// fournissent que leur DamageSource ; le déplacement/orientation vient de Projectile.
public abstract partial class BouleDeNeigeProjectile : Projectile
{
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		base._Ready();   // couches, durée de vie et détection des corps
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.SpriteFrames = ConstruireAnimations();
		_sprite.Play("vol");
	}

	// « vol » = la boule simple (sprite existant) ; « impact » = l'éclat de neige,
	// l'animation de mort partagée par toutes les boules (assets/entities/boule_de_neige).
	private static SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		frames.AddAnimation("vol");
		frames.SetAnimationLoop("vol", true);
		frames.AddFrame("vol", GD.Load<Texture2D>("res://assets/props/boule_de_neige.png"));
		AnimationsSprite.EnregistrerAnimation(frames, "impact",
			AnimationsSprite.ChargerFrames("res://assets/entities/boule_de_neige"), 18f, false);
		return frames;
	}

	// Éclatement : redresse la boule puis joue l'éclat de neige avant de se libérer.
	protected override void Disparaitre()
	{
		Rotation = 0f;

		if (_sprite.SpriteFrames.GetFrameCount("impact") > 0)
		{
			_sprite.Play("impact");
			_sprite.AnimationFinished += QueueFree;
		}
		else
		{
			QueueFree();
		}
	}
}
