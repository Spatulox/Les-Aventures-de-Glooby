using Godot;

// Éclat de glace tiré par le canon du Lutin Mecha : un Projectile qui file à plat
// (l'orientation dans le sens de la vitesse vient de la base) puis joue un éclat de
// glace à l'impact avant de disparaître. Pendant de BouleDeNeigeProjectile pour la
// famille « glace » — même idiome, assets différents.
public partial class EclatGlace : Projectile
{
	private const string DossierImpact = "res://assets/projectiles/eclat_glace/impact";
	private const string TextureVol = "res://assets/projectiles/eclat_glace/eclat_glace.png";

	protected override DamageSource Source => DamageSource.EclatGlace;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		base._Ready();   // couches, durée de vie et détection des corps
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.SpriteFrames = ConstruireAnimations();
		_sprite.Play("vol");
	}

	// « vol » = l'éclat seul (sprite fixe, l'orientation suffit à l'animer) ;
	// « impact » = l'éclatement en cristaux.
	private static SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		frames.AddAnimation("vol");
		frames.SetAnimationLoop("vol", true);
		frames.AddFrame("vol", GD.Load<Texture2D>(TextureVol));
		AnimationsSprite.EnregistrerAnimation(frames, "impact",
			AnimationsSprite.ChargerFrames(DossierImpact), 14f, false);
		return frames;
	}

	// Éclatement : redresse l'éclat puis joue les cristaux avant de se libérer.
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
