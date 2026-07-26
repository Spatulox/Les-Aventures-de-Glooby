using Godot;

// Segment de sol d'usine en bois : pièce posable côte à côte pour former un
// plancher continu de n'importe quelle longueur, équivalent usine de
// SolBanquise / SolGrotte. Les 3 centres (A/B/C) sont des variantes
// décoratives interchangeables (mêmes bords, joint invisible sur une jointure
// de planche) ; les embouts portent la poutre en coupe et la planche cassée.
//
// Chaque type a sa propre scène (scenes/sol/usine/SolUsineBoisXxx.tscn) qui
// porte son sprite ET sa CollisionShape2D : la scène est la SEULE source de
// vérité, le script ne réapplique rien au runtime (sinon régler la collision
// dans l'éditeur serait inutile). C'est aussi ce que la rangée SolUsineBois
// instancie — elle n'invente aucune géométrie.
//
// Repère : origine au bord gauche du segment (sprite non centré) et surface de
// marche à y = 8 en local sur toutes les pièces — c'est le dessus de planche
// réellement dessiné, et les pentes (PenteUsineBois) se calent dessus. Un
// écart, même de quelques pixels, crée une micro-marche à la jointure que le
// joueur interprète comme une pente parasite (voir Player.GererPenteRaide).
public partial class SegmentSolUsineBois : StaticBody2D
{
	public enum TypeSegment { CentreA, CentreB, CentreC, EmboutGauche, EmboutDroit }

	// Largeur affichée d'un emplacement (172px natif ×2), identique au sol banquise.
	public const float LargeurSegment = 344f;
}
