using Godot;

// Marqueur de position (Marker2D) où le joueur apparaît en arrivant dans une scène
// par une TRANSITION (ZoneChargementScene), et non par « Continuer ». Une scène peut
// en poser plusieurs, chacun avec un Id distinct (ex. "village", "depuis_usine") : la
// zone de chargement qui mène ici indique lequel viser via son PointEntreeCible, et
// Player._Ready téléporte le joueur sur le PointEntree dont l'Id correspond. C'est ce
// qui permet de revenir de monde2 côté est de monde1 au lieu de respawn au village.
public partial class PointEntree : Marker2D
{
	// Groupe interrogé par Player pour retrouver tous les points d'entrée de la scène.
	public const string Groupe = "points_entree";

	// Identifiant de cette porte, référencé par ZoneChargementScene.PointEntreeCible.
	// À renseigner par instance dans l'éditeur.
	[Export] public string Id = "";

	public override void _Ready()
	{
		AddToGroup(Groupe);
	}

	// Retrouve le point d'entrée d'Id donné dans l'arbre courant (null si absent, ex.
	// Id inconnu ou scène sans point d'entrée — le joueur retombe alors sur la position
	// authorée du nœud Joueur).
	public static PointEntree Trouver(SceneTree arbre, string id)
	{
		if (arbre == null || string.IsNullOrEmpty(id))
			return null;

		foreach (var noeud in arbre.GetNodesInGroup(Groupe))
			if (noeud is PointEntree point && point.Id == id)
				return point;

		return null;
	}
}
