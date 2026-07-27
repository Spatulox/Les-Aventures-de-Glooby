using Godot;

// Ennemi « Gardien des ronces » (grotte florale) : la sentinelle de base du lieu. Il patrouille
// en va-et-vient tant qu'il est seul, puis passe par une phase de REPÉRAGE quand le joueur entre
// dans sa portée. Cette phase dure DureeDetection et se déroule en trois temps :
//   t = 0            il sursaute de surprise et se fige, tourné vers sa cible ;
//   t = DelaiRuee    il bondit sur elle (ruée courte et rapide, ~2× sa taille) ;
//   fin de la ruée   il se refige jusqu'à la fin de la phase, puis marche sur elle.
// La marche est plus lente que le joueur, donc semable en courant, mais il ne lâche pas tant que
// sa cible reste à portée.
//
// Sa faiblesse : le côté du joueur est verrouillé au repérage (et la ruée part toujours au bout,
// esquivable d'un pas de côté). Si le joueur passe de l'AUTRE CÔTÉ, le gardien doit tout
// recommencer — nouveau sursaut, nouvelle ruée, nouvelle immobilité — ce qui laisse le temps de
// le semer ou de le viser. La ruée est bornée pour s'arrêter à hauteur du joueur au lieu de le
// dépasser : sans ça, un dépassement inverserait le côté et le ferait osciller sans fin.
//
// Il blesse au contact (ZoneContact) et se tue normalement (PV), contrairement aux ennemis
// « obstacles » de la banquise (ours, bonhomme) que l'on ne fait qu'étourdir.
//
// Frames chargées depuis res://assets/ennemis/grotte_florale/gardien_ronces/{idle,marche,mort}.
// Pas d'animation de repérage dédiée (budget) : la base rejoue idle quand velocite.X == 0, et
// marche pendant la ruée.
public partial class GardienRonces : PnjMechant
{
	private enum EtatGardien { Patrouille, Detection, Ruee, Poursuite }

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
	// Longueur du bond, ~2× la taille du gardien (sa boîte de collision fait 40 px).
	[Export] public float DistanceRuee = 80f;
	// Vitesse du bond : bref mais vif, sinon il ne se distingue pas de la marche de poursuite.
	[Export] public float VitesseRuee = 260f;

	private EtatGardien _etat = EtatGardien.Patrouille;
	private float _tempsDetection;   // temps écoulé depuis le début du repérage
	private float _minuteurRuee;     // garde-fou de la ruée (gardien bloqué contre un mur)
	private bool _rueeFaite;         // une seule ruée par phase de repérage
	private float _xDebutRuee;
	private float _longueurRuee;
	private int _coteJoueur = 1;     // côté du joueur verrouillé au repérage (-1 = gauche, 1 = droite)

	// Patrouille hors de portée, sinon enchaîne repérage, ruée et marche vers le joueur. La base
	// applique gravité, MoveAndSlide et l'animation : on ne décide ici que la vélocité.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		if (joueur == null || distance > PorteeDetection)
		{
			_etat = EtatGardien.Patrouille;
			Patrouiller(dt, ref velocite);
			return;
		}

		float ecart = joueur.GlobalPosition.X - GlobalPosition.X;
		int cote = CoteDuJoueur(ecart);

		switch (_etat)
		{
			case EtatGardien.Patrouille:
				DemarrerDetection(cote, ref velocite);
				break;

			case EtatGardien.Detection:
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
					_etat = EtatGardien.Poursuite;
				break;

			// Direction verrouillée au départ : la ruée va toujours au bout, ce qui la rend
			// esquivable d'un pas de côté. Le changement de côté n'est réexaminé qu'après.
			case EtatGardien.Ruee:
				_tempsDetection += dt;   // l'horloge du repérage continue de tourner pendant le bond
				velocite.X = _coteJoueur * VitesseRuee;
				_minuteurRuee -= dt;
				if (Mathf.Abs(GlobalPosition.X - _xDebutRuee) >= _longueurRuee || _minuteurRuee <= 0f)
					_etat = _tempsDetection >= DureeDetection ? EtatGardien.Poursuite : EtatGardien.Detection;
				break;

			case EtatGardien.Poursuite:
				if (cote != _coteJoueur)
				{
					DemarrerDetection(cote, ref velocite);
					break;
				}
				velocite.X = Mathf.Abs(ecart) <= DistanceArret ? 0f : Mathf.Sign(ecart) * VitessePoursuite;
				break;
		}
	}

	// Côté où se trouve le joueur, avec l'hystérésis de DistanceArret : tant qu'il est collé au
	// gardien, on conserve le côté verrouillé. Sans cela, les quelques pixels de dépassement d'une
	// poursuite au contact inverseraient le signe et relanceraient le repérage en boucle.
	private int CoteDuJoueur(float ecart)
		=> Mathf.Abs(ecart) > DistanceArret ? Mathf.Sign(ecart) : _coteJoueur;

	// Repérage : le gardien sursaute de surprise puis se fige, tourné vers le joueur. Le côté est
	// verrouillé ici — s'il change, on repasse par cette méthode, et c'est toute la phase qui
	// recommence (sursaut et ruée compris).
	private void DemarrerDetection(int cote, ref Vector2 velocite)
	{
		_coteJoueur = cote != 0 ? cote : 1;
		_etat = EtatGardien.Detection;
		_tempsDetection = 0f;
		_rueeFaite = false;
		velocite.X = 0f;
		DefinirOrientation(_coteJoueur < 0);

		if (IsOnFloor())   // pas de second sursaut si la phase se relance alors qu'il est en l'air
			velocite.Y = ImpulsionSursaut;
	}

	// Bond vers le joueur, au plus DistanceRuee mais jamais au-delà de sa position : dépasser le
	// joueur inverserait son côté et relancerait le repérage, faisant osciller le gardien autour de
	// lui sans fin. Si la place manque (joueur déjà au contact), la ruée est simplement sautée.
	private void DemarrerRuee(float ecart, ref Vector2 velocite)
	{
		_rueeFaite = true;
		_longueurRuee = Mathf.Min(DistanceRuee, Mathf.Max(0f, Mathf.Abs(ecart) - DistanceArret));
		if (_longueurRuee <= 1f)
			return;

		_etat = EtatGardien.Ruee;
		_xDebutRuee = GlobalPosition.X;
		// Garde-fou : deux fois le temps théorique du bond, pour qu'un gardien bloqué contre un
		// mur (la distance ne progresse plus) ne reste pas coincé à pousser dedans.
		_minuteurRuee = 2f * _longueurRuee / Mathf.Max(1f, VitesseRuee);
		velocite.X = _coteJoueur * VitesseRuee;
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
