using Godot;

// PNJ amical « Pingouin » : un habitant du village, même gabarit que le joueur. Il déambule
// (logique héritée de PnjAmical) et est un bavard automatique : au passage du joueur, sa
// bulle défile toute seule (sans appui de touche) en piochant une réplique au hasard, et il
// joue son animation « parler » pendant la conversation (repli automatique sur idle sinon).
// Visuel = carré placeholder noir tant que res://assets/pnj/pingouin/{idle,marche,parler} est
// vide ; dès que les frames y sont déposées, l'AnimatedSprite2D prend le relais automatiquement.
public partial class Pingouin : PnjAmical, TalkativeAutomatique
{
	// Délai (secondes) entre deux répliques en défilement automatique.
	[Export] public float IntervalleAuto { get; set; } = 2.5f;

	// Définit le pingouin comme bavard aléatoire (une réplique au hasard à chaque tour).
	protected override void Initialiser()
	{
		Aleatoire = true;
	}

	// Hooks TalkativeAutomatique appelés par le moteur : rien de spécifique pour l'instant
	// (le pingouin pourrait y réagir — changer d'expression, se tourner vers le joueur...).
	public void Incrementer() { }

	public void Cacher() { }

	// Animations du pingouin, chargées depuis res://assets/pnj/pingouin/{idle,marche,parler}.
	// « parler » (fusionnée depuis l'ancien pingouin) est jouée automatiquement par PnjAmical
	// pendant les dialogues. Dossiers vides => PnjAmical garde le carré placeholder.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/pingouin/idle", 6f, true);
		AjouterAnimation(frames, "marche", "res://assets/pnj/pingouin/marche", 8f, true);
		AjouterAnimation(frames, "parler", "res://assets/pnj/pingouin/parler", 7f, true);
		return frames;
	}
}
