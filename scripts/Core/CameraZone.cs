using Godot;

// Zone de caméra façon Hollow Knight : ajuste les limites de la Camera2D du
// joueur (et le fond de région) selon la salle où il se trouve, sans recharger
// de scène. Les limites sont DÉRIVÉES de la CollisionShape2D de la zone (dessine
// le rectangle dans l'éditeur, les bornes suivent) - pas de saisie manuelle.
// La zone n'est PAS déclenchée par BodyEntered : c'est le Player qui, chaque
// frame, détecte par position (Contient) la zone qui le contient et l'applique
// (robuste aux téléportations/respawn, plus besoin de RegionTrigger séparés).
public partial class CameraZone : DeclencheurZone, IZoneCamera
{
	// Groupe rassemblant toutes les zones caméra : le Player le parcourt pour
	// trouver la zone qui le contient (détection continue, sans BodyEntered).
	public const string Groupe = "zones_camera";

	// Marge sous le bas de la salle pour le filet anti-chute (remplace l'ancien +300 codé en dur).
	[Export] public float MargeChuteVide = 300f;

	// Fond de région à afficher dans cette zone (ex. "banquise", "grotte").
	// Vide = ne pas toucher au fond. Remplace les anciens RegionTrigger.
	[Export] public string NomRegion = "";

	// Nature de la salle. Enum (et non booléen) pour pouvoir en ajouter d'autres
	// - Interieur, Arene... - sans casser les zones déjà posées.
	public enum TypeZone { Exterieur, Souterrain }

	// Conditionne la météo : aucun blizzard sous terre. Le défaut Exterieur
	// évite d'avoir à retoucher les zones existantes.
	[Export] public TypeZone Type = TypeZone.Exterieur;

	// État météo PROPRE à cette salle : mémorisé ici pour qu'un aller-retour
	// hors de la zone ne permette ni d'annuler ni de re-tirer le blizzard.
	private readonly MeteoZone _meteo = new();

	// Détection par sondage (Contient) côté Player : on s'inscrit juste au groupe
	// et on ne branche PAS BodyEntered (retour false = pas de câblage du signal).
	protected override bool PreparerDeclencheur()
	{
		AddToGroup(Groupe);
		return false;
	}

	// IZoneCamera : applique les limites caméra de cette zone au joueur, bascule le
	// fond de région associé et la météo. Appelée par le Player UNE SEULE FOIS par
	// entrée dans la zone (hystérésis de MettreAJourZoneCamera) - c'est donc le
	// bon endroit pour le tirage au sort du blizzard.
	// (Contient + le calcul des bornes sont mutualisés dans DeclencheurZone.)
	public void Appliquer(Player joueur)
	{
		AppliquerCommeSalle(joueur, NomRegion, MargeChuteVide);

		bool blizzard = _meteo.AuChangementDeZone(Type == TypeZone.Souterrain);
		GestionnaireMeteo.Instance?.AfficherBlizzard(this, blizzard, changementDeZone: true);
	}

	// La minuterie météo tourne dans TOUTES les salles, même celles où le joueur
	// n'est pas : le monde continue de vivre et un blizzard commencé s'épuise.
	// Seule la salle courante se voit à l'écran (filtre côté GestionnaireMeteo).
	public override void _Process(double delta)
	{
		if (_meteo.Avancer(delta))
			GestionnaireMeteo.Instance?.AfficherBlizzard(this, _meteo.BlizzardActif);
	}
}
