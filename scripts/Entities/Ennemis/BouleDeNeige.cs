using Godot;

// Boule de neige lancée par le Bonhomme de neige : même Projectile que celui du
// joueur (dégâts, immunité au tireur, couches de collision, durée de vie), avec
// deux spécificités — une trajectoire en cloche (via la surcharge Initialiser à
// vecteur vitesse, la boule monte puis retombe) et un éclat de neige animé à
// l'impact plutôt qu'une simple disparition. L'orientation en vol vient de
// Projectile, partagée avec la boule du joueur.
public partial class BouleDeNeige : Projectile
{
	// Blesse le joueur comme le contact d'un méchant (1 PV).
	protected override DamageSource Source => DamageSource.ContactMechant;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		base._Ready();   // couches, durée de vie et détection des corps
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.SpriteFrames = ConstruireAnimations();
		_sprite.Play("vol");
	}

	// « vol » = la boule simple (sprite existant) ; « impact » = l'éclat de neige.
	private static SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		frames.AddAnimation("vol");
		frames.SetAnimationLoop("vol", true);
		frames.AddFrame("vol", GD.Load<Texture2D>("res://assets/props/boule_de_neige.png"));
		AnimationsSprite.EnregistrerAnimation(frames, "impact",
			AnimationsSprite.ChargerFrames("res://assets/props/boule_neige_impact"), 18f, false);
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
