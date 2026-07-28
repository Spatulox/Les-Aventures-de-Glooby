using Godot;

// Base des méchants « fonceurs » : les ennemis qui patrouillent, SURSAUTENT en repérant le
// joueur, lui foncent dessus après un court télégraphe, puis le poursuivent au pas. C'est le
// schéma d'attaque du gardien des ronces, extrait ici pour être partagé (locomotive jouet...).
//
// Cycle d'états :
//   Patrouille   va-et-vient de PnjMechant tant que le joueur est hors de portée ;
//   Detection    t = 0 : petit saut de surprise, le méchant se fige, tourné vers sa cible ;
//   Ruee         t = DelaiRuee : il bondit/fonce sur elle, direction VERROUILLÉE au repérage ;
//   Poursuite    fin de la phase (DureeDetection) : il marche sur elle, plus lentement qu'elle ;
//   Immobilise   état de vulnérabilité optionnel, déclenché par la sous-classe (Immobiliser).
//
// La faiblesse commune : le côté du joueur est verrouillé au repérage. S'il passe de l'AUTRE
// CÔTÉ, toute la phase recommence — nouveau sursaut, nouvelle ruée, nouvelle immobilité — ce qui
// laisse le temps de semer le méchant ou de le viser. La ruée part toujours au bout : elle
// s'esquive d'un pas de côté.
//
// Points d'extension des sous-classes :
//   RueeBorneeParJoueur   borner la ruée à hauteur du joueur (bond court) ou la laisser filer
//                         sur toute DistanceRuee (charge en ligne droite qui le dépasse) ;
//   InterrompreRuee()     couper la ruée en cours (impact contre un mur...), typiquement suivi
//                         d'un Immobiliser() pour ouvrir une fenêtre de punition ;
//   Etat                  lecture de l'état courant (dégâts doublés, animation dédiée...).
//
// Les animations suivent l'état si la scène en fournit les frames (« detection », « charge »,
// « etourdi ») ; sinon on retombe sur le couple idle/marche de PnjMechant — un méchant sans ces
// dossiers reste donc parfaitement fonctionnel.
public abstract partial class MechantFonceur : PnjMechant
{
	protected enum EtatFonceur { Patrouille, Detection, Ruee, Poursuite, Immobilise }

	// Vitesse de marche quand il a repéré le joueur (volontairement < Speed du joueur : 220).
	[Export] public float VitessePoursuite = 55f;
	// Distance en deçà de laquelle il cesse d'avancer : évite qu'il tremble sur le joueur.
	// Sert aussi d'hystérésis au changement de côté (voir CoteDuJoueur).
	[Export] public float DistanceArret = 8f;
	// Durée totale du repérage, ruée comprise : la fenêtre de fuite offerte au joueur.
	[Export] public float DureeDetection = 1.2f;
	// Petit sursaut de surprise au repérage : bien plus mou que le JumpVelocity de base (-420).
	[Export] public float ImpulsionSursaut = -180f;
	// Délai entre le sursaut et la ruée : le temps de lire le télégraphe et de s'écarter.
	[Export] public float DelaiRuee = 0.5f;
	// Longueur de la ruée (bornée à hauteur du joueur si RueeBorneeParJoueur).
	[Export] public float DistanceRuee = 80f;
	// Vitesse de la ruée : brève mais vive, sinon elle ne se distingue pas de la poursuite.
	[Export] public float VitesseRuee = 260f;

	private EtatFonceur _etat = EtatFonceur.Patrouille;
	private float _tempsDetection;   // temps écoulé depuis le début du repérage
	private float _minuteurRuee;     // garde-fou de la ruée (méchant bloqué contre un mur)
	private float _minuteurImmobile; // décompte de l'état de vulnérabilité
	private bool _rueeFaite;         // une seule ruée par phase de repérage
	private float _xDebutRuee;
	private float _longueurRuee;
	private int _coteJoueur = 1;     // côté du joueur verrouillé au repérage (-1 = gauche, 1 = droite)

	// État courant, pour les sous-classes (animation dédiée, dégâts doublés...).
	protected EtatFonceur Etat => _etat;

	// Vrai (défaut) : la ruée s'arrête à hauteur du joueur au lieu de le dépasser — sans ça, un
	// dépassement inverserait son côté et ferait osciller le méchant autour de lui sans fin.
	// Faux : la ruée file sur toute DistanceRuee, quitte à dépasser la cible (charge en ligne
	// droite d'une locomotive, qui a besoin d'aller percuter un mur).
	protected virtual bool RueeBorneeParJoueur => true;

	// Patrouille hors de portée, sinon enchaîne repérage, ruée et marche vers le joueur. La base
	// PnjMechant applique gravité, MoveAndSlide et l'animation : on ne décide ici que la vélocité.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		// Immobilisation : le méchant est hors-jeu (et vulnérable) jusqu'à la fin du décompte,
		// joueur à portée ou non.
		if (_etat == EtatFonceur.Immobilise)
		{
			velocite.X = 0f;
			_minuteurImmobile -= dt;
			if (_minuteurImmobile <= 0f)
				_etat = EtatFonceur.Patrouille;
			return;
		}

		if (joueur == null || distance > PorteeDetection)
		{
			_etat = EtatFonceur.Patrouille;
			Patrouiller(dt, ref velocite);
			return;
		}

		float ecart = joueur.GlobalPosition.X - GlobalPosition.X;
		int cote = CoteDuJoueur(ecart);

		switch (_etat)
		{
			case EtatFonceur.Patrouille:
				DemarrerDetection(cote, ref velocite);
				break;

			case EtatFonceur.Detection:
				velocite.X = 0f;
				// La base n'oriente le sprite que s'il se déplace : à l'arrêt, on le fait ici.
				DefinirOrientation(_coteJoueur < 0);
				if (cote != _coteJoueur)
				{
					DemarrerDetection(cote, ref velocite);
					break;
				}
				_tempsDetection += dt;
				if (!_rueeFaite && _tempsDetection >= DelaiRuee)
					DemarrerRuee(ecart, ref velocite);
				else if (_tempsDetection >= DureeDetection)
					_etat = EtatFonceur.Poursuite;
				break;

			// Direction verrouillée au départ : la ruée va toujours au bout, ce qui la rend
			// esquivable d'un pas de côté. Le changement de côté n'est réexaminé qu'après.
			case EtatFonceur.Ruee:
				_tempsDetection += dt;   // l'horloge du repérage tourne pendant la ruée
				velocite.X = _coteJoueur * VitesseRuee;
				_minuteurRuee -= dt;

				if (InterrompreRuee())
				{
					velocite.X = 0f;
					// La surcharge a pu enchaîner sur une immobilisation ; sinon on termine
					// la ruée normalement plutôt que de rester coincé dedans.
					if (_etat == EtatFonceur.Ruee)
						TerminerRuee();
					break;
				}

				if (Mathf.Abs(GlobalPosition.X - _xDebutRuee) >= _longueurRuee || _minuteurRuee <= 0f)
					TerminerRuee();
				break;

			case EtatFonceur.Poursuite:
				if (cote != _coteJoueur)
				{
					DemarrerDetection(cote, ref velocite);
					break;
				}
				velocite.X = Mathf.Abs(ecart) <= DistanceArret ? 0f : Mathf.Sign(ecart) * VitessePoursuite;
				break;
		}
	}

	// Hook appelé à chaque frame de ruée : renvoyer vrai coupe le bond en cours (impact contre
	// un mur, obstacle...). La surcharge peut appeler Immobiliser() pour ouvrir une fenêtre de
	// punition ; sans ça, la ruée se termine simplement (poursuite ou nouveau repérage).
	protected virtual bool InterrompreRuee() => false;

	// Immobilise le méchant, vulnérable et inoffensif, pendant la durée donnée. Appelée par les
	// sous-classes (déraillement contre un mur, étourdissement...) ; le méchant repart ensuite
	// en patrouille, donc en repérage complet si le joueur est encore là.
	protected void Immobiliser(float duree)
	{
		_etat = EtatFonceur.Immobilise;
		_minuteurImmobile = duree;
	}

	// Fin de la ruée : poursuite si la phase de repérage est écoulée, sinon retour à l'immobilité
	// du repérage le temps qu'elle finisse.
	private void TerminerRuee()
	{
		_etat = _tempsDetection >= DureeDetection ? EtatFonceur.Poursuite : EtatFonceur.Detection;
	}

	// Côté où se trouve le joueur, avec l'hystérésis de DistanceArret : tant qu'il est collé au
	// méchant, on conserve le côté verrouillé. Sans cela, les quelques pixels de dépassement d'une
	// poursuite au contact inverseraient le signe et relanceraient le repérage en boucle.
	private int CoteDuJoueur(float ecart)
		=> Mathf.Abs(ecart) > DistanceArret ? Mathf.Sign(ecart) : _coteJoueur;

	// Repérage : le méchant sursaute de surprise puis se fige, tourné vers le joueur. Le côté est
	// verrouillé ici — s'il change, on repasse par cette méthode, et c'est toute la phase qui
	// recommence (sursaut et ruée compris).
	private void DemarrerDetection(int cote, ref Vector2 velocite)
	{
		_coteJoueur = cote != 0 ? cote : 1;
		_etat = EtatFonceur.Detection;
		_tempsDetection = 0f;
		_rueeFaite = false;
		velocite.X = 0f;
		DefinirOrientation(_coteJoueur < 0);

		if (IsOnFloor())   // pas de second sursaut si la phase se relance alors qu'il est en l'air
			velocite.Y = ImpulsionSursaut;
	}

	// Ruée vers le joueur, au plus DistanceRuee et — si RueeBorneeParJoueur — jamais au-delà de
	// sa position : le dépasser inverserait son côté et relancerait le repérage, faisant osciller
	// le méchant autour de lui sans fin. Si la place manque, la ruée est simplement sautée.
	private void DemarrerRuee(float ecart, ref Vector2 velocite)
	{
		_rueeFaite = true;
		_longueurRuee = RueeBorneeParJoueur
			? Mathf.Min(DistanceRuee, Mathf.Max(0f, Mathf.Abs(ecart) - DistanceArret))
			: DistanceRuee;
		if (_longueurRuee <= 1f)
			return;

		_etat = EtatFonceur.Ruee;
		_xDebutRuee = GlobalPosition.X;
		// Garde-fou : deux fois le temps théorique de la ruée, pour qu'un méchant bloqué contre un
		// mur (la distance ne progresse plus) ne reste pas coincé à pousser dedans.
		_minuteurRuee = 2f * _longueurRuee / Mathf.Max(1f, VitesseRuee);
		velocite.X = _coteJoueur * VitesseRuee;
	}

	// L'animation suit l'état quand la scène fournit les frames correspondantes ; sinon on garde
	// le choix idle/marche de PnjMechant (cas du gardien des ronces, sans anims dédiées).
	protected override void MettreAJourAnimation(Vector2 velocite)
	{
		bool jouee = _etat switch
		{
			EtatFonceur.Detection => JouerSiPresente("detection"),
			EtatFonceur.Ruee => JouerSiPresente("charge"),
			EtatFonceur.Immobilise => JouerSiPresente("etourdi"),
			_ => false,
		};

		if (!jouee)
			base.MettreAJourAnimation(velocite);
	}
}
