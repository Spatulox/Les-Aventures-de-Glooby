using Godot;

// Segment de sol de grotte : pièce posable côte à côte pour former un sol
// continu de n'importe quelle longueur, équivalent grotte de SolBanquise.
// Les 3 centres (A/B/C) sont des variantes décoratives interchangeables (mêmes
// bords, auto-tuilables) ; les embouts (fin gauche/droite) portent la cassure
// de glace et les stalactites décoratives qui débordent hors collision.
//
// Chaque type a sa propre scène (scenes/sol/grotte/SolGrotteXxx.tscn) qui porte son
// sprite ET sa CollisionShape2D : la scène est la SEULE source de vérité, le
// script ne réapplique rien au runtime (sinon régler la collision dans l'éditeur
// serait inutile). Surface de marche : y = -84 en local sur toutes les pièces —
// un écart, même de quelques pixels, crée une micro-marche à la jointure que le
// joueur interprète comme une pente parasite (voir Player.GererPenteRaide).
public partial class SolGrotte : StaticBody2D
{
	public enum TypeSegment { CentreA, CentreB, CentreC, EmboutGauche, EmboutDroit }

	// Largeur affichée d'un centre (128px natif x2) : pas de progression dans une ligne.
	public const float LargeurCentre = 256f;
	public const float LargeurEmboutGauche = 182f;
	public const float LargeurEmboutDroit = 178f;
}
