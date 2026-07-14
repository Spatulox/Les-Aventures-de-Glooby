using Godot;

// PNJ amical « Père Noël » : le grand patron, planté sur place (statique, DistancePatrouille
// = 0). Comme tout PnjAmical, son dialogue passe par le moteur partagé Talkative/
// DeclencheurDialogue ; il accueille le joueur à son passage. Visuel via l'AnimatedSprite2D
// de PnjAmical chargé depuis res://assets/pnj/pere_noel/idle (repli sur le carré placeholder
// tant que le dossier est vide).
public partial class PereNoel : PnjAmical
{
	// Père Noël immobile.
	protected override void Initialiser()
	{
		DistancePatrouille = 0f;
	}

	// Une seule animation « idle » depuis le dossier de frames (statique, pas de marche).
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/pere_noel/idle", 5f, true);
		return frames;
	}
}
