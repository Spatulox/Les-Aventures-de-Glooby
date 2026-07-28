using Godot;

// Base commune aux projectiles (boule de neige...) : déplacement horizontal à vitesse
// constante + gravité + durée de vie, dégâts au contact via un DamageSource, puis
// disparition. Le projectile s'oriente en vol dans le sens de sa vitesse, si bien
// qu'il pique du nez en retombant. Garde une référence à son instanciateur (le
// LivingEntity qui l'a tiré) pour ne jamais le blesser avec son propre projectile.
public abstract partial class Projectile : Area2D
{
	[Export] public float Vitesse = 320f;
	[Export] public float Gravite = 480f;
	[Export] public float DureeVie = 3f;

	public int Direction = 1;

	// Source de dégâts infligée au contact (fournie par la sous-classe).
	protected abstract DamageSource Source { get; }

	private LivingEntity _instanciateur;
	private float _vitesseChute;
	private float _tempsRestant;
	private bool _impact;
	// Sprite du projectile, s'il en a un : sert UNIQUEMENT au miroir de l'orientation
	// (voir OrienterSurLaVitesse). Optionnel — un projectile sans sprite reste valide.
	private AnimatedSprite2D _sprite;

	// Configure le tir : qui l'a lancé (immunisé) et dans quelle direction.
	// À appeler avant l'ajout à l'arbre.
	public void Initialiser(LivingEntity instanciateur, int direction)
	{
		_instanciateur = instanciateur;
		Direction = direction;
	}

	// Configure un tir en cloche : vecteur vitesse initial complet (composante Y
	// négative = le projectile monte d'abord avant de retomber, là où la surcharge
	// ci-dessus part toujours à l'horizontale). À appeler avant l'ajout à l'arbre.
	public void Initialiser(LivingEntity instanciateur, Vector2 velocite)
	{
		_instanciateur = instanciateur;
		Direction = velocite.X < 0f ? -1 : 1;
		Vitesse = Mathf.Abs(velocite.X);
		_vitesseChute = velocite.Y;
	}

	// Vélocité courante du projectile, pour les sous-classes qui veulent s'orienter
	// dessus (une boule en cloche qui pointe dans le sens de sa chute).
	protected Vector2 VelociteCourante => new Vector2(Direction * Vitesse, _vitesseChute);

	public override void _Ready()
	{
		// Un projectile ne se déclare sur aucun layer (rien ne doit le heurter) et masque
		// tout ce sur quoi on marche — terrain ET plateformes traversables, pour être
		// arrêté par le décor solide au lieu de le traverser — plus le joueur et les PNJ
		// (pour blesser). Posé en code pour que toute scène de projectile soit correcte
		// d'office, quels que soient les réglages du .tscn.
		CollisionLayer = 0;
		CollisionMask = Constantes.MasqueProjectile;

		// Un tir vit dans le même plan de rendu que celui qui l'a lancé : sinon il passe
		// derrière le joueur et les plateformes qu'il est censé heurter, et l'impact ne se
		// lit plus. Comme les projectiles sont instanciés en cours de partie (donc en fin
		// d'arbre), ils se dessinent après le joueur à z égal, ce qui est le bon ordre.
		ZIndex = Constantes.ZJoueur;

		_tempsRestant = DureeVie;
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_impact)
			return;

		var dt = (float)delta;
		_vitesseChute += Gravite * dt;
		Position += new Vector2(Direction * Vitesse, _vitesseChute) * dt;
		OrienterSurLaVitesse();

		_tempsRestant -= dt;
		if (_tempsRestant <= 0f)
			Eclater();
	}

	// Oriente le projectile dans le sens de son déplacement : il part à plat et pique
	// progressivement du nez à mesure que la gravité le fait retomber.
	//
	// Vers la GAUCHE, une rotation seule ne suffit pas : l'angle vaut alors ~180°, ce qui
	// RETOURNE le sprite (cadeau la tête en bas, boule de neige à l'envers) au lieu de le
	// miroiter. Un tir vers la gauche est le MIROIR d'un tir vers la droite, pas sa rotation
	// d'un demi-tour. On compose donc un miroir horizontal (FlipH sur le sprite, jamais sur
	// la racine : ça déformerait aussi la forme de collision) avec l'angle du vecteur opposé
	// — les deux se combinent exactement en la réflexion voulue, et l'objet reste debout.
	private void OrienterSurLaVitesse()
	{
		var vitesse = VelociteCourante;
		bool versLaGauche = Direction < 0;

		Rotation = versLaGauche ? (-vitesse).Angle() : vitesse.Angle();
		if (_sprite != null)
			_sprite.FlipH = versLaGauche;
	}

	private void OnBodyEntered(Node body)
	{
		// Traverse son propre instanciateur : ni dégât, ni éclatement.
		if (body == _instanciateur)
			return;

		Degats.Infliger(body, Source);
		Eclater();
	}

	// Marque l'impact, stoppe le projectile puis lance son effet de disparition.
	protected void Eclater()
	{
		if (_impact)
			return;

		_impact = true;
		SetPhysicsProcess(false);
		Disparaitre();
	}

	// Effet de disparition du projectile. Par défaut, libère immédiatement le nœud.
	protected virtual void Disparaitre() => QueueFree();
}
