using Godot;

// Mur de glace fondable : bloc solide tant que le pouvoir de chaleur n'a pas
// été utilisé dessus. Melt() le fait fondre définitivement (état persistant
// dans GameState, pour rester fondu si le joueur revient sur cet écran).
public partial class MurFondable : StaticBody2D
{
	[Export] public string IdMur = "";

	public override void _Ready()
	{
		if (string.IsNullOrEmpty(IdMur))
			IdMur = GetPath().ToString();

		if (GameState.Instance.EstMurFondu(IdMur))
			QueueFree();
	}

	public void Melt()
	{
		GameState.Instance.MarquerMurFondu(IdMur);

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		var sprite = GetNode<Sprite2D>("Sprite2D");
		var tween = CreateTween();
		tween.TweenProperty(sprite, "modulate:a", 0f, 0.6f);
		tween.Parallel().TweenProperty(sprite, "scale:y", sprite.Scale.Y * 0.7f, 0.6f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
