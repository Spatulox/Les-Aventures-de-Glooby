using Godot;

// PNJ méchant « Lanceur de boules de neige » : patrouille tranquillement, mais dès que le
// joueur entre dans sa portée il s'arrête, lui fait face et lui envoie des boules de neige
// (la même boule_de_neige.tscn que le joueur, réutilisée) à intervalle régulier.
// Visuel = carré placeholder rouge tant que res://assets/pnj/lanceur_boule_neige/{idle,marche}
// est vide ; dès que les frames y seront déposées, l'AnimatedSprite2D prend le relais.
public partial class LanceurBouleNeige : PnjMechant
{
	// Scène du projectile lancé (réutilise boule_de_neige.tscn, comme le joueur).
	[Export] public PackedScene SceneBouleDeNeige;

	// Délai (secondes) entre deux tirs quand le joueur reste à portée.
	[Export] public float IntervalleTir = 1.6f;

	private float _minuteurTir;

	// À portée : le lanceur s'immobilise, fait face au joueur et tire au rythme voulu ;
	// sinon il reprend sa patrouille (comportement hérité).
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		_minuteurTir -= dt;

		if (joueur == null || distance > PorteeDetection)
		{
			Patrouiller(dt, ref velocite);
			return;
		}

		// Immobile face au joueur pendant qu'il vise.
		velocite.X = 0f;
		int direction = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		if (direction != 0)
			DefinirOrientation(direction < 0);

		if (_minuteurTir <= 0f)
		{
			_minuteurTir = IntervalleTir;
			Tirer(direction == 0 ? 1 : direction);
		}
	}

	// Instancie une boule de neige et l'envoie dans la direction du joueur. Le lanceur
	// s'enregistre comme instanciateur pour ne jamais se blesser avec son propre tir.
	private void Tirer(int direction)
	{
		if (SceneBouleDeNeige == null)
			return;

		var boule = SceneBouleDeNeige.Instantiate<Node2D>();
		if (boule is Projectile projectile)
			projectile.Initialiser(this, direction);
		GetParent().AddChild(boule);
		boule.GlobalPosition = GlobalPosition + new Vector2(direction * 18f, -4f);
	}

	// Animations du lanceur. Les dossiers sont encore vides : ConstruireAnimations renvoie
	// des animations sans frame, donc PnjMechant garde le carré placeholder jusqu'à ce que
	// les PNG soient déposés dans res://assets/pnj/lanceur_boule_neige/{idle,marche}.
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", "res://assets/pnj/lanceur_boule_neige/idle", 6f, true);
		AjouterAnimation(frames, "marche", "res://assets/pnj/lanceur_boule_neige/marche", 8f, true);
		return frames;
	}
}
