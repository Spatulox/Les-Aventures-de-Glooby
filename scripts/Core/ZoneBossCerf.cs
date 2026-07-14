using Godot;

// Salle de boss du Cerf (Rodolphe) : spécialise ZoneBoss avec les bornes de charge
// de l'arène et la fin de partie (écran de fin) à la défaite du boss.
public partial class ZoneBossCerf : ZoneBoss
{
	protected override void ConfigurerBoss(Boss boss)
	{
		// Bornes de charge du Cerf = rectangle de l'arène (plus de LimiteGauche/Droite
		// à saisir) : le boss ne peut pas sortir de sa zone. Elles suivent la taille
		// du rectangle dessiné dans l'éditeur, comme les limites caméra.
		if (boss is BossCerf cerf && CalculerLimitesDepuisForme(out int g, out int d, out int _, out int _))
		{
			cerf.LimiteGauche = g;
			cerf.LimiteDroite = d;
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
