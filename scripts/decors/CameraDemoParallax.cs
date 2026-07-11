using Godot;

// Fait osciller la caméra horizontalement pour valider visuellement les
// vitesses de défilement d'une pile de Parallax2D (scènes de démo des packs
// de décor). Aucun rôle en jeu réel, juste un outil de contrôle qualité.
public partial class CameraDemoParallax : Camera2D
{
	[Export] public float Amplitude = 1200f;
	[Export] public float DureeAllerRetour = 12f;

	private float _temps;

	public override void _Process(double delta)
	{
		_temps += (float)delta;
		float phase = _temps / DureeAllerRetour * Mathf.Tau;
		Position = new Vector2(Mathf.Sin(phase) * Amplitude, Position.Y);
	}
}
