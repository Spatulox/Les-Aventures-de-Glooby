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

	// Subis par le joueur au contact d'un PNJ méchant (contact simple ou charge).
	ContactMechant,
}

// Helpers de DamageSource : associe à chaque source son montant de dégâts.
// (Un enum C# ne peut pas porter de valeur associée comme en Java/Kotlin, d'où
// cette table de correspondance centralisée.)
public static class DamageSourceExtensions
{
	public static int MontantDegats(this DamageSource source) => source switch
	{
		DamageSource.Snowball => 2,
		DamageSource.Fire => 1,
		DamageSource.Stalactite => 1,
		DamageSource.ChargeBoss => 1,
		DamageSource.SouffleGivre => 2,
		DamageSource.EclatGlace => 1,
		DamageSource.EcrasementMecha => 2,
		DamageSource.JouetExplosif => 2,
		DamageSource.ContactMechant => 1,
		_ => 1,
	};

	// Vrai si la source est une attaque du joueur (par opposition aux pièges et aux
	// attaques subies). Sert au mode debug, qui ne surpuissance que les coups portés
	// par le joueur — ceux qu'il encaisse gardent leur montant normal.
	public static bool EstDuJoueur(this DamageSource source)
		=> source is DamageSource.Snowball or DamageSource.Fire;
}
