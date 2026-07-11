using Godot;

// Base commune aux projectiles (boule de neige...) : déplacement horizontal à vitesse
// constante + gravité + durée de vie, dégâts au contact via un DamageSource, puis
// disparition. Garde une référence à son instanciateur (le LivingEntity qui l'a tiré)
// pour ne jamais le blesser avec son propre projectile.
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

	// Configure le tir : qui l'a lancé (immunisé) et dans quelle direction.
	// À appeler avant l'ajout à l'arbre.
	public void Initialiser(LivingEntity instanciateur, int direction)
	{
		_instanciateur = instanciateur;
		Direction = direction;
	}

	public override void _Ready()
	{
		_tempsRestant = DureeVie;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_impact)
			return;

		var dt = (float)delta;
		_vitesseChute += Gravite * dt;
		Position += new Vector2(Direction * Vitesse, _vitesseChute) * dt;

		_tempsRestant -= dt;
		if (_tempsRestant <= 0f)
			Eclater();
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
