using Godot;

// Zone de transition entre deux scènes de niveau. Quand le joueur entre dans
// l'Area2D (typiquement placée à la sortie d'un lieu, ex. la fin de la banquise),
// elle remplace complètement la scène courante par la scène cible
// (GetTree().ChangeSceneToFile) — contrairement à ZoneBoss qui, lui, instancie
// son contenu DANS le monde. Le changement est optionnellement précédé d'un fondu
// au noir (DureeFondu).
//
// La cible est référencée par CHEMIN (string), pas par un PackedScene embarqué : deux
// niveaux qui se renvoient l'un vers l'autre (monde1 <-> monde2) créeraient sinon une
// dépendance circulaire de ressources. Godot n'arrive pas à charger un .tscn qui
// s'embarque lui-même en boucle (« Parse Error: Busy ») : la seconde référence tombe à
// null, et la zone de retour restait donc inerte. Un chemin, chargé à la volée, n'a pas
// ce cycle. À assigner par instance dans l'éditeur.
public partial class ZoneChargementScene : DeclencheurZone
{
	// Chemin de la scène à charger à l'entrée du joueur (vide = zone inerte,
	// avertissement). Ex. "res://scenes/niveaux/monde1.tscn".
	[Export(PropertyHint.File, "*.tscn")] public string CheminSceneSuivante = "";

	// Id du PointEntree où faire apparaître le joueur dans la scène cible (ex.
	// "depuis_usine" pour revenir côté est de monde1). Vide = position authorée du
	// nœud Joueur de la scène cible (comportement d'origine). Doit correspondre à
	// l'Id d'un PointEntree présent dans la scène cible.
	[Export] public string PointEntreeCible = "";

	// Durée du fondu au noir avant le changement (0 = bascule immédiate, sans voile).
	[Export] public float DureeFondu = 0.5f;

	// Une transition ne doit se déclencher qu'une fois, même si le joueur oscille
	// sur le bord de la zone pendant le fondu.
	public override void _Ready()
	{
		UneSeuleFois = true;
		base._Ready();
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		if (string.IsNullOrEmpty(CheminSceneSuivante))
		{
			GD.PushWarning($"ZoneChargementScene '{Name}' : CheminSceneSuivante non assigné, transition ignorée.");
			return;
		}

		// Voile noir mutualisé (Effets) : il est rattaché à Root et non à cette zone,
		// donc il survit au ChangeSceneToFile qui libère la scène courante — sans quoi
		// le fondu de sortie ne jouerait jamais et l'écran resterait noir.
		Effets.FondreAuNoirPuis(this, DureeFondu, ChangerScene);
	}

	// Bascule effective vers la scène suivante (différée pour rester hors du
	// traitement de signal/physique en cours). On mémorise d'abord la porte visée :
	// GameState survit au ChangeSceneToFile, Player._Ready la consommera pour
	// choisir son PointEntree de spawn.
	private void ChangerScene()
	{
		if (GameState.Instance != null)
			GameState.Instance.PointEntreeDemande = PointEntreeCible;

		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, CheminSceneSuivante);
	}
}
