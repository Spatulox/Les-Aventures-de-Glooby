using Godot;

// Le pantalon de rechange : l'objet que Glooby traverse tout le jeu pour obtenir.
// Ramassé au contact comme tout ElementRamassable, il se mémorise dans GameState puis,
// si CheminSceneSuite est renseigné, enchaîne sur cette scène après un fondu au noir.
//
// C'est LUI qui clôt la partie, et non un minuteur posé sur l'arène : le boss le lâche
// (ou la cage le libère) là où il est tombé, parfois à l'autre bout de la salle, et le
// joueur doit avoir tout son temps pour aller le chercher.
public partial class PantalonPickup : ElementRamassable
{
	// Identifiant de mémoire persistante : le pantalon ne se ramasse qu'une fois par
	// partie, quelle que soit la fin par laquelle on l'obtient.
	public const string IdPantalon = "pantalon_obtenu";

	// Scène chargée après le ramassage (vide = on reste dans le monde).
	[Export(PropertyHint.File, "*.tscn")] public string CheminSceneSuite = "";

	// Durée d'un demi-fondu au noir avant la bascule.
	[Export] public float DureeFondu = 0.6f;

	// Halo : un tour complet en 6 s, et une respiration ample mais lente.
	[Export] public float DureeTourAura = 6f;
	[Export] public float AmpleurPulseAura = 0.1f;
	[Export] public float DureePulseAura = 1.2f;

	protected override bool EstDejaConsomme() => GameState.Instance.EstConsomme(IdPantalon);

	// Le pantalon lui-même n'a qu'une seule frame : c'est le halo qui bouge. Il tourne
	// lentement et respire, ce qui signale l'objet final au bout d'une arène sans avoir
	// à animer le vêtement. Un nœud « Aura » absent n'est pas une erreur (l'objet reste
	// simplement statique, comme les autres ramassables).
	protected override void PreparerVisuel()
	{
		Effets.Flottaison(this, 6f, 0.8f);

		var aura = GetNodeOrNull<Node2D>("Aura");
		if (aura == null)
			return;

		Effets.RotationContinue(aura, DureeTourAura);
		Effets.Pulsation(aura, AmpleurPulseAura, DureePulseAura);
	}

	protected override void Ramasser()
	{
		GameState.Instance.MarquerConsomme(IdPantalon);
		GameState.Instance.Sauvegarder();

		if (string.IsNullOrEmpty(CheminSceneSuite))
			return;

		// L'arbre est capturé MAINTENANT : ElementRamassable libère ce nœud juste après
		// Ramasser(), et le rappel de fin de fondu ne pourrait plus lui demander son
		// GetTree(). Le voile, lui, vit sous la racine et survit au changement de scène.
		var arbre = GetTree();
		Effets.FondreAuNoirPuis(this, DureeFondu, () => arbre.ChangeSceneToFile(CheminSceneSuite));
	}
}
