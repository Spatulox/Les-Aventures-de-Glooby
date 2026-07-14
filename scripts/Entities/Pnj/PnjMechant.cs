using Godot;

// Base commune à tous les PNJ hostiles (lanceur de boules de neige, fonceur...).
// Pendant amical de PnjAmical : une LivingEntity qui déambule en va-et-vient sur le sol,
// mais qui, elle, blesse le joueur au contact via une Area2D « ZoneContact » posée en
// enfant dans sa scène. Le pipeline d'animation est actif (comme le Player et les Boss) :
// chaque sous-classe pointe ConstruireAnimations() vers ses dossiers de frames. Tant que
// ces dossiers sont vides, aucune frame n'est chargée et on retombe automatiquement sur le
// carré placeholder (Sprite2D) ; dès que les PNG existeront, l'AnimatedSprite2D prend le
// relais sans autre changement.
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

	// Carré placeholder, affiché tant qu'aucune frame d'animation n'est disponible.
	protected Sprite2D Sprite;

	// AnimatedSprite2D construit à la volée quand ConstruireAnimations() fournit de
	// vraies frames ; reste null tant que les dossiers d'assets sont vides (repli carré).
	private AnimatedSprite2D _anim;

	private float _xDepart;
	private int _direction = 1;   // 1 = vers la droite, -1 = vers la gauche
	private float _minuteurPause;

	public override void _Ready()
	{
		Sprite = GetNode<Sprite2D>("Sprite2D");
		Pv = PvMax;
		AddToGroup("pnj");
		_xDepart = GlobalPosition.X;

		// Câble la zone de contact (facultative) : au chevauchement du joueur, elle inflige
		// des dégâts de contact. Chaque scène de méchant fournit son Area2D « ZoneContact ».
		var zoneContact = GetNodeOrNull<Area2D>("ZoneContact");
		if (zoneContact != null)
			zoneContact.BodyEntered += SurContact;

		Initialiser();

		// Pipeline d'animation : on ne monte l'AnimatedSprite2D que si les frames "idle"
		// existent réellement. Sinon (dossiers encore vides) on garde le carré placeholder.
		var frames = ConstruireAnimations();
		if (frames != null && frames.GetFrameCount("idle") > 0)
		{
			_anim = new AnimatedSprite2D { SpriteFrames = frames };
			AddChild(_anim);
			_anim.Play("idle");
			Sprite.Visible = false;
		}
	}

	// Hook d'init des sous-classes (récupération de nœuds, état de départ...).
	protected virtual void Initialiser() { }

	// Construit les animations du méchant (idle, marche...) via AnimationsSprite, en pointant
	// vers res://assets/pnj/<nom>/{idle,marche}. Fournie par chaque sous-classe ; peut pointer
	// vers des dossiers vides (aucune frame => carré placeholder conservé).
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

		// Anime le méchant selon qu'il marche ou non (sans effet tant que _anim est null).
		if (_anim != null)
			_anim.Play(Mathf.Abs(velocite.X) > 1f ? "marche" : "idle");
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

	// Oriente le placeholder et l'AnimatedSprite2D éventuel vers la gauche/droite.
	protected void DefinirOrientation(bool versLaGauche)
	{
		Sprite.FlipH = versLaGauche;
		if (_anim != null)
			_anim.FlipH = versLaGauche;
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
