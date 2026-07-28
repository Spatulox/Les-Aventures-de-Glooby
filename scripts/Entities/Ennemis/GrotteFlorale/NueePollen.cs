using Godot;

// Ennemi « Nuée de pollen » (grotte florale) : le seul VOLANT du bestiaire (SubitGravite =
// false, il pilote lui-même sa vitesse verticale). Au repos il dérive en va-et-vient autour de
// son point de départ en ondulant doucement ; quand le joueur entre dans sa portée il fond sur
// lui en diagonale, lentement mais sans se soucier du relief — c'est un ennemi qui force à
// bouger plutôt qu'un mur. Il blesse au contact (ZoneContact) et crève au moindre coup
// (il garde le PvMax de référence, 3 : une boule de neige l'abat).
//
// Frames : res://assets/ennemis/grotte_florale/nuee_pollen/{vol,mort}. Il n'a pas d'idle ni de
// marche : « vol » sert des deux, d'où la surcharge de MettreAJourAnimation.
public partial class NueePollen : PnjMechant
{
	// Vitesse de dérive vers le joueur une fois repéré (bien en deçà du Speed du joueur).
	[Export] public float VitesseVol = 50f;
	// Amplitude (px) et rythme (aller-retour par seconde) de l'ondulation au repos.
	[Export] public float AmplitudeOndulation = 12f;
	[Export] public float VitesseOndulation = 1.2f;

	// Altitude de croisière : hauteur autour de laquelle la nuée ondule quand elle n'a pas de
	// proie. Mémorisée au départ, comme le point d'ancrage de la patrouille horizontale.
	private float _yDepart;
	private float _phase;

	// Volant : la gravité ne s'applique pas, la nuée impose sa propre vitesse verticale.
	protected override bool SubitGravite => false;

	protected override void Initialiser()
	{
		_yDepart = GlobalPosition.Y;
	}

	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		if (joueur != null && distance <= PorteeDetection)
		{
			// Fond sur le joueur en ligne droite, à vitesse constante dans les deux axes.
			velocite = GlobalPosition.DirectionTo(joueur.GlobalPosition) * VitesseVol;
			return;
		}

		// Sans proie : va-et-vient horizontal de la base, plus une ondulation qui ramène la
		// nuée vers son altitude de croisière (la vitesse verticale suit l'écart à viser).
		Patrouiller(dt, ref velocite);
		_phase += dt * VitesseOndulation * Mathf.Tau;
		float altitudeVisee = _yDepart + Mathf.Sin(_phase) * AmplitudeOndulation;
		velocite.Y = (altitudeVisee - GlobalPosition.Y) * 4f;
	}

	// Un seul cycle d'animation pour tous ses états de vie.
	protected override void MettreAJourAnimation(Vector2 velocite) => JouerSiPresente("vol");

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/grotte_florale/nuee_pollen";
		AjouterAnimation(frames, "vol", $"{b}/vol", 8f, true);
		AjouterAnimation(frames, "mort", $"{b}/mort", 10f, false);
		return frames;
	}
}
