using Godot;

// Mini-jouet explosif largué par le Boss Lutin Mecha : un petit soldat de bois kamikaze.
// Trois états seulement, dans cet ordre et sans retour en arrière :
//   Chute     — il descend lentement suspendu à son parachute, en se balançant ;
//   Fonce     — parachute largué (le passage est visible : le parachute se détache et
//               dérive). Il reste alors PLANTÉ SUR PLACE tant qu'il n'a pas repéré le
//               joueur ; c'est la détection qui allume la mèche ET lance la course ;
//   Explosion — il éclate, blesse tout joueur dans le rayon, puis se libère.
//
// L'explosion est le SEUL dénouement ET la seule source de dégâts : elle survient au
// contact du joueur, à la fin de la mèche, ou si le joueur le détruit avant l'impact
// (PvMax = 1 => Mourir() explose). Le contact ne blesse jamais de lui-même, et la zone
// de contact n'est même armée qu'à partir de l'état Fonce : tant qu'il descend sous son
// parachute, le jouet est inoffensif et traversable.
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
	// Délai avant auto-explosion une fois la mèche allumée, même si le joueur reste hors
	// d'atteinte : la mèche ne s'allume qu'à la VUE du joueur (voir PorteeVue).
	[Export] public float DureeMeche = 4f;
	// Portée de vue servant de repli quand la scène ne porte pas d'Area2D « ZoneDetection » ;
	// avec la zone, c'est la taille de sa forme qui décide (réglable par instance).
	[Export] public float PorteeVue = 200f;
	// Rayon de souffle en pixels : le jouet blesse plus loin que son corps.
	[Export] public float RayonSouffle = 42f;
	// Battement entre le déclenchement et le souffle. C'est ce qui sépare le CONTACT de
	// l'EXPLOSION : toucher le jouet ne blesse pas, ça amorce l'éclatement ; le joueur
	// qui sort du rayon pendant ce délai ne prend rien. Sans lui, contact et dégâts
	// tombaient sur la même frame et se confondaient.
	[Export] public float DelaiSouffle = 0.25f;

	public Etat EtatCourant { get; private set; } = Etat.Chute;

	private AnimatedSprite2D _sprite;
	private Node2D _parachute;
	private Area2D _zoneDegats;
	private float _minuteurMeche;
	// Allumée par la VUE du joueur, elle ne s'éteint plus ensuite : une fois amorcé, le
	// jouet court et le compte à rebours va jusqu'au bout même si le joueur se dérobe.
	// Ce même drapeau commande le mouvement : avant lui, le jouet ne bouge pas.
	private bool _mecheAllumee;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_parachute = GetNodeOrNull<Node2D>("Parachute");
		_zoneDegats = GetNode<Area2D>("ZoneDegats");

		AppliquerCollisionsPnj();
		MasquerApercuEditeur();
		// Vue du jouet : si la scène porte une Area2D « ZoneDetection », sa forme
		// (réglable par instance) définit la portée à la place de PorteeVue.
		CablerZoneDetection();
		Pv = PvMax;
		AddToGroup("pnj");

		_sprite.SpriteFrames = ConstruireAnimations();
		_sprite.Play("chute");
		_minuteurMeche = DureeMeche;

		_zoneDegats.BodyEntered += SurContact;
		// La zone de contact reste DÉSARMÉE pendant la descente : un jouet qui flotte
		// encore sous son parachute ne doit rien faire au joueur qui le frôle. Elle ne
		// s'arme qu'au largage (LarguerParachute) — sans quoi, sous la pluie de cadeaux
		// du Père Noël, le joueur encaissait une explosion imparable en se déplaçant.
		ArmerZoneDegats(false);

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

		// Fonce : gravité toujours (il doit rester posé au sol), le reste dépend de la vue.
		AppliquerGravite(ref velocite, dt);

		// La mèche s'allume à la VUE du joueur, et ne s'éteint plus ensuite : un jouet qui
		// n'a jamais aperçu personne attend indéfiniment au lieu de se saborder tout seul
		// dans son coin, et le joueur qui se dérobe ensuite ne l'éteint pas — d'où le test
		// unique, qui coupe aussi la détection une fois amorcé.
		if (!_mecheAllumee && JoueurAPortee(out float distanceVue) != null && distanceVue <= PorteeVue)
			Amorcer();

		// Tant qu'il n'a rien vu, il reste PLANTÉ : pas de course, pas de mèche. Le joueur
		// peut donc traverser l'arène en évitant les jouets déjà posés au lieu de se faire
		// pourchasser par toute la fournée dès l'atterrissage.
		if (!_mecheAllumee)
		{
			AppliquerFriction(ref velocite, dt);
			Velocity = velocite;
			MoveAndSlide();
			return;
		}

		// Amorcé : course kamikaze vers le joueur. On vise le joueur le plus proche et non
		// celui de la zone de détection — une fois lancé, il poursuit même hors de vue.
		var joueur = JoueurLePlusProche(out float _);
		if (joueur != null)
		{
			int direction = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
			velocite.X = direction * VitesseFonce;
			// ATTENTION, sens inverse du reste du jeu : les frames du petit soldat le
			// montrent tourné vers la GAUCHE, là où tous les autres sprites (Boss Cerf,
			// Lutin Mecha, PNJ...) regardent à droite au repos. On retourne donc quand il
			// court vers la droite, sinon il avance en marche arrière.
			_sprite.FlipH = direction > 0;
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
		// Il se met en position mais reste immobile : l'animation de course est figée sur sa
		// première frame (le petit soldat au garde-à-vous) jusqu'à ce qu'il repère le joueur.
		_sprite.Play("fonce");
		_sprite.Pause();
		// C'est ici, et pas avant, que le jouet devient dangereux au contact.
		ArmerZoneDegats(true);

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

	// Repérage du joueur : la mèche s'allume et la course démarre en même temps (l'animation
	// de course, figée depuis le largage, repart). Sans retour possible à l'immobilité.
	private void Amorcer()
	{
		_mecheAllumee = true;
		_sprite.Play("fonce");
	}

	// Contact avec le joueur : le kamikaze fait son office. Le contact ne blesse jamais
	// par lui-même — il ne fait que déclencher l'explosion, seule source de dégâts du
	// jouet. Ceinture et bretelles avec la zone désarmée : un chevauchement déjà en
	// cours au moment du largage ne doit pas non plus valoir explosion en pleine chute.
	private void SurContact(Node2D corps)
	{
		if (EtatCourant == Etat.Fonce && corps is Player)
			Exploser();
	}

	// Active/désactive la zone de contact. En différé : on peut être appelé depuis un
	// signal de collision, donc en plein flush des requêtes physiques, où toute
	// modification de forme est refusée.
	private void ArmerZoneDegats(bool arme)
		=> _zoneDegats.GetNode<CollisionShape2D>("CollisionShape2D")
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, !arme);

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

		// Le souffle part APRÈS le battement, et seulement là : c'est l'explosion qui
		// blesse, jamais le contact qui l'a amorcée.
		GetTree().CreateTimer(DelaiSouffle).Timeout += AppliquerSouffle;

		SetPhysicsProcess(false);
		// Les formes se désactivent en différé (même raison : on peut être en plein flush).
		ArmerZoneDegats(false);
		GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		_sprite.AnimationFinished += QueueFree;
	}

	// Souffle : blesse le joueur s'il est ENCORE dans le rayon au moment de l'éclatement.
	// Le rayon est relevé À LA DISTANCE, et non en agrandissant la zone de dégâts :
	// modifier une forme depuis un signal de contact (flush des requêtes physiques) est
	// interdit, et l'agrandissement ne serait pas pris en compte par la requête suivante.
	private void AppliquerSouffle()
	{
		// Le jouet a pu être libéré entre-temps (fin de l'animation d'explosion).
		if (!IsInstanceValid(this))
			return;

		var joueur = JoueurLePlusProche(out float distance);
		if (joueur == null || distance > RayonSouffle)
			return;

		int direction = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		joueur.Blesser(direction == 0 ? 1 : direction, DamageSource.JouetExplosif);
	}
}
