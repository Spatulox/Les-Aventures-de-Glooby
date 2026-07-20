// Constantes partagées du jeu : source de vérité unique pour les valeurs
// réutilisées par les salles, le monde et les entités (évite les
// redéclarations qui pourraient diverger silencieusement).
public static class Constantes
{
	// Taille d'une tuile en pixels (grille des feuilles PixelLab 32x32).
	public const int TailleTuile = 32;

	// Layers physiques : une catégorie par layer, sans recouvrement. C'est ce qui
	// rend les règles lisibles — auparavant le layer 1 signifiait terrain ET
	// joueur, ce qui obligeait à redéclarer tout le terrain sur un second layer
	// pour que les PNJ puissent le masquer sans se cogner au joueur.
	public const uint LayerTerrain = 1 << 0; // layer 1 dans l'éditeur
	public const uint LayerJoueur = 1 << 1;  // layer 2
	public const uint LayerPnj = 1 << 2;     // layer 3

	// Plateformes traversables (one-way), sur leur propre layer pour pouvoir le
	// retirer temporairement du masque du joueur pendant une traversée, sans
	// toucher au reste des collisions.
	public const uint LayerPlateformesTraversables = 1 << 4; // layer 5

	// Ce que masque un corps qui marche, joueur comme PNJ : le terrain et les
	// plateformes traversables, rien d'autre. Les layers joueur et PNJ ne sont
	// masqués par aucun corps, donc aucune collision joueur↔PNJ ni PNJ↔PNJ
	// n'est possible — l'invariant tient par construction, pas par réglage.
	public const uint MasqueMarcheur = LayerTerrain | LayerPlateformesTraversables; // 17

	// Ce que masque un projectile : le terrain (pour éclater) plus le joueur et
	// les PNJ (pour blesser) — la même scène est tirée par le joueur et par les
	// ennemis, le tireur étant filtré par Projectile.Initialiser.
	public const uint MasqueProjectile = LayerTerrain | LayerJoueur | LayerPnj; // 7
}
