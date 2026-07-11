using Godot;

// Pickup du Pouvoir de Chaleur : léger mouvement de flottaison procédural
// (pas de nouvelle génération pour l'ambiance de la salle).
public partial class PouvoirChaleurPickup : ElementRamassable
{
	protected override bool EstDejaConsomme() => GameState.Instance.PouvoirChaleurActif;

	protected override void PreparerVisuel() => Effets.Flottaison(this, 6f, 0.8f);

	protected override void Ramasser() => GameState.Instance.ObtenirPouvoirChaleur();
}
