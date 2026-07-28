using Godot;

// Onde de choc qui s'étale AU SOL de part et d'autre du point d'impact, puis s'efface.
// Sa règle d'esquive tient en une ligne : elle court au ras du sol, donc un joueur EN L'AIR
// au bon moment lui passe au-dessus. C'est ce qui en fait une attaque lisible — on la voit
// venir et on saute — plutôt qu'un dégât inévitable.
//
// Réutilisable : les frames viennent d'un dossier exporté, donc un autre boss peut poser la
// même mécanique avec son propre visuel. Le Lutin Mecha et le Cerf ont chacun leur version
// procédurale (un ColorRect étiré, cf. BossLutinMecha.CreerOndeDeChoc) — cette classe est
// leur remplaçante quand on a de vraies frames, mais leur migration n'est pas faite ici.
//
// La zone ne touche QU'UNE FOIS : une onde qui s'étale traverse le joueur, elle ne doit pas
// le frapper à chaque frame de croissance.
public partial class OndeDeChoc : Area2D
{
	[Export] public string DossierFrames = "res://assets/pnj/boss_pere_noel/punch_onde";

	// Distance atteinte de CHAQUE côté du point d'impact (la zone fait donc 2 × Portee).
	[Export] public float Portee = 160f;
	[Export] public float Duree = 0.3f;
	// Hauteur de la zone de dégâts : volontairement basse, c'est ce qui laisse le saut
	// passer par-dessus. Le visuel, lui, peut être bien plus haut (l'arc de l'onde).
	[Export] public float HauteurZone = 24f;
	[Export] public DamageSource Degats = DamageSource.OndeDeChoc;
	[Export] public float Fps = 14f;

	private CollisionShape2D _forme;
	private AnimatedSprite2D _sprite;
	private bool _dejaTouche;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = Constantes.LayerJoueur;

		_forme = GetNode<CollisionShape2D>("CollisionShape2D");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Forme locale à l'instance : sans ça, deux ondes simultanées se partageraient la
		// même RectangleShape2D et leurs tweens de croissance se marcheraient dessus.
		var rectangle = new RectangleShape2D { Size = new Vector2(0f, HauteurZone) };
		_forme.Shape = rectangle;

		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AnimationsSprite.EnregistrerAnimation(frames, "onde",
			AnimationsSprite.ChargerFrames(DossierFrames), Fps, false);
		_sprite.SpriteFrames = frames;
		_sprite.Play("onde");

		BodyEntered += SurEntree;

		// L'art de l'onde est déjà dessiné à sa pleine largeur : c'est la ZONE qui grandit,
		// et le sprite est mis à l'échelle pour que le visuel colle à la portée demandée.
		float largeurArt = LargeurArt(frames);
		if (largeurArt > 0f)
			_sprite.Scale = new Vector2(Portee * 2f / largeurArt, _sprite.Scale.Y);

		var tween = CreateTween();
		tween.TweenProperty(rectangle, "size", new Vector2(Portee * 2f, HauteurZone), Duree);
		tween.Parallel().TweenProperty(_sprite, "modulate:a", 0f, Duree);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	private static float LargeurArt(SpriteFrames frames)
	{
		var premiere = frames.GetFrameTexture("onde", 0);
		return premiere?.GetWidth() ?? 0f;
	}

	private void SurEntree(Node2D corps)
	{
		if (_dejaTouche || corps is not Player joueur)
			return;

		// L'onde court au sol : un joueur en l'air la saute.
		if (!joueur.IsOnFloor())
			return;

		_dejaTouche = true;
		int recul = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		joueur.Blesser(recul == 0 ? 1 : recul, Degats);
	}
}
