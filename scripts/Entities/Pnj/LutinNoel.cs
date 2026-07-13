using Godot;

// PNJ amical « Lutin du Père Noël » : plus petit que le joueur, présent dans la grotte.
// Ne fait pour l'instant que déambuler (logique héritée de PnjAmical). Visuel = carré
// placeholder vert ; les animations réelles viendront de res://assets/pnj/lutin_noel/.
public partial class LutinNoel : PnjAmical
{
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
