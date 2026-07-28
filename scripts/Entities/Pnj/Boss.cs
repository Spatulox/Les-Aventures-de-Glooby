using Godot;

// Base commune à tous les boss : une LivingEntity animée (AnimatedSprite2D) qui charge
// ses animations par dossier et joue une séquence de mort animée. Les PV, l'encaissement
// des dégâts et les aides de déplacement viennent de LivingEntity ; l'IA, les patterns et
// le contenu (animations, nombres de PV/dégâts) sont fournis par chaque sous-classe
// (ex. BossCerf) via ConstruireAnimations()/Initialiser().
public abstract partial class Boss : LivingEntity
{
	protected AnimatedSprite2D Sprite;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AppliquerCollisionsPnj();
		MasquerApercuEditeur();
		Sprite.SpriteFrames = ConstruireAnimations();
		Pv = PvMax;
		Initialiser();
	}

	// ---- Combat en deux phases ----
	// Schéma commun aux boss du jeu : au premier coup qui fait passer les PV sous
	// SeuilPhase2, le boss « s'énerve » (animation de transition) et durcit ses patterns.
	// Mutualisé ici parce que les trois boss (Cerf, Lutin Mecha, Père Noël) le rejouaient
	// à l'identique ; chacun ne garde que ce qui lui est propre (l'animation et le
	// durcissement), via BasculeEnPhase2().

	// Fraction de PvMax en deçà de laquelle la phase 2 se déclenche.
	[Export] public float SeuilPhase2 = 0.5f;

	// Phase courante du combat (1 puis 2). En lecture seule pour l'extérieur (barre de vie,
	// zone d'arène) et pour les sous-classes, qui pondèrent leurs patterns dessus.
	public int Phase { get; protected set; } = 1;

	// À appeler depuis ApresDegats. Renvoie vrai UNE SEULE FOIS, à l'instant exact de la
	// bascule, pour que la sous-classe y branche son animation et ses réglages de phase 2.
	protected bool BasculeEnPhase2()
	{
		if (Phase != 1 || Pv > Mathf.CeilToInt(PvMax * SeuilPhase2))
			return false;

		Phase = 2;
		return true;
	}

	// ---- Portée du joueur (zones d'engagement) ----
	// Un boss ne tire pas ses attaques au hasard : il joue ce qui PORTE. Deux Area2D enfants
	// FACULTATIVES, redimensionnables par instance dans l'éditeur, découpent l'espace autour
	// de lui et pilotent la pondération de ses patterns :
	//   ZoneCorpsACorps — le joueur est collé : les attaques de contact priment ;
	//   ZoneDistance    — l'anneau utile de ses attaques à distance ;
	//   hors des deux   — plus rien ne porte, le boss se rapproche au lieu d'attaquer.
	// Mutualisé ici parce que le Lutin Mecha et le Père Noël en font le même usage ; ce qui
	// reste propre à chacun, c'est la table de poids et la façon de se rapprocher (marche,
	// bond, cheminée). Opt-in : un boss qui frappe à portée unique (le Cerf) n'appelle rien.
	protected const string NomZoneCorpsACorps = "ZoneCorpsACorps";
	protected const string NomZoneDistance = "ZoneDistance";

	// Rayons de REPLI, utilisés seulement quand la scène ne porte pas les deux zones : un boss
	// dont le .tscn n'a pas encore été outillé garde un comportement correct, simplement moins
	// finement réglable qu'avec des formes dessinées.
	[Export] public float RayonCorpsACorps = 110f;
	[Export] public float RayonDistance = 340f;

	// Vrai quand les DEUX zones ont été trouvées : il en manque une et le découpage n'a plus
	// de sens, on retombe alors entièrement sur les rayons.
	private bool _zonesEngagementCablees;

	// À appeler depuis Initialiser() par un boss qui pondère ses attaques selon l'éloignement.
	protected void CablerZonesEngagement()
	{
		bool corpsACorps = CablerZonePresence(NomZoneCorpsACorps);
		bool distance = CablerZonePresence(NomZoneDistance);
		_zonesEngagementCablees = corpsACorps && distance;
	}

	// La lecture du terrain : les zones font foi, les rayons ne servent que de repli.
	protected PorteeJoueur EvaluerPortee()
	{
		if (_zonesEngagementCablees)
		{
			if (JoueurDansZone(NomZoneCorpsACorps) != null)
				return PorteeJoueur.CorpsACorps;
			return JoueurDansZone(NomZoneDistance) != null ? PorteeJoueur.Distance : PorteeJoueur.HorsPortee;
		}

		var joueur = JoueurLePlusProche(out float ecart);
		if (joueur == null)
			return PorteeJoueur.HorsPortee;
		if (ecart <= RayonCorpsACorps)
			return PorteeJoueur.CorpsACorps;
		return ecart <= RayonDistance ? PorteeJoueur.Distance : PorteeJoueur.HorsPortee;
	}

	// ---- Salve de projectiles visée ----
	// Les deux boss de l'usine (Lutin Mecha, Père Noël) tirent le MÊME EclatGlace de la même
	// façon ; leur tir est donc mutualisé ici. Rien n'y est propre à un boss : c'est « lâche
	// ce projectile-là depuis mon milieu, dans l'axe du joueur ». Opt-in — un boss qui ne
	// tire pas (le Cerf) n'appelle simplement rien de tout ça.

	// Avancée du point de tir devant le boss, dans le sens du regard : sans elle le
	// projectile naîtrait au beau milieu du torse.
	[Export] public float AvanceeTir = 30f;

	// Phase 2 : délai entre les deux projectiles de la salve. Ils partent À LA SUITE et non
	// ensemble — un joueur qui a esquivé le premier doit encore lire le second, qui est
	// re-visé au moment où il part.
	[Export] public float DelaiSecondTir = 0.22f;

	// Le MILIEU du boss, en global. L'AnimatedSprite2D est posé à moins sa demi-hauteur
	// (convention des scènes de boss), donc sa position locale EST le centre du corps.
	// Passer par ToGlobal plutôt qu'ajouter à GlobalPosition tient compte de l'échelle de
	// l'instance : un boss agrandi tire depuis son milieu à lui, pas depuis un décalage fixe.
	protected Vector2 PointDeTir(int direction)
		=> ToGlobal(Sprite.Position + new Vector2(direction * AvanceeTir, 0f));

	// La salve complète : UN projectile en phase 1, DEUX à la suite en phase 2.
	protected void TirerSalveVisee(PackedScene scene, int direction, float vitesse)
	{
		if (scene == null)
			return;

		TirerProjectileVise(scene, direction, vitesse);

		if (Phase < 2)
			return;

		GetTree().CreateTimer(DelaiSecondTir).Timeout += () =>
		{
			// Le boss a pu tomber — ou être libéré — pendant le délai.
			if (IsInstanceValid(this) && !EstVaincu)
				TirerProjectileVise(scene, direction, vitesse);
		};
	}

	// Un projectile lâché du milieu du boss, DANS L'AXE DU JOUEUR. L'éclat vole en ligne
	// droite (sa scène règle Gravite = 0) : tiré à plat, il filait sur toute la longueur de
	// la salle sans jamais menacer un joueur qui n'était pas exactement à sa hauteur — il
	// faut le viser. L'orientation en vol suit la vitesse, c'est la base Projectile qui s'en
	// charge, et la surcharge vectorielle d'Initialiser conserve la norme du tir.
	protected void TirerProjectileVise(PackedScene scene, int direction, float vitesse)
	{
		var origine = PointDeTir(direction);
		var projectile = scene.Instantiate<Projectile>();
		projectile.Initialiser(this, DirectionVersJoueur(origine, direction) * vitesse);
		projectile.GlobalPosition = origine;
		GetParent().AddChild(projectile);
	}

	// Ligne de visée unitaire, du point de tir vers le joueur. Repli sur l'horizontale dans
	// le sens du regard quand il n'y a pas de joueur en scène, ou qu'il est pile sur le
	// point de tir : un vecteur nul normalisé enverrait le projectile n'importe où.
	protected Vector2 DirectionVersJoueur(Vector2 origine, int directionRepli)
	{
		var joueur = JoueurLePlusProche(out _);
		if (joueur == null)
			return new Vector2(directionRepli, 0f);

		var vers = joueur.GlobalPosition - origine;
		return vers.LengthSquared() < 1f ? new Vector2(directionRepli, 0f) : vers.Normalized();
	}

	// Hook d'init des sous-classes (récupération de nœuds, RNG, état/anim de départ...).
	protected virtual void Initialiser() { }

	// Chaque boss fournit son jeu d'animations (doit inclure AnimationMort).
	protected abstract SpriteFrames ConstruireAnimations();

	// Nom de l'animation jouée à la mort (surchargeable).
	protected virtual string AnimationMort => "vaincu";

	// Objet lâché sur place à la mort du boss (le pantalon du Père Noël...). Le butin
	// appartient au boss et non à l'arène : deux boss qui partagent la même salle ne
	// lâchent pas forcément la même chose, et le régler ici évite de le dupliquer sur
	// chaque zone. Vide = le boss ne lâche rien.
	[Export] public PackedScene Butin;

	// Effacement du corps après la mise à mort. Délai avant que le vaincu ne s'efface,
	// puis durée du fondu. 0 (défaut) = le corps RESTE sur place : c'est ce que veulent
	// les boss dotés d'une vraie animation « vaincu », qui se figent dans leur dernière
	// frame. À renseigner pour ceux dont la mort est procédurale, qui laisseraient sinon
	// un sprite écrasé et translucide traîner dans l'arène.
	//
	// Sans effet sur le butin : LacherButin le pose en FRÈRE du boss, il survit donc à
	// l'effacement.
	[Export] public float DelaiEffacement;
	[Export] public float DureeEffacement = 0.8f;

	// Séquence de mort animée : joue l'anim de mort, coupe la physique et la collision,
	// lâche le butin, puis délègue à la base (marque vaincu, stoppe et émet Vaincu).
	protected override void Mourir()
	{
		Sprite.Play(AnimationMort);
		SetPhysicsProcess(false);
		GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		LacherButin();
		base.Mourir();
		ProgrammerEffacement();
	}

	// Efface le corps après DelaiEffacement, le temps que la mise à mort se joue. Le
	// délai laisse aussi passer tout ce qui s'accroche à Vaincu (persistance, épilogue
	// d'arène) : ces abonnés ont déjà été notifiés au moment où le nœud disparaît, et
	// ceux de ZoneBoss vérifient IsInstanceValid avant de le relire.
	private void ProgrammerEffacement()
	{
		if (DelaiEffacement <= 0f)
			return;

		GetTree().CreateTimer(DelaiEffacement).Timeout += () =>
		{
			if (IsInstanceValid(this))
				Effets.Disparaitre(this, Scale, DureeEffacement);
		};
	}

	// Dépose le butin à l'endroit exact où le boss est tombé, en frère de lui-même :
	// il reste donc en place quand le boss est libéré. L'ajout est DIFFÉRÉ, le coup
	// fatal arrivant en général d'un contact, donc en plein flush des requêtes
	// physiques, où Godot refuse d'ajouter un nœud portant une forme de collision.
	private void LacherButin()
	{
		if (Butin == null || GetParent() == null)
			return;

		var objet = Butin.Instantiate<Node2D>();
		objet.Position = Position;
		GetParent().CallDeferred(Node.MethodName.AddChild, objet);
	}

	// Helper générique : ajoute à un SpriteFrames une animation depuis un dossier de
	// PNG (triés par nom). Réutilisable par tous les boss et PNJ. Simple façade
	// « depuis un dossier » au-dessus de AnimationsSprite (chargement + registre
	// partagés avec le Player et l'UI).
	protected static void AjouterAnimation(SpriteFrames frames, string nom, string dossier, float fps, bool boucle)
	{
		AnimationsSprite.EnregistrerAnimation(frames, nom, AnimationsSprite.ChargerFrames(dossier), fps, boucle);
	}
}
