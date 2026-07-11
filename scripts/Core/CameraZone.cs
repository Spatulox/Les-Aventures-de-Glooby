using Godot;

// Zone de caméra façon Hollow Knight : ajuste les limites de la Camera2D du
// joueur en entrant dans la salle, sans recharger de scène. Les limites sont
// DÉRIVÉES de la CollisionShape2D de la zone (dessine le rectangle dans
// l'éditeur, les bornes suivent) - pas de saisie manuelle. Les zones se
// chevauchent volontairement aux transitions - la dernière traversée gagne.
public partial class CameraZone : DeclencheurZone
{
	// Marge sous le bas de la salle pour le filet anti-chute (remplace l'ancien +300 codé en dur).
	[Export] public float MargeChuteVide = 300f;

	protected override void SurEntreeJoueur(Player joueur)
	{
		if (!CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b))
		{
			GD.PushWarning($"CameraZone '{Name}' : aucun RectangleShape2D exploitable, limites ignorées.");
			return;
		}

		joueur.DefinirZoneCamera(g, d, h, b, MargeChuteVide);
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
