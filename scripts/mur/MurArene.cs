using Godot;

// Paroi qui FERME une arène sur les côtés. Les limites de caméra d'une ZoneBoss ne
// bloquent personne : elles cadrent l'image, mais joueur, PNJ et boss peuvent très bien
// continuer à marcher au-delà, hors champ. Ce mur pose la butée physique correspondante,
// sur la couche de collision 1 comme le sol — donc opposable à tout ce qui a la couche 1
// dans son masque (joueur, PnjAmical, Boss).
//
// MurNonAgrippable, et ce n'est pas un détail : le jeu a le wall jump, et une paroi
// pleine de la hauteur de la salle se remonterait jusqu'à sortir par le haut, soit
// exactement ce qu'on cherche à empêcher.
//
// Sans sprite, contrairement à MurGrotte : elle se pose au ras de la limite caméra, donc
// au bord de l'écran, et le fond de la salle est déjà derrière. C'est son rectangle de
// collision qui la rend visible et déplaçable dans l'éditeur ; on l'étire par le `scale`
// de l'instance, comme une CameraZone.
public partial class MurArene : StaticBody2D, MurNonAgrippable
{
}
