using Godot;

// Le Père Noël : le patron de l'usine, en boss. Il dispose de son propre art
// (assets/pnj/boss_pere_noel) avec quatre animations : « idle », une marche, un punch au sol
// et un lancer vers le bas. Les TÉLÉGRAPHES, eux, restent procéduraux (Effets + tweens
// d'écrasement) et se lisent sur la pose de repos : les poses d'attaque ne se jouent qu'au
// déclenchement, sinon l'animation mangerait la fenêtre d'esquive.
//   Va-et-vient      — entre deux attaques il ne se fige pas : il avance sur le joueur,
//                      recule quand celui-ci le colle, et piétine d'avant en arrière à
//                      bonne distance. C'est l'état Idle lui-même qui marche (voir
//                      PatinerAutourDuJoueur), pas un état à part : le repositionnement
//                      ne doit jamais coûter un temps d'attaque ;
//   Salve de cadeaux — il plonge la main dans sa hotte (télégraphe : il se ramasse et
//                      rougit) puis largue des MiniJouetExplosif au parachute ; il reste
//                      essoufflé ensuite, fenêtre où les coups portés comptent double ;
//   Lancer de cadeau — il sort un cadeau piégé et le lance EN CLOCHE vers le joueur ; le
//                      CadeauExplosif éclate au premier contact (joueur ou sol) et souffle
//                      autour de lui. Deux cadeaux à la suite en phase 2 ;
//   Punch au sol     — il frappe le plancher : une OndeDeChoc s'étale de part et d'autre,
//                      AU RAS DU SOL, donc esquivable en sautant. N'est tiré que si le
//                      joueur est à portée — une onde qui n'atteint personne serait un
//                      tour perdu, et le boss tient une bande de distance de 120 à 220 px ;
//   Cheminée         — il s'évapore et se rematérialise de l'autre côté du joueur,
//                      intouchable le temps du passage, et ENCHAÎNE aussitôt sur une
//                      attaque. Repositionnement ponctuel, à ne pas confondre avec le
//                      va-et-vient : c'est une prise à revers, pas un trajet.
// Un seul DamageSource lui est propre : OndeDeChoc (porté par la scène d'onde). Les deux
// projectiles portent déjà le leur.
public partial class BossPereNoel : Boss, BossBorne
{
	// Jet = la frame où le cadeau quitte la main ; Punch = celle où l'onde part. Les deux
	// s'appellent ainsi plutôt que « Lancer » parce que Lancer(Pattern) est déjà la méthode
	// de dispatch des patterns.
	private enum Etat { Intro, Idle, ArmementCadeaux, Largage, ArmementLancer, Jet, ArmementPunch, Punch, Disparition, Reapparition, TransitionPhase, Vaincu }
	private enum Pattern { SalveCadeaux, LancerCadeau, PunchSol, Cheminee }

	// ---- Va-et-vient ----
	// Le boss tient une BANDE de distance [DistanceConfort, DistanceEngagement] plutôt qu'un
	// seuil unique : au-delà il avance (ses deux attaques — largage au-dessus de lui, éclat à
	// l'horizontale — ne couvrent pas toute l'arène), en deçà il recule pour se redonner de
	// l'air, et dans la bande il piétine d'avant en arrière. Il n'est donc jamais planté.
	[Export] public float VitesseMarche = 95f;
	[Export] public float VitesseRecul = 70f;
	[Export] public float DistanceConfort = 120f;
	[Export] public float DistanceEngagement = 220f;
	// Période du piétinement dans la bande : au-delà, le pas s'inverse.
	[Export] public float DureeOscillation = 0.4f;

	// ---- Salve de cadeaux ----
	// Fenêtre d'esquive : le boss fouille sa hotte tout ce temps avant de larguer.
	[Export] public float DelaiArmementCadeaux = 0.5f;
	[Export] public float DureeLargage = 0.25f;
	// Récompense de l'esquive : le Père Noël souffle après sa salve, coups doublés.
	[Export] public float DureeEssouffle = 0.55f;
	[Export] public int MultiplicateurVulnerable = 2;
	// Même règle que le Lutin Mecha : c'est le même jouet, un seul en phase 1.
	[Export] public int CadeauxPhase1 = 1;
	[Export] public int CadeauxPhase2 = 3;
	[Export] public float HauteurLargage = 110f;     // au-dessus du boss : les cadeaux descendent
	[Export] public float EcartLargage = 44f;
	[Export] public PackedScene SceneCadeau;

	// ---- Lancer de cadeau explosif ----
	// Fenêtre d'esquive : il arme son bras avant que le cadeau parte.
	[Export] public float DelaiArmementLancer = 0.45f;
	// Couvre l'animation « lancer_bas » (5 frames à 12 fps ≈ 0,42 s).
	[Export] public float DureeJet = 0.42f;
	[Export] public float VitesseCadeau = 260f;
	// Composante verticale initiale : le cadeau monte d'abord, d'où la cloche. Sans elle
	// le tir serait tendu et n'aurait plus rien d'un lancer à la main.
	[Export] public float ArcCadeau = 180f;
	[Export] public PackedScene SceneCadeauExplosif;

	// ---- Punch au sol ----
	// Fenêtre d'esquive : il lève le poing avant de frapper — c'est là qu'on saute.
	[Export] public float DelaiArmementPunch = 0.5f;
	// Couvre l'animation « punch_sol » (3 frames à 10 fps = 0,3 s).
	[Export] public float DureePunch = 0.3f;
	// Distance atteinte de chaque côté du boss. Réglée au-dessus de DistanceConfort pour
	// que l'attaque menace vraiment dans la bande où il se tient.
	[Export] public float PorteeOnde = 160f;
	[Export] public float DureeOnde = 0.3f;
	[Export] public PackedScene ScenePunchOnde;

	// ---- Cheminée (téléportation) ----
	[Export] public float DureeDisparition = 0.25f;
	[Export] public float DureeReapparition = 0.25f;
	// Distance à laquelle il se repose, de l'autre côté du joueur. Volontairement DANS la
	// bande de tir : sinon il ressortirait hors de portée et perdrait son tour à revenir.
	[Export] public float DistanceReapparition = 150f;
	// Garde-fou : jamais rematérialisé collé à un mur de l'arène.
	[Export] public float MargeBords = 60f;

	// ---- Phases ----
	// SeuilPhase2 et Phase viennent de Boss : la bascule est mutualisée (BasculeEnPhase2).
	[Export] public float DureeTransitionPhase = 0.8f;

	// Bornes de l'arène (posées par ZoneBossPereNoel depuis son rectangle).
	[Export] public float LimiteGauche { get; set; } = 80f;
	[Export] public float LimiteDroite { get; set; } = 2800f;

	private Etat _etat = Etat.Intro;
	private float _timerEtat = 1.4f;
	private int _direction = 1;
	private bool _vulnerable;
	// Sens du piétinement (+1 = il avance sur le joueur, -1 = il recule) et son chrono.
	private int _sensVaEtVient = 1;
	private float _timerOscillation;
	// Dernier pattern joué : sert uniquement à interdire deux Cheminée d'affilée.
	private Pattern _dernierPattern = Pattern.LancerCadeau;
	private readonly RandomNumberGenerator _rng = new();

	protected override void Initialiser()
	{
		_rng.Randomize();
		Sprite.Play("idle");
	}

	// Le dossier de la marche s'appelle « walk » côté assets (nommage d'origine, laissé tel
	// quel pour ne pas casser les .import qui pointent le fichier source) ; c'est le nom
	// d'ANIMATION qui suit la convention française du projet.
	// punch_sol et lancer_bas ne bouclent pas et se terminent sur une copie de la frame
	// idle : le retour au repos est déjà dans les assets, rien à enchaîner à la main.
	// L'onde du punch n'est PAS listée ici — son cadre fait 204×74, incompatible avec
	// l'ancrage du boss (AnimatedSprite2D à -63 sur un cadre 128) : c'est une scène à part.
	protected override SpriteFrames ConstruireAnimations()
	{
		const string racine = "res://assets/pnj/boss_pere_noel";
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", $"{racine}/idle", 5f, true);
		AjouterAnimation(frames, "marche", $"{racine}/walk", 8f, true);
		AjouterAnimation(frames, "punch_sol", $"{racine}/punch_sol", 10f, false);
		AjouterAnimation(frames, "lancer_bas", $"{racine}/lancer_bas", 12f, false);
		return frames;
	}

	// Pas d'animation « vaincu » générée : la base joue AnimationMort à la mort, on la
	// renvoie sur idle et c'est l'affaissement procédural de Mourir qui fait la chute.
	protected override string AnimationMort => "idle";

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		_timerEtat -= dt;

		var velocite = Velocity;
		AppliquerGravite(ref velocite, dt);
		// L'idle est le SEUL état qui pousse le boss horizontalement : c'est là que vit le
		// va-et-vient. Tous les autres restent strictement plantés — un télégraphe qui
		// glisse ne se lit plus, et une pose de combat mobile brouillerait l'esquive.
		if (_etat != Etat.Idle)
			AppliquerFriction(ref velocite, dt);

		switch (_etat)
		{
			case Etat.Intro:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			// Idle « actif » : il tient sa distance en marchant tant que le chrono tourne.
			case Etat.Idle:
				PatinerAutourDuJoueur(ref velocite, dt);
				if (_timerEtat <= 0f)
					ChoisirPattern();
				break;

			// Télégraphe de la salve : il fouille sa hotte, immobile — fenêtre d'esquive.
			case Etat.ArmementCadeaux:
				if (_timerEtat <= 0f)
					LargerCadeaux();
				break;

			case Etat.Largage:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			// Télégraphe du lancer : il arme son bras, immobile.
			case Etat.ArmementLancer:
				if (_timerEtat <= 0f)
					LancerCadeauExplosif();
				break;

			case Etat.Jet:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			// Télégraphe du punch : il lève le poing — c'est ici qu'on prépare son saut.
			case Etat.ArmementPunch:
				if (_timerEtat <= 0f)
					FrapperLeSol();
				break;

			case Etat.Punch:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.Disparition:
				if (_timerEtat <= 0f)
					SeRematerialiser();
				break;

			// Sortie de cheminée : il frappe TOUT DE SUITE, sans repasser par l'idle —
			// c'est ce qui fait de la téléportation une prise à revers et non un répit.
			case Etat.Reapparition:
				if (_timerEtat <= 0f)
					AttaquerImmediatement();
				break;

			case Etat.TransitionPhase:
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

	// Coup double pendant la fenêtre d'essoufflement qui suit la salve de cadeaux.
	protected override int AjusterDegats(int brut) => _vulnerable ? brut * MultiplicateurVulnerable : brut;

	// Intouchable pendant le passage par la cheminée : sans ça, une boule de neige
	// tirée au hasard le cueillerait alors qu'il n'est plus vraiment là.
	public override bool IsInvincibleToDamage(DamageSource source)
		=> base.IsInvincibleToDamage(source) || _etat is Etat.Disparition or Etat.Reapparition;

	// Bascule en phase 2 à mi-vie : il perd son flegme et enchaîne bien plus vite.
	protected override void ApresDegats(int degats)
	{
		if (BasculeEnPhase2())
			DeclencherTransitionPhase2();
		else
			// Pas d'animation « touché » dédiée (économie assumée, comme les autres
			// boss) : le rouge du costume encaisse par un flash clair.
			Effets.FlashCouleur(Sprite, new Color(1.6f, 1.3f, 1.3f), 0.05f, 0.15f);
	}

	protected override void Mourir()
	{
		_etat = Etat.Vaincu;
		base.Mourir();   // joue AnimationMort, coupe la physique et la collision

		// Faute d'animation de chute, il s'affaisse sur place. Effets.FondreVersLeBas
		// ferait exactement cet écrasement mais libérerait le nœud dès la fin de
		// l'écrasement, sans laisser le temps de voir la chute : on garde ce tween local
		// pour l'affaissement, et c'est DelaiEffacement (réglé sur la scène du boss) qui
		// efface ensuite le corps — sinon il resterait à plat par terre indéfiniment.
		// Descendre l'origine du même facteur que l'échelle garde les pieds au sol, le
		// sprite étant posé à moins sa demi-hauteur.
		const float facteur = 0.25f;
		var tween = Sprite.CreateTween();
		tween.TweenProperty(Sprite, "scale:y", facteur, 0.6f).SetTrans(Tween.TransitionType.Sine);
		tween.Parallel().TweenProperty(Sprite, "position:y", Sprite.Position.Y * facteur, 0.6f);
		tween.Parallel().TweenProperty(Sprite, "modulate:a", 0.55f, 0.6f);
	}

	// ---- États ----

	private void PasserEnIdle()
	{
		_vulnerable = false;
		_etat = Etat.Idle;
		// Phase 2 : temps de respiration réduit, donc patterns plus rapprochés.
		_timerEtat = Phase == 1 ? _rng.RandfRange(0.45f, 0.8f) : _rng.RandfRange(0.2f, 0.45f);
		// L'anim n'est pas fixée ici : PatinerAutourDuJoueur la choisit chaque frame selon
		// qu'il marche ou qu'il soit bloqué contre une borne.
	}

	// Le va-et-vient, joué pendant toute la respiration entre deux attaques. Il vise une
	// BANDE de distance : trop loin il avance, trop près il recule, et entre les deux il
	// piétine d'avant en arrière — de quoi paraître vivant sans jamais fuir le combat.
	// Il reste tourné vers le joueur en permanence, y compris en marche arrière.
	private void PatinerAutourDuJoueur(ref Vector2 velocite, float dt)
	{
		ViserLeJoueur();

		var joueur = JoueurLePlusProche(out float distance);
		if (joueur == null)
			return;

		float vitesse;
		if (distance > DistanceEngagement)
		{
			// Assez loin pour qu'aucune attaque ne porte : il fond sur le joueur.
			_sensVaEtVient = 1;
			vitesse = VitesseMarche;
		}
		else if (distance < DistanceConfort)
		{
			// Collé : il se redonne de l'air plutôt que de tirer à bout portant.
			_sensVaEtVient = -1;
			vitesse = VitesseRecul;
		}
		else
		{
			// Dans la bande : simple balancement, le pas s'inverse à chaque période.
			_timerOscillation -= dt;
			if (_timerOscillation <= 0f)
			{
				_sensVaEtVient = -_sensVaEtVient;
				_timerOscillation = DureeOscillation;
			}
			vitesse = VitesseRecul;
		}

		// _direction pointe le joueur : un sens à -1 recule donc sans changer le FlipH.
		int pas = _sensVaEtVient * _direction;

		// Contre une borne de l'arène, le pas est simplement annulé : inutile de pousser
		// le mur, et l'anim repasse en idle pour ne pas patiner sur place.
		if ((pas < 0 && GlobalPosition.X <= LimiteGauche + MargeBords)
			|| (pas > 0 && GlobalPosition.X >= LimiteDroite - MargeBords))
		{
			_sensVaEtVient = -_sensVaEtVient;
			Sprite.Play("idle");
			return;
		}

		velocite.X = pas * vitesse;
		Sprite.Play("marche");
	}

	// Choisit la prochaine action, toujours tourné vers le joueur. Plus de détour par une
	// marche d'approche : le repositionnement se fait en continu pendant l'idle, donc CHAQUE
	// fin d'idle débouche sur une attaque, quelle que soit la distance.
	private void ChoisirPattern()
	{
		ViserLeJoueur();

		var pattern = _rng.Randf() switch
		{
			< 0.30f => Pattern.SalveCadeaux,
			< 0.60f => Pattern.LancerCadeau,
			< 0.85f => Pattern.PunchSol,
			_ => Pattern.Cheminee,
		};

		// Deux cheminées d'affilée le feraient clignoter d'un bout à l'autre de l'arène
		// sans jamais frapper : le second passage est converti en lancer.
		if (pattern == Pattern.Cheminee && _dernierPattern == Pattern.Cheminee)
			pattern = Pattern.LancerCadeau;

		// Le punch ne porte qu'au sol et autour de lui : hors de portée il ne serait qu'un
		// temps mort. On lui substitue le lancer, la seule attaque à allonge franche.
		if (pattern == Pattern.PunchSol && !JoueurAPorteeDuPunch())
			pattern = Pattern.LancerCadeau;

		Lancer(pattern);
	}

	// Le joueur est-il dans le rayon que l'onde balaiera ? Testé sur le seul axe X : l'onde
	// court au sol, sa hauteur ne rentre pas en compte (c'est le saut qui l'esquive).
	private bool JoueurAPorteeDuPunch()
	{
		var joueur = JoueurLePlusProche(out _);
		return joueur != null && Mathf.Abs(joueur.GlobalPosition.X - GlobalPosition.X) <= PorteeOnde;
	}

	// Sortie de cheminée : il attaque sans respirer. La cheminée est exclue du tirage —
	// elle vient justement de se jouer, et réapparaître deux fois serait illisible. Le punch
	// l'est aussi : il vient de se reposer À DistanceReapparition, donc hors de son rayon.
	private void AttaquerImmediatement()
	{
		ViserLeJoueur();
		Lancer(_rng.Randf() < 0.5f ? Pattern.SalveCadeaux : Pattern.LancerCadeau);
	}

	private void Lancer(Pattern pattern)
	{
		_dernierPattern = pattern;
		// Les TÉLÉGRAPHES se lisent tous sur la pose de repos : on repasse sur l'idle, sinon
		// la marche continuerait de défiler sur place. Les poses d'attaque (punch_sol,
		// lancer_bas) ne se jouent qu'au déclenchement, dans FrapperLeSol / LancerCadeauExplosif.
		Sprite.Play("idle");
		switch (pattern)
		{
			case Pattern.SalveCadeaux: ArmerCadeaux(); break;
			case Pattern.LancerCadeau: ArmerLancer(); break;
			case Pattern.PunchSol: ArmerPunch(); break;
			case Pattern.Cheminee: DisparaitreParLaCheminee(); break;
		}
	}

	// Télégraphe de la salve : il se ramasse sur sa hotte et rougit, assez longtemps
	// pour être lu. Le « se ramasse » est un écrasement du sprite, pas une frame.
	private void ArmerCadeaux()
	{
		_etat = Etat.ArmementCadeaux;
		_timerEtat = DelaiArmementCadeaux;
		Effets.FlashCouleur(Sprite, new Color(1.7f, 0.9f, 0.9f), DelaiArmementCadeaux * 0.6f, DelaiArmementCadeaux * 0.4f);
		AnimerEcrasement(0.86f, DelaiArmementCadeaux);
	}

	private void LargerCadeaux()
	{
		_etat = Etat.Largage;
		_timerEtat = DureeLargage + DureeEssouffle;
		// L'essoufflement commence dès le largage : la fenêtre de riposte est la
		// contrepartie immédiate d'une salve esquivée.
		_vulnerable = true;

		if (SceneCadeau == null)
			return;

		int nombre = Phase == 1 ? CadeauxPhase1 : CadeauxPhase2;
		for (int i = 0; i < nombre; i++)
		{
			// Étale les cadeaux de part et d'autre pour qu'ils ne se superposent pas.
			float decalage = (i - (nombre - 1) / 2f) * EcartLargage;
			var cadeau = SceneCadeau.Instantiate<Node2D>();
			cadeau.GlobalPosition = GlobalPosition + new Vector2(decalage, -HauteurLargage);
			GetParent().AddChild(cadeau);
		}
	}

	// Télégraphe du lancer : le cadeau chauffe dans sa main (teinte orangée) avant de partir.
	private void ArmerLancer()
	{
		_etat = Etat.ArmementLancer;
		_timerEtat = DelaiArmementLancer;
		Effets.FlashCouleur(Sprite, new Color(1.7f, 1.2f, 0.7f), DelaiArmementLancer * 0.6f, DelaiArmementLancer * 0.4f);
	}

	// Un cadeau en phase 1, deux à la suite en phase 2 (même règle que la salve). Le tir
	// n'utilise PAS TirerSalveVisee de Boss : celui-ci vise à plat, alors qu'un cadeau se
	// lance en cloche — d'où le vecteur vitesse complet passé à Initialiser.
	private void LancerCadeauExplosif()
	{
		_etat = Etat.Jet;
		_timerEtat = DureeJet;
		Sprite.Play("lancer_bas");

		JeterUnCadeau();

		if (Phase < 2)
			return;

		GetTree().CreateTimer(DelaiSecondTir).Timeout += () =>
		{
			// Le boss a pu tomber — ou être libéré — pendant le délai (même garde que
			// TirerSalveVisee dans Boss).
			if (IsInstanceValid(this) && !EstVaincu)
				JeterUnCadeau();
		};
	}

	private void JeterUnCadeau()
	{
		if (SceneCadeauExplosif == null)
			return;

		var cadeau = SceneCadeauExplosif.Instantiate<Projectile>();
		cadeau.Initialiser(this, new Vector2(_direction * VitesseCadeau, -ArcCadeau));
		cadeau.GlobalPosition = PointDeTir(_direction);
		GetParent().AddChild(cadeau);
	}

	// Télégraphe du punch : il se ramasse et rougit — même vocabulaire que la salve, mais
	// l'écrasement est plus marqué : c'est un coup porté vers le bas.
	private void ArmerPunch()
	{
		_etat = Etat.ArmementPunch;
		_timerEtat = DelaiArmementPunch;
		Effets.FlashCouleur(Sprite, new Color(1.7f, 0.8f, 0.8f), DelaiArmementPunch * 0.6f, DelaiArmementPunch * 0.4f);
		AnimerEcrasement(0.78f, DelaiArmementPunch);
	}

	// Le poing touche le plancher : l'onde part du SOL sous le boss, et vit dans le parent
	// pour ne pas suivre ses déplacements ni mourir avec lui.
	private void FrapperLeSol()
	{
		_etat = Etat.Punch;
		_timerEtat = DureePunch;
		Sprite.Play("punch_sol");

		if (ScenePunchOnde == null)
			return;

		var onde = ScenePunchOnde.Instantiate<OndeDeChoc>();
		onde.Portee = PorteeOnde;
		onde.Duree = DureeOnde;
		onde.GlobalPosition = GlobalPosition;
		GetParent().AddChild(onde);
	}

	// Départ par la cheminée : il s'efface. La position ne change qu'une fois invisible
	// (SeRematerialiser), pour que le saut ne soit jamais vu.
	private void DisparaitreParLaCheminee()
	{
		_etat = Etat.Disparition;
		_timerEtat = DureeDisparition;
		Effets.Fondu(Sprite, 0f, DureeDisparition);
	}

	// Retour : il se repose de l'autre côté du joueur, dans les bornes de l'arène.
	private void SeRematerialiser()
	{
		_etat = Etat.Reapparition;
		_timerEtat = DureeReapparition;

		var joueur = JoueurLePlusProche(out float _);
		if (joueur != null)
		{
			float minimum = LimiteGauche + MargeBords;
			float maximum = LimiteDroite - MargeBords;

			// De l'autre côté : s'il était à gauche du joueur, il ressort à sa droite.
			float cote = joueur.GlobalPosition.X >= GlobalPosition.X ? 1f : -1f;
			float cible = joueur.GlobalPosition.X + cote * DistanceReapparition;

			// L'arène est étroite : si ce côté-là sort des bornes, on prend l'autre plutôt
			// que de le laisser clamper contre le mur, collé au joueur.
			if (cible < minimum || cible > maximum)
			{
				float miroir = joueur.GlobalPosition.X - cote * DistanceReapparition;
				if (miroir >= minimum && miroir <= maximum)
					cible = miroir;
			}

			GlobalPosition = new Vector2(Mathf.Clamp(cible, minimum, maximum), GlobalPosition.Y);
		}

		Velocity = Vector2.Zero;
		ViserLeJoueur();
		Effets.Fondu(Sprite, 1f, DureeReapparition);
	}

	// La phase est déjà passée à 2 par BasculeEnPhase2 : il ne reste que la mise en scène.
	private void DeclencherTransitionPhase2()
	{
		_etat = Etat.TransitionPhase;
		_timerEtat = DureeTransitionPhase;
		Velocity = Vector2.Zero;
		Sprite.Play("idle");   // le rugissement se joue sur place, pas en marchant
		Effets.FlashCouleur(Sprite, new Color(1.8f, 1.1f, 1.1f), 0.15f, 0.35f);
		AnimerEcrasement(1.15f, DureeTransitionPhase);
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

	// Écrasement/étirement vertical bref, retour à l'échelle normale : le seul moyen de
	// donner du corps à une pose quand il n'existe qu'une animation. Reste local au
	// boss (Effets.Flottaison et Balancement bouclent, eux, et ne conviennent pas ici).
	private void AnimerEcrasement(float facteur, float duree)
	{
		var tween = Sprite.CreateTween();
		tween.TweenProperty(Sprite, "scale:y", facteur, duree * 0.5f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(Sprite, "scale:y", 1f, duree * 0.5f).SetTrans(Tween.TransitionType.Sine);
	}
}
