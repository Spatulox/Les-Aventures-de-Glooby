using Godot;

// Segment de mur de grotte : bloc de paroi solide (StaticBody2D) posable pour
// habiller les parois de la grotte, pendant vertical de SolGrotte. Les 3 centres
// (A/B/C) sont des variantes de briques de glace interchangeables ; le haut
// coiffe la paroi (glace ruisselante) et le bas en pose la base.
//
// Chaque type a sa propre scène (scenes/mur/MurGrotteXxx.tscn) portant son sprite
// ET sa CollisionShape2D : la scène est la SEULE source de vérité, le script ne
// réapplique rien au runtime. Le mur reste sur la couche de collision 1 (non
// traversable), comme le sol.
public partial class MurGrotte : StaticBody2D
{
	public enum TypeSegment { Haut, CentreA, CentreB, CentreC, Bas }

	// Hauteurs affichées de chaque pièce (natif x2) : pas de progression verticale d'une colonne.
	public const float HauteurHaut = 68f;
	public const float HauteurCentre = 122f;
	public const float HauteurBas = 78f;
}
