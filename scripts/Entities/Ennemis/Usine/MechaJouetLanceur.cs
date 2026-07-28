using Godot;

// Ennemi « Mécha jouet lanceur » (usine du Père Noël) : la déclinaison usine du LANCEUR de la
// banquise (BonhommeDeNeige). Même structure d'états — statique, il attend que le joueur entre
// dans sa portée, ARME son bras (télégraphe visible), lance un projectile en cloche, puis
// recharge. Il sort de la même chaîne de montage que son patron : sa scène lui met un
// CadeauExplosif dans le bras, le même que celui du Père Noël.
//
// La scène du projectile est un simple PackedScene typé Projectile : n'importe quel projectile
// du jeu s'y branche sans toucher au code (il tirait une BouleDeNeige jusqu'ici).
//
// Différence assumée avec le bonhomme, qui justifie une classe à part plutôt qu'une sous-classe :
// le bonhomme est un obstacle de neige (il ne perd jamais de PV, il fond au pouvoir de chaleur et
// s'étourdit à la boule) ; l'automate, lui, est en bois et MEURT normalement en PV. Le code
// partagé vit dans PnjMechant : contact, orientation et séquence de mort animée.
public partial class MechaJouetLanceur : PnjMechant
{
	private enum EtatMecha { Idle, Armer, Lancer }

	// Scène du projectile en cloche — scenes/projectiles/CadeauExplosif.tscn.
	[Export] public PackedScene SceneProjectile;
	// Durée du télégraphe « le bras se charge » avant le lancer : la fenêtre d'esquive.
	[Export] public float DureeArmer = 0.6f;
	// Durée de la pose de lancer (la boule part au tout début, quand le bras fouette).
	[Export] public float DureeLancer = 0.35f;
	// Délai entre deux salves tant que le joueur reste à portée.
	[Export] public float CadenceTir = 2.0f;
	// Vitesse horizontale et hauteur d'arc du projectile (trajectoire en cloche).
	[Export] public float VitesseProjectile = 170f;
	[Export] public float ArcProjectile = 200f;

	private EtatMecha _etat = EtatMecha.Idle;
	private float _minuteur;
	private float _recharge;
	private int _dirTir = 1;

	// Statique : il ne se déplace jamais, il vise et tire. La base applique gravité,
	// MoveAndSlide, orientation et mort.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		velocite.X = 0f;

		if (_recharge > 0f)
			_recharge -= dt;

		switch (_etat)
		{
			case EtatMecha.Idle:
				if (joueur != null && distance <= PorteeDetection && _recharge <= 0f)
					EntrerArmer(joueur);
				break;

			case EtatMecha.Armer:
				if (Decompter(dt))
					EntrerLancer();
				break;

			case EtatMecha.Lancer:
				if (Decompter(dt))
				{
					_etat = EtatMecha.Idle;
					_recharge = CadenceTir;
				}
				break;
		}
	}

	// L'animation suit l'état, pas la vitesse (le mécha ne se déplace jamais).
	protected override void MettreAJourAnimation(Vector2 velocite)
	{
		switch (_etat)
		{
			case EtatMecha.Armer: JouerSiPresente("armer"); break;
			case EtatMecha.Lancer: JouerSiPresente("lancer"); break;
			default: JouerSiPresente("idle"); break;
		}
	}

	// Vise le joueur et arme : la direction du tir est verrouillée dès le télégraphe.
	private void EntrerArmer(Player joueur)
	{
		_dirTir = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		if (_dirTir == 0)
			_dirTir = 1;

		_etat = EtatMecha.Armer;
		_minuteur = DureeArmer;
		DefinirOrientation(_dirTir < 0);

		// Le sprite retenu porte déjà le bras levé au repos : la seule pose d'armement ne
		// tranche pas assez sur l'idle. On double donc le télégraphe de deux effets
		// procéduraux (aucune frame supplémentaire à générer) : un flash chaud, et une
		// ANTICIPATION en écrase-étire — le mécha se tasse sur ses appuis avant de lancer,
		// et se détend au tir. Les deux se lisent instantanément à petite échelle.
		Effets.FlashCouleur(Sprite, new Color(1.6f, 1.15f, 1.0f), 0.12f, DureeArmer - 0.12f);
		AnimerEchelle(new Vector2(0.9f, 1.1f), DureeArmer);
	}

	// La boule part au début de la pose de lancer, au moment où le bras fouette vers l'avant.
	private void EntrerLancer()
	{
		_etat = EtatMecha.Lancer;
		_minuteur = DureeLancer;
		AnimerEchelle(Vector2.One, DureeLancer * 0.5f);   // détente : fin de l'anticipation
		Tirer();
	}

	// Petite déformation d'échelle du sprite (écrase-étire) sur une durée donnée.
	private void AnimerEchelle(Vector2 cible, float duree)
	{
		var tween = CreateTween();
		tween.TweenProperty(Sprite, "scale", cible, duree).SetTrans(Tween.TransitionType.Sine);
	}

	private void Tirer()
	{
		if (SceneProjectile == null)
			return;

		// Typé sur la BASE Projectile, pas sur une scène en particulier : c'est ce qui permet
		// d'échanger le projectile depuis la scène sans retoucher ce code.
		var projectile = SceneProjectile.Instantiate<Projectile>();
		// Cloche : vitesse horizontale vers le joueur + poussée vers le haut. Le mécha
		// s'enregistre comme tireur pour ne pas se blesser avec son propre projectile.
		projectile.Initialiser(this, new Vector2(_dirTir * VitesseProjectile, -ArcProjectile));
		GetParent().AddChild(projectile);
		projectile.GlobalPosition = GlobalPosition + new Vector2(_dirTir * 14f, -20f);
	}

	// Décompte le minuteur courant ; renvoie vrai quand il atteint 0 (transition d'état).
	private bool Decompter(float dt)
	{
		_minuteur -= dt;
		return _minuteur <= 0f;
	}

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/usine/mecha_lanceur";
		AjouterAnimation(frames, "idle", $"{b}/idle", 6f, true);
		AjouterAnimation(frames, "armer", $"{b}/armer", 8f, false);
		AjouterAnimation(frames, "lancer", $"{b}/lancer", 14f, false);
		AjouterAnimation(frames, "mort", $"{b}/mort", 8f, false);
		return frames;
	}
}
