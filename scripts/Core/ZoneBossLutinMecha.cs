using Godot;

// Arène du Lutin Mecha (usine du Père Noël) : spécialise ZoneBoss avec les bornes de
// déplacement du boss et la persistance de sa défaite. Contrairement à ZoneBossCerf,
// elle n'enchaîne PAS sur l'écran de fin : ce boss est une alternative au Cerf, la
// partie continue après sa chute.
public partial class ZoneBossLutinMecha : ZoneBoss
{
	protected override void ConfigurerBoss(Boss boss)
	{
		// Bornes de déplacement = rectangle de l'arène dessiné dans l'éditeur, comme
		// les limites caméra : le boss ne peut pas sortir de sa zone.
		if (boss is BossLutinMecha mecha && CalculerLimitesDepuisForme(out int g, out int d, out int _, out int _))
		{
			mecha.LimiteGauche = g;
			mecha.LimiteDroite = d;
		}
	}

	protected override void DemarrerCombat(Player joueur)
	{
		Boss.Vaincu += SurVictoire;
	}

	private void SurVictoire()
	{
		// Persiste la défaite : la zone ne fera plus réapparaître ce boss après chargement.
		GameState.Instance.MarquerBossVaincu(NomBoss);
		GameState.Instance.Sauvegarder();
	}
}
