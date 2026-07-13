using Godot;

// PNJ amical « Lutin du Père Noël » : plus petit que le joueur, présent dans la grotte.
// Il déambule (logique héritée de PnjAmical) et est un bavard automatique : au passage du
// joueur, sa bulle défile toute seule (sans appui de touche) en piochant une réplique au
// hasard. Visuel = carré placeholder vert ; les animations réelles viendront de
// res://assets/pnj/lutin_noel/.
public partial class LutinNoel : PnjAmical, TalkativeAutomatique
{
	// Délai (secondes) entre deux répliques en défilement automatique.
	[Export] public float IntervalleAuto { get; set; } = 2.5f;

	// Définit le lutin comme bavard aléatoire (une réplique au hasard à chaque tour).
	protected override void Initialiser()
	{
		Aleatoire = true;
	}

	// Hooks TalkativeAutomatique appelés par le moteur : rien de spécifique pour l'instant
	// (le lutin pourrait y réagir — changer d'expression, se tourner vers le joueur...).
	public void Incrementer() { }

	public void Cacher() { }

	// --- Animations (à décommenter quand les frames existeront) ---
	// protected SpriteFrames ConstruireAnimations()
	// {
	//     var frames = new SpriteFrames();
	//     frames.RemoveAnimation("default");
	//     AjouterAnimation(frames, "idle", "res://assets/pnj/lutin_noel/idle", 6f, true);
	//     AjouterAnimation(frames, "marche", "res://assets/pnj/lutin_noel/marche", 8f, true);
	//     return frames;
	// }
}
