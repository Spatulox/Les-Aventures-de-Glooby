using Godot;

// Zone de caméra façon Hollow Knight : ajuste les limites de la Camera2D du
// joueur en entrant dans la salle, sans recharger de scène. Les zones se
// chevauchent volontairement aux transitions - la dernière traversée gagne.
public partial class CameraZone : Area2D
{
	public int LimGauche;
	public int LimDroite;
	public int LimHaut;
	public int LimBas;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player joueur)
			return;

		var camera = joueur.GetNode<Camera2D>("Camera2D");
		camera.LimitLeft = LimGauche;
		camera.LimitRight = LimDroite;
		camera.LimitTop = LimHaut;
		camera.LimitBottom = LimBas;

		// Le filet de sécurité (chute dans le vide) doit suivre la salle
		// active, pas une valeur absolue fixe.
		joueur.SeuilChuteVide = LimBas + 300f;
	}
}
