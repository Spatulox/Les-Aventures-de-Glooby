using Godot;

// Zone de caméra façon Hollow Knight : ajuste les limites de la Camera2D du
// joueur (et le fond de région) selon la salle où il se trouve, sans recharger
// de scène. Les limites sont DÉRIVÉES de la CollisionShape2D de la zone (dessine
// le rectangle dans l'éditeur, les bornes suivent) - pas de saisie manuelle.
// La zone n'est PAS déclenchée par BodyEntered : c'est le Player qui, chaque
// frame, détecte par position (Contient) la zone qui le contient et l'applique
// (robuste aux téléportations/respawn, plus besoin de RegionTrigger séparés).
public partial class CameraZone : DeclencheurZone
{
	// Groupe rassemblant toutes les zones caméra : le Player le parcourt pour
	// trouver la zone qui le contient (détection continue, sans BodyEntered).
	public const string Groupe = "zones_camera";

	// Marge sous le bas de la salle pour le filet anti-chute (remplace l'ancien +300 codé en dur).
	[Export] public float MargeChuteVide = 300f;

	// Fond de région à afficher dans cette zone (ex. "banquise", "grotte").
	// Vide = ne pas toucher au fond. Remplace les anciens RegionTrigger.
	[Export] public string NomRegion = "";

	// Détection par sondage (Contient) côté Player : on s'inscrit juste au groupe
	// et on ne branche PAS BodyEntered (retour false = pas de câblage du signal).
	protected override bool PreparerDeclencheur()
	{
		AddToGroup(Groupe);
		return false;
	}

	// Applique les limites caméra de cette zone au joueur, et bascule le fond de
	// région associé. Appelée par le Player quand il entre dans la zone.
	public void Appliquer(Player joueur)
	{
		if (!CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b))
		{
			GD.PushWarning($"CameraZone '{Name}' : aucun RectangleShape2D exploitable, limites ignorées.");
			return;
		}

		joueur.DefinirZoneCamera(g, d, h, b, MargeChuteVide);

		if (!string.IsNullOrEmpty(NomRegion))
			BackgroundManager.Instance?.AfficherRegion(NomRegion);
	}

	// Le point monde (ex. position de respawn) est-il dans le rectangle de la zone ?
	public bool Contient(Vector2 point)
	{
		if (!CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b))
			return false;

		return point.X >= g && point.X <= d && point.Y >= h && point.Y <= b;
	}

	// Bornes = AABB monde du rectangle de collision. Utilise la transform globale
	// de la CollisionShape2D (donc tout décalage/scale du nœud est respecté). La
	// forme est trouvée par type, pas par nom : marche pour le camera_zone.tscn
	// comme pour d'anciens nœuds auto-nommés. Rotation non gérée (AABB alignée).
	private bool CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b)
	{
		g = d = h = b = 0;

		CollisionShape2D forme = null;
		foreach (var enfant in GetChildren())
		{
			if (enfant is CollisionShape2D cs)
			{
				forme = cs;
				break;
			}
		}

		if (forme?.Shape is not RectangleShape2D rect)
			return false;

		var centre = forme.GlobalPosition;
		var demi = rect.Size * 0.5f * forme.GlobalScale;
		g = Mathf.RoundToInt(centre.X - demi.X);
		d = Mathf.RoundToInt(centre.X + demi.X);
		h = Mathf.RoundToInt(centre.Y - demi.Y);
		b = Mathf.RoundToInt(centre.Y + demi.Y);
		return true;
	}
}
