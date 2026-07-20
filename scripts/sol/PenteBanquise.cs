using Godot;
using System.Collections.Generic;

// Pente de sol de banquise, raccordable avec les segments SolBanquise :
// fabriquée par décalage de colonnes du segment A, ses bords gauche/droit
// sont exactement ceux d'un segment plat, aux extrémités la surface est
// donc au même niveau qu'un sol plat posé à côté (surface locale à y=-50
// du côté bas... côté gauche pour les deux sens, le côté droit étant plus
// haut/bas de DeniveleTotal). La collision est un CollisionPolygon2D dont
// le dessus suit la diagonale de neige : pas de marches invisibles.
// Le .tscn embarque le visuel du type par défaut (DouceMontante) pour que la
// pièce soit VISIBLE dans l'éditeur ; _Ready le ré-applique ensuite depuis Type.
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

	public override void _Ready()
	{
		// Layer 1 (terrain, vu par le joueur) + layer sol des PNJ. Voir Constantes.
		CollisionLayer |= Constantes.LayerSolPnj;

		var config = Configs[Type];
		int d = config.Denivele;

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);
		sprite.Position = new Vector2(0, config.Montante ? -d : d);

		// Parallélogramme : dessus en diagonale sur la neige, flancs verticaux.
		float hautDroit = config.Montante ? -50f - 2 * d : -50f + 2 * d;
		var polygone = GetNode<CollisionPolygon2D>("CollisionPolygon2D");
		polygone.Polygon = new[]
		{
			new Vector2(-172, -50),
			new Vector2(172, hautDroit),
			new Vector2(172, hautDroit + 112),
			new Vector2(-172, 62),
		};
	}
}
