using Godot;

// État météo d'UNE salle : tirage du blizzard et minuterie associée. Ce n'est
// pas un Node - c'est un simple objet que la zone (CameraZone) garde en champ,
// pour que la logique soit réutilisable par n'importe quelle autre zone plus
// tard (ex. ZoneBoss) sans dupliquer le tirage.
//
// La météo est MÉMORISÉE par zone : sans ça, le joueur pourrait sortir de la
// salle et y rerentrer pour annuler un blizzard (ou relancer le tirage en
// boucle jusqu'à l'obtenir). Le garde-fou est un compte à rebours UNIQUE,
// partagé par les deux issues du tirage : tant qu'il n'est pas écoulé, entrer
// dans la zone ne fait que restituer l'état déjà décidé.
public class MeteoZone
{
	// Probabilité de déclencher un blizzard à un changement de zone.
	// TEMPORAIRE : à 1f pour les tests (valeur de jeu = 0.1f).
	public const float ChanceBlizzard = 0.2f;

	// Bornes de la durée d'un blizzard, en secondes.
	public const float DureeMin = 10f;
	public const float DureeMax = 30f;

	// Après un tirage raté, délai avant d'avoir de nouveau le droit de tirer :
	// c'est ce qui empêche de re-tirer en spammant les allers-retours.
	public const float DelaiEntreTirages = 15f;

	public bool BlizzardActif { get; private set; }

	// Temps restant avant la fin du blizzard (s'il est actif) ou avant le
	// prochain tirage autorisé (sinon) - voir le commentaire de classe.
	private float _tempsRestant;

	// Appelée une fois par entrée dans la salle. Ne tire au sort que si le
	// compte à rebours mémorisé est écoulé ; en souterrain, jamais de blizzard.
	// Retourne l'état météo à afficher.
	public bool AuChangementDeZone(bool souterrain)
	{
		if (souterrain)
		{
			BlizzardActif = false;
			_tempsRestant = 0f;
			return false;
		}

		if (_tempsRestant > 0f)
			return BlizzardActif;

		BlizzardActif = GD.Randf() < ChanceBlizzard;
		_tempsRestant = BlizzardActif
			? (float)GD.RandRange(DureeMin, DureeMax)
			: DelaiEntreTirages;

		return BlizzardActif;
	}

	// Fait s'écouler la minuterie. Retourne true si l'état météo vient de
	// changer (fin de blizzard), pour que l'appelant rafraîchisse l'affichage.
	public bool Avancer(double delta)
	{
		if (_tempsRestant <= 0f)
			return false;

		_tempsRestant -= (float)delta;
		if (_tempsRestant > 0f)
			return false;

		if (!BlizzardActif)
			return false; // Fin d'un délai d'attente : rien de visible ne change.

		BlizzardActif = false;
		_tempsRestant = DelaiEntreTirages;
		return true;
	}
}
