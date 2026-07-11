// Origine d'un dégât (boule de neige, feu...). Chaque source porte son montant
// de dégâts, centralisé ici pour éviter d'éparpiller des nombres magiques dans
// les entités qui infligent des dégâts.
public enum DamageSource
{
	Snowball,
	Fire,
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
		_ => 1,
	};
}
