using Godot;
using System.Collections.Generic;

// Rendu de la météo, calqué sur BackgroundManager (singleton, fondu plutôt que
// bascule brute) mais sur un CanvasLayer : le voile sombre et les flocons sont
// en espace ÉCRAN, donc ils suivent la caméra gratuitement, sans dépendre de la
// position du joueur dans le monde.
//
// Le gestionnaire ne décide RIEN : l'état météo appartient à chaque salle
// (MeteoZone, porté par CameraZone), qui vient ici demander son affichage.
public partial class GestionnaireMeteo : CanvasLayer
{
	public static GestionnaireMeteo Instance { get; private set; }

	[Export] public float DureeFondu = 1f;

	// Salle dont la météo est actuellement à l'écran : les zones font tourner
	// leur minuterie même quand le joueur est ailleurs, on ignore donc les
	// notifications qui ne viennent pas de la salle courante.
	private CameraZone _zoneActive;

	// Le conteneur porte le modulate (un CanvasLayer n'en a pas) : un seul
	// fondu suffit pour le voile ET les flocons.
	private Control _blizzard;
	private readonly List<GpuParticles2D> _flocons = new();
	private bool _actif;

	// Le pointeur statique doit lâcher prise à la sortie d'arbre, comme celui de
	// BackgroundManager : sans ça il désigne encore le gestionnaire de la scène quittée
	// (chaque niveau a le sien), et la première demande de la nouvelle scène tentait un
	// fondu sur un voile déjà libéré — ObjectDisposedException intermittente.
	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public override void _Ready()
	{
		Instance = this;

		_blizzard = GetNode<Control>("Blizzard");

		// Les variantes de flocons sont récupérées par parcours : en ajouter une
		// dans la scène ne demande aucune modification de ce script.
		foreach (Node enfant in _blizzard.GetChildren())
		{
			if (enfant is GpuParticles2D particules)
				_flocons.Add(particules);
		}
	}

	// Demande d'affichage venant d'une salle. Le changement de salle a priorité
	// (il redéfinit la salle courante) ; un simple tick d'une autre salle est
	// ignoré.
	public void AfficherBlizzard(CameraZone zone, bool actif, bool changementDeZone = false)
	{
		if (changementDeZone)
			_zoneActive = zone;
		else if (zone != _zoneActive)
			return;

		if (actif == _actif)
			return;

		_actif = actif;
		Effets.Fondu(_blizzard, actif ? 1f : 0f, DureeFondu);

		// Le son bascule ICI, et pas dans GestionnaireAudio : c'est le seul endroit
		// qui sache que le blizzard VISIBLE change. Les minuteries tournent dans
		// toutes les salles, donc sans le filtre par salle ci-dessus l'expiration
		// d'un blizzard à l'autre bout de la carte couperait la musique du joueur.
		GestionnaireAudio.Instance?.DefinirEtat(actif ? "blizzard" : AmbianceSonore.EtatNormal);

		foreach (var particules in _flocons)
			particules.Emitting = actif;
	}
}
