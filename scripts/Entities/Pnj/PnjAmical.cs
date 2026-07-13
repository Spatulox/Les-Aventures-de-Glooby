using System;
using System.Collections.Generic;
using Godot;

// Base commune à tous les PNJ amicaux (pingouin, lutin...). Une LivingEntity marquée
// FriendlyLivingEntity (donc insensible à tous les dégâts) qui déambule tranquillement
// en va-et-vient sur le sol. Le pipeline d'animation est actif (via AnimationsSprite,
// comme le Player et les Boss) : chaque sous-classe pointe ConstruireAnimations() vers
// ses dossiers de frames. Tant que ces dossiers sont vides, aucune frame n'est chargée
// et on retombe automatiquement sur le carré placeholder (Sprite2D) ; dès que les PNG
// existeront, l'AnimatedSprite2D prend le relais sans autre changement. Chaque type de
// PNJ dérive cette classe.
//
// Tout PNJ est aussi Talkative : la capacité est portée par la classe, mais c'est
// l'instance qui décide si elle parle. Un PNJ est bavard uniquement si on lui renseigne
// des Lignes ET qu'on lui ajoute un DeclencheurDialogue enfant (comme PanneauBavard) ;
// sinon il reste muet. Il s'immobilise pendant une conversation.
public abstract partial class PnjAmical : LivingEntity, FriendlyLivingEntity, Talkative
{
	// ---- Déambulation (réglages) ----
	[Export] public float DistancePatrouille = 60f; // amplitude du va-et-vient autour du point de départ
	[Export] public float VitessePatrouille = 30f;  // vitesse horizontale de marche
	[Export] public float TempsPause = 1.2f;         // pause à chaque extrémité

	// Carré placeholder, affiché tant qu'aucune frame d'animation n'est disponible.
	protected Sprite2D Sprite;

	// AnimatedSprite2D construit à la volée quand ConstruireAnimations() fournit de
	// vraies frames ; reste null tant que les dossiers d'assets sont vides (repli carré).
	private AnimatedSprite2D _anim;

	private float _xDepart;
	private int _direction = 1;   // 1 = vers la droite, -1 = vers la gauche
	private float _minuteurPause;
	private bool _enConversation;

	public override void _Ready()
	{
		Sprite = GetNode<Sprite2D>("Sprite2D");
		Pv = PvMax;
		AddToGroup("pnj");
		_xDepart = GlobalPosition.X;
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

	// Construit les animations du PNJ (idle, marche...) via AnimationsSprite, en pointant
	// vers res://assets/pnj/<nom>/{idle,marche}. Fournie par chaque sous-classe ; peut
	// pointer vers des dossiers vides (aucune frame => carré placeholder conservé).
	protected abstract SpriteFrames ConstruireAnimations();

	// Ajoute une animation à un SpriteFrames depuis un dossier de PNG (façade partagée
	// avec les boss au-dessus de AnimationsSprite). Réutilisable par toutes les sous-classes.
	protected static void AjouterAnimation(SpriteFrames frames, string nom, string dossier, float fps, bool boucle)
	{
		AnimationsSprite.EnregistrerAnimation(frames, nom, AnimationsSprite.ChargerFrames(dossier), fps, boucle);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		var velocite = Velocity;

		AppliquerGravite(ref velocite, dt);

		if (_enConversation)
		{
			// En pleine conversation : le PNJ reste immobile face au joueur.
			velocite.X = 0f;
		}
		else if (_minuteurPause > 0f)
		{
			// En pause à une extrémité : on s'arrête horizontalement.
			_minuteurPause -= dt;
			velocite.X = 0f;
		}
		else
		{
			velocite.X = _direction * VitessePatrouille;

			// Inversion + pause quand on atteint une extrémité du trajet.
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

			Sprite.FlipH = _direction < 0;
			if (_anim != null)
				_anim.FlipH = _direction < 0;
		}

		Velocity = velocite;
		MoveAndSlide();

		// Anime le PNJ selon qu'il marche ou non (sans effet tant que _anim est null).
		if (_anim != null)
			_anim.Play(Mathf.Abs(velocite.X) > 1f ? "marche" : "idle");
	}

	// ---- Dialogue (Talkative) ----
	// Répliques du PNJ (vide = muet). Renseignées au cas par cas dans monde.tscn : c'est
	// ce qui distingue un PNJ bavard d'un PNJ muet, tous deux de la même classe.
	[Export] public string[] Lignes { get; set; } = Array.Empty<string>();

	// Ancrage (local) de la bulle au-dessus de la tête du carré placeholder.
	[Export] public Vector2 AncrageBulle = new(0f, -30f);

	// Vrai : afficher UNE seule réplique tirée au hasard au lieu de tout faire défiler.
	[Export] public bool Aleatoire { get; set; }

	// Vrai : le dialogue démarre au simple passage du joueur (sinon : sur la touche).
	[Export] public bool AuPassage;

	// Vrai : dialogue à usage unique pour toute la partie (mémorisé via GameState).
	[Export] public bool UneSeuleFois;

	// Identifiant persistant du dialogue (requis si UneSeuleFois ; unique dans le jeu).
	[Export] public string IdDialogue = "";

	public IReadOnlyList<string> Dialogue => Lignes;

	public Vector2 PointBulle => ToGlobal(AncrageBulle);

	public bool DeclencheAuPassage => AuPassage;

	public bool PeutParler()
	{
		if (UneSeuleFois && !string.IsNullOrEmpty(IdDialogue))
			return !GameState.Instance.EstConsomme(IdDialogue);
		return true;
	}

	// Début de conversation : le PNJ s'immobilise et fait face au joueur.
	public void SurDebutDialogue() => _enConversation = true;

	// Fin de conversation : le PNJ reprend sa déambulation ; mémorise le dialogue à usage unique.
	public void SurFinDialogue()
	{
		_enConversation = false;
		if (UneSeuleFois && !string.IsNullOrEmpty(IdDialogue))
			GameState.Instance.MarquerConsomme(IdDialogue);
	}
}
