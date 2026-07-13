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
}
