using Godot;

// Pickup du Pouvoir de Chaleur : léger mouvement de flottaison procédural
// (pas de nouvelle génération pour l'ambiance de la salle).
public partial class PouvoirChaleurPickup : Area2D
{
	public override void _Ready()
	{
		if (GameState.Instance.PouvoirChaleurActif)
		{
			QueueFree();
			return;
		}

		BodyEntered += OnBodyEntered;

		var tween = CreateTween().SetLoops();
		tween.TweenProperty(this, "position:y", Position.Y - 6f, 0.8f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(this, "position:y", Position.Y + 6f, 0.8f).SetTrans(Tween.TransitionType.Sine);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
			return;

		GameState.Instance.ObtenirPouvoirChaleur();
		QueueFree();
	}
}
