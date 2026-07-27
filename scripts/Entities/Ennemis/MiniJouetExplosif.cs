using Godot;

// Mini-jouet explosif largué par le Boss Lutin Mecha : un petit soldat de bois kamikaze.
// Trois états seulement, dans cet ordre et sans retour en arrière :
//   Chute     — il descend lentement suspendu à son parachute, en se balançant ;
//   Fonce     — parachute largué (le passage est visible : le parachute se détache et
//               dérive), il court vers le joueur, mèche qui se consume ;
//   Explosion — il éclate, blesse tout joueur dans le rayon, puis se libère.
//
// L'explosion est le SEUL dénouement : elle survient au contact du joueur, à la fin de
// la mèche, ou si le joueur le détruit avant l'impact (PvMax = 1 => Mourir() explose).
// Le boss n'a qu'à instancier la scène et l'ajouter à l'arbre : le jouet se débrouille.
public partial class MiniJouetExplosif : LivingEntity
{
	public enum Etat { Chute, Fonce, Explosion }

	// Descente sous parachute : lente et régulière (pas la gravité de LivingEntity),
	// pour laisser au joueur le temps de voir arriver le jouet.
	[Export] public float VitesseChute = 55f;
	// Amplitude du balancement du parachute pendant la descente (degrés).
	[Export] public float AngleBalancement = 12f;
	// Course kamikaze : plus lent que le joueur (220), donc distançable.
	[Export] public float VitesseFonce = 130f;
	// Délai avant auto-explosion une fois au sol, même si le joueur reste hors d'atteinte.
	[Export] public float DureeMeche = 4f;
	// Rayon de souffle en pixels : le jouet blesse plus loin que son corps.
	[Export] public float RayonSouffle = 42f;

	public Etat EtatCourant { get; private set; } = Etat.Chute;

	private AnimatedSprite2D _sprite;
	private Node2D _parachute;
	private Area2D _zoneDegats;
	private float _minuteurMeche;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_parachute = GetNodeOrNull<Node2D>("Parachute");
		_zoneDegats = GetNode<Area2D>("ZoneDegats");

		AppliquerCollisionsPnj();
		MasquerApercuEditeur();
		Pv = PvMax;
		AddToGroup("pnj");

		_sprite.SpriteFrames = ConstruireAnimations();
		_sprite.Play("chute");
		_minuteurMeche = DureeMeche;

		_zoneDegats.BodyEntered += SurContact;

		// Le parachute se balance pendant toute la descente (effet partagé, pas d'asset animé).
		if (_parachute != null)
			Effets.Balancement(_parachute, AngleBalancement, 0.7f);
	}

	// Le jouet n'hérite ni de Boss ni de PnjMechant (il ne patrouille pas et ne subit pas
	// la gravité pendant sa descente) : il construit ses animations directement avec le
	// helper partagé AnimationsSprite.
	private static SpriteFrames ConstruireAnimations()
	{
		const string racine = "res://assets/ennemis/usine/mini_jouet_explosif";
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		Ajouter(frames, "chute", $"{racine}/chute", 6f, true);
		Ajouter(frames, "fonce", $"{racine}/fonce", 12f, true);
		Ajouter(frames, "explosion", $"{racine}/explosion", 16f, false);
		return frames;
	}

	private static void Ajouter(SpriteFrames frames, string nom, string dossier, float fps, bool boucle)
		=> AnimationsSprite.EnregistrerAnimation(frames, nom, AnimationsSprite.ChargerFrames(dossier), fps, boucle);

	public override void _PhysicsProcess(double delta)
	{
		if (EtatCourant == Etat.Explosion)
			return;

		float dt = (float)delta;
		var velocite = Velocity;

		if (EtatCourant == Etat.Chute)
		{
			// Descente à vitesse constante : le parachute annule la gravité.
			velocite = new Vector2(0f, VitesseChute);
			Velocity = velocite;
			MoveAndSlide();

			if (IsOnFloor())
				LarguerParachute();
			return;
		}

		// Fonce : gravité normale + course vers le joueur, mèche qui se consume.
		AppliquerGravite(ref velocite, dt);

		var joueur = JoueurLePlusProche(out float _);
		if (joueur != null)
		{
			int direction = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
			velocite.X = direction * VitesseFonce;
			_sprite.FlipH = direction < 0;
		}
		else
		{
			AppliquerFriction(ref velocite, dt);
		}

		Velocity = velocite;
		MoveAndSlide();

		_minuteurMeche -= dt;
		if (_minuteurMeche <= 0f)
			Exploser();
	}

	// Passage Chute -> Fonce, rendu VISIBLE : le parachute est détaché du jouet et part
	// en dérive (il n'est donc pas simplement masqué), et la course démarre.
	private void LarguerParachute()
	{
		EtatCourant = Etat.Fonce;
		_sprite.Play("fonce");

		if (_parachute == null)
			return;

		// Repasse le parachute en frère du jouet pour qu'il garde sa position à l'écran
		// pendant que le jouet s'en va, puis le fait disparaître en dérivant vers le haut.
		var position = _parachute.GlobalPosition;
		_parachute.Reparent(GetParent());
		_parachute.GlobalPosition = position;
		var tween = _parachute.CreateTween();
		tween.TweenProperty(_parachute, "global_position", position + new Vector2(14f, -26f), 1.1f);
		Effets.Disparaitre(_parachute, _parachute.Scale * 0.8f, 1.1f);
		_parachute = null;
	}

	// Contact avec le joueur : le kamikaze fait son office.
	private void SurContact(Node2D corps)
	{
		if (corps is Player)
			Exploser();
	}

	// Détruit avant l'impact (boule de neige du joueur) : même dénouement, l'explosion
	// sert donc aussi d'animation de mort — d'où la surcharge plutôt qu'une anim dédiée.
	protected override void Mourir()
	{
		base.Mourir();
		Exploser();
	}

	// Explosion : blesse tout joueur dans le rayon de souffle, coupe la physique et la
	// collision, joue l'éclatement puis se libère.
	public void Exploser()
	{
		if (EtatCourant == Etat.Explosion)
			return;

		EtatCourant = Etat.Explosion;
		Velocity = Vector2.Zero;
		_sprite.Play("explosion");

		// Le souffle porte plus loin que le corps. Il est relevé À LA DISTANCE, et non en
		// agrandissant la zone de dégâts : Exploser() part le plus souvent d'un signal de
		// contact, donc en plein flush des requêtes physiques — modifier une forme à cet
		// instant est interdit, et l'agrandissement ne serait de toute façon pas encore
		// pris en compte par la requête de chevauchement qui suivrait.
		var joueur = JoueurLePlusProche(out float distance);
		if (joueur != null && distance <= RayonSouffle)
		{
			int direction = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
			joueur.Blesser(direction == 0 ? 1 : direction, DamageSource.JouetExplosif);
		}

		SetPhysicsProcess(false);
		// Les formes se désactivent en différé (même raison : on peut être en plein flush).
		_zoneDegats.GetNode<CollisionShape2D>("CollisionShape2D")
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		_sprite.AnimationFinished += QueueFree;
	}
}
