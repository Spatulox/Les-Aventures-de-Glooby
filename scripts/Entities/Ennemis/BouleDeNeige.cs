using Godot;

// Boule de neige lancée par le Bonhomme de neige : projectile Area2D à
// trajectoire en cloche (vélocité initiale libre + gravité, contrairement au
// Projectile de base du joueur qui ne fait que retomber). Blesse le joueur au
// contact puis éclate en un petit éclat de neige (animation « impact ») avant
// de disparaître. Fichier neuf, indépendant de la boule_de_neige.tscn du joueur.
public partial class BouleDeNeige : Area2D
{
	[Export] public float Gravite = 520f;
	[Export] public float DureeVie = 4f;
	// Source de dégâts : l'attaque d'un PNJ méchant (1 PV), comme le contact.
	private const DamageSource Source = DamageSource.ContactMechant;

	private Vector2 _velocite;
	private float _tempsRestant;
	private bool _eclate;
	private AnimatedSprite2D _sprite;

	// Configure le tir : vecteur vitesse initial (composante verticale négative
	// = la boule monte d'abord, d'où la cloche). À appeler avant l'ajout à l'arbre.
	public void Lancer(Vector2 velocite) => _velocite = velocite;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.SpriteFrames = ConstruireAnimations();
		_tempsRestant = DureeVie;
		_sprite.Play("vol");
		BodyEntered += OnBodyEntered;
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

	public override void _PhysicsProcess(double delta)
	{
		if (_eclate)
			return;

		var dt = (float)delta;
		_velocite.Y += Gravite * dt;
		Position += _velocite * dt;
		// Oriente la boule dans le sens de sa chute (petit plus visuel).
		Rotation = _velocite.Angle();

		_tempsRestant -= dt;
		if (_tempsRestant <= 0f)
			Eclater();
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player)
			Degats.Infliger(body, Source);
		Eclater();
	}

	// Stoppe la boule et joue l'éclat de neige, puis se libère.
	private void Eclater()
	{
		if (_eclate)
			return;

		_eclate = true;
		SetPhysicsProcess(false);
		Rotation = 0f;

		if (_sprite.SpriteFrames.HasAnimation("impact") && _sprite.SpriteFrames.GetFrameCount("impact") > 0)
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
