using Godot;

// Le Père Noël : le patron de l'usine, en boss. Là où le Lutin Mecha dispose d'un jeu
// d'animations complet, le Père Noël n'a QU'UNE animation générée (« idle », le dossier
// du PNJ amical) — le budget PixelLab est clos (BUDGET.md), on ne régénère rien. Tout son
// langage corporel est donc procédural (Effets + tweens), et ses attaques réutilisent des
// scènes déjà en place plutôt que d'en inventer :
//   Salve de cadeaux — il plonge la main dans sa hotte (télégraphe : il se ramasse et
//                      rougit) puis largue des MiniJouetExplosif ; il reste essoufflé
//                      ensuite, fenêtre où les coups portés comptent double ;
//   Jet de givre     — il se givre visiblement puis tire un EclatGlace (éventail de
//                      trois en phase 2) ;
//   Cheminée         — il s'évapore et se rematérialise ailleurs, de l'autre côté du
//                      joueur. C'est son SEUL déplacement, faute d'animation de marche,
//                      et il est intouchable le temps du passage.
// Aucun DamageSource nouveau : les deux projectiles portent déjà le leur.
public partial class BossPereNoel : Boss, BossBorne
{
	private enum Etat { Intro, Idle, ArmementCadeaux, Largage, ArmementGivre, Jet, Disparition, Reapparition, TransitionPhase, Vaincu }
	private enum Pattern { SalveCadeaux, JetGivre, Cheminee }

	// ---- Salve de cadeaux ----
	// Fenêtre d'esquive : le boss fouille sa hotte tout ce temps avant de larguer.
	[Export] public float DelaiArmementCadeaux = 0.9f;
	[Export] public float DureeLargage = 0.4f;
	// Récompense de l'esquive : le Père Noël souffle après sa salve, coups doublés.
	[Export] public float DureeEssouffle = 0.9f;
	[Export] public int MultiplicateurVulnerable = 2;
	// Même règle que le Lutin Mecha : c'est le même jouet, un seul en phase 1.
	[Export] public int CadeauxPhase1 = 1;
	[Export] public int CadeauxPhase2 = 3;
	[Export] public float HauteurLargage = 90f;      // au-dessus du boss : les cadeaux descendent
	[Export] public float EcartLargage = 44f;
	[Export] public PackedScene SceneCadeau;

	// ---- Jet de givre ----
	// Fenêtre d'esquive : le givre se voit monter sur lui avant le départ de l'éclat.
	[Export] public float DelaiArmementGivre = 0.8f;
	[Export] public float DureeJet = 0.4f;
	[Export] public float VitesseEclat = 300f;
	// Ouverture de l'éventail de phase 2, en degrés de part et d'autre de l'horizontale.
	[Export] public float AngleEventail = 18f;

	[Export] public PackedScene SceneEclatGlace;

	// ---- Cheminée (téléportation) ----
	[Export] public float DureeDisparition = 0.35f;
	[Export] public float DureeReapparition = 0.35f;
	// Distance à laquelle il se repose, de l'autre côté du joueur.
	[Export] public float DistanceReapparition = 220f;
	// Garde-fou : jamais rematérialisé collé à un mur de l'arène.
	[Export] public float MargeBords = 60f;

	// ---- Phases ----
	[Export] public float SeuilPhase2 = 0.5f;        // fraction de PV déclenchant la phase 2
	[Export] public float DureeTransitionPhase = 0.8f;

	// Bornes de l'arène (posées par ZoneBossPereNoel depuis son rectangle).
	[Export] public float LimiteGauche { get; set; } = 80f;
	[Export] public float LimiteDroite { get; set; } = 2800f;

	public int Phase { get; private set; } = 1;

	private Etat _etat = Etat.Intro;
	private float _timerEtat = 1.4f;
	private int _direction = 1;
	private bool _vulnerable;
	private readonly RandomNumberGenerator _rng = new();

	protected override void Initialiser()
	{
		_rng.Randomize();
		Sprite.Play("idle");
	}

	// Une seule animation : le dossier idle du Père Noël, partagé avec son PNJ amical
	// (PereNoel.cs). Toutes les poses de combat sont jouées dessus.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/pere_noel/idle", 5f, true);
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
		// Le Père Noël ne marche jamais : il se déplace en disparaissant. Sa vitesse
		// horizontale est donc toujours ramenée à zéro, quel que soit l'état.
		AppliquerFriction(ref velocite, dt);

		switch (_etat)
		{
			case Etat.Intro:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.Idle:
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

			case Etat.Reapparition:
				if (_timerEtat <= 0f)
					PasserEnIdle();
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
		if (Phase == 1 && Pv <= Mathf.CeilToInt(PvMax * SeuilPhase2))
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
		// ferait exactement cet écrasement mais LIBÈRE le nœud à la fin : le boss doit
		// rester dans l'arène (barre de vie liée, signal Vaincu déjà émis), d'où ce
		// tween local. Descendre l'origine du même facteur que l'échelle garde les
		// pieds au sol, le sprite étant posé à moins sa demi-hauteur.
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
		_timerEtat = Phase == 1 ? _rng.RandfRange(1.0f, 1.6f) : _rng.RandfRange(0.5f, 1.0f);
		Sprite.Play("idle");
	}

	// Choisit la prochaine action puis se tourne vers le joueur. En phase 1 il passe
	// souvent par la cheminée (lisible, peu agressif) ; en phase 2 il attaque bien plus.
	private void ChoisirPattern()
	{
		ViserLeJoueur();

		if (Phase == 1 && _rng.Randf() < 0.3f)
		{
			DisparaitreParLaCheminee();
			return;
		}

		// Tirage PROPRE au choix d'attaque : le partager avec le repli ci-dessus
		// fausserait la répartition, le tirage retenu ne couvrant plus [0,1).
		float tirage = _rng.Randf();
		var pattern = tirage switch
		{
			< 0.45f => Pattern.SalveCadeaux,
			< 0.8f => Pattern.JetGivre,
			_ => Pattern.Cheminee,
		};

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

	// Un éclat en phase 1, éventail de trois en phase 2 : même scène, trois angles.
	private void Tirer()
	{
		_etat = Etat.Jet;
		_timerEtat = DureeJet;

		if (SceneEclatGlace == null)
			return;

		if (Phase == 1)
		{
			TirerEclat(0f);
			return;
		}

		TirerEclat(-AngleEventail);
		TirerEclat(0f);
		TirerEclat(AngleEventail);
	}

	// Un éclat tiré à angleDegres au-dessus/en dessous de l'horizontale. On passe par
	// la surcharge vectorielle de Projectile.Initialiser : c'est elle qui gère un tir
	// autre qu'à plat.
	private void TirerEclat(float angleDegres)
	{
		var eclat = SceneEclatGlace.Instantiate<Projectile>();
		var velocite = new Vector2(_direction * VitesseEclat, 0f).Rotated(Mathf.DegToRad(angleDegres) * _direction);
		eclat.Initialiser(this, velocite);
		eclat.GlobalPosition = GlobalPosition + new Vector2(_direction * 30f, -50f);
		GetParent().AddChild(eclat);
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
			// De l'autre côté : s'il était à gauche du joueur, il ressort à sa droite.
			float cote = joueur.GlobalPosition.X >= GlobalPosition.X ? 1f : -1f;
			float cible = joueur.GlobalPosition.X + cote * DistanceReapparition;
			GlobalPosition = new Vector2(
				Mathf.Clamp(cible, LimiteGauche + MargeBords, LimiteDroite - MargeBords),
				GlobalPosition.Y);
		}

		Velocity = Vector2.Zero;
		ViserLeJoueur();
		Effets.Fondu(Sprite, 1f, DureeReapparition);
	}

	private void DeclencherTransitionPhase2()
	{
		Phase = 2;
		_etat = Etat.TransitionPhase;
		_timerEtat = DureeTransitionPhase;
		Velocity = Vector2.Zero;
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
