using Godot;

// Ennemi « Locomotive jouet » (usine du Père Noël) : la déclinaison usine du FONCEUR de la
// banquise (OursDeNeige). Même structure d'états — elle roule tranquillement en va-et-vient,
// repère le joueur, TÉLÉGRAPHIE sa charge (coup de sifflet, la cheminée crache plus fort),
// puis fonce en ligne droite, direction verrouillée au départ : esquivable d'un pas de côté.
//
// Deux differences assumées avec l'ours, qui justifient une classe à part plutôt qu'une
// sous-classe de OursDeNeige :
//   - elle a un vrai TÉLÉGRAPHE (Etat.Detection) avant de charger, là où l'ours part sec ;
//   - elle MEURT normalement en PV (l'ours est un obstacle qu'on ne fait qu'étourdir).
// Le code réellement partagé vit dans PnjMechant : patrouille, contact, orientation et
// séquence de mort animée (dossier mort/ rempli => rien à écrire ici).
//
// Boucle de jeu : charge -> mur -> déraillement. Le déraillement est la fenêtre de punition,
// pendant laquelle les coups comptent double.
public partial class LocomotiveJouet : PnjMechant
{
	private enum EtatLoco { Roulement, Detection, Charge, Deraille }

	// Vitesse de la ruée : supérieure au Speed du joueur (220) pour qu'elle le dépasse.
	[Export] public float VitesseCharge = 250f;
	// Durée du télégraphe (sifflet + fumée) : la fenêtre d'esquive.
	[Export] public float DureeDetection = 0.7f;
	// Sécurité : si elle ne rencontre aucun mur, la charge s'arrête d'elle-même.
	[Export] public float DureeChargeMax = 2.5f;
	// Durée du déraillement après impact contre un mur.
	[Export] public float DureeDeraillement = 2f;
	// Les coups portés pendant le déraillement comptent double (récompense de l'esquive).
	[Export] public int MultiplicateurVulnerable = 2;

	private EtatLoco _etat = EtatLoco.Roulement;
	private float _minuteur;
	private int _dirCharge = 1;

	// Machine à états pilotée frame par frame : la base applique gravité, MoveAndSlide,
	// orientation et mort, on ne fixe donc ici que velocite.X.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		switch (_etat)
		{
			case EtatLoco.Roulement:
				Patrouiller(dt, ref velocite);
				if (joueur != null && distance <= PorteeDetection)
					EntrerDetection(joueur);
				break;

			// Télégraphe : elle s'arrête, siffle et crache — le joueur a le temps de s'écarter.
			case EtatLoco.Detection:
				velocite.X = 0f;
				if (Decompter(dt))
					EntrerCharge();
				break;

			case EtatLoco.Charge:
				velocite.X = _dirCharge * VitesseCharge;
				// IsOnWall() reflète le MoveAndSlide de la frame précédente : l'impact est
				// donc détecté une frame après le contact, ce qui est invisible en jeu.
				if (IsOnWall())
					EntrerDeraillement();
				else if (Decompter(dt))
					EntrerRoulement();
				break;

			// Déraillée contre le mur : immobile et vulnérable le temps du décompte.
			case EtatLoco.Deraille:
				velocite.X = 0f;
				if (Decompter(dt))
					EntrerRoulement();
				break;
		}
	}

	// L'animation suit l'état, pas la vitesse : on court-circuite le choix idle/marche de la base.
	protected override void MettreAJourAnimation(Vector2 velocite)
	{
		switch (_etat)
		{
			case EtatLoco.Detection: JouerSiPresente("detection"); break;
			case EtatLoco.Charge: JouerSiPresente("charge"); break;
			case EtatLoco.Deraille: JouerSiPresente("etourdi"); break;
			default: JouerSiPresente("idle"); break;
		}
	}

	// Coup double pendant le déraillement.
	protected override int AjusterDegats(int brut)
		=> _etat == EtatLoco.Deraille ? brut * MultiplicateurVulnerable : brut;

	// Verrouille la direction vers le joueur DÈS le télégraphe : la charge part ensuite en
	// ligne droite sans suivi, ce qui la rend esquivable d'un pas de côté.
	private void EntrerDetection(Player joueur)
	{
		_dirCharge = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		if (_dirCharge == 0)
			_dirCharge = 1;

		_etat = EtatLoco.Detection;
		_minuteur = DureeDetection;
		DefinirOrientation(_dirCharge < 0);
	}

	private void EntrerCharge()
	{
		_etat = EtatLoco.Charge;
		_minuteur = DureeChargeMax;
	}

	private void EntrerDeraillement()
	{
		_etat = EtatLoco.Deraille;
		_minuteur = DureeDeraillement;
		Effets.FlashCouleur(Sprite, new Color(1.5f, 1.3f, 1f), 0.05f, 0.25f);
	}

	private void EntrerRoulement()
	{
		_etat = EtatLoco.Roulement;
	}

	// Rien à écrire pour la mort : la base joue le dossier « mort » (démantèlement en
	// pièces de bois, frames dessinées en procédural) puis efface la locomotive en fondu.

	// Décompte le minuteur courant ; renvoie vrai quand il atteint 0 (transition d'état).
	private bool Decompter(float dt)
	{
		_minuteur -= dt;
		return _minuteur <= 0f;
	}

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
