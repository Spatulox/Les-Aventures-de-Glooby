using Godot;

// Salle de boss du Cerf (Rodolphe) : spécialise ZoneBoss avec les bornes de charge
// de l'arène et la fin de partie (écran de fin) à la défaite du boss.
public partial class ZoneBossCerf : ZoneBoss
{
	[Export] public float LimiteGauche = 5984f;
	[Export] public float LimiteDroite = 8480f;

	protected override void ConfigurerBoss(Boss boss)
	{
		if (boss is BossCerf cerf)
		{
			cerf.LimiteGauche = LimiteGauche;
			cerf.LimiteDroite = LimiteDroite;
		}
	}

	protected override void DemarrerCombat(Player joueur)
	{
		Boss.Vaincu += SurVictoire;
	}

	private void SurVictoire()
	{
		// Persiste la défaite : la zone ne respawnera plus ce boss après chargement.
		GameState.Instance.MarquerBossVaincu(NomBoss);
		GameState.Instance.Sauvegarder();

		var minuteur = GetTree().CreateTimer(2.5);
		minuteur.Timeout += () => GetTree().ChangeSceneToFile("res://scenes/ui/ecran_fin.tscn");
	}
}
