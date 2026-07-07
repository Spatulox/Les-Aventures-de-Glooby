using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 220f;
	[Export] public float Acceleration = 1600f;
	[Export] public float Friction = 1400f;
	[Export] public float JumpVelocity = -420f;
	[Export] public float Gravity = 1200f;
	[Export] public float MaxFallSpeed = 900f;
	[Export] public float CoyoteTime = 0.12f;
	[Export] public float SlideSpeed = 420f;
	[Export] public float SlideDuration = 0.35f;
	[Export] public float SlideCooldown = 0.4f;

	private Sprite2D _sprite;
	private CollisionShape2D _colDebout;
	private CollisionShape2D _colGlisse;

	private float _coyoteTimer;
	private float _slideTimer;
	private float _slideCooldownTimer;
	private bool _enGlissade;
	private int _directionGlissade = 1;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_colDebout = GetNode<CollisionShape2D>("CollisionDebout");
		_colGlisse = GetNode<CollisionShape2D>("CollisionGlisse");
		_colGlisse.Disabled = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		var velocity = Velocity;

		var auSol = IsOnFloor();
		_coyoteTimer = auSol ? CoyoteTime : Mathf.Max(0f, _coyoteTimer - dt);
		if (_slideCooldownTimer > 0f)
			_slideCooldownTimer -= dt;

		velocity.Y = Mathf.Min(velocity.Y + Gravity * dt, MaxFallSpeed);

		var direction = Input.GetAxis("move_left", "move_right");

		if (_enGlissade)
		{
			_slideTimer -= dt;
			velocity.X = _directionGlissade * SlideSpeed;
			if (_slideTimer <= 0f || (!auSol && velocity.Y > 0f))
				FinirGlissade();
		}
		else
		{
			if (Mathf.Abs(direction) > 0.01f)
			{
				velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * dt);
				_sprite.FlipH = direction < 0f;
			}
			else
			{
				velocity.X = Mathf.MoveToward(velocity.X, 0f, Friction * dt);
			}

			if (Input.IsActionJustPressed("jump") && _coyoteTimer > 0f)
			{
				velocity.Y = JumpVelocity;
				_coyoteTimer = 0f;
			}
			if (Input.IsActionJustReleased("jump") && velocity.Y < 0f)
			{
				velocity.Y *= 0.5f;
			}

			if (Input.IsActionJustPressed("slide") && auSol && _slideCooldownTimer <= 0f)
			{
				var directionGlissade = Mathf.Abs(direction) > 0.01f
					? (int)Mathf.Sign(direction)
					: (_sprite.FlipH ? -1 : 1);
				DemarrerGlissade(directionGlissade);
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void DemarrerGlissade(int direction)
	{
		_enGlissade = true;
		_directionGlissade = direction;
		_slideTimer = SlideDuration;
		_colDebout.Disabled = true;
		_colGlisse.Disabled = false;
	}

	private void FinirGlissade()
	{
		_enGlissade = false;
		_slideCooldownTimer = SlideCooldown;
		_colDebout.Disabled = false;
		_colGlisse.Disabled = true;
	}
}
