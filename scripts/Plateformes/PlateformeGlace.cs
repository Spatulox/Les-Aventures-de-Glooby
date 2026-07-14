using Godot;

// Plateforme de glace éphémère posée par le pouvoir de glace du joueur : elle
// réutilise le comportement one-way de PlateformeUnidirectionnelle (le joueur
// s'y tient comme sur le sol et peut sauter au travers), s'affiche avec une
// teinte glacée et un petit pop, puis fond automatiquement après DureeVie.
// Ainsi le joueur comble un trou le temps de le traverser, sans pouvoir bâtir
// de structures permanentes.
public partial class PlateformeGlace : PlateformeUnidirectionnelle
{
	[Export] public float DureeVie = 4f;

	public override void _Ready()
	{
		base._Ready();

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Modulate = new Color(0.6f, 0.85f, 1f);

		// Petit pop d'apparition (même esprit que Player.JouerApparition).
		var echelleFinale = Scale;
		Scale = echelleFinale * 0.6f;
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "scale", echelleFinale, 0.2f);

		// Fonte automatique : fondu (Effets.Disparaitre) après DureeVie.
		GetTree().CreateTimer(DureeVie).Timeout += () =>
		{
			if (IsInstanceValid(this))
				Effets.Disparaitre(this, new Vector2(Scale.X, Scale.Y * 0.7f), 0.5f);
		};
	}
}
