using Godot;

// Salle de boss du Cerf (Rodolphe) : spécialise ZoneBoss avec les bornes de charge
// de l'arène et la fin de partie (écran de fin) à la défaite du boss.
public partial class ZoneBossCerf : ZoneBoss
{
	// Scène à charger après la victoire. VIDE (défaut) = on reste dans le monde : la
	// partie continue et c'est la suite du niveau (zone débloquée par une PorteInterne
	// à BossRequis) qui prend le relais — cas de ReindeerBoss, dont le jardin s'ouvre
	// après Rodolphe. À renseigner (ex. "res://scenes/ui/ecran_fin.tscn") pour qu'un
	// boss termine la partie.
	[Export(PropertyHint.File, "*.tscn")] public string CheminSceneVictoire = "";

	// Battement avant la bascule, le temps de voir l'animation de mort.
	[Export] public float DelaiVictoire = 2.5f;

	// Plus de ConfigurerBoss ici : les bornes de charge sont posées génériquement par
	// ZoneBoss via le contrat BossBorne, que BossCerf implémente.

	protected override void DemarrerCombat(Player joueur)
	{
		Boss.Vaincu += SurVictoire;
	}

	private void SurVictoire()
	{
		// Persiste la défaite : la zone ne respawnera plus ce boss après chargement,
		// et toute PorteInterne dont le BossRequis vaut ce nom s'ouvre. NomChoisi et non
		// NomBoss : dans une arène à deux boss, seul celui réellement apparu est marqué.
		GameState.Instance.MarquerBossVaincu(NomChoisi);
		GameState.Instance.Sauvegarder();

		if (string.IsNullOrEmpty(CheminSceneVictoire))
			return;

		var minuteur = GetTree().CreateTimer(DelaiVictoire);
		minuteur.Timeout += () => GetTree().ChangeSceneToFile(CheminSceneVictoire);
	}
}
