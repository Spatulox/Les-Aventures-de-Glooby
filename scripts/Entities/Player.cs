using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 220f;
	[Export] public float Acceleration = 1600f;
	[Export] public float Friction = 1400f;
	[Export] public float JumpVelocity = -420f;
	[Export] public float Gravity = 1200f;
	[Export] public float MaxFallSpeed = 900f;
	[Export] public float CoyoteTime = 0.12f;
	[Export] public float JumpBufferTime = 0.12f;
	[Export] public float SlideSpeed = 420f;
	[Export] public float SlideSpeedBonusGlace = 1.4f;
	[Export] public float SlideDuration = 0.35f;
	[Export] public float SlideCooldown = 0.4f;
	[Export] public float LancerCooldown = 0.5f;
	[Export] public float LancerDuree = 0.35f;
	[Export] public float DegatsDuree = 0.4f;
	[Export] public float InvincibiliteDuree = 1.0f;
	[Export] public float ClignoteInterval = 0.08f;
	[Export] public float DelaiRuptureFragile = 0.4f;
	[Export] public float DureeTraverseePlateforme = 0.3f;
	// Ajusté dynamiquement par la CameraZone active (le monde continu a des
	// salles à des profondeurs très différentes - un seuil absolu unique
	// déclencherait le filet de sécurité en permanence dans les salles profondes).
	public float SeuilChuteVide = 700f;
	[Export] public PackedScene SceneBouleDeNeige;

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _colDebout;
	private CollisionShape2D _colGlisse;
	private TileMapLayer _coucheSol;

	private float _coyoteTimer;
	private float _bufferSautTimer;
	private float _slideTimer;
	private float _slideCooldownTimer;
	private float _slideVitesseActuelle;
	private float _lancerTimer;
	private float _lancerCooldownTimer;
	private float _degatsTimer;
	private float _invincibiliteTimer;
	private float _clignoteTimer;
	private bool _enGlissade;
	private bool _enLancer;
	private bool _enDegats;
	private int _directionRegard = 1;

	private float _timerFragile;
	private Vector2I _celluleFragileActuelle;
	private bool _celluleFragileValide;

	private float _traverseeTimer;

	public override void _Ready()
	{
		AddToGroup("joueur");

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_colDebout = GetNode<CollisionShape2D>("CollisionDebout");
		_colGlisse = GetNode<CollisionShape2D>("CollisionGlisse");
		_colGlisse.Disabled = true;

		ChargerAnimations();
		_coucheSol = GetTree().GetFirstNodeInGroup("sol") as TileMapLayer;
		GameState.Instance.JoueurMort += OnJoueurMort;

		JouerApparition();
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		var velocity = Velocity;

		var auSol = IsOnFloor();
		_coyoteTimer = auSol ? CoyoteTime : Mathf.Max(0f, _coyoteTimer - dt);

		if (Input.IsActionJustPressed("jump"))
			_bufferSautTimer = JumpBufferTime;
		else
			_bufferSautTimer = Mathf.Max(0f, _bufferSautTimer - dt);

		if (_slideCooldownTimer > 0f)
			_slideCooldownTimer -= dt;
		if (_lancerCooldownTimer > 0f)
			_lancerCooldownTimer -= dt;

		velocity.Y = Mathf.Min(velocity.Y + Gravity * dt, MaxFallSpeed);

		var direction = Input.GetAxis("move_left", "move_right");
		if (Mathf.Abs(direction) > 0.01f)
			_directionRegard = (int)Mathf.Sign(direction);

		GererGlaceFragile(auSol, dt);
		bool vientDeTraverser = GererTraverseePlateforme(auSol, dt);

		if (_enGlissade)
		{
			_slideTimer -= dt;
			velocity.X = _directionRegard * _slideVitesseActuelle;
			// La glissade se termine uniquement à l'épuisement du minuteur : une
			// glissade lancée au sol se poursuit en l'air (ex. tremplin/rebord)
			// au lieu d'être coupée dès qu'on quitte le sol.
			if (_slideTimer <= 0f)
				FinirGlissade();
		}
		else
		{
			if (Mathf.Abs(direction) > 0.01f)
				velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * dt);
			else
				velocity.X = Mathf.MoveToward(velocity.X, 0f, Friction * ObtenirFrictionSol(auSol) * dt);

			if (_bufferSautTimer > 0f && _coyoteTimer > 0f && !vientDeTraverser)
			{
				velocity.Y = JumpVelocity;
				_coyoteTimer = 0f;
				_bufferSautTimer = 0f;
			}
			if (Input.IsActionJustReleased("jump") && velocity.Y < 0f)
				velocity.Y *= 0.5f;

			if (Input.IsActionJustPressed("slide") && auSol && _slideCooldownTimer <= 0f)
				DemarrerGlissade();

			if (Input.IsActionJustPressed("lancer") && _lancerCooldownTimer <= 0f)
				Lancer();

			if (Input.IsActionJustPressed("manger"))
				Manger();

			if (Input.IsActionJustPressed("pouvoir_chaleur"))
				UtiliserPouvoirChaleur();
		}

		if (_enLancer)
		{
			_lancerTimer -= dt;
			if (_lancerTimer <= 0f)
				_enLancer = false;
		}
		if (_enDegats)
		{
			_degatsTimer -= dt;
			if (_degatsTimer <= 0f)
				_enDegats = false;
		}

		GererInvincibilite(dt);

		Velocity = velocity;
		MoveAndSlide();

		if (GlobalPosition.Y > SeuilChuteVide)
		{
			TomberDansLeVide();
			return;
		}

		MettreAJourAnimation(auSol, direction);
	}

	// Filet de sécurité : une chute manquée dans un trou ne doit jamais se
	// terminer en vide sans fond, elle renvoie au dernier campement activé.
	private void TomberDansLeVide()
	{
		GameState.Instance?.RespawnAuCheckpoint();
		TeleporterAuCheckpoint();
	}

	private void OnJoueurMort()
	{
		GameState.Instance.RespawnAuCheckpoint();
		TeleporterAuCheckpoint();
	}

	private void TeleporterAuCheckpoint()
	{
		GlobalPosition = GameState.Instance.CheckpointPosition;
		Velocity = Vector2.Zero;
		JouerApparition();
	}

	public bool EstInvincible => _invincibiliteTimer > 0f;
	public bool EstEnGlissade => _enGlissade;

	public void SubirDegats(int direction, int quantite = 1)
	{
		if (EstInvincible)
			return;

		GameState.Instance?.Degats(quantite);
		_enDegats = true;
		_degatsTimer = DegatsDuree;
		_invincibiliteTimer = InvincibiliteDuree;
		Velocity = new Vector2(-direction * 120f, -180f);
	}

	private void GererInvincibilite(float dt)
	{
		if (_invincibiliteTimer <= 0f)
		{
			_sprite.Visible = true;
			return;
		}

		_invincibiliteTimer -= dt;
		_clignoteTimer -= dt;
		if (_clignoteTimer <= 0f)
		{
			_clignoteTimer = ClignoteInterval;
			_sprite.Visible = !_sprite.Visible;
		}

		if (_invincibiliteTimer <= 0f)
			_sprite.Visible = true;
	}

	// Manger un poisson (soin) : pas d'animation dédiée pour économiser le budget
	// de génération, un flash vert suffit comme retour visuel immédiat.
	private void Manger()
	{
		if (GameState.Instance == null || !GameState.Instance.ManagerPoisson())
			return;

		Effets.FlashCouleur(_sprite, new Color(0.6f, 1f, 0.6f), 0.08f, 0.25f);
	}

	// Pouvoir de Chaleur : aura courte portée qui fait fondre les murs de
	// glace fondable à proximité. Flash orange procédural, pas de nouvel
	// asset d'effet visuel.
	private void UtiliserPouvoirChaleur()
	{
		if (GameState.Instance?.PouvoirChaleurActif != true)
			return;

		var espace = GetWorld2D().DirectSpaceState;
		var forme = new CircleShape2D { Radius = 40f };
		var param = new PhysicsShapeQueryParameters2D
		{
			Shape = forme,
			Transform = new Transform2D(0, GlobalPosition + new Vector2(_directionRegard * 24f, 0)),
			CollideWithBodies = true,
			CollideWithAreas = false,
		};

		foreach (var resultat in espace.IntersectShape(param))
		{
			if (resultat["collider"].As<GodotObject>() is MurFondable mur)
				mur.Melt();
		}

		Effets.FlashCouleur(_sprite, new Color(1f, 0.7f, 0.3f), 0.1f, 0.3f);
	}

	private void DemarrerGlissade()
	{
		_enGlissade = true;
		_slideTimer = SlideDuration;
		_slideVitesseActuelle = SlideSpeed;

		if (ObtenirDonneesSol(out var estGlace, out _, out _) && estGlace)
			_slideVitesseActuelle *= SlideSpeedBonusGlace;

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

	private void Lancer()
	{
		_enLancer = true;
		_lancerTimer = LancerDuree;
		_lancerCooldownTimer = LancerCooldown;

		if (SceneBouleDeNeige == null)
			return;

		var boule = SceneBouleDeNeige.Instantiate<Node2D>();
		GetParent().AddChild(boule);
		boule.GlobalPosition = GlobalPosition + new Vector2(_directionRegard * 18f, -4f);
		if (boule is Snowball snowball)
			snowball.Direction = _directionRegard;
	}

	private void GererGlaceFragile(bool auSol, float dt)
	{
		if (!auSol || !ObtenirDonneesSol(out _, out var estFragile, out var coordsCellule) || !estFragile)
		{
			_celluleFragileValide = false;
			return;
		}

		if (!_celluleFragileValide || coordsCellule != _celluleFragileActuelle)
		{
			_celluleFragileActuelle = coordsCellule;
			_celluleFragileValide = true;
			_timerFragile = 0f;
		}

		_timerFragile += dt;
		if (_timerFragile >= DelaiRuptureFragile)
		{
			_coucheSol.SetCell(coordsCellule, -1);
			_celluleFragileValide = false;
		}
	}

	// Bas + saut au sol : retire temporairement le layer des plateformes
	// traversables du masque de collision, le temps de tomber au travers.
	// Sans effet si le sol actuel n'est pas une plateforme traversable (le
	// layer 1 du terrain normal n'est jamais concerné).
	private bool GererTraverseePlateforme(bool auSol, float dt)
	{
		if (_traverseeTimer > 0f)
		{
			_traverseeTimer -= dt;
			if (_traverseeTimer <= 0f)
				CollisionMask |= Constantes.LayerPlateformesTraversables;
			return true;
		}

		if (auSol && Input.IsActionPressed("bas") && Input.IsActionJustPressed("jump"))
		{
			CollisionMask &= ~Constantes.LayerPlateformesTraversables;
			_traverseeTimer = DureeTraverseePlateforme;
			_bufferSautTimer = 0f;
			return true;
		}

		return false;
	}

	// Requête physique directe sous les pieds (comme UtiliserPouvoirChaleur),
	// pas une lecture des dernières collisions de MoveAndSlide : évite un
	// décalage d'une frame et fonctionne pour un objet autonome (pas une tuile).
	private float ObtenirFrictionSol(bool auSol)
	{
		if (!auSol)
			return 1f;

		var espace = GetWorld2D().DirectSpaceState;
		var param = new PhysicsPointQueryParameters2D
		{
			Position = GlobalPosition + new Vector2(0, 18f),
			CollideWithBodies = true,
			CollideWithAreas = false,
		};

		foreach (var resultat in espace.IntersectPoint(param))
		{
			if (resultat["collider"].As<GodotObject>() is PlateformeGlissante glissante)
				return glissante.FacteurFriction;
		}

		return 1f;
	}

	private bool ObtenirDonneesSol(out bool estGlace, out bool estFragile, out Vector2I coordsCellule)
	{
		estGlace = false;
		estFragile = false;
		coordsCellule = Vector2I.Zero;

		if (_coucheSol == null)
			return false;

		var positionPieds = GlobalPosition + new Vector2(0, 2f);
		coordsCellule = _coucheSol.LocalToMap(_coucheSol.ToLocal(positionPieds));
		var donnees = _coucheSol.GetCellTileData(coordsCellule);
		if (donnees == null)
			return false;

		estGlace = (bool)donnees.GetCustomData(TileSetFabrique.DonneeIsIce);
		estFragile = (bool)donnees.GetCustomData(TileSetFabrique.DonneeIsFragile);
		return true;
	}

	private void MettreAJourAnimation(bool auSol, float direction)
	{
		_sprite.FlipH = _directionRegard < 0;

		string animation;
		if (_enGlissade)
			animation = "glissade";
		else if (_enDegats)
			animation = "degats";
		else if (_enLancer)
			animation = "lancer";
		else if (!auSol)
			animation = Velocity.Y < 0f ? "saut_montee" : "saut_chute";
		else if (Mathf.Abs(direction) > 0.01f)
			animation = "course";
		else
			animation = "idle";

		if (_sprite.Animation != animation)
			_sprite.Play(animation);
	}

	private void JouerApparition()
	{
		Scale = new Vector2(0.6f, 0.6f);
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Back);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "scale", Vector2.One, 0.35f);
	}

	private void ChargerAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");

		EnregistrerAnimation(frames, "idle", ChargerFrames("res://assets/player/idle"), 6f, true);
		EnregistrerAnimation(frames, "course", ChargerFrames("res://assets/player/course"), 12f, true);
		EnregistrerAnimation(frames, "glissade", ChargerFrames("res://assets/player/glissade"), 14f, true);
		EnregistrerAnimation(frames, "lancer", ChargerFrames("res://assets/player/lancer"), 16f, false);
		EnregistrerAnimation(frames, "degats", ChargerFrames("res://assets/player/degats"), 10f, false);

		var sautFrames = ChargerFrames("res://assets/player/saut");
		int findeMontee = Mathf.Min(4, sautFrames.Length - 1);
		EnregistrerAnimation(frames, "saut_montee", sautFrames, 12f, false, 0, findeMontee);
		EnregistrerAnimation(frames, "saut_chute", sautFrames, 10f, true, findeMontee + 1, sautFrames.Length - 1);

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}

	private static Texture2D[] ChargerFrames(string dossier)
	{
		var fichiers = new List<string>();
		foreach (var fichier in DirAccess.GetFilesAt(dossier))
		{
			if (fichier.EndsWith(".png"))
				fichiers.Add(fichier);
		}
		fichiers.Sort();

		var textures = new Texture2D[fichiers.Count];
		for (int i = 0; i < fichiers.Count; i++)
			textures[i] = GD.Load<Texture2D>($"{dossier}/{fichiers[i]}");
		return textures;
	}

	private static void EnregistrerAnimation(SpriteFrames frames, string nom, Texture2D[] toutesLesFrames, float fps, bool boucle, int debut = 0, int fin = -1)
	{
		if (fin < 0)
			fin = toutesLesFrames.Length - 1;

		frames.AddAnimation(nom);
		frames.SetAnimationSpeed(nom, fps);
		frames.SetAnimationLoop(nom, boucle);

		for (int i = debut; i <= fin; i++)
			frames.AddFrame(nom, toutesLesFrames[i]);
	}
}
