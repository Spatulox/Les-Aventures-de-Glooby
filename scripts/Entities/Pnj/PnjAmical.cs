using System;
using System.Collections.Generic;
using Godot;

// Base commune à tous les PNJ amicaux (pingouin, lutin...). Une LivingEntity marquée
// FriendlyLivingEntity (donc insensible à tous les dégâts) qui déambule tranquillement
// en va-et-vient sur le sol. Le visuel est pour l'instant un simple carré placeholder
// (Sprite2D) ; le pipeline d'animation réel est prêt mais commenté (voir plus bas),
// à activer quand les vraies frames existeront. Chaque type de PNJ dérive cette classe.
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

	// Carré placeholder. Deviendra un AnimatedSprite2D quand les animations arriveront.
	protected Sprite2D Sprite;

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

		// --- Animations (à activer quand les frames existeront) ---
		// Remplacer le Sprite2D de la scène par un AnimatedSprite2D nommé "AnimatedSprite2D",
		// puis :
		//   var anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		//   anim.SpriteFrames = ConstruireAnimations();
		//   anim.Play("idle");
		// ConstruireAnimations() est fourni (commenté) par chaque sous-classe et charge les
		// dossiers res://assets/pnj/<nom>/{idle,marche} comme le fait Boss.AjouterAnimation.
	}

	// Hook d'init des sous-classes (récupération de nœuds, état de départ...).
	protected virtual void Initialiser() { }

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
		}

		Velocity = velocite;
		MoveAndSlide();

		// --- Animation (à activer avec le pipeline ci-dessus) ---
		// anim.Play(Mathf.Abs(velocite.X) > 1f ? "marche" : "idle");
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
