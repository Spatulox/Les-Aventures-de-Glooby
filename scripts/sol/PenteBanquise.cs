using Godot;
using System.Collections.Generic;

// Pente de sol de banquise, raccordable avec les segments SolBanquise :
// fabriquée par décalage de colonnes du segment A, ses bords gauche/droit
// sont exactement ceux d'un segment plat, aux extrémités la surface est
// donc au même niveau qu'un sol plat posé à côté (surface locale à y=-29
// côté gauche pour les deux sens, le côté droit étant plus haut/bas de
// DeniveleTotal). La collision est un CollisionPolygon2D dont
// le dessus suit la diagonale de neige : pas de marches invisibles.
// Chaque type a sa propre scène, qui porte sprite ET CollisionPolygon2D : elle
// est la seule source de vérité, le script ne réapplique rien au runtime.
// Surface de marche : y = -29 à l'entrée (côté gauche), bas du polygone à 62.
public partial class PenteBanquise : StaticBody2D
{
	public enum TypePente { DouceMontante, DouceDescendante, ForteMontante, ForteDescendante }

	[Export] public TypePente Type = TypePente.DouceMontante;

	// Largeur d'un emplacement, identique aux segments plats.
	public const float LargeurSegment = SolBanquise.LargeurSegment;

	private record Config(string Texture, int Denivele, bool Montante);

	// Denivele en pixels natifs (68 -> ~22 deg, 171 -> 45 deg), x2 à l'écran.
	private static readonly Dictionary<TypePente, Config> Configs = new()
	{
		[TypePente.DouceMontante] = new("res://assets/sol/pente_douce_montante.png", 68, true),
		[TypePente.DouceDescendante] = new("res://assets/sol/pente_douce_descendante.png", 68, false),
		[TypePente.ForteMontante] = new("res://assets/sol/pente_forte_montante.png", 171, true),
		[TypePente.ForteDescendante] = new("res://assets/sol/pente_forte_descendante.png", 171, false),
	};

	// Écart de hauteur (en pixels écran) entre les deux extrémités : la pièce
	// suivante côté haut se place DeniveleTotal plus haut que côté bas.
	public float DeniveleTotal => Configs[Type].Denivele * 2f;
}
