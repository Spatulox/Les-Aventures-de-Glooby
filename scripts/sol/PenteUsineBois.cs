using Godot;

// Pente de sol d'usine en bois, raccordable avec les segments SolUsineBois : une
// dalle de planches inclinée d'épaisseur constante, dont la surface (diagonale)
// est calée, à ses deux extrémités, sur la même hauteur qu'un sol plat posé à
// côté (surface locale y = 8, comme SolUsineBois) — le côté haut étant plus haut
// de DeniveleTotal. La collision est un CollisionPolygon2D qui épouse la surface
// et la base : pas de marches invisibles.
//
// Chaque type a sa propre scène (scenes/sol/usine/PenteUsineXxx.tscn) qui
// porte le Sprite2D ET le CollisionPolygon2D : la scène est la seule source de
// vérité, le script ne réapplique rien au runtime (comme PenteBanquise).
public partial class PenteUsineBois : StaticBody2D
{
	public enum TypePente { DouceMontante, DouceDescendante, ForteMontante, ForteDescendante }

	[Export] public TypePente Type = TypePente.DouceMontante;

	// Largeur d'un emplacement, identique aux segments plats.
	public const float LargeurSegment = SolUsineBois.LargeurSegment;

	// Une pente forte (~45°) se dévale en glissade (Player.GererPenteRaide) ; les
	// douces (~22°) restent praticables à pied.
	public bool EstForte => Type is TypePente.ForteMontante or TypePente.ForteDescendante;

	// Écart de hauteur (en pixels écran) entre les deux extrémités : la pièce
	// suivante côté haut se place DeniveleTotal plus haut que côté bas.
	// Dénivelés natifs 68 (~22°) et 171 (~45°), ×2 à l'écran.
	public float DeniveleTotal => (EstForte ? 171f : 68f) * 2f;
}
