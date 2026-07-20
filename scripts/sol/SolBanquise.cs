using Godot;

// Segment de sol de banquise : pièce posable côte à côte pour former un sol
// continu de n'importe quelle longueur. Les 3 centres partagent les mêmes
// bords (greffés depuis le segment A, auto-tuilable) donc sont
// interchangeables ; les embouts portent la cassure de glace et les
// stalactites décoratives (hors collision).
//
// Chaque type a sa propre scène (scenes/sol/SolBanquiseXxx.tscn) qui porte son
// sprite ET sa CollisionShape2D. La scène est la SEULE source de vérité : le
// script ne réapplique plus rien au runtime, sinon régler la collision dans
// l'éditeur ne servirait à rien (elle serait écrasée au lancement, et joueur
// comme PNJ se poseraient à une hauteur qui ne correspond pas au visuel).
// Surface de marche : y = -29 en local, bas de la collision à y = 62.
public partial class SolBanquise : StaticBody2D
{
	public enum TypeSegment { CentreA, CentreB, CentreC, EmboutGauche, EmboutDroit }

	// Largeur d'un emplacement dans une ligne (172px natif x2).
	public const float LargeurSegment = 344f;

	public override void _Ready()
	{
		// Seul réglage encore fait en code : la géométrie vient de la scène.
		// Layer 1 (terrain, vu par le joueur) + layer sol des PNJ. Voir Constantes.
		CollisionLayer |= Constantes.LayerSolPnj;
	}
}
