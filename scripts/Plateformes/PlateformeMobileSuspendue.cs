using Godot;

// Variante suspendue de la plateforme mobile en bois. Elle hérite de
// PlateformeMobile pour en réutiliser les réglages ([Export] AngleDegres,
// Distance, Vitesse, AllerRetour) et le gizmo éditeur, et n'ajoute que le
// dressing :
//  - une poulie fixe en haut + une chaîne qui se retrace en continu de la poulie
//    jusqu'au sommet de la plateforme (elle s'allonge quand la plateforme descend) ;
//  - un léger balancement horizontal (pendule) qui incline naturellement la chaîne.
//
// Le nœud scripté est la RACINE de la scène (comme PlateformeMobileBois) : c'est
// ce qui rend Distance/Vitesse/AngleDegres réglables par instance depuis le
// niveau. Poulie et chaîne sont donc des ENFANTS du corps mobile ; pour qu'elles
// paraissent fixes dans le monde, on les contre-déplace du déplacement courant.
// Tout est calculé en coordonnées LOCALES du corps : on ne relit jamais
// GlobalPosition après l'avoir posée (valeur périmée sur un AnimatableBody2D
// sync_to_physics).
//
// Le balancement est une simple translation horizontale : le joueur reste porté
// (comme sur une plateforme mobile horizontale) et la collision ne tourne JAMAIS,
// donc pas de glissade parasite.
[Tool]
public partial class PlateformeMobileSuspendue : PlateformeMobile
{
	// Sommet du deck en coordonnées locales du corps (sprite ×2, dessus dessiné à
	// y=8 natif → (8-35)*2 = -54) : point d'accroche de la chaîne.
	private const float AncrageDeckY = -54f;

	// Position de la poulie, relative à la position POSÉE de la plateforme.
	[Export] public Vector2 DecalagePoulie = new Vector2(0f, -170f);
	[Export] public NodePath CheminPoulie = "Poulie";
	[Export] public NodePath CheminChaine = "Chaine";
	[Export] public float AmplitudeBalancement = 6f;   // pixels
	[Export] public float VitesseBalancement = 2.2f;   // rad/s

	private Node2D _poulie;
	private Sprite2D _chaine;
	private float _tempsBalancement;

	public override void _Ready()
	{
		base._Ready();          // SyncToPhysics + Origine + gizmo éditeur
		if (CheminPoulie != null && !CheminPoulie.IsEmpty)
			_poulie = GetNodeOrNull<Node2D>(CheminPoulie);
		if (CheminChaine != null && !CheminChaine.IsEmpty)
			_chaine = GetNodeOrNull<Sprite2D>(CheminChaine);
		MettreAJourAccroche(Vector2.Zero);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint())
		{
			MettreAJourAccroche(Vector2.Zero);   // aperçu au repos, pas de simulation
			return;
		}

		var dt = (float)delta;
		_tempsBalancement += dt;
		float balancement = AmplitudeBalancement * Mathf.Sin(_tempsBalancement * VitesseBalancement);

		// Va-et-vient (hérité) + balancement horizontal en une seule assignation.
		Vector2 deplacement = AvancerDeplacement(dt) + new Vector2(balancement, 0f);
		GlobalPosition = Origine + deplacement;
		MettreAJourAccroche(deplacement);
	}

	// Recale la poulie (qui doit rester immobile dans le monde alors qu'elle suit
	// le corps) et retrace la chaîne entre elle et le sommet du deck : la longueur
	// suit le déplacement vertical, l'inclinaison suit le balancement horizontal.
	private void MettreAJourAccroche(Vector2 deplacement)
	{
		if (_poulie == null || _chaine == null)
			return;

		Vector2 haut = DecalagePoulie - deplacement;
		Vector2 versDeck = new Vector2(0f, AncrageDeckY) - haut;

		_poulie.Position = haut;
		_chaine.Position = haut;
		_chaine.Rotation = versDeck.Angle() - Mathf.Pi / 2f;   // la texture du maillon pointe +Y
		if (_chaine.RegionEnabled)
		{
			var rect = _chaine.RegionRect;
			rect.Size = new Vector2(rect.Size.X, versDeck.Length() / Mathf.Max(0.001f, _chaine.Scale.Y));
			_chaine.RegionRect = rect;
		}
	}
}
