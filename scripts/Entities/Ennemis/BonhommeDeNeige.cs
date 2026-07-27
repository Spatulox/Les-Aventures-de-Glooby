using Godot;

// Ennemi « Bonhomme de neige » (malicieux) : ennemi statique qui, dès que le
// joueur entre dans sa portée, arme un tir (télégraphe), lance une boule de
// neige en cloche, puis recharge. Fichier neuf, indépendant du
// LanceurBouleNeige existant. Machine à états : Idle / Armer / Etourdi / Fonte.
//
// Il ne meurt jamais par perte de PV : le joueur ne dispose que de deux prises
// sur lui, et TakeDamage les traite sans jamais toucher aux PV —
//   - une boule de neige (DamageSource.Snowball) l'ÉTOURDIT le temps de
//     DureeEtourdissement (il cesse de viser et de tirer) ;
//   - le pouvoir de chaleur (DamageSource.Fire) le fait FONDRE définitivement.
// Toute autre source de dégâts le laisse indifférent.
public partial class BonhommeDeNeige : LivingEntity, Etourdissable
{
	private enum Etat { Idle, Armer, Lancer, Etourdi, Fonte }

	// Scène du projectile en cloche (scenes/ennemis/BouleDeNeige.tscn).
	[Export] public PackedScene SceneBoule;
	// Distance de détection du joueur.
	[Export] public float Portee = 220f;
	// Délai entre deux salves quand le joueur reste à portée.
	[Export] public float CadenceTir = 2.0f;
	// Durée du télégraphe « il forme la boule » avant le lancer.
	[Export] public float DureeArmer = 0.5f;
	// Durée de l'étourdissement infligé par une boule de neige du joueur.
	[Export] public float DureeEtourdissement = 1.5f;
	// Durée de l'affaissement final (≈ la durée de l'animation « fondre »).
	[Export] public float DureeFonte = 0.6f;
	// Échelle verticale atteinte en fin de fonte (0.12 = écrasé en flaque).
	[Export] public float EcrasementFonte = 0.12f;
	// Vitesse horizontale et hauteur d'arc du projectile (trajectoire en cloche).
	[Export] public float VitesseProjectile = 170f;
	[Export] public float ArcProjectile = 200f;

	private Etat _etat = Etat.Idle;
	private float _minuteur;
	private float _rechargeMinuteur;
	private int _dirTir = 1;
	private AnimatedSprite2D _sprite;
	// Vrai tant qu'on attend le signal AnimationFinished du lancer : évite de se
	// désabonner d'un signal jamais connecté (dossier d'animation vide, ou tir
	// interrompu par un étourdissement).
	private bool _attenteFinLancer;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AppliquerCollisionsPnj();
		MasquerApercuEditeur();
		// Zone de détection facultative : si la scène porte une Area2D « ZoneDetection », sa taille
		// (réglable par instance) définit la portée à la place de Portee.
		CablerZoneDetection();
		_sprite.SpriteFrames = ConstruireAnimations();
		Pv = PvMax;   // décoratif : les PV ne sont jamais consommés (voir TakeDamage)
		AddToGroup("pnj");
		JouerSiPresente("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_etat == Etat.Fonte)
			return;

		float dt = (float)delta;
		var v = Velocity;
		AppliquerGravite(ref v, dt);   // reste au sol
		v.X = 0f;                       // statique
		if (_rechargeMinuteur > 0f)
			_rechargeMinuteur -= dt;

		// Joueur à portée : piloté par la ZoneDetection si la scène en fournit une (distance 0 dans
		// la zone), sinon par la distance à Portee (repli).
		var joueur = JoueurAPortee(out float distance);

		switch (_etat)
		{
			case Etat.Idle:
				if (joueur != null && distance <= Portee && _rechargeMinuteur <= 0f)
					EntrerArmer(joueur);
				break;

			case Etat.Armer:
				_minuteur -= dt;
				if (_minuteur <= 0f)
					EntrerLancer();
				break;

			case Etat.Lancer:
				// Le lancer effectif se fait à la fin de l'animation (voir OnLancerFini).
				break;

			case Etat.Etourdi:
				// Assommé par une boule de neige : ni visée, ni tir, jusqu'à la fin du décompte.
				_minuteur -= dt;
				if (_minuteur <= 0f)
				{
					_etat = Etat.Idle;
					_sprite.Play();          // relance l'idle mis en pause par l'étourdissement
				}
				break;
		}

		Velocity = v;
		MoveAndSlide();
	}

	private void EntrerArmer(Player joueur)
	{
		_etat = Etat.Armer;
		_minuteur = DureeArmer;
		_dirTir = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		if (_dirTir == 0)
			_dirTir = 1;
		_sprite.FlipH = _dirTir < 0;
		JouerSiPresente("armer");
	}

	private void EntrerLancer()
	{
		_etat = Etat.Lancer;
		if (JouerSiPresente("lancer"))
		{
			_sprite.AnimationFinished += OnLancerFini;
			_attenteFinLancer = true;
		}
		else
		{
			OnLancerFini();   // pas d'anim : on lance tout de suite
		}
	}

	// Fin de l'animation de lancer : instancie la boule et repart en recharge.
	private void OnLancerFini()
	{
		OublierFinLancer();
		Tirer();
		_rechargeMinuteur = CadenceTir;
		_etat = Etat.Idle;
		JouerSiPresente("idle");
	}

	// Se désabonne du signal de fin de lancer, s'il est effectivement connecté.
	private void OublierFinLancer()
	{
		if (!_attenteFinLancer)
			return;

		_sprite.AnimationFinished -= OnLancerFini;
		_attenteFinLancer = false;
	}

	private void Tirer()
	{
		if (SceneBoule == null)
			return;

		var boule = SceneBoule.Instantiate<BouleDeNeige>();
		// Cloche : vitesse horizontale vers le joueur + poussée vers le haut. Le
		// bonhomme s'enregistre comme tireur pour ne pas se blesser lui-même.
		boule.Initialiser(this, new Vector2(_dirTir * VitesseProjectile, -ArcProjectile));
		GetParent().AddChild(boule);
		boule.GlobalPosition = GlobalPosition + new Vector2(_dirTir * 16f, -18f);
	}

	// Le bonhomme n'encaisse que les deux attaques du joueur, et jamais en PV : la
	// boule de neige l'étourdit, le pouvoir de chaleur le fait fondre. On ne délègue
	// donc pas à base.TakeDamage (qui retirerait des PV, et qui avec l'option de test
	// « ennemis tués en un coup » one-shot toute source venant du joueur).
	public override void TakeDamage(DamageSource source)
	{
		switch (source)
		{
			case DamageSource.Fire:
				EntrerFonte();
				break;

			case DamageSource.Snowball:
				Etourdir(DureeEtourdissement);
				break;
		}
	}

	// Insensible à tout sauf aux deux attaques du joueur — et plus rien ne l'atteint
	// une fois la fonte lancée. Degats.Infliger n'appelle TakeDamage que si cette
	// méthode renvoie false : la boule de neige doit donc rester « non invincible »
	// pour que l'étourdissement passe.
	public override bool IsInvincibleToDamage(DamageSource source)
	{
		if (_etat == Etat.Fonte)
			return true;

		return source is not (DamageSource.Fire or DamageSource.Snowball);
	}

	// Étourdi par une boule de neige (Etourdissable) : le tir en cours est annulé et le
	// bonhomme se fige (idle en pause) le temps du décompte. Pas d'animation dédiée — l'idle
	// figé suffit et évite de générer un jeu de frames supplémentaire.
	public void Etourdir(float duree)
	{
		OublierFinLancer();
		_etat = Etat.Etourdi;
		_minuteur = duree;
		_rechargeMinuteur = CadenceTir;   // il ne repart pas en tir dès le réveil
		Effets.FlashCouleur(_sprite, new Color(0.6f, 0.85f, 1f), 0.1f, 0.3f);
		JouerSiPresente("idle");
		_sprite.Pause();
	}

	// Fondu par le pouvoir de chaleur : joue la fonte puis disparaît définitivement.
	private void EntrerFonte()
	{
		if (_etat == Etat.Fonte)
			return;

		OublierFinLancer();
		_etat = Etat.Fonte;
		SetPhysicsProcess(false);
		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col != null)
			col.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		// L'affaissement porte le QueueFree : le bonhomme s'écrase vers le sol (sa
		// base reste posée) en se fondant, pendant que joue l'animation « fondre ».
		if (JouerSiPresente("fondre"))
		{
			var texture = _sprite.SpriteFrames.GetFrameTexture("fondre", 0);
			Effets.FondreVersLeBas(_sprite, EcrasementFonte, texture.GetHeight() * 0.5f, DureeFonte, this);
		}
		else
		{
			QueueFree();
		}
	}

	// Joue une animation si elle a au moins une frame ; renvoie vrai si jouée.
	private bool JouerSiPresente(string nom)
	{
		if (_sprite.SpriteFrames.HasAnimation(nom) && _sprite.SpriteFrames.GetFrameCount(nom) > 0)
		{
			_sprite.Play(nom);
			return true;
		}
		return false;
	}

	private SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/pnj/bonhomme_neige";
		AnimationsSprite.EnregistrerAnimation(frames, "idle", AnimationsSprite.ChargerFrames($"{b}/idle"), 5f, true);
		AnimationsSprite.EnregistrerAnimation(frames, "armer", AnimationsSprite.ChargerFrames($"{b}/armer"), 8f, false);
		AnimationsSprite.EnregistrerAnimation(frames, "lancer", AnimationsSprite.ChargerFrames($"{b}/lancer"), 12f, false);
		AnimationsSprite.EnregistrerAnimation(frames, "fondre", AnimationsSprite.ChargerFrames($"{b}/fondre"), 8f, false);
		return frames;
	}
}
