using Godot;

// Zone de déclenchement : exécute une action quand le joueur entre dans l'Area2D.
// Deux usages : héritage (override SurEntreeJoueur) ou composition (connecter le
// signal JoueurEntre depuis un parent). UneSeuleFois limite à un seul déclenchement.
public partial class DeclencheurZone : Area2D
{
	[Signal] public delegate void JoueurEntreEventHandler(Player joueur);

	[Export] public bool UneSeuleFois;

	private bool _declenche;

	public override void _Ready()
	{
		if (!PreparerDeclencheur())   // permet à une sous-classe d'annuler (ex. QueueFree)
			return;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player joueur)
			return;
		if (UneSeuleFois && _declenche)
			return;

		_declenche = true;
		SurEntreeJoueur(joueur);
		EmitSignal(SignalName.JoueurEntre, joueur);
	}

	// Init avant branchement ; retourner false pour ne pas s'activer.
	protected virtual bool PreparerDeclencheur() => true;

	// Hook d'héritage ; par défaut ne fait rien (usage signal pur).
	protected virtual void SurEntreeJoueur(Player joueur) { }

	// Rectangle de collision de la zone → bornes monde (AABB alignée). Utilise la
	// transform globale de la CollisionShape2D (décalage/scale respectés ; rotation
	// non gérée). Réutilisé par les zones caméra ET la salle de boss pour dériver
	// limites/bornes du rectangle dessiné dans l'éditeur, sans saisie manuelle.
	protected bool CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b)
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

	// Le point monde (respawn, position joueur...) est-il dans le rectangle de la
	// zone ? Détection par position, sans BodyEntered (robuste aux téléportations).
	public bool Contient(Vector2 point)
	{
		if (!CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b))
			return false;

		return point.X >= g && point.X <= d && point.Y >= h && point.Y <= b;
	}

	// Applique cette zone comme salle caméra : cale les limites de la Camera2D du
	// joueur sur le rectangle de la zone et bascule le fond de région. Partagé par
	// CameraZone (salle normale) et ZoneBoss (arène de boss) - voir IZoneCamera.
	protected void AppliquerCommeSalle(Player joueur, string nomRegion, float margeChute)
	{
		if (!CalculerLimitesDepuisForme(out int g, out int d, out int h, out int b))
		{
			GD.PushWarning($"Zone '{Name}' : aucun RectangleShape2D exploitable, limites ignorées.");
			return;
		}

		joueur.DefinirZoneCamera(g, d, h, b, margeChute);

		if (!string.IsNullOrEmpty(nomRegion))
			BackgroundManager.Instance?.AfficherRegion(nomRegion);
	}
}
