using Godot;

// Ennemi « Gardien des ronces » (grotte florale) : la sentinelle de base du lieu. Il patrouille
// en va-et-vient tant qu'il est seul, et dès que le joueur entre dans sa portée il marche droit
// sur lui — plus lentement que le joueur, donc semable en courant, mais il ne lâche pas tant
// que sa cible reste à portée. Il blesse au contact (ZoneContact) et se tue normalement (PV),
// contrairement aux ennemis « obstacles » de la banquise (ours, bonhomme) que l'on ne fait
// qu'étourdir.
//
// Frames chargées depuis res://assets/ennemis/grotte_florale/gardien_ronces/{idle,marche,mort}.
public partial class GardienRonces : PnjMechant
{
	// Vitesse de marche quand il a repéré le joueur (volontairement < Speed du joueur : 220).
	[Export] public float VitessePoursuite = 55f;
	// Distance en deçà de laquelle il cesse d'avancer : évite qu'il tremble sur le joueur.
	[Export] public float DistanceArret = 8f;

	// Patrouille hors de portée, marche vers le joueur sinon. La base applique gravité,
	// MoveAndSlide, orientation et animation : on ne décide ici que la vitesse horizontale.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		if (joueur == null || distance > PorteeDetection)
		{
			Patrouiller(dt, ref velocite);
			return;
		}

		float ecart = joueur.GlobalPosition.X - GlobalPosition.X;
		velocite.X = Mathf.Abs(ecart) <= DistanceArret ? 0f : Mathf.Sign(ecart) * VitessePoursuite;
	}

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/grotte_florale/gardien_ronces";
		AjouterAnimation(frames, "idle", $"{b}/idle", 6f, true);
		AjouterAnimation(frames, "marche", $"{b}/marche", 10f, true);
		AjouterAnimation(frames, "mort", $"{b}/mort", 8f, false);
		return frames;
	}
}
