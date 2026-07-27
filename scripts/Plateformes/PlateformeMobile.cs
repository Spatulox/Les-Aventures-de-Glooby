using Godot;

// Bloc de glace flottant qui va-et-vient en LIGNE DROITE, direction réglée
// directement dans l'inspecteur : un angle en degrés + une distance en pixels.
// AnimatableBody2D + sync_to_physics : le joueur est correctement entraîné par
// le mouvement (CharacterBody2D hérite la vitesse de la plateforme porteuse).
//
// Convention d'angle (repère Godot, Y vers le bas) : 0° = droite, 90° = bas,
// 180° = gauche, 270° = haut. Pas besoin de deviner : le script est [Tool] et
// dessine la trajectoire (ligne + point d'arrivée) EN DIRECT dans l'éditeur.
//
// Traversable comme PlateformeUnidirectionnelle : la .tscn place la collision sur
// le layer traversable (Constantes.LayerPlateformesTraversables = 16, masque 0) et
// coche one_way_collision sur la CollisionShape2D → on peut sauter DEDANS par en
// dessous et atterrir dessus, et redescendre en bas+saut (Player.GererTraverseePlateforme).
[Tool]
public partial class PlateformeMobile : AnimatableBody2D
{
	private float _angleDegres = 0f;
	// Direction du mouvement, en degrés (voir convention ci-dessus).
	// PropertyHint.Range -> slider 0..360 dans l'inspecteur (pas de 1°).
	[Export(PropertyHint.Range, "0,360,1,suffix:°")] public float AngleDegres
	{
		get => _angleDegres;
		set { _angleDegres = value; QueueRedraw(); }
	}

	private float _distance = 260f;
	// Amplitude du va-et-vient, en pixels globaux.
	[Export] public float Distance
	{
		get => _distance;
		set { _distance = Mathf.Max(0f, value); QueueRedraw(); }
	}

	[Export] public float Vitesse = 60f;
	[Export] public bool AllerRetour = true;

	// Origine figée au lancement : on part de la position posée et on oscille sur
	// UN côté (origine -> origine + direction*Distance -> retour).
	private Vector2 _origine;
	private float _progression;
	private int _sens = 1;

	// Position posée dans l'éditeur, autour de laquelle se fait le va-et-vient.
	protected Vector2 Origine => _origine;

	protected Vector2 Direction => Vector2.Right.Rotated(Mathf.DegToRad(_angleDegres));

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			QueueRedraw();   // pas de simulation en éditeur, juste le gizmo
			return;
		}

		SyncToPhysics = true;
		_origine = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || _distance <= 0f)
			return;

		GlobalPosition = _origine + AvancerDeplacement((float)delta);
	}

	// Avance le va-et-vient d'un pas de temps et renvoie le décalage à appliquer
	// depuis Origine. Isolé de _PhysicsProcess pour que les variantes
	// (PlateformeMobileSuspendue) composent leur propre déplacement par-dessus
	// sans redupliquer la logique d'aller-retour.
	protected Vector2 AvancerDeplacement(float dt)
	{
		if (_distance <= 0f)
			return Vector2.Zero;

		_progression += Vitesse * dt * _sens;

		if (AllerRetour)
		{
			if (_progression >= _distance) { _progression = _distance; _sens = -1; }
			else if (_progression <= 0f) { _progression = 0f; _sens = 1; }
		}
		else
		{
			_progression = Mathf.PosMod(_progression, _distance);
		}

		return Direction * _progression;
	}

	// Gizmo éditeur : trajectoire de la plateforme (compensée de l'échelle du
	// nœud pour rester juste même si l'instance est mise à l'échelle).
	public override void _Draw()
	{
		if (!Engine.IsEditorHint())
			return;

		float echelle = Mathf.Abs(GlobalScale.X) < 0.001f ? 1f : GlobalScale.X;
		Vector2 fin = Direction * (_distance / echelle);
		var couleur = new Color(0.4f, 0.9f, 1f);
		DrawLine(Vector2.Zero, fin, couleur, 2f);
		DrawCircle(fin, 6f, couleur);
	}
}
