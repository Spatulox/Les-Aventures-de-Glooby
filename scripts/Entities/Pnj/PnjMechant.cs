using Godot;

// Base commune à tous les PNJ hostiles (lanceur de boules de neige, fonceur...).
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

	// AnimatedSprite2D de la scène : ses SpriteFrames sont chargés au démarrage (comme Boss).
	protected AnimatedSprite2D Sprite;

	private float _xDepart;
	private int _direction = 1;   // 1 = vers la droite, -1 = vers la gauche
	private float _minuteurPause;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Pv = PvMax;
		AddToGroup("pnj");
		_xDepart = GlobalPosition.X;

		// Câble la zone de contact (facultative) : au chevauchement du joueur, elle inflige
		// des dégâts de contact. Chaque scène de méchant fournit son Area2D « ZoneContact ».
		var zoneContact = GetNodeOrNull<Area2D>("ZoneContact");
		if (zoneContact != null)
			zoneContact.BodyEntered += SurContact;

		Initialiser();

		// Charge les animations dans l'AnimatedSprite2D de la scène puis lance l'idle
		// (sans effet tant que le dossier idle est vide : le méchant reste alors invisible).
		Sprite.SpriteFrames = ConstruireAnimations();
		if (Sprite.SpriteFrames.GetFrameCount("idle") > 0)
			Sprite.Play("idle");
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

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		var velocite = Velocity;

		AppliquerGravite(ref velocite, dt);

		// Le joueur le plus proche sert de cible aux comportements d'attaque des sous-classes.
		var joueur = JoueurLePlusProche(out float distance);
		DeciderMouvement(dt, ref velocite, joueur, distance);

		Velocity = velocite;
		MoveAndSlide();

		// Oriente le sprite dans le sens du déplacement horizontal.
		if (Mathf.Abs(velocite.X) > 1f)
			DefinirOrientation(velocite.X < 0f);

		// Anime le méchant selon qu'il marche ou non.
		Sprite.Play(Mathf.Abs(velocite.X) > 1f && Sprite.SpriteFrames.GetFrameCount("marche") > 0 ? "marche" : "idle");
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

	// Renvoie le joueur le plus proche (groupe "joueur") et sa distance, ou null s'il n'y en a pas.
	protected Player JoueurLePlusProche(out float distance)
	{
		distance = float.MaxValue;
		Player plusProche = null;
		foreach (var noeud in GetTree().GetNodesInGroup("joueur"))
		{
			if (noeud is not Player joueur)
				continue;
			float d = GlobalPosition.DistanceTo(joueur.GlobalPosition);
			if (d < distance)
			{
				distance = d;
				plusProche = joueur;
			}
		}
		return plusProche;
	}

	// Contact de la zone hostile avec le joueur : lui inflige des dégâts avec recul,
	// poussé dans le sens opposé au méchant.
	private void SurContact(Node2D corps)
	{
		if (corps is not Player joueur)
			return;
		int direction = Mathf.Sign(GlobalPosition.X - joueur.GlobalPosition.X);
		joueur.Blesser(direction, DamageSource.ContactMechant);
	}
}
