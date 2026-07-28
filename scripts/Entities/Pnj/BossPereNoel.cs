using Godot;

// Le Père Noël : le patron de l'usine, en boss. Il dispose de son propre art
// (assets/pnj/boss_pere_noel, distinct du Père Noël souriant du PNJ amical) avec deux
// animations générées : « idle » et une marche. Les POSES de combat, elles, n'ont pas
// d'animation dédiée — le budget PixelLab est clos (BUDGET.md), on ne régénère rien : les
// télégraphes restent donc procéduraux (Effets + tweens d'écrasement), et les attaques
// réutilisent des scènes déjà en place plutôt que d'en inventer :
//   Va-et-vient      — entre deux attaques il ne se fige pas : il avance sur le joueur,
//                      recule quand celui-ci le colle, et piétine d'avant en arrière à
//                      bonne distance. C'est l'état Idle lui-même qui marche (voir
//                      PatinerAutourDuJoueur), pas un état à part : le repositionnement
//                      ne doit jamais coûter un temps d'attaque ;
//   Salve de cadeaux — il plonge la main dans sa hotte (télégraphe : il se ramasse et
//                      rougit) puis largue des MiniJouetExplosif ; il reste essoufflé
//                      ensuite, fenêtre où les coups portés comptent double ;
//   Jet de givre     — il se givre visiblement puis tire un EclatGlace DANS L'AXE DU
//                      JOUEUR, depuis son milieu ; deux éclats à la suite en phase 2 ;
//   Cheminée         — il s'évapore et se rematérialise de l'autre côté du joueur,
//                      intouchable le temps du passage, et ENCHAÎNE aussitôt sur une
//                      attaque. Repositionnement ponctuel, à ne pas confondre avec le
//                      va-et-vient : c'est une prise à revers, pas un trajet.
// Aucun DamageSource nouveau : les deux projectiles portent déjà le leur.
public partial class BossPereNoel : Boss, BossBorne
{
	private enum Etat { Intro, Idle, ArmementCadeaux, Largage, ArmementGivre, Jet, Disparition, Reapparition, TransitionPhase, Vaincu }
	private enum Pattern { SalveCadeaux, JetGivre, Cheminee }

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

	// ---- Jet de givre ----
	// Fenêtre d'esquive : le givre se voit monter sur lui avant le départ de l'éclat.
	[Export] public float DelaiArmementGivre = 0.45f;
	[Export] public float DureeJet = 0.25f;
	[Export] public float VitesseEclat = 300f;

	[Export] public PackedScene SceneEclatGlace;

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
	private Pattern _dernierPattern = Pattern.JetGivre;
	private readonly RandomNumberGenerator _rng = new();

	protected override void Initialiser()
	{
		_rng.Randomize();
		Sprite.Play("idle");
	}

	// Deux animations générées : l'idle et la marche. Le dossier de la marche s'appelle
	// « walk » côté assets (nommage d'origine, laissé tel quel pour ne pas casser les
	// .import qui pointent le fichier source) ; c'est le nom d'ANIMATION qui suit la
	// convention française du projet. Toutes les poses de combat sont jouées sur l'idle.
	protected override SpriteFrames ConstruireAnimations()
	{
		const string racine = "res://assets/pnj/boss_pere_noel";
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", $"{racine}/idle", 5f, true);
		AjouterAnimation(frames, "marche", $"{racine}/walk", 8f, true);
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

			// Télégraphe du jet : le givre monte sur lui, il ne bouge pas.
			case Etat.ArmementGivre:
				if (_timerEtat <= 0f)
					Tirer();
				break;

			case Etat.Jet:
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
			< 0.45f => Pattern.SalveCadeaux,
			< 0.80f => Pattern.JetGivre,
			_ => Pattern.Cheminee,
		};

		// Deux cheminées d'affilée le feraient clignoter d'un bout à l'autre de l'arène
		// sans jamais frapper : le second passage est converti en jet de givre.
		if (pattern == Pattern.Cheminee && _dernierPattern == Pattern.Cheminee)
			pattern = Pattern.JetGivre;

		Lancer(pattern);
	}

	// Sortie de cheminée : il attaque sans respirer. La cheminée est exclue du tirage —
	// elle vient justement de se jouer, et réapparaître deux fois serait illisible.
	private void AttaquerImmediatement()
	{
		ViserLeJoueur();
		Lancer(_rng.Randf() < 0.5f ? Pattern.SalveCadeaux : Pattern.JetGivre);
	}

	private void Lancer(Pattern pattern)
	{
		_dernierPattern = pattern;
		// Toutes les poses de combat sont immobiles : on repasse sur l'idle, sinon la marche
		// continuerait de défiler sur place pendant le télégraphe.
		Sprite.Play("idle");
		switch (pattern)
		{
			case Pattern.SalveCadeaux: ArmerCadeaux(); break;
			case Pattern.JetGivre: ArmerGivre(); break;
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

	// Télégraphe du jet : le givre monte sur lui (teinte bleue) avant le tir.
	private void ArmerGivre()
	{
		_etat = Etat.ArmementGivre;
		_timerEtat = DelaiArmementGivre;
		Effets.FlashCouleur(Sprite, new Color(0.75f, 0.95f, 1.7f), DelaiArmementGivre * 0.6f, DelaiArmementGivre * 0.4f);
	}

	// Un éclat en phase 1, deux à la suite en phase 2. Le tir lui-même (point de départ au
	// milieu du boss, visée sur le joueur, salve de phase 2) vit dans Boss : le Lutin Mecha
	// tire exactement la même chose.
	private void Tirer()
	{
		_etat = Etat.Jet;
		_timerEtat = DureeJet;
		TirerSalveVisee(SceneEclatGlace, _direction, VitesseEclat);
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
