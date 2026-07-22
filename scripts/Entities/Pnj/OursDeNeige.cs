using Godot;

// PNJ méchant « Fonceur » : patrouille tranquillement, mais dès que le joueur entre dans sa
// portée il se rue sur lui à vive allure (et le blesse au contact via la ZoneContact héritée).
// Frames chargées depuis res://assets/pnj/fonceur/{idle,marche} dans l'AnimatedSprite2D de la
// scène (invisible tant que les dossiers restent vides).
public partial class Fonceur : PnjMechant
{
	// Vitesse horizontale de la ruée (bien plus rapide que la patrouille).
	[Export] public float VitesseCharge = 140f;

	// À portée : le fonceur se précipite vers le joueur ; sinon il reprend sa patrouille.
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

	// Animations du fonceur depuis res://assets/pnj/fonceur/{idle,marche}. Dossiers encore
	// vides : ConstruireAnimations renvoie des animations sans frame (fonceur invisible)
	// jusqu'à ce que les PNG y soient déposés.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/fonceur/idle", 6f, true);
		AjouterAnimation(frames, "marche", "res://assets/pnj/fonceur/marche", 8f, true);
		return frames;
	}
}
