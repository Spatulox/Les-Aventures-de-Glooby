using Godot;

// Projectile du lancer de boule de neige : vitesse constante, légère gravité,
// éclate (fondu + agrandissement) au contact plutôt que d'utiliser des frames dédiées.
public partial class Snowball : Area2D
{
	[Export] public float Vitesse = 320f;
	[Export] public float Gravite = 480f;
	[Export] public float DureeVie = 3f;

	public int Direction = 1;

	private Vector2 _velocite;
	private float _tempsRestant;
	private bool _impact;

	public override void _Ready()
	{
		_velocite = new Vector2(Direction * Vitesse, 0f);
		_tempsRestant = DureeVie;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_impact)
			return;

		var dt = (float)delta;
		_velocite.Y += Gravite * dt;
		Position += _velocite * dt;

		_tempsRestant -= dt;
		if (_tempsRestant <= 0f)
			Eclater();
	}

	private void OnBodyEntered(Node body)
	{
		if (body is BossCerf boss)
			boss.SubirDegats(1);
		Eclater();
	}

	private void Eclater()
	{
		if (_impact)
			return;

		_impact = true;
		SetPhysicsProcess(false);

		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Scale * 1.6f, 0.12f);
		tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.12f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
