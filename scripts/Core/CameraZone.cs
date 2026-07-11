using Godot;

// Zone de caméra façon Hollow Knight : ajuste les limites de la Camera2D du
// joueur en entrant dans la salle, sans recharger de scène. Les zones se
// chevauchent volontairement aux transitions - la dernière traversée gagne.
public partial class CameraZone : DeclencheurZone
{
	[Export] public int LimGauche;
	[Export] public int LimDroite;
	[Export] public int LimHaut;
	[Export] public int LimBas;

	protected override void SurEntreeJoueur(Player joueur)
	{
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
