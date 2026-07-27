using Godot;

// Ennemi « Ours de neige » : patrouille tranquillement, puis dès que le joueur entre dans sa
// portée il donne un BREF coup de charge (Etat.Charge) — plus rapide que le joueur mais en
// ligne droite, direction verrouillée au départ : esquivable en pas de côté. La charge est
// suivie d'une récupération (Etat.Recuperation) pendant laquelle il s'arrête et ne peut pas
// relancer, ce qui ouvre une fenêtre pour le dépasser ou le viser.
//
// Comme le bonhomme de neige, il est ÉTOURDISSABLE (Etourdissable) : une boule de neige le fige
// (Etat.Etourdi) au lieu de lui infliger des dégâts, et il ne perd JAMAIS de PV (jamais tué) —
// c'est un obstacle qu'on neutralise le temps de passer. Il blesse toujours au contact via la
// ZoneContact héritée de PnjMechant.
//
// Frames chargées depuis res://assets/pnj/ours_de_neige/{idle,marche} dans l'AnimatedSprite2D de
// la scène (invisible tant que les dossiers restent vides).
public partial class OursDeNeige : PnjMechant, Etourdissable
{
	private enum EtatOurs { Patrouille, Charge, Recuperation, Etourdi }

	// Vitesse horizontale de la ruée : volontairement supérieure au Speed du joueur (220) pour
	// qu'il le dépasse pendant l'élan, mais seulement le temps de DureeCharge.
	[Export] public float VitesseCharge = 200f;
	// Durée du bref coup de charge.
	[Export] public float DureeCharge = 1f;
	// Récupération après la charge : l'ours s'arrête et ne peut pas relancer (fenêtre d'esquive).
	[Export] public float DureeRecuperation = 1.2f;
	// Durée de l'étourdissement infligé par une boule de neige du joueur.
	[Export] public float DureeEtourdissement = 1.5f;

	private EtatOurs _etat = EtatOurs.Patrouille;
	private float _minuteur;
	private int _dirCharge = 1;

	// Machine à états simple pilotée frame par frame : la base PnjMechant applique gravité,
	// MoveAndSlide et le choix idle/marche, on ne fixe donc ici que velocite.X.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		switch (_etat)
		{
			case EtatOurs.Etourdi:
				velocite.X = 0f;
				if (Decompter(dt))
					_etat = EtatOurs.Patrouille;
				break;

			case EtatOurs.Patrouille:
				Patrouiller(dt, ref velocite);
				if (joueur != null && distance <= PorteeDetection)
					DemarrerCharge(joueur);
				break;

			case EtatOurs.Charge:
				velocite.X = _dirCharge * VitesseCharge;
				if (Decompter(dt))
				{
					_etat = EtatOurs.Recuperation;
					_minuteur = DureeRecuperation;
				}
				break;

			case EtatOurs.Recuperation:
				velocite.X = 0f;
				if (Decompter(dt))
					_etat = EtatOurs.Patrouille;
				break;
		}
	}

	// Verrouille la direction de la charge vers le joueur au moment du départ : l'ours fonce
	// ensuite en ligne droite (pas de suivi), ce qui la rend esquivable d'un pas de côté.
	private void DemarrerCharge(Player joueur)
	{
		_dirCharge = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		if (_dirCharge == 0)
			_dirCharge = 1;
		_etat = EtatOurs.Charge;
		_minuteur = DureeCharge;
	}

	// Décompte le minuteur courant ; renvoie vrai quand il atteint 0 (transition d'état).
	private bool Decompter(float dt)
	{
		_minuteur -= dt;
		return _minuteur <= 0f;
	}

	// Etourdissable : une boule de neige fige l'ours (aucun PV perdu). Un flash bleu sert de
	// retour visuel — pas d'animation dédiée (budget) : la base rejoue idle car velocite.X == 0.
	public void Etourdir(float duree)
	{
		_etat = EtatOurs.Etourdi;
		_minuteur = duree;
		Effets.FlashCouleur(Sprite, new Color(0.6f, 0.85f, 1f), 0.1f, 0.3f);
	}

	// L'ours n'encaisse que la boule de neige, et jamais en PV (jamais tué) : elle l'étourdit.
	// Insensible à toute autre source. On ne délègue pas à base.TakeDamage (qui retirerait des PV
	// et, avec l'option de test « ennemis tués en un coup », one-shot toute source du joueur).
	public override bool IsInvincibleToDamage(DamageSource source) => source is not DamageSource.Snowball;

	public override void TakeDamage(DamageSource source)
	{
		if (source == DamageSource.Snowball)
			Etourdir(DureeEtourdissement);
	}

	// Animations de l'ours de neige depuis res://assets/pnj/ours_de_neige/{idle,marche}. Dossiers
	// encore vides : ConstruireAnimations renvoie des animations sans frame (ours de neige invisible)
	// jusqu'à ce que les PNG y soient déposés.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/ours_de_neige/idle", 6f, true);
		AjouterAnimation(frames, "marche", "res://assets/pnj/ours_de_neige/marche", 8f, true);
		return frames;
	}
}
