using Godot;

// Pickup du Pouvoir de Glace : même base que le pickup de chaleur (ramassé au
// contact, débloque le pouvoir dans GameState). Léger mouvement de flottaison
// procédural, pas de nouvel asset.
public partial class PouvoirGlacePickup : ElementRamassable
{
	protected override bool EstDejaConsomme() => GameState.Instance.PouvoirGlaceActif;

	protected override void PreparerVisuel() => Effets.Flottaison(this, 6f, 0.8f);

	protected override void Ramasser() => GameState.Instance.ObtenirPouvoirGlace();
}
