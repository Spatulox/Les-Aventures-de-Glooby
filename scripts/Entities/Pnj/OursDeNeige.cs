using Godot;

// PNJ méchant « Ours de neige » : patrouille tranquillement, mais dès que le joueur entre dans sa
// portée il se rue sur lui à vive allure (et le blesse au contact via la ZoneContact héritée).
// Frames chargées depuis res://assets/pnj/ours_de_neige/{idle,marche} dans l'AnimatedSprite2D de la
// scène (invisible tant que les dossiers restent vides).
public partial class OursDeNeige : PnjMechant
{
	// Vitesse horizontale de la ruée (bien plus rapide que la patrouille).
	[Export] public float VitesseCharge = 140f;

	// À portée : l'ours de neige se précipite vers le joueur ; sinon il reprend sa patrouille.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		if (joueur == null || distance > PorteeDetection)
		{
			Patrouiller(dt, ref velocite);
			return;
		}

		int direction = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		velocite.X = direction * VitesseCharge;
	}

	// Animations de l'ours de neige depuis res://assets/pnj/ours_de_neige/{idle,marche}. Dossiers
	// encore vides : ConstruireAnimations renvoie des animations sans frame (ours de neige invisible)
	// jusqu'à ce que les PNG y soient déposés.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/ours_de_neige/idle", 6f, true);
		AjouterAnimation(frames, "marche", "res://assets/pnj/ours_de_neige/marche", 8f, true);
		return frames;
	}
}
