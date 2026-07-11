using Godot;

// Bloc de glace flottant qui suit un Path2D interne (courbe éditable dans
// l'éditeur). AnimatableBody2D + sync_to_physics : le joueur est correctement
// entraîné par le mouvement (CharacterBody2D hérite la vitesse de la
// plateforme porteuse tant que sync_to_physics est actif).
public partial class PlateformeMobile : AnimatableBody2D
{
	[Export] public float Vitesse = 60f;
	[Export] public bool AllerRetour = true;

	private Curve2D _courbe;
	// Position de départ figée une fois pour toutes : le Path2D est un enfant
	// de ce corps qui se déplace, donc échantillonner sa position globale en
	// direct créerait une boucle de rétroaction (la cible s'éloigne à chaque
	// fois que le corps bouge). On échantillonne la courbe dans son espace
	// local et on ajoute cette origine fixe à la place.
	private Vector2 _origine;
	private float _progression;
	private int _sens = 1;

	public override void _Ready()
	{
		SyncToPhysics = true;

		_courbe = GetNode<Path2D>("Path2D").Curve;
		_origine = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		var longueur = _courbe.GetBakedLength();
		if (longueur <= 0f)
			return;

		_progression += Vitesse * dt * _sens;

		if (AllerRetour)
		{
			if (_progression >= longueur)
			{
				_progression = longueur;
				_sens = -1;
			}
			else if (_progression <= 0f)
			{
				_progression = 0f;
				_sens = 1;
			}
		}
		else
		{
			_progression = Mathf.PosMod(_progression, longueur);
		}

		GlobalPosition = _origine + _courbe.SampleBaked(_progression);
	}
}
