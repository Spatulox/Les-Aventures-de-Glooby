using Godot;

// Écran de fin provisoire : "Acte 2 terminé". Appuyer sur une touche relance
// depuis le début (rien de définitif, juste pour ne jamais rester bloqué).
public partial class EcranFin : Node2D
{
	public override void _UnhandledInput(InputEvent evenement)
	{
		if (evenement is InputEventKey { Pressed: true })
			GetTree().ChangeSceneToFile("res://scenes/ecran01.tscn");
	}
}
