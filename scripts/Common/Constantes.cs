// Constantes partagées du jeu : source de vérité unique pour les valeurs
// réutilisées par les salles, le monde et les entités (évite les
// redéclarations qui pourraient diverger silencieusement).
public static class Constantes
{
	// Taille d'une tuile en pixels (grille des feuilles PixelLab 32x32).
	public const int TailleTuile = 32;

	// Layer physique dédié aux plateformes traversables (one-way). Séparé du
	// layer 1 (terrain/décor normal) pour pouvoir le retirer temporairement
	// du collision_mask du joueur pendant une traversée, sans toucher au
	// reste des collisions.
	public const uint LayerPlateformesTraversables = 1 << 4; // layer 5 dans l'éditeur
}
