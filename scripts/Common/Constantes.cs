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

	// Ce que masque un projectile : tout ce sur quoi on marche (terrain ET
	// plateformes traversables — le sol de monde1 est entièrement bâti en
	// PlateformeUnidirectionnelle, sans le layer 5 les tirs traverseraient donc le
	// plancher) plus le joueur et les PNJ (pour blesser) — la même scène est tirée
	// par le joueur et par les ennemis, le tireur étant filtré par
	// Projectile.Initialiser. Le « one-way » ne s'applique qu'aux corps physiques :
	// une Area2D détecte la plateforme quel que soit le sens d'arrivée, un projectile
	// éclate donc dessus aussi bien par-dessous que par-dessus — voulu, un tir est
	// arrêté par le décor solide.
	public const uint MasqueProjectile = MasqueMarcheur | LayerJoueur | LayerPnj; // 23

	// Strates de rendu (z_index), de l'arrière vers l'avant. Les valeurs
	// négatives décrivent l'existant déjà posé dans les .tscn ; elles sont
	// listées ici pour qu'on puisse lire l'empilement d'un coup d'œil.
	public const int ZFond = -100;    // ciels fixes : FondBanquise / FondGrotte
	public const int ZDecor = -1;     // props de décor (Rocher, ColonneGlace, …)
	public const int ZPlanDeJeu = 0;  // sol, plateformes, PNJ, projectiles

	// Le joueur est volontairement une strate au-dessus du plan de jeu. À z égal,
	// Godot dessine dans l'ordre de l'arbre : tout ce qui est instancié en cours
	// de partie (plateformes de glace posées par le pouvoir, futurs spawns)
	// arrive en fin d'arbre, donc après le joueur, et le recouvrirait.
	public const int ZJoueur = 1;

	public const int ZDialogue = 100; // bulles de dialogue, au-dessus de tout le reste
}
