using Godot;

// Ennemi « Fleur carnivore » (grotte florale) : plante enracinée qui embusque. Au repos elle
// reste refermée (idle) ; quand le joueur entre dans sa portée elle s'ouvre (télégraphe visible
// : c'est la fenêtre pour reculer), puis claque des mâchoires — la morsure ne blesse que les
// joueurs présents dans son Area2D « ZoneMorsure » au moment du claquement. Elle se referme
// ensuite et récupère le temps de CadenceMorsure.
//
// Elle ne se déplace jamais : toute la mise en scène passe par les animations, pilotées par
// cette machine à états (d'où la surcharge de MettreAJourAnimation, qui neutralise le choix
// idle/marche de la base). Elle meurt normalement de ses PV (animation « mort » de la base).
//
// Frames : res://assets/ennemis/grotte_florale/fleur_carnivore/{idle,ouverture,morsure,mort} —
// la fermeture réutilise les frames d'ouverture jouées à l'envers (aucun asset supplémentaire).
public partial class FleurCarnivore : PnjMechant
{
	private enum EtatFleur { Repos, Ouverture, Morsure, Fermeture }

	// Délai de récupération après une morsure, pendant lequel elle reste refermée.
	[Export] public float CadenceMorsure = 1.5f;

	private EtatFleur _etat = EtatFleur.Repos;
	private float _recharge;
	private Area2D _zoneMorsure;

	protected override void Initialiser()
	{
		_zoneMorsure = GetNodeOrNull<Area2D>("ZoneMorsure");
		// Un seul abonnement pour toute la vie de la fleur : l'enchaînement des états se lit
		// dans SurFinAnimation (ouverture -> morsure -> fermeture -> repos).
		Sprite.AnimationFinished += SurFinAnimation;
	}

	// Enracinée : la vitesse horizontale reste nulle, on ne décide ici que du déclenchement
	// de l'embuscade.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		velocite.X = 0f;

		if (_recharge > 0f)
			_recharge -= dt;

		if (_etat != EtatFleur.Repos || _recharge > 0f || joueur == null || distance > PorteeDetection)
			return;

		// Elle tourne sa tête vers sa proie avant de s'ouvrir.
		DefinirOrientation(joueur.GlobalPosition.X < GlobalPosition.X);
		Enchainer(EtatFleur.Ouverture, "ouverture");
	}

	// Fin d'une animation non bouclée : passe à l'étape suivante de la morsure.
	private void SurFinAnimation()
	{
		// Une fois vaincue, c'est l'animation de mort qui joue : la séquence de morsure
		// ne doit surtout pas reprendre la main dessus.
		if (EstVaincu)
			return;

		switch (_etat)
		{
			case EtatFleur.Ouverture:
				// Les mâchoires claquent : seuls les joueurs à portée de gueule sont touchés.
				BlesserJoueursDansZone(_zoneMorsure);
				Enchainer(EtatFleur.Morsure, "morsure");
				break;

			case EtatFleur.Morsure:
				Enchainer(EtatFleur.Fermeture, "fermeture");
				break;

			case EtatFleur.Fermeture:
				_etat = EtatFleur.Repos;
				_recharge = CadenceMorsure;
				JouerSiPresente("idle");
				break;
		}
	}

	// Entre dans un état et joue son animation. Si le dossier de frames est vide, l'étape est
	// franchie immédiatement pour que la séquence ne reste jamais bloquée.
	private void Enchainer(EtatFleur etat, string animation)
	{
		_etat = etat;
		if (!JouerSiPresente(animation))
			SurFinAnimation();
	}

	// L'animation est entièrement pilotée par la machine à états ci-dessus.
	protected override void MettreAJourAnimation(Vector2 velocite) { }

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/grotte_florale/fleur_carnivore";
		AjouterAnimation(frames, "idle", $"{b}/idle", 5f, true);
		AjouterAnimation(frames, "ouverture", $"{b}/ouverture", 12f, false);
		AjouterAnimation(frames, "morsure", $"{b}/morsure", 14f, false);
		AjouterAnimation(frames, "mort", $"{b}/mort", 8f, false);
		// Se refermer = s'ouvrir à l'envers : on dérive l'animation au lieu d'en générer une.
		AnimationsSprite.EnregistrerAnimation(frames, "fermeture", AnimationsSprite.ChargerFrames($"{b}/ouverture"), 10f, false, inverse: true);
		return frames;
	}
}
