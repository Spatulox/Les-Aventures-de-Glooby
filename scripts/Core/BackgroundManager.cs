using Godot;
using System.Collections.Generic;

// Gère les fonds par région (pas par écran) : un seul Parallax2D visible à
// la fois parmi ceux enregistrés, fondu croisé de 0.5s au changement de
// région plutôt qu'une bascule brute. Les CameraZone appellent AfficherRegion()
// (via leur NomRegion) quand le joueur entre dans leur salle.
public partial class BackgroundManager : Node2D
{
	public static BackgroundManager Instance { get; private set; }

	[Export] public float DureeFondu = 0.5f;

	private readonly Dictionary<string, Node2D> _fonds = new();
	private string _regionActive = "";

	public override void _Ready()
	{
		Instance = this;

		foreach (Node enfant in GetChildren())
		{
			if (enfant is Node2D fond)
				_fonds[enfant.Name] = fond;
		}
	}

	public void AfficherRegion(string nom)
	{
		if (nom == _regionActive || !_fonds.ContainsKey(nom))
			return;

		_regionActive = nom;

		foreach (var (cle, fond) in _fonds)
		{
			var tween = CreateTween();
			float cible = cle == nom ? 1f : 0f;
			tween.TweenProperty(fond, "modulate:a", cible, DureeFondu);
		}

		// Pas de hook musique ici : l'audio n'est PAS calé sur la région (le village
		// et la banquise partagent ce fond sans partager leur musique), et ce point
		// est de toute façon derrière l'early-return ci-dessus. Le branchement se
		// fait dans DeclencheurZone.AppliquerCommeSalle, via CameraZone.NomAmbiance.
	}
}
