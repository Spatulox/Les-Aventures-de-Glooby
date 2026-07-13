using Godot;

// PNJ amical « Pingouin » : un habitant du village, même gabarit que le joueur. Ne fait
// pour l'instant que déambuler (logique héritée de PnjAmical). Visuel = carré placeholder
// noir ; les animations réelles viendront de res://assets/pnj/pingouin/.
public partial class Pingouin : PnjAmical
{
	// --- Animations (à décommenter quand les frames existeront) ---
	// protected SpriteFrames ConstruireAnimations()
	// {
	//     var frames = new SpriteFrames();
	//     frames.RemoveAnimation("default");
	//     AjouterAnimation(frames, "idle", "res://assets/pnj/pingouin/idle", 6f, true);
	//     AjouterAnimation(frames, "marche", "res://assets/pnj/pingouin/marche", 8f, true);
	//     return frames;
	// }
}
