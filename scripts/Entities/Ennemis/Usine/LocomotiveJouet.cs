using Godot;

// Ennemi « Locomotive jouet » (usine du Père Noël) : la déclinaison usine du FONCEUR de la grotte
// florale (GardienRonces). Même machine à états, héritée de MechantFonceur — elle roule
// tranquillement en va-et-vient, repère le joueur (petit soubresaut sur ses rails), TÉLÉGRAPHIE
// sa charge (coup de sifflet, la cheminée crache plus fort), puis fonce en ligne droite,
// direction verrouillée au repérage : esquivable d'un pas de côté. Passé la charge, elle
// poursuit le joueur au ralenti tant qu'il reste à portée.
//
// Deux differences assumées avec le gardien, réglées par ses points d'extension :
//   - sa charge N'EST PAS bornée à hauteur du joueur (RueeBorneeParJoueur = false) : elle le
//     dépasse et va percuter le décor, d'où la boucle de jeu charge -> mur -> déraillement ;
//   - le déraillement (InterrompreRuee + Immobiliser) est sa fenêtre de punition, pendant
//     laquelle les coups comptent double.
// Elle MEURT normalement en PV (contrairement à l'ours de neige, simple obstacle étourdissable).
//
// Les animations sont pilotées par l'état dans MechantFonceur (detection / charge / etourdi),
// il n'y a donc rien à câbler ici. La mort réutilise le dossier mort/ de PnjMechant.
public partial class LocomotiveJouet : MechantFonceur
{
	// Durée du déraillement après impact contre un mur.
	[Export] public float DureeDeraillement = 2f;
	// Les coups portés pendant le déraillement comptent double (récompense de l'esquive).
	[Export] public int MultiplicateurVulnerable = 2;

	// Réglages de la locomotive : une charge plus longue et plus rapide que le bond du gardien
	// (il lui faut atteindre un mur), après un télégraphe plus lisible. Ce sont les défauts des
	// exports de MechantFonceur — chaque instance reste réglable dans l'inspecteur.
	public LocomotiveJouet()
	{
		VitesseRuee = 250f;        // > Speed du joueur (220) : la charge le dépasse
		DistanceRuee = 320f;       // assez long pour aller chercher un mur
		DelaiRuee = 0.7f;          // sifflet + fumée avant de s'élancer
		DureeDetection = 1.4f;
		ImpulsionSursaut = -150f;  // soubresaut discret : elle reste sur ses rails
		VitessePoursuite = 45f;    // lourde : elle se sème facilement en courant
	}

	// La charge file sur toute DistanceRuee au lieu de s'arrêter à hauteur du joueur : c'est ce
	// dépassement qui l'amène jusqu'au mur, et donc au déraillement.
	protected override bool RueeBorneeParJoueur => false;

	// Impact : la locomotive déraille et reste immobile, vulnérable, le temps du décompte.
	// IsOnWall() reflète le MoveAndSlide de la frame précédente : l'impact est donc détecté une
	// frame après le contact, ce qui est invisible en jeu.
	protected override bool InterrompreRuee()
	{
		if (!IsOnWall())
			return false;

		Immobiliser(DureeDeraillement);
		Effets.FlashCouleur(Sprite, new Color(1.5f, 1.3f, 1f), 0.05f, 0.25f);
		return true;
	}

	// Coup double pendant le déraillement.
	protected override int AjusterDegats(int brut)
		=> Etat == EtatFonceur.Immobilise ? brut * MultiplicateurVulnerable : brut;

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/usine/locomotive_jouet";
		AjouterAnimation(frames, "idle", $"{b}/idle", 8f, true);
		AjouterAnimation(frames, "detection", $"{b}/detection", 8f, false);
		AjouterAnimation(frames, "charge", $"{b}/charge", 14f, true);
		AjouterAnimation(frames, "etourdi", $"{b}/etourdi", 6f, true);
		AjouterAnimation(frames, "mort", $"{b}/mort", 8f, false);
		return frames;
	}
}
