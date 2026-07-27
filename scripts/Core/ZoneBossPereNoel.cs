using Godot;

// Arène finale (BossEnd) : spécialise ZoneBoss avec la persistance de la défaite et
// l'enchaînement sur l'écran de fin. Elle héberge DEUX boss — le Père Noël par défaut,
// le Lutin Mecha si le joueur a donné ses 50 poissons au lutin CGT — et ne connaît le
// type ni de l'un ni de l'autre : les bornes viennent du contrat BossBorne.
public partial class ZoneBossPereNoel : ZoneBoss
{
	// Scène à charger après la victoire. VIDE (défaut) = on reste dans le monde. À
	// renseigner (ex. "res://scenes/ui/ecran_fin.tscn") pour qu'il termine la partie.
	[Export(PropertyHint.File, "*.tscn")] public string CheminSceneVictoire = "";

	// Battement avant la bascule, le temps de le voir s'affaisser.
	[Export] public float DelaiVictoire = 2.5f;

	// Pas de ConfigurerBoss : les bornes sont posées génériquement par ZoneBoss via le
	// contrat BossBorne — c'est ce qui borne aussi le boss caché, d'une autre classe.

	protected override void DemarrerCombat(Player joueur)
	{
		Boss.Vaincu += SurVictoire;
	}

	private void SurVictoire()
	{
		// NomChoisi et non NomBoss : dans une arène à deux boss, seul celui réellement
		// combattu est marqué vaincu.
		GameState.Instance.MarquerBossVaincu(NomChoisi);
		GameState.Instance.Sauvegarder();

		if (string.IsNullOrEmpty(CheminSceneVictoire))
			return;

		var minuteur = GetTree().CreateTimer(DelaiVictoire);
		minuteur.Timeout += () => GetTree().ChangeSceneToFile(CheminSceneVictoire);
	}
}
