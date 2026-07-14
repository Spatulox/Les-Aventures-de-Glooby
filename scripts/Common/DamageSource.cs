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
		DamageSource.ContactMechant => 1,
		_ => 1,
	};
}
