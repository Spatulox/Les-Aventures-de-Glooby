using Godot;

// Variante suspendue de la plateforme mobile en bois. Elle hérite de
// PlateformeMobile pour en réutiliser les réglages ([Export] AngleDegres,
// Distance, Vitesse, AllerRetour) et le gizmo éditeur, et n'ajoute que le
// dressing :
//  - une poulie fixe en haut + une chaîne qui se retrace en continu de la poulie
//    jusqu'au sommet de la plateforme (elle s'allonge quand la plateforme descend) ;
//  - un léger balancement horizontal (pendule) qui incline naturellement la chaîne.
// Le va-et-vient et le balancement sont posés en UNE seule assignation de
// GlobalPosition (on ne relit jamais GlobalPosition après coup — valeur périmée
// sur un AnimatableBody sync_to_physics). Le balancement est une simple
// translation horizontale : le joueur reste porté (comme sur une plateforme
// mobile horizontale) et la collision ne tourne JAMAIS, donc pas de glissade
// parasite. Poulie et chaîne sont des nœuds frères, désignés par NodePath.
[Tool]
public partial class PlateformeMobileSuspendue : PlateformeMobile
{
	// Sommet du deck en coordonnées locales du corps (sprite ×2, dessus dessiné à
	// y=8 natif → (8-35)*2 = -54) : point d'accroche de la chaîne.
	private const float AncrageDeckY = -54f;

	[Export] public NodePath CheminPoulie;
	[Export] public NodePath CheminChaine;
	[Export] public float AmplitudeBalancement = 6f;   // pixels
	[Export] public float VitesseBalancement = 2.2f;   // rad/s

	private Node2D _poulie;
	private Sprite2D _chaine;
	private Vector2 _origine;
	private float _progression;
	private int _sens = 1;
	private float _tempsBalancement;

	private Vector2 Direction => Vector2.Right.Rotated(Mathf.DegToRad(AngleDegres));

	public override void _Ready()
	{
		base._Ready();          // SyncToPhysics + gizmo éditeur
		_origine = GlobalPosition;
		if (CheminPoulie != null && !CheminPoulie.IsEmpty)
			_poulie = GetNodeOrNull<Node2D>(CheminPoulie);
		if (CheminChaine != null && !CheminChaine.IsEmpty)
			_chaine = GetNodeOrNull<Sprite2D>(CheminChaine);
		MettreAJourChaine();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || Distance <= 0f)
		{
			MettreAJourChaine();
			return;
		}

		var dt = (float)delta;
		_progression += Vitesse * dt * _sens;

		if (AllerRetour)
		{
			if (_progression >= Distance) { _progression = Distance; _sens = -1; }
			else if (_progression <= 0f) { _progression = 0f; _sens = 1; }
		}
		else
		{
			_progression = Mathf.PosMod(_progression, Distance);
		}

		_tempsBalancement += dt;
		float balancement = AmplitudeBalancement * Mathf.Sin(_tempsBalancement * VitesseBalancement);

		// Va-et-vient vertical + balancement horizontal en une seule assignation.
		GlobalPosition = _origine + Direction * _progression + new Vector2(balancement, 0f);
		MettreAJourChaine();
	}

	// Retrace la chaîne : de la poulie (fixe) jusqu'au sommet du deck. La longueur
	// suit le déplacement vertical, l'inclinaison suit le balancement horizontal.
	private void MettreAJourChaine()
	{
		if (_poulie == null || _chaine == null)
			return;

		Vector2 haut = _poulie.GlobalPosition;
		Vector2 bas = GlobalPosition + new Vector2(0f, AncrageDeckY);
		Vector2 direction = bas - haut;

		_chaine.GlobalPosition = haut;
		_chaine.Rotation = direction.Angle() - Mathf.Pi / 2f;   // la texture du maillon pointe +Y
		if (_chaine.RegionEnabled)
		{
			var rect = _chaine.RegionRect;
			rect.Size = new Vector2(rect.Size.X, direction.Length() / Mathf.Max(0.001f, _chaine.Scale.Y));
			_chaine.RegionRect = rect;
		}
	}
}
