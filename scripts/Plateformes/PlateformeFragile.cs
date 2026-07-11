using Godot;

// Plateforme fragile : le joueur doit rester dessus pour la faire céder.
// 3 textures de fissuration progressive (1/3 du délai chacune) puis
// effondrement (collision coupée, sprite masqué). Réapparaît après
// DelaiRespawn si RespawnActif est vrai. Quitte la plateforme avant la fin
// du délai réinitialise la fissuration.
public partial class PlateformeFragile : StaticBody2D
{
	[Export] public float DelaiEffondrementTotal = 1.5f;
	[Export] public bool RespawnActif = true;
	[Export] public float DelaiRespawn = 3f;

	private static readonly string[] TexturesEtats =
	{
		"res://assets/plateformes/fragile_etat1.png",
		"res://assets/plateformes/fragile_etat2.png",
		"res://assets/plateformes/fragile_etat3.png",
	};

	private Sprite2D _sprite;
	private CollisionShape2D _collision;
	private Area2D _zoneDetection;

	private bool _joueurPresent;
	private float _timer;
	private bool _effondree;
	private float _timerRespawn;

	public bool EstEffondree => _effondree;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		_zoneDetection = GetNode<Area2D>("ZoneDetection");

		_sprite.Texture = GD.Load<Texture2D>(TexturesEtats[0]);
		_sprite.Scale = new Vector2(2, 2);

		_zoneDetection.BodyEntered += body => { if (body is Player) _joueurPresent = true; };
		_zoneDetection.BodyExited += body => { if (body is Player) { _joueurPresent = false; RéinitialiserFissuration(); } };
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		if (_effondree)
		{
			if (!RespawnActif)
				return;

			_timerRespawn += dt;
			if (_timerRespawn >= DelaiRespawn)
				Réapparaitre();
			return;
		}

		if (!_joueurPresent)
			return;

		_timer += dt;
		int etat = Mathf.Min(2, (int)(_timer / (DelaiEffondrementTotal / 3f)));
		_sprite.Texture = GD.Load<Texture2D>(TexturesEtats[etat]);

		if (_timer >= DelaiEffondrementTotal)
			Effondrer();
	}

	private void RéinitialiserFissuration()
	{
		if (_effondree)
			return;

		_timer = 0f;
		_sprite.Texture = GD.Load<Texture2D>(TexturesEtats[0]);
	}

	private void Effondrer()
	{
		_effondree = true;
		_timerRespawn = 0f;
		_collision.Disabled = true;
		_sprite.Visible = false;
	}

	private void Réapparaitre()
	{
		_effondree = false;
		_timer = 0f;
		_joueurPresent = false;
		_collision.Disabled = false;
		_sprite.Visible = true;
		_sprite.Texture = GD.Load<Texture2D>(TexturesEtats[0]);
	}
}
