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

	// Le layer 1 est ambigu : le terrain ET le joueur s'y trouvent (le joueur n'a
	// pas de collision_layer explicite, donc il hérite du défaut). Un PNJ qui
	// masquerait le layer 1 pour tenir sur le sol se cognerait donc aussi au
	// joueur. Ce layer 2 est le sol vu par les PNJ : tout terrain plein s'y
	// déclare EN PLUS du layer 1, et les PNJ ne masquent que celui-ci.
	// Conséquence voulue : le joueur (layer 1 seul) leur reste invisible.
	// Ajouté au layer 1 plutôt que substitué → un terrain qu'on oublierait de
	// marquer resterait solide pour le joueur (la panne est côté PNJ, pas côté
	// joueur, ce qui est le sens le moins grave).
	public const uint LayerSolPnj = 1 << 1; // layer 2 dans l'éditeur

	// Ce qu'un PNJ doit masquer pour marcher : le sol plein + les plateformes
	// traversables. Volontairement sans le layer 1, pour ne pas heurter le joueur.
	public const uint MasqueSolPnj = LayerSolPnj | LayerPlateformesTraversables; // 18
}
