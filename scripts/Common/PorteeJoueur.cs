// Où se tient le joueur par rapport à un boss, du point de vue de ce qui PORTE. C'est la
// lecture que fait le boss du terrain avant de choisir son attaque : inutile de frapper le
// sol quand le joueur est à l'autre bout de l'arène, inutile de tirer quand il est collé.
// Trois cas seulement — trois zones concentriques — pour que la pondération des patterns
// reste lisible dans chaque IA (voir Boss.EvaluerPortee).
public enum PorteeJoueur
{
	// Collé au boss : ses attaques de contact (punch au sol, saut écrasant) sont les seules
	// à valoir le coup.
	CorpsACorps,

	// Dans l'anneau utile de ses attaques à distance (projectiles, largages).
	Distance,

	// Hors de tout : plus rien ne porte, le boss doit d'abord se rapprocher.
	HorsPortee,
}
