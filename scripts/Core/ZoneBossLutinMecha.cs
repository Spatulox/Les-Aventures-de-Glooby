using Godot;

// Arène du Lutin Mecha (usine du Père Noël) : spécialise ZoneBoss avec les bornes de
// déplacement du boss et la persistance de sa défaite. Contrairement à ZoneBossCerf,
// elle n'enchaîne PAS sur l'écran de fin : ce boss est une alternative au Cerf, la
// partie continue après sa chute.
public partial class ZoneBossLutinMecha : ZoneBoss
{
	// Plus de ConfigurerBoss ici : les bornes de déplacement sont posées génériquement
	// par ZoneBoss via le contrat BossBorne, que BossLutinMecha implémente.

	protected override void DemarrerCombat(Player joueur)
	{
		Boss.Vaincu += SurVictoire;
	}

	private void SurVictoire()
	{
		// Persiste la défaite : la zone ne fera plus réapparaître ce boss après chargement.
		// NomChoisi et non NomBoss : dans une arène à deux boss, seul celui réellement
		// combattu est marqué vaincu.
		GameState.Instance.MarquerBossVaincu(NomChoisi);
		GameState.Instance.Sauvegarder();
	}
}
