using System;
using System.Collections.Generic;
using Godot;

// Base commune à tous les PNJ amicaux (pingouin, lutin...). Une LivingEntity marquée
// FriendlyLivingEntity (donc insensible à tous les dégâts) qui déambule tranquillement
// en va-et-vient sur le sol. Le pipeline d'animation est actif (via AnimationsSprite,
// comme le Player et les Boss) : la scène porte un AnimatedSprite2D « AnimatedSprite2D »
// dont les SpriteFrames sont chargés au démarrage depuis les dossiers pointés par
// ConstruireAnimations() (res://assets/pnj/<nom>/{idle,marche,...}). Tant qu'un dossier
// est vide, l'animation correspondante n'a aucune frame (le PNJ reste invisible) ; dès
// que les PNG y sont déposés, ils s'affichent sans autre changement. Chaque type de PNJ
// dérive cette classe.
//
// Tout PNJ est aussi Talkative : la capacité est portée par la classe, mais c'est
// l'instance qui décide si elle parle. Un PNJ est bavard uniquement si on lui renseigne
// des Lignes ET qu'on lui ajoute un DeclencheurDialogue enfant (comme PanneauBois) ;
// sinon il reste muet. Il s'immobilise pendant une conversation.
public abstract partial class PnjAmical : LivingEntity, FriendlyLivingEntity, OllamaTalkative
{
	// ---- Déambulation (réglages) ----
	[Export] public float DistancePatrouille = 60f; // amplitude du va-et-vient autour du point de départ
	[Export] public float VitessePatrouille = 30f;  // vitesse horizontale de marche
	[Export] public float TempsPause = 1.2f;         // pause à chaque extrémité

	// Sens dans lequel l'art du PNJ est dessiné, avant tout miroir. Les sprites PNJ du projet
	// (ex. pingouin) regardent à gauche par défaut ; passer à true pour un art tourné à droite.
	[Export] public bool ArtRegardeADroite;

	// AnimatedSprite2D de la scène : ses SpriteFrames sont chargés au démarrage (comme Boss).
	protected AnimatedSprite2D Sprite;

	private float _xDepart;
	private int _direction = 1;   // 1 = vers la droite, -1 = vers la gauche
	private float _minuteurPause;
	private bool _enConversation;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AppliquerCollisionsPnj();
		MasquerApercuEditeur();
		Pv = PvMax;
		AddToGroup("pnj");
		_xDepart = GlobalPosition.X;
		Initialiser();

		// Charge les animations dans l'AnimatedSprite2D de la scène puis lance l'idle
		// (sans effet tant que le dossier idle est vide : le PNJ reste alors invisible).
		Sprite.SpriteFrames = ConstruireAnimations();
		if (Sprite.SpriteFrames.GetFrameCount("idle") > 0)
			Sprite.Play("idle");
	}

	// Hook d'init des sous-classes (récupération de nœuds, état de départ...).
	protected virtual void Initialiser() { }

	// Construit les animations du PNJ (idle, marche...) via AnimationsSprite, en pointant
	// vers res://assets/pnj/<nom>/{idle,marche}. Fournie par chaque sous-classe ; peut
	// pointer vers des dossiers vides (aucune frame => animation vide, PNJ invisible).
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
		else if (DistancePatrouille <= 0f)
		{
			// PNJ statique (gréviste tenant sa pancarte, Père Noël...) : aucune
			// déambulation, il reste planté sur son point de départ en idle.
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

			// Miroir selon le sens de marche ET l'orientation native de l'art : un art tourné
			// à gauche doit être retourné pour aller à droite (et inversement).
			Sprite.FlipH = ArtRegardeADroite ? _direction < 0 : _direction > 0;
		}

		Velocity = velocite;
		MoveAndSlide();

		// Anime le PNJ selon son état.
		Sprite.Play(ChoisirAnimation(velocite));
	}

	// Choisit l'animation à jouer en se limitant à ce que le jeu de frames fournit vraiment :
	// "parler" pendant une conversation si elle a des frames (ex. pingouin qui bavarde), sinon
	// "marche" quand le PNJ avance et qu'un dossier marche non vide existe, à défaut "idle".
	// Ce repli évite de jouer une animation vide pour les PNJ statiques (sans marche/parler).
	private string ChoisirAnimation(Vector2 velocite)
	{
		var frames = Sprite.SpriteFrames;
		if (_enConversation && frames.GetFrameCount("parler") > 0)
			return "parler";
		if (Mathf.Abs(velocite.X) > 1f && frames.GetFrameCount("marche") > 0)
			return "marche";
		return "idle";
	}

	// ---- Dialogue (Talkative) ----
	// Répliques du PNJ (vide = muet). Renseignées au cas par cas dans monde.tscn : c'est
	// ce qui distingue un PNJ bavard d'un PNJ muet, tous deux de la même classe.
	[Export] public string[] Lignes { get; set; } = Array.Empty<string>();

	// Ancrage (local) de la bulle au-dessus de la tête du PNJ.
	[Export] public Vector2 AncrageBulle = new(0f, -30f);

	// Vrai : afficher UNE seule réplique tirée au hasard au lieu de tout faire défiler.
	[Export] public bool Aleatoire { get; set; }

	// Vrai : le dialogue démarre au simple passage du joueur (sinon : sur la touche).
	[Export] public bool AuPassage;

	// Vrai : dialogue à usage unique pour toute la partie (mémorisé via GameState).
	[Export] public bool UneSeuleFois;

	// Identifiant persistant du dialogue (requis si UneSeuleFois ; unique dans le jeu).
	[Export] public string IdDialogue = "";

	// ---- Dialogue dynamique (OllamaTalkative) ----
	// Opt-in : ce PNJ génère-t-il sa réplique à la volée via le LLM local ? Décoché (ou
	// Ollama indisponible), il garde ses Lignes statiques ci-dessus (repli silencieux).
	[Export] public bool DialogueDynamique;

	// Contexte/personnalité PROPRE au PNJ, combiné au contexte global par OllamaService.
	[Export(PropertyHint.MultilineText)] public string Contexte { get; set; } = "";

	// Amorce fixe envoyée au modèle (le joueur ne saisit rien : l'invite lance la génération).
	// Volontairement NEUTRE sur le ton ET sans salutation imposée : c'est le Contexte (rôle) du
	// PNJ qui décide s'il est aimable, ronchon, timide… et s'il salue ou entre dans le vif.
	[Export(PropertyHint.MultilineText)] public string Invite { get; set; } = "Dis une courte réplique dans ton caractère ; saluer Glooby n'est pas obligatoire.";

	// Longueur cible de la réplique générée (nombre de mots moyen). Petit = PNJ laconique,
	// grand = PNJ bavard. Borne aussi la longueur côté modèle (voir OllamaService.GenererFlux).
	[Export] public int MotMoyenParReponse { get; set; } = 10;

	// Dynamique réellement actif seulement si l'opt-in est coché ET qu'Ollama est prêt.
	public bool DialogueDynamiqueActif => DialogueDynamique && OllamaService.Instance is { Disponible: true };

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
