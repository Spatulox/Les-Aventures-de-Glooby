using Godot;

// PNJ amical « Lutin d'usine » : un lutin du Père Noël affairé à son atelier, planté sur
// place (statique, DistancePatrouille = 0). Comme tout PnjAmical, son dialogue passe par le
// moteur partagé Talkative/DeclencheurDialogue ; il parle au passage du joueur. Visuel via
// l'AnimatedSprite2D de la scène chargé depuis res://assets/pnj/lutin_usine/<pose>.
//
// Deux poses au choix par instance (comme LutinCgt) : à son établi, ou un paquet dans les
// bras. Elles ne changent que le dossier de frames — le lutin est immobile dans les deux
// cas, il n'y a donc toujours qu'une seule animation « idle ».
public partial class LutinUsine : PnjAmical
{
	public enum PoseLutinUsine { Etabli, Paquet }

	[Export] public PoseLutinUsine Pose { get; set; } = PoseLutinUsine.Etabli;

	// Lutin immobile à son poste de travail.
	protected override void Initialiser()
	{
		DistancePatrouille = 0f;
	}

	// Une seule animation « idle », prise dans le dossier de la pose choisie.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", DossierPose, 4f, true);
		return frames;
	}

	private string DossierPose => Pose == PoseLutinUsine.Paquet
		? "res://assets/pnj/lutin_usine/paquet"
		: "res://assets/pnj/lutin_usine/idle";
}
