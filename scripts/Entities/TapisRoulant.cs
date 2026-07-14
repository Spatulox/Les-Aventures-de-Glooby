using Godot;

// Tapis roulant de l'usine : segments raccordables + embouts à rouleaux.
// Le joueur qui marche dessus est poussé via ConstantLinearVelocity (le
// mécanisme Godot standard des tapis : le corps ne bouge pas, la vélocité
// est transmise aux corps posés dessus). Le défilement visuel de la bande
// est procédural : les frames sont la même image dont seule la bande de
// cuir est décalée, donc chaque frame reste raccordable ; la cadence est
// calée sur la vitesse, et le sens négatif joue l'animation à l'envers.
public partial class TapisRoulant : StaticBody2D
{
	[Export] public int NombreSegments = 3;
	[Export] public bool AvecEmbouts = true;
	[Export] public float Vitesse = 80f;

	private const float LargeurSegment = 80f;
	private const float LargeurEmbout = 48f;
	private const int NombreFrames = 4;
	// Décalage de bande par frame (LargeurSegment / NombreFrames).
	private const float PasParFrame = 20f;

	public override void _Ready()
	{
		ConstantLinearVelocity = new Vector2(Vitesse, 0);

		var framesBande = new SpriteFrames();
		framesBande.RemoveAnimation("default");
		framesBande.AddAnimation("defile");
		framesBande.SetAnimationSpeed("defile", Mathf.Abs(Vitesse) / PasParFrame);
		framesBande.SetAnimationLoop("defile", true);
		for (int i = 0; i < NombreFrames; i++)
			framesBande.AddFrame("defile", GD.Load<Texture2D>($"res://assets/props/noel/tapis_segment_{i}.png"));

		float x = 0f;
		if (AvecEmbouts)
		{
			AjouterEmbout(x, miroir: false);
			x += LargeurEmbout;
		}

		for (int i = 0; i < NombreSegments; i++)
		{
			var sprite = new AnimatedSprite2D
			{
				SpriteFrames = framesBande,
				Centered = false,
				Position = new Vector2(x, 0),
			};
			AddChild(sprite);
			if (Vitesse >= 0)
				sprite.Play("defile");
			else
				sprite.PlayBackwards("defile");
			x += LargeurSegment;
		}

		if (AvecEmbouts)
			AjouterEmbout(x, miroir: true);

		// Collision : dessus de la bande (rangée 13 du sprite) sur toute la
		// longueur utile.
		float longueur = NombreSegments * LargeurSegment + (AvecEmbouts ? 2 * LargeurEmbout : 0f);
		var forme = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(longueur, 24) },
			Position = new Vector2(longueur / 2f, 25),
		};
		AddChild(forme);
	}

	private void AjouterEmbout(float x, bool miroir)
	{
		AddChild(new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://assets/props/noel/tapis_embout.png"),
			Centered = false,
			Position = new Vector2(x, 0),
			FlipH = miroir,
		});
	}
}
