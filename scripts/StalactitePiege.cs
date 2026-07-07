using Godot;

// Stalactite-piège : tremble un court instant après détection du joueur puis
// tombe. Réutilisée par le Chemin du Pouvoir et le Boss Cerf (piétinement).
public partial class StalactitePiege : Area2D
{
	[Export] public float DelaiAvantChute = 0.6f;
	[Export] public float VitesseChute = 500f;
	[Export] public float DistanceChute = 260f;

	private Sprite2D _sprite;
	private Area2D _zoneDetection;
	private bool _declenchee;
	private bool _tombe;
	private float _vitesseActuelle;
	private float _distanceParcourue;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_zoneDetection = GetNode<Area2D>("ZoneDetection");
		_zoneDetection.BodyEntered += OnDetection;
		BodyEntered += OnImpact;
	}

	private void OnDetection(Node2D body)
	{
		if (_declenchee || body is not Player)
			return;

		DeclencherImmediatement();
	}

	// Déclenchement direct (utilisé par le pattern piétinement du Boss Cerf,
	// indépendamment de la position du joueur).
	public void DeclencherImmediatement()
	{
		if (_declenchee)
			return;

		_declenchee = true;
		_ = SecouerPuisTomber();
	}

	private async System.Threading.Tasks.Task SecouerPuisTomber()
	{
		var tween = CreateTween();
		for (int i = 0; i < 5; i++)
		{
			tween.TweenProperty(_sprite, "position:x", 2f, DelaiAvantChute / 10f);
			tween.TweenProperty(_sprite, "position:x", -2f, DelaiAvantChute / 10f);
		}
		await ToSignal(tween, Tween.SignalName.Finished);
		_sprite.Position = Vector2.Zero;
		_tombe = true;
	}

	private void OnImpact(Node2D body)
	{
		if (!_tombe)
			return;

		if (body is Player joueur)
			joueur.SubirDegats(0);
		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_tombe)
			return;

		var dt = (float)delta;
		_vitesseActuelle += VitesseChute * dt;
		float pas = _vitesseActuelle * dt;
		Position += new Vector2(0, pas);
		_distanceParcourue += pas;

		if (_distanceParcourue >= DistanceChute)
			QueueFree();
	}
}
