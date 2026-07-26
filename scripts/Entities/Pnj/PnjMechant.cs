using Godot;

// Base commune à tous les PNJ hostiles (lanceur de boules de neige, ours de neige...).
// Pendant amical de PnjAmical : une LivingEntity qui déambule en va-et-vient sur le sol,
// mais qui, elle, blesse le joueur au contact via une Area2D « ZoneContact » posée en
// enfant dans sa scène. Le pipeline d'animation est actif (comme le Player et les Boss) :
// la scène porte un AnimatedSprite2D « AnimatedSprite2D » dont les SpriteFrames sont chargés
// au démarrage depuis les dossiers pointés par ConstruireAnimations(). Tant qu'un dossier est
// vide, l'animation correspondante n'a aucune frame (méchant invisible) ; dès que les PNG y
// sont déposés, ils s'affichent sans autre changement.
//
// Le comportement d'attaque propre à chaque méchant (lancer, foncer...) est branché en
// surchargeant DeciderMouvement() ; par défaut, un méchant se contente de patrouiller.
public abstract partial class PnjMechant : LivingEntity
{
	// ---- Déambulation (réglages) ----
	[Export] public float DistancePatrouille = 60f; // amplitude du va-et-vient autour du point de départ
	[Export] public float VitessePatrouille = 30f;  // vitesse horizontale de marche
	[Export] public float TempsPause = 1.2f;         // pause à chaque extrémité

	// Distance (px) en deçà de laquelle le méchant « voit » le joueur et peut l'attaquer.
	[Export] public float PorteeDetection = 160f;

	// Contact continu : le méchant blesse le joueur à CHAQUE frame où il le chevauche, au lieu
	// du seul instant d'entrée dans la ZoneContact. À réserver aux méchants qui restent collés
	// à leur cible (nuée volante, poursuivant) — l'invincibilité du joueur espace les coups.
	[Export] public bool ContactContinu = false;

	// AnimatedSprite2D de la scène : ses SpriteFrames sont chargés au démarrage (comme Boss).
	protected AnimatedSprite2D Sprite;

	private Area2D _zoneContact;
	private float _xDepart;
	private int _direction = 1;   // 1 = vers la droite, -1 = vers la gauche
	private float _minuteurPause;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AppliquerCollisionsPnj();
		MasquerApercuEditeur();
		Pv = PvMax;
		AddToGroup("pnj");
		_xDepart = GlobalPosition.X;

		// Câble la zone de contact (facultative) : au chevauchement du joueur, elle inflige
		// des dégâts de contact. Chaque scène de méchant fournit son Area2D « ZoneContact ».
		_zoneContact = GetNodeOrNull<Area2D>("ZoneContact");
		if (_zoneContact != null)
			_zoneContact.BodyEntered += SurContact;

		// Câble la zone de détection (facultative) : si la scène porte une Area2D « ZoneDetection »,
		// c'est sa taille (réglable par instance) qui définit la portée, à la place de PorteeDetection.
		CablerZoneDetection();

		Initialiser();

		// Charge les animations dans l'AnimatedSprite2D de la scène puis lance l'idle
		// (sans effet tant que le dossier idle est vide : le méchant reste alors invisible).
		Sprite.SpriteFrames = ConstruireAnimations();
		JouerSiPresente("idle");
	}

	// Hook d'init des sous-classes (récupération de nœuds, état de départ...).
	protected virtual void Initialiser() { }

	// Construit les animations du méchant (idle, marche...) via AnimationsSprite, en pointant
	// vers res://assets/pnj/<nom>/{idle,marche}. Fournie par chaque sous-classe ; peut pointer
	// vers des dossiers vides (aucune frame => animation vide, méchant invisible).
	protected abstract SpriteFrames ConstruireAnimations();

	// Ajoute une animation à un SpriteFrames depuis un dossier de PNG (façade partagée avec
	// les boss/PNJ amicaux au-dessus de AnimationsSprite). Réutilisable par les sous-classes.
	protected static void AjouterAnimation(SpriteFrames frames, string nom, string dossier, float fps, bool boucle)
	{
		AnimationsSprite.EnregistrerAnimation(frames, nom, AnimationsSprite.ChargerFrames(dossier), fps, boucle);
	}

	// Vrai si le méchant est soumis à la gravité (marcheur). Les volants (nuée de pollen)
	// renvoient false : ils pilotent alors eux-mêmes velocite.Y dans DeciderMouvement.
	protected virtual bool SubitGravite => true;

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		var velocite = Velocity;

		if (SubitGravite)
			AppliquerGravite(ref velocite, dt);

		// Le joueur à portée sert de cible aux comportements d'attaque des sous-classes (piloté par
		// la ZoneDetection si la scène en fournit une, sinon par la distance à PorteeDetection).
		var joueur = JoueurAPortee(out float distance);
		DeciderMouvement(dt, ref velocite, joueur, distance);

		Velocity = velocite;
		MoveAndSlide();

		// Oriente le sprite dans le sens du déplacement horizontal.
		if (Mathf.Abs(velocite.X) > 1f)
			DefinirOrientation(velocite.X < 0f);

		// Méchant « collant » : tant qu'il chevauche le joueur, il le blesse frame après frame.
		if (ContactContinu)
			BlesserJoueursDansZone(_zoneContact);

		MettreAJourAnimation(velocite);
	}

	// Choisit l'animation de la frame : « marche » quand le méchant se déplace, « idle » sinon.
	// Les méchants dont l'animation est pilotée par leur propre machine à états (fleur carnivore,
	// bulbe explosif) surchargent pour ne rien faire ici.
	protected virtual void MettreAJourAnimation(Vector2 velocite)
	{
		if (Mathf.Abs(velocite.X) <= 1f || !JouerSiPresente("marche"))
			JouerSiPresente("idle");
	}

	// Joue une animation si la scène en fournit effectivement les frames ; renvoie vrai si jouée.
	// Évite d'appeler Play sur une animation absente (dossier de PNG encore vide).
	protected bool JouerSiPresente(string nom)
	{
		if (Sprite.SpriteFrames == null || !Sprite.SpriteFrames.HasAnimation(nom) || Sprite.SpriteFrames.GetFrameCount(nom) == 0)
			return false;

		Sprite.Play(nom);
		return true;
	}

	// Décide du déplacement horizontal de la frame. Par défaut : patrouille en va-et-vient.
	// Les sous-classes surchargent pour attaquer (foncer sur le joueur, s'arrêter pour tirer...).
	protected virtual void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		Patrouiller(dt, ref velocite);
	}

	// Va-et-vient sur le sol autour du point de départ, avec pause à chaque extrémité.
	// Réutilisable par les sous-classes qui veulent patrouiller hors de portée du joueur.
	protected void Patrouiller(float dt, ref Vector2 velocite)
	{
		if (_minuteurPause > 0f)
		{
			_minuteurPause -= dt;
			velocite.X = 0f;
			return;
		}

		velocite.X = _direction * VitessePatrouille;

		float ecart = GlobalPosition.X - _xDepart;
		if (ecart >= DistancePatrouille && _direction > 0)
		{
			_direction = -1;
			_minuteurPause = TempsPause;
		}
		else if (ecart <= -DistancePatrouille && _direction < 0)
		{
			_direction = 1;
			_minuteurPause = TempsPause;
		}
	}

	// Oriente l'AnimatedSprite2D vers la gauche/droite.
	protected void DefinirOrientation(bool versLaGauche)
	{
		Sprite.FlipH = versLaGauche;
	}

	// Contact de la zone hostile avec le joueur : lui inflige des dégâts avec recul,
	// poussé dans le sens opposé au méchant.
	private void SurContact(Node2D corps)
	{
		if (corps is Player joueur)
			BlesserJoueur(joueur);
	}

	// Blesse le joueur avec recul, poussé dans le sens opposé au méchant. Point d'entrée
	// partagé de toutes les agressions d'un méchant (contact simple, morsure, explosion...).
	protected void BlesserJoueur(Player joueur, DamageSource source = DamageSource.ContactMechant)
	{
		int direction = Mathf.Sign(GlobalPosition.X - joueur.GlobalPosition.X);
		joueur.Blesser(direction, source);
	}

	// Blesse tous les joueurs présents dans une Area2D du méchant (zone de morsure, souffle
	// d'explosion...) : attaque « à la frame », par opposition au contact déclenché à l'entrée.
	protected void BlesserJoueursDansZone(Area2D zone, DamageSource source = DamageSource.ContactMechant)
	{
		if (zone == null)
			return;

		foreach (var corps in zone.GetOverlappingBodies())
		{
			if (corps is Player joueur)
				BlesserJoueur(joueur, source);
		}
	}

	// ---- Mort ----
	// Mort générique d'un méchant : il cesse de bouger et de blesser, joue son animation
	// « mort » si sa scène en fournit les frames, puis s'efface. Tout méchant dont le dossier
	// mort/ est rempli en profite sans une ligne de code supplémentaire.
	protected override void Mourir()
	{
		base.Mourir();
		SetPhysicsProcess(false);
		DesactiverCollisions();

		if (JouerSiPresente("mort"))
			Sprite.AnimationFinished += SurFinAnimationMort;
		else
			QueueFree();
	}

	// Fin de l'animation de mort : le méchant s'estompe puis se libère.
	private void SurFinAnimationMort()
	{
		Sprite.AnimationFinished -= SurFinAnimationMort;
		Effets.Disparaitre(Sprite, Sprite.Scale, 0.3f, this);
	}

	// Coupe toutes les collisions du méchant (corps et zones) : un mourant ne bloque plus le
	// joueur et ne le blesse plus pendant son animation de mort.
	protected void DesactiverCollisions()
	{
		foreach (var enfant in GetChildren())
		{
			if (enfant is CollisionShape2D forme)
				forme.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			else if (enfant is Area2D zone)
				zone.SetDeferred(Area2D.PropertyName.Monitoring, false);
		}
	}
}
