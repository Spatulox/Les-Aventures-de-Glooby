using Godot;
using System.Collections.Generic;

// Le héros jouable (LivingEntity) : contrôleur à minuteurs — coyote time + tampon de
// saut, glissade accélérée (plus vive sur la glace), lancer de boule de neige, courte
// invincibilité post-coup, rupture de tuile fragile, filet anti-chute relatif à la zone.
// PV/gravité/friction/saut/dégâts viennent de LivingEntity ; ses PV vivent dans GameState.
public partial class Player : LivingEntity
{
	[Export] public float Speed = 220f;
	[Export] public float Acceleration = 1600f;
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
	[Export] public PackedScene ScenePlateformeGlace;
	// Cadence de pose des plateformes de glace tant que la touche est tenue :
	// espace les plateformes dans le temps (elles se juxtaposent en pont pendant
	// que le joueur avance, au lieu de s'empiler sur place).
	[Export] public float IntervallePoseGlace = 0.22f;
	[Export] public float OffsetPoseGlaceX = 40f;
	[Export] public float OffsetPoseGlaceY = 40f;

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _colDebout;
	private CollisionShape2D _colGlisse;
	private TileMapLayer _coucheSol;
	private Camera2D _camera;
	private IZoneCamera _zoneCameraActive;

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

	private float _glaceSpawnTimer;

	public override void _Ready()
	{
		AddToGroup("joueur");

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_colDebout = GetNode<CollisionShape2D>("CollisionDebout");
		_colGlisse = GetNode<CollisionShape2D>("CollisionGlisse");
		_colGlisse.Disabled = true;
		_camera = GetNode<Camera2D>("Camera2D");

		ChargerAnimations();
		_coucheSol = GetTree().GetFirstNodeInGroup("sol") as TileMapLayer;
		GameState.Instance.JoueurMort += OnJoueurMort;

		JouerApparition();

		// Partie chargée depuis le menu (le monde ne se recharge pas) : replace le
		// joueur à son checkpoint. Nouvelle partie => position zéro, on garde le spawn.
		if (GameState.Instance.CheckpointIdActif != "" && GameState.Instance.CheckpointPosition != Vector2.Zero)
			TeleporterAuCheckpoint();
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		var velocity = Velocity;

		var auSol = IsOnFloor();
		_coyoteTimer = auSol ? CoyoteTime : Mathf.Max(0f, _coyoteTimer - dt);

		// À portée d'un PNJ parlant, Espace (partagé avec "action") sert à parler :
		// on n'arme pas le saut pour éviter un saut parasite pendant le dialogue.
		if (!GameState.Instance.DialogueDisponible && Input.IsActionJustPressed("jump"))
			_bufferSautTimer = JumpBufferTime;
		else
			_bufferSautTimer = Mathf.Max(0f, _bufferSautTimer - dt);

		if (_slideCooldownTimer > 0f)
			_slideCooldownTimer -= dt;
		if (_lancerCooldownTimer > 0f)
			_lancerCooldownTimer -= dt;
		if (_glaceSpawnTimer > 0f)
			_glaceSpawnTimer -= dt;

		AppliquerGravite(ref velocity, dt);

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
				AppliquerFriction(ref velocity, dt, ObtenirFrictionSol(auSol));

			if (_bufferSautTimer > 0f && _coyoteTimer > 0f && !vientDeTraverser)
			{
				Sauter(ref velocity);
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

			if (Input.IsActionPressed("pouvoir_glace"))
				UtiliserPouvoirGlace();
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

		MettreAJourZoneCamera();

		if (GlobalPosition.Y > SeuilChuteVide)
		{
			TomberDansLeVide();
			return;
		}

		MettreAJourAnimation(auSol, direction);
	}

	// Appelée par la CameraZone active : applique les limites caméra de la salle
	// courante et recale le filet anti-chute juste sous son bas (façon Hollow Knight).
	public void DefinirZoneCamera(int gauche, int droite, int haut, int bas, float margeChute)
	{
		_camera.LimitLeft = gauche;
		_camera.LimitRight = droite;
		_camera.LimitTop = haut;
		_camera.LimitBottom = bas;
		SeuilChuteVide = bas + margeChute;
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
		MettreAJourZoneCamera();
		JouerApparition();
	}

	// Détection continue de la salle : chaque frame, on applique la zone caméra
	// (limites caméra + fond de région) qui contient le joueur. Toute IZoneCamera
	// compte - CameraZone (salle normale) comme ZoneBoss (arène). Remplace le
	// déclenchement par BodyEntered, qui ratait les téléportations (respawn) et
	// imposait des RegionTrigger séparés pour le fond.
	//
	// Hystérésis : tant que la zone courante contient encore le joueur, on ne
	// touche à rien (requête de groupe évitée). Sinon on cherche une nouvelle
	// zone ; si aucune ne le contient (petit trou entre zones, saut au-dessus,
	// chute), on GARDE la zone courante - on ne réassigne qu'en cas de match.
	private void MettreAJourZoneCamera()
	{
		if (_zoneCameraActive is Node courant && IsInstanceValid(courant)
			&& _zoneCameraActive.Contient(GlobalPosition))
			return;

		foreach (var noeud in GetTree().GetNodesInGroup(CameraZone.Groupe))
		{
			if (noeud is IZoneCamera zone && zone.Contient(GlobalPosition))
			{
				_zoneCameraActive = zone;
				zone.Appliquer(this);
				return;
			}
		}
	}

	public bool EstInvincible => _invincibiliteTimer > 0f;
	public bool EstEnGlissade => _enGlissade;

	// LivingEntity : les PV du joueur vivent dans GameState (persistants, HUD, respawn),
	// donc tout coup y est routé. Pendant l'invincibilité post-coup, le joueur ignore
	// toute source de dégâts.
	public override bool IsInvincibleToDamage(DamageSource source) => EstInvincible;

	// Damageable : coup non directionnel (recul neutre).
	public override void TakeDamage(DamageSource source) => Blesser(0, source);

	// Encaisse un coup d'une source avec recul directionnel (boss, pièges comme les
	// stalactites...). Le montant vient de la source : toute forme de dégât = DamageSource.
	// Les PV du joueur vivent dans GameState (persistants, HUD, respawn).
	public void Blesser(int direction, DamageSource source)
	{
		if (EstInvincible)
			return;

		GameState.Instance?.Degats(source.MontantDegats());
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

	// Pouvoir de Glace : touche maintenue, pose une plateforme de glace éphémère
	// devant le joueur à cadence régulée (IntervallePoseGlace). En avançant, les
	// plateformes se juxtaposent en pont pour combler un trou. Chaque pose
	// consomme du mana ; à sec, plus rien ne se pose (voir GameState).
	private void UtiliserPouvoirGlace()
	{
		if (_glaceSpawnTimer > 0f || ScenePlateformeGlace == null)
			return;
		if (GameState.Instance?.PeutUtiliserPouvoirGlace(GameState.Instance.CoutPlateformeGlace) != true)
			return;

		_glaceSpawnTimer = IntervallePoseGlace;

		var plateforme = ScenePlateformeGlace.Instantiate<Node2D>();
		GetParent().AddChild(plateforme);
		plateforme.GlobalPosition = GlobalPosition + new Vector2(_directionRegard * OffsetPoseGlaceX, OffsetPoseGlaceY);

		GameState.Instance.ConsommerManaGlace(GameState.Instance.CoutPlateformeGlace);
		Effets.FlashCouleur(_sprite, new Color(0.6f, 0.85f, 1f), 0.06f, 0.2f);
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
		// Init (instanciateur + direction) AVANT l'ajout à l'arbre : _Ready lit ces valeurs.
		// Le joueur s'enregistre comme instanciateur pour être immunisé contre sa propre boule.
		if (boule is Projectile projectile)
			projectile.Initialiser(this, _directionRegard);
		GetParent().AddChild(boule);
		boule.GlobalPosition = GlobalPosition + new Vector2(_directionRegard * 18f, -4f);
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

		AnimationsSprite.EnregistrerAnimation(frames, "idle", AnimationsSprite.ChargerFrames("res://assets/player/idle"), 6f, true);
		AnimationsSprite.EnregistrerAnimation(frames, "course", AnimationsSprite.ChargerFrames("res://assets/player/course"), 12f, true);
		AnimationsSprite.EnregistrerAnimation(frames, "glissade", AnimationsSprite.ChargerFrames("res://assets/player/glissade"), 14f, true);
		AnimationsSprite.EnregistrerAnimation(frames, "lancer", AnimationsSprite.ChargerFrames("res://assets/player/lancer"), 16f, false);
		AnimationsSprite.EnregistrerAnimation(frames, "degats", AnimationsSprite.ChargerFrames("res://assets/player/degats"), 10f, false);

		var sautFrames = AnimationsSprite.ChargerFrames("res://assets/player/saut");
		int findeMontee = Mathf.Min(4, sautFrames.Length - 1);
		AnimationsSprite.EnregistrerAnimation(frames, "saut_montee", sautFrames, 12f, false, 0, findeMontee);
		AnimationsSprite.EnregistrerAnimation(frames, "saut_chute", sautFrames, 10f, true, findeMontee + 1, sautFrames.Length - 1);

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}
}
