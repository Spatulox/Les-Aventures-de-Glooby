using Godot;

// Le Lutin Mecha : gros automate de bois piloté par un lutin, boss de l'usine du Père Noël
// (alternative au Boss Cerf). Sous-classe de Boss qui fournit son IA — machine à états
// complète, deux phases, trois attaques TÉLÉGRAPHIÉES par une pose d'armement distincte,
// pour que le joueur apprenne à les lire :
//   Saut écrasant  — s'accroupit (pose « saut_accroupi », la plus longue) puis bondit et
//                    retombe en propageant une onde de choc au sol. Sa trajectoire VISE le
//                    point où se tenait le joueur à l'instant où il s'est accroupi : c'est
//                    à la fois son attaque de corps à corps et sa façon de refermer l'écart
//                    (il ne se téléporte pas — ça, c'est le Père Noël) ;
//   Tir de glace   — arme son canon (« tir_armement », givre qui se charge) puis tire un
//                    EclatGlace depuis son milieu, visé sur le joueur — deux à la suite en
//                    phase 2 (salve mutualisée dans Boss avec le Père Noël) ;
//   Drop de jouets — ouvre sa trappe (« trappe ») et largue des MiniJouetExplosif ; la
//                    fermeture rejoue la même animation À L'ENVERS (aucun asset dédié).
// Le choix du pattern est PONDÉRÉ PAR LA DISTANCE, lue sur les deux zones d'engagement de
// Boss (ZoneCorpsACorps / ZoneDistance, redimensionnables par instance dans l'éditeur) :
// voir ChoisirPattern. Hors des deux, il ne tente plus rien et vient chercher le joueur.
//
// Le lutin pilote est le point faible narratif : il reste visible dans le cockpit ouvert
// sur toutes les poses, et s'extrait du tas de planches à la défaite (animation « vaincu »).
public partial class BossLutinMecha : Boss, BossBorne
{
	private enum Etat { Intro, Idle, Deplacement, SautAccroupi, SautVol, SautImpact, TirArmement, TirFeu, Trappe, TransitionPhase, Vaincu }
	private enum Pattern { SautEcrasant, TirGlace, DropJouets }

	// ---- Déplacement ----
	[Export] public float VitesseDeplacement = 70f;   // marche pataude, bien plus lente que le joueur
	[Export] public float DureeDeplacement = 1.6f;

	// ---- Saut écrasant ----
	// Fenêtre d'esquive : le boss reste accroupi tout ce temps avant de bondir.
	[Export] public float DelaiAccroupi = 0.9f;
	[Export] public float ImpulsionSaut = -560f;
	// PLAFOND de la vitesse horizontale du bond, pas sa valeur : celle-ci est recalculée à
	// chaque saut pour retomber sur le point visé (voir Bondir). Le plafond borne l'allonge —
	// au-delà, le mecha planerait au lieu de bondir, et un joueur qui s'enfuit très loin doit
	// pouvoir lui coûter deux sauts.
	[Export] public float VitesseSautMax = 280f;
	[Export] public float DureeImpact = 0.5f;
	// Portée horizontale de l'onde de choc au sol, de part et d'autre du boss.
	[Export] public float PorteeOndeChoc = 110f;
	// Récompense de l'esquive : le mecha reste planté dans le sol après son écrasement,
	// vapeur au nez — les coups portés pendant cette fenêtre comptent double.
	[Export] public int MultiplicateurVulnerable = 2;

	// ---- Tir de glace ----
	// Fenêtre d'esquive : le givre se charge visiblement pendant ce délai avant le tir.
	[Export] public float DelaiArmementTir = 0.9f;
	[Export] public float DureeTir = 0.5f;
	[Export] public float VitesseEclat = 300f;
	[Export] public PackedScene SceneEclatGlace;

	// ---- Drop de jouets ----
	[Export] public float DureeOuvertureTrappe = 0.5f;
	// Un seul jouet en phase 1 : le pattern doit rester lisible et gérable. La phase 2
	// triple la mise.
	[Export] public int JouetsPhase1 = 1;
	[Export] public int JouetsPhase2 = 3;
	[Export] public float HauteurLargage = 90f;      // au-dessus du boss : les jouets descendent en parachute
	[Export] public float EcartLargage = 40f;
	[Export] public PackedScene SceneMiniJouet;

	// ---- Phases ----
	// Le seuil et le comptage de phase viennent de Boss (schéma commun aux trois boss).
	[Export] public float DureeTransitionPhase = 0.8f;

	// Bornes de l'arène (posées par ZoneBossLutinMecha depuis son rectangle).
	[Export] public float LimiteGauche { get; set; } = 80f;
	[Export] public float LimiteDroite { get; set; } = 2800f;

	private Etat _etat = Etat.Intro;
	private float _timerEtat = 1.4f;
	private int _direction = 1;
	private Pattern _patternChoisi;
	private bool _vulnerable;

	// Point de chute visé par le bond en cours, figé À L'INSTANT où le mecha s'accroupit. C'est
	// tout l'enjeu de l'attaque : il tombe là où le joueur ÉTAIT au début du télégraphe, pas là
	// où il est — la pose accroupie devient le signal qu'il faut bouger, et bouger suffit.
	private float _cibleSautX;
	// Vitesse horizontale calculée au décollage, tenue pendant tout le vol.
	private float _vitesseSautX;

	private readonly RandomNumberGenerator _rng = new();

	protected override void Initialiser()
	{
		_rng.Randomize();
		CablerZonesEngagement();
		Sprite.Play("idle");
	}

	protected override SpriteFrames ConstruireAnimations()
	{
		const string racine = "res://assets/pnj/boss_lutin_mecha";
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");

		AjouterAnimation(frames, "idle", $"{racine}/idle", 6f, true);
		AjouterAnimation(frames, "marche", $"{racine}/marche", 9f, true);
		AjouterAnimation(frames, "saut_accroupi", $"{racine}/saut_accroupi", 8f, false);
		AjouterAnimation(frames, "saut_vol", $"{racine}/saut_vol", 8f, true);
		AjouterAnimation(frames, "saut_impact", $"{racine}/saut_impact", 10f, false);
		AjouterAnimation(frames, "tir_armement", $"{racine}/tir_armement", 6f, false);
		AjouterAnimation(frames, "tir", $"{racine}/tir", 10f, false);
		AjouterAnimation(frames, "transition", $"{racine}/transition", 8f, false);
		AjouterAnimation(frames, "vaincu", $"{racine}/vaincu", 8f, false);

		// La trappe s'ouvre puis se referme en rejouant les mêmes frames à l'envers :
		// une seule animation générée couvre les deux sens (économie assumée).
		var trappe = AnimationsSprite.ChargerFrames($"{racine}/trappe");
		AnimationsSprite.EnregistrerAnimation(frames, "trappe_ouverture", trappe, 8f, false);
		AnimationsSprite.EnregistrerAnimation(frames, "trappe_fermeture", trappe, 8f, false, inverse: true);

		return frames;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		_timerEtat -= dt;

		var velocite = Velocity;
		AppliquerGravite(ref velocite, dt);

		switch (_etat)
		{
			case Etat.Intro:
				velocite.X = 0f;
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.Idle:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					ChoisirPattern();
				break;

			// Marche : repositionnement en phase 1, rapprochement quand le joueur est hors de
			// portée. Elle s'interrompt dès qu'il est au contact, pour ne pas lui rentrer
			// dedans et pour repartir attaquer sans attendre la fin du chrono.
			case Etat.Deplacement:
				velocite.X = _direction * VitesseDeplacement;
				if (_timerEtat <= 0f || AtteintUneBorne() || EvaluerPortee() == PorteeJoueur.CorpsACorps)
					PasserEnIdle();
				break;

			// Télégraphe du saut : immobile et accroupi, c'est la fenêtre d'esquive.
			case Etat.SautAccroupi:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					Bondir(ref velocite);
				break;

			case Etat.SautVol:
				// Vitesse figée au décollage : la retoucher en vol ferait du bond un missile
				// à tête chercheuse, et l'esquive ne vaudrait plus rien.
				velocite.X = _vitesseSautX;
				// Le contact avec le sol termine le bond — pas un minuteur, pour que
				// l'impact tombe toujours pile à l'atterrissage quelle que soit la hauteur.
				if (velocite.Y >= 0f && IsOnFloor())
				{
					velocite.X = 0f;
					Ecraser();
				}
				break;

			case Etat.SautImpact:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			// Télégraphe du tir : le givre se charge, le boss ne bouge pas.
			case Etat.TirArmement:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					Tirer();
				break;

			case Etat.TirFeu:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.Trappe:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.TransitionPhase:
				AppliquerFriction(ref velocite, dt);
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.Vaincu:
				velocite.X = 0f;
				break;
		}

		Velocity = velocite;
		MoveAndSlide();
	}

	// Coup double pendant la fenêtre de vulnérabilité qui suit l'écrasement.
	protected override int AjusterDegats(int brut) => _vulnerable ? brut * MultiplicateurVulnerable : brut;

	// Bascule en phase 2 à mi-vie : le mecha se déglingue partiellement puis enchaîne plus vite.
	protected override void ApresDegats(int degats)
	{
		if (BasculeEnPhase2())
			DeclencherTransitionPhase2();
		else
			// Pas d'animation « touché » dédiée (économie assumée, comme le Boss Cerf) :
			// le bois encaisse par un flash clair, lisible sur toutes les poses.
			Effets.FlashCouleur(Sprite, new Color(1.5f, 1.35f, 1.1f), 0.05f, 0.15f);
	}

	protected override void Mourir()
	{
		_etat = Etat.Vaincu;
		base.Mourir();
	}

	// ---- États ----

	private void PasserEnIdle()
	{
		_vulnerable = false;
		_etat = Etat.Idle;
		// Phase 2 : temps de respiration réduit, donc patterns plus rapprochés.
		_timerEtat = Phase == 1 ? _rng.RandfRange(1.0f, 1.7f) : _rng.RandfRange(0.5f, 1.0f);
		Sprite.Play("idle");
	}

	// Choisit la prochaine action puis se tourne vers le joueur. Le tirage est PONDÉRÉ PAR LA
	// DISTANCE (zones d'engagement) : chaque attaque n'est vraiment bonne que dans sa tranche.
	//   collé      — le saut écrasant est SON attaque de contact, il domine ; la trappe
	//                largue juste au-dessus de lui, elle vaut aussi de près ;
	//   à distance — le canon prime, le seul à couvrir vraiment l'écart ;
	//   hors de portée — plus rien ne sert : il vient chercher le joueur en marchant, ou d'un
	//                bond, son saut faisant à la fois trajet et attaque (le mecha ne se
	//                téléporte pas, c'est la marque du Père Noël).
	private void ChoisirPattern()
	{
		ViserLeJoueur();

		var portee = EvaluerPortee();

		if (portee == PorteeJoueur.HorsPortee)
		{
			if (_rng.Randf() < 0.5f)
				DemarrerAccroupi();
			else
				DemarrerDeplacement();
			return;
		}

		// En phase 1 il prend encore le temps de se repositionner sans frapper (lisible, peu
		// agressif) ; en phase 2 chaque fin d'idle débouche sur une attaque.
		if (Phase == 1 && _rng.Randf() < 0.25f)
		{
			DemarrerDeplacement();
			return;
		}

		// Tirage PROPRE au choix d'attaque. Il partageait celui du déplacement ci-dessus,
		// ce qui faussait toute la répartition : en phase 1 le tirage retenu ne couvrait
		// plus [0,1) mais [0.35,1), et le drop de jouets — calé tout en haut de
		// l'intervalle — ne sortait qu'une fois sur sept environ au lieu de sa part.
		float tirage = _rng.Randf();
		_patternChoisi = portee == PorteeJoueur.CorpsACorps
			? tirage switch
			{
				< 0.55f => Pattern.SautEcrasant,
				< 0.85f => Pattern.DropJouets,
				_ => Pattern.TirGlace,
			}
			: tirage switch
			{
				< 0.55f => Pattern.TirGlace,
				< 0.80f => Pattern.SautEcrasant,
				_ => Pattern.DropJouets,
			};

		switch (_patternChoisi)
		{
			case Pattern.SautEcrasant: DemarrerAccroupi(); break;
			case Pattern.TirGlace: DemarrerArmementTir(); break;
			case Pattern.DropJouets: DemarrerTrappe(); break;
		}
	}

	private void DemarrerDeplacement()
	{
		_etat = Etat.Deplacement;
		_timerEtat = DureeDeplacement;
		Sprite.Play("marche");
	}

	// Télégraphe du saut : pose accroupie tenue assez longtemps pour être lue et esquivée.
	// C'est ICI que le point de chute est figé — donc au tout début du télégraphe, laissant
	// au joueur l'intégralité de DelaiAccroupi pour s'en écarter. Le viser au décollage
	// rendrait l'attaque imparable ; le viser ici en fait un piège de position.
	private void DemarrerAccroupi()
	{
		_etat = Etat.SautAccroupi;
		_timerEtat = DelaiAccroupi;
		Sprite.Play("saut_accroupi");
		ViserLeJoueur();

		var joueur = JoueurLePlusProche(out float _);
		// Sans joueur en scène il saute sur place plutôt que dans une direction arbitraire.
		_cibleSautX = joueur != null ? joueur.GlobalPosition.X : GlobalPosition.X;
		_cibleSautX = Mathf.Clamp(_cibleSautX, LimiteGauche, LimiteDroite);
	}

	// Décollage : la trajectoire est calée sur le point de chute mémorisé. Le vol dure le temps
	// que la gravité ramène le mecha à sa hauteur de départ (2·|v0|/g) ; la vitesse horizontale
	// n'est donc que l'écart à couvrir divisé par ce temps. Plafonnée à VitesseSautMax : un
	// joueur assez loin le fait retomber court, et lui coûte un second saut.
	private void Bondir(ref Vector2 velocite)
	{
		_etat = Etat.SautVol;

		float dureeVol = 2f * Mathf.Abs(ImpulsionSaut) / Gravity;
		float ecart = _cibleSautX - GlobalPosition.X;
		_vitesseSautX = Mathf.Clamp(ecart / dureeVol, -VitesseSautMax, VitesseSautMax);

		velocite.Y = ImpulsionSaut;
		velocite.X = _vitesseSautX;
		Sprite.Play("saut_vol");

		// Il regarde là où il atterrit, et non plus le joueur : celui-ci a pu le contourner
		// pendant le télégraphe, et un mecha qui bondit à reculons ne se lirait pas.
		if (!Mathf.IsZeroApprox(ecart))
		{
			_direction = ecart > 0f ? 1 : -1;
			Sprite.FlipH = _direction < 0;
		}
	}

	// Atterrissage : pose d'impact + onde de choc qui court au sol des deux côtés.
	private void Ecraser()
	{
		_etat = Etat.SautImpact;
		_timerEtat = DureeImpact;
		_vulnerable = true;
		Sprite.Play("saut_impact");
		CreerOndeDeChoc();
	}

	// Télégraphe du tir : le canon se charge (givre visible) avant le départ de l'éclat.
	private void DemarrerArmementTir()
	{
		_etat = Etat.TirArmement;
		_timerEtat = DelaiArmementTir;
		Sprite.Play("tir_armement");
	}

	// L'éclat part du MILIEU du mecha et vise le joueur : tiré à hauteur de jambes et à
	// plat, il passait sous un joueur perché et ne menaçait jamais un joueur en l'air.
	// Salve mutualisée dans Boss (partagée avec le Père Noël) : un éclat en phase 1, deux à
	// la suite en phase 2.
	private void Tirer()
	{
		_etat = Etat.TirFeu;
		_timerEtat = DureeTir;
		Sprite.Play("tir");
		TirerSalveVisee(SceneEclatGlace, _direction, VitesseEclat);
	}

	// Ouverture de la trappe, largage, puis fermeture : la fermeture rejoue l'ouverture
	// à l'envers, et le largage des jouets tombe pile quand la trappe est grande ouverte.
	private void DemarrerTrappe()
	{
		_etat = Etat.Trappe;
		_timerEtat = DureeOuvertureTrappe * 2f;
		Sprite.Play("trappe_ouverture");

		var minuteur = GetTree().CreateTimer(DureeOuvertureTrappe);
		minuteur.Timeout += () =>
		{
			if (EstVaincu)
				return;
			LargerJouets(Phase == 1 ? JouetsPhase1 : JouetsPhase2);
			Sprite.Play("trappe_fermeture");
		};
	}

	private void LargerJouets(int nombre)
	{
		if (SceneMiniJouet == null)
			return;

		for (int i = 0; i < nombre; i++)
		{
			// Étale les jouets de part et d'autre du boss pour qu'ils ne se superposent pas.
			float decalage = (i - (nombre - 1) / 2f) * EcartLargage;
			var jouet = SceneMiniJouet.Instantiate<Node2D>();
			jouet.GlobalPosition = GlobalPosition + new Vector2(decalage, -HauteurLargage);
			GetParent().AddChild(jouet);
		}
	}

	private void DeclencherTransitionPhase2()
	{
		_etat = Etat.TransitionPhase;
		_timerEtat = DureeTransitionPhase;
		Velocity = Vector2.Zero;
		Sprite.Play("transition");
		Effets.FlashCouleur(Sprite, new Color(1.6f, 1.2f, 1.2f), 0.15f, 0.35f);
	}

	// ---- Aides ----

	// Oriente le boss vers le joueur (le sprite de base regarde à droite).
	private void ViserLeJoueur()
	{
		var joueur = JoueurLePlusProche(out float _);
		if (joueur != null)
			_direction = joueur.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;
		Sprite.FlipH = _direction < 0;
	}

	private bool AtteintUneBorne()
		=> (_direction > 0 && GlobalPosition.X >= LimiteDroite)
		|| (_direction < 0 && GlobalPosition.X <= LimiteGauche);

	// Onde de choc de l'atterrissage : une zone qui s'étale au sol de part et d'autre du
	// boss, avec un visuel de poussière. Ne touche qu'une fois, et pas le joueur en l'air.
	private void CreerOndeDeChoc()
	{
		var zone = new Area2D { CollisionLayer = 0, CollisionMask = Constantes.LayerJoueur };
		var forme = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 22) } };
		zone.AddChild(forme);

		var visuel = new ColorRect
		{
			Color = new Color(0.85f, 0.72f, 0.45f, 0.5f),
			Size = new Vector2(20, 22),
			Position = new Vector2(-10, -11),
		};
		zone.AddChild(visuel);

		AddChild(zone);
		zone.Position = new Vector2(0f, -6f);

		bool dejaTouche = false;
		zone.BodyEntered += (Node2D corps) =>
		{
			if (dejaTouche || corps is not Player joueur)
				return;
			// L'onde court AU SOL : un joueur en l'air au bon moment la saute.
			if (!joueur.IsOnFloor())
				return;
			dejaTouche = true;
			int recul = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
			joueur.Blesser(recul == 0 ? _direction : recul, DamageSource.EcrasementMecha);
		};

		float largeur = PorteeOndeChoc * 2f;
		var tween = CreateTween();
		tween.TweenProperty(forme.Shape, "size", new Vector2(largeur, 22), 0.28f);
		tween.Parallel().TweenProperty(visuel, "size", new Vector2(largeur, 22), 0.28f);
		tween.Parallel().TweenProperty(visuel, "position:x", -largeur / 2f, 0.28f);
		tween.Parallel().TweenProperty(visuel, "color:a", 0f, 0.28f);
		tween.TweenCallback(Callable.From(zone.QueueFree));
	}
}
