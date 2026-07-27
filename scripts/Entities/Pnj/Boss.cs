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

	// Hook d'init des sous-classes (récupération de nœuds, RNG, état/anim de départ...).
	protected virtual void Initialiser() { }

	// Chaque boss fournit son jeu d'animations (doit inclure AnimationMort).
	protected abstract SpriteFrames ConstruireAnimations();

	// Nom de l'animation jouée à la mort (surchargeable).
	protected virtual string AnimationMort => "vaincu";

	// Séquence de mort animée : joue l'anim de mort, coupe la physique et la collision,
	// puis délègue à la base (marque vaincu, stoppe et émet Vaincu).
	protected override void Mourir()
	{
		Sprite.Play(AnimationMort);
		SetPhysicsProcess(false);
		GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		base.Mourir();
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
