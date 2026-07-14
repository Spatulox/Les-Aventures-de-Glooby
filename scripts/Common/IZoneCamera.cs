using Godot;

// Zone qui pilote la Camera2D du joueur : limites de la salle + fond de région.
// Implémentée par CameraZone (salle normale) ET par ZoneBoss (arène de boss), pour
// que le Player les détecte de façon uniforme - sondage par position chaque frame
// (Contient), sans BodyEntered. Contient(...) est fourni par DeclencheurZone.
public interface IZoneCamera
{
	// Le point monde est-il dans le rectangle de la zone ?
	bool Contient(Vector2 point);

	// Cale la caméra du joueur sur cette salle (limites + fond de région).
	void Appliquer(Player joueur);
}
