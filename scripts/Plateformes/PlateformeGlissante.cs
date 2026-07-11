using Godot;

// Plateforme glissante : glace lisse au lieu de neige. N'a aucune logique de
// déplacement propre ; expose juste FacteurFriction, lu par Player via une
// requête physique sous ses pieds (voir Player.ObtenirFrictionSol). 1 =
// friction normale, proche de 0 = très glissant.
public partial class PlateformeGlissante : StaticBody2D
{
	[Export] public float FacteurFriction = 0.12f;
}
