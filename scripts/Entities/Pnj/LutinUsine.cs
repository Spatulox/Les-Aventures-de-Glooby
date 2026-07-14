using Godot;

// PNJ amical « Lutin d'usine » : un lutin du Père Noël affairé à son atelier, planté sur
// place (statique, DistancePatrouille = 0). Comme tout PnjAmical, son dialogue passe par le
// moteur partagé Talkative/DeclencheurDialogue ; il parle au passage du joueur. Visuel via
// l'AnimatedSprite2D de PnjAmical chargé depuis res://assets/pnj/lutin_usine/idle (repli sur
// le carré placeholder tant que le dossier est vide).
public partial class LutinUsine : PnjAmical
{
	// Lutin immobile à son poste de travail.
	protected override void Initialiser()
	{
		DistancePatrouille = 0f;
	}

	// Une seule animation « idle » depuis le dossier de frames (lutin statique, pas de marche).
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/lutin_usine/idle", 4f, true);
		return frames;
	}
}
