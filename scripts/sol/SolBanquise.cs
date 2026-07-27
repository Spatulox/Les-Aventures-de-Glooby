using Godot;

// Segment de sol de banquise : pièce posable côte à côte pour former un sol
// continu de n'importe quelle longueur. Les 3 centres partagent les mêmes
// bords (greffés depuis le segment A, auto-tuilable) donc sont
// interchangeables ; les embouts portent la cassure de glace et les
// stalactites décoratives (hors collision).
//
// Chaque type a sa propre scène (scenes/sol/banquise/SolBanquiseXxx.tscn) qui porte son
// sprite ET sa CollisionShape2D. La scène est la SEULE source de vérité : le
// script ne réapplique plus rien au runtime, sinon régler la collision dans
// l'éditeur ne servirait à rien (elle serait écrasée au lancement, et joueur
// comme PNJ se poseraient à une hauteur qui ne correspond pas au visuel).
// Surface de marche : y = -46 en local — c'est le dessus de neige réellement dessiné
// dans sol_centre_*.png, la collision est calée dessus. Toutes les
// pièces de sol (embouts, pentes) doivent partager cette hauteur : un écart, même
// de quelques pixels, crée une micro-marche à la jointure — donc des normales de
// coin parasites que le joueur interprète comme une pente (voir Player.GererPenteRaide).
public partial class SolBanquise : StaticBody2D
{
	public enum TypeSegment { CentreA, CentreB, CentreC, EmboutGauche, EmboutDroit }

	// Largeur d'un emplacement dans une ligne (172px natif x2).
	public const float LargeurSegment = 344f;
}
