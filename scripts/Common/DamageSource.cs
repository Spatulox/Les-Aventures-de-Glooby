// Origine d'un dégât. Toute forme de dégât du jeu est un DamageSource (boule de
// neige, feu, stalactite, attaques du boss...) : chaque source porte son montant,
// centralisé ici pour éviter d'éparpiller des nombres magiques dans les entités.
public enum DamageSource
{
	// Infligés par le joueur.
	Snowball,
	Fire,

	// Subis par le joueur (pièges, attaques du boss).
	Stalactite,
	ChargeBoss,
	SouffleGivre,

	// Attaques du Lutin Mecha : éclat tiré par son canon, onde de choc de son saut
	// écrasant, et explosion des mini-jouets qu'il largue.
	EclatGlace,
	EcrasementMecha,
	JouetExplosif,

	// Onde de choc qui court AU SOL (punch du Père Noël) : la seule attaque qu'on esquive
	// en étant en l'air, d'où sa source propre — c'est OndeDeChoc qui porte cette règle.
	OndeDeChoc,

	// Subis par le joueur au contact d'un PNJ méchant (contact simple ou charge).
	ContactMechant,
}

// Helpers de DamageSource : associe à chaque source son montant de dégâts.
// (Un enum C# ne peut pas porter de valeur associée comme en Java/Kotlin, d'où
// cette table de correspondance centralisée.)
//
// DEUX ÉCHELLES COEXISTENT ICI, et il ne faut surtout pas les mélanger :
//   - les coups PORTÉS par le joueur (Snowball, Fire) se comptent sur les PV des
//     ennemis, qui sont tous exprimés au TIERS DE POINT (un ennemi « à 1 PV » vaut
//     3 dans les scènes). Cette échelle fine existe pour une seule raison : pouvoir
//     affaiblir la boule de neige d'un tiers sans casser les one-shot, ce qu'un
//     montant entier de 2 ne permettait pas (2 → 1 aurait été une division par deux) ;
//   - les coups SUBIS par le joueur (pièges, boss, contact) se comptent sur les PV du
//     joueur, gérés par GameState et volontairement laissés à leur échelle d'origine.
//     Ne les rescalez pas : ce sont des cœurs à l'écran, pas des points d'ennemi.
public static class DamageSourceExtensions
{
	public static int MontantDegats(this DamageSource source) => source switch
	{
		// Échelle « tiers de point » : 4 = 1⅓ ancien point, soit les 2/3 de l'ancienne
		// boule à 2. Elle continue d'abattre d'un coup tout ennemi à 3 (les « 1 PV »).
		DamageSource.Snowball => 4,
		DamageSource.Fire => 3,
		DamageSource.Stalactite => 1,
		DamageSource.ChargeBoss => 1,
		DamageSource.SouffleGivre => 2,
		DamageSource.EclatGlace => 1,
		DamageSource.EcrasementMecha => 2,
		DamageSource.JouetExplosif => 2,
		DamageSource.OndeDeChoc => 2,
		DamageSource.ContactMechant => 1,
		_ => 1,
	};

	// Vrai si la source est une attaque du joueur (par opposition aux pièges et aux
	// attaques subies). Sert au mode debug, qui ne surpuissance que les coups portés
	// par le joueur — ceux qu'il encaisse gardent leur montant normal.
	public static bool EstDuJoueur(this DamageSource source)
		=> source is DamageSource.Snowball or DamageSource.Fire;
}
