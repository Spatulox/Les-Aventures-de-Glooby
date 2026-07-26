using Godot;

// Ennemi « Bulbe explosif » (grotte florale) : piège vivant, immobile. Tant qu'on le laisse
// tranquille il respire (idle) ; dès que le joueur s'approche — ou qu'une boule de neige le
// touche — il se gonfle (télégraphe : c'est le temps donné au joueur pour fuir ou le faire
// éclater à distance), puis explose en blessant tout joueur présent dans sa « ZoneExplosion »
// avant de disparaître.
//
// Il n'a pas d'animation de mort et ne perd jamais de PV : l'explosion EST sa mort. La boule
// de neige ne fait donc que l'amorcer plus tôt — d'où la surcharge de TakeDamage, qui ne
// délègue pas à la base (laquelle retirerait des PV, et en ModeDebug le tuerait d'un coup).
//
// Frames : res://assets/ennemis/grotte_florale/bulbe_explosif/{idle,gonflement,explosion}.
public partial class BulbeExplosif : PnjMechant
{
	private enum EtatBulbe { Dormant, Gonflement, Explosion }

	// Portée du souffle : rayon de la « ZoneExplosion » de la scène (réglable par instance).
	// La distance d'amorçage, elle, vient de la « ZoneDetection » (ou de PorteeDetection).
	[Export] public DamageSource DegatsExplosion = DamageSource.ContactMechant;
	// Temps d'estompage du souffle une fois l'explosion jouée, avant libération du nœud.
	[Export] public float DureeEstompage = 0.25f;

	private EtatBulbe _etat = EtatBulbe.Dormant;
	private Area2D _zoneExplosion;

	protected override void Initialiser()
	{
		_zoneExplosion = GetNodeOrNull<Area2D>("ZoneExplosion");
		Sprite.AnimationFinished += SurFinAnimation;
	}

	// Immobile : il ne fait que guetter l'arrivée du joueur pour s'amorcer.
	protected override void DeciderMouvement(float dt, ref Vector2 velocite, Player joueur, float distance)
	{
		velocite.X = 0f;

		if (_etat == EtatBulbe.Dormant && joueur != null && distance <= PorteeDetection)
			Amorcer();
	}

	// Démarre le gonflement (sans effet si le bulbe est déjà amorcé ou en train d'exploser).
	public void Amorcer()
	{
		if (_etat != EtatBulbe.Dormant)
			return;

		_etat = EtatBulbe.Gonflement;
		if (!JouerSiPresente("gonflement"))
			Exploser();
	}

	// Fin d'une animation non bouclée : gonflement -> explosion, explosion -> disparition.
	private void SurFinAnimation()
	{
		if (_etat == EtatBulbe.Gonflement)
			Exploser();
		else if (_etat == EtatBulbe.Explosion)
			Effets.Disparaitre(Sprite, Sprite.Scale, DureeEstompage, this);
	}

	// Souffle : blesse les joueurs à portée, coupe les collisions (le bulbe n'est plus un
	// obstacle) et joue l'explosion, dont la fin libère le nœud.
	private void Exploser()
	{
		_etat = EtatBulbe.Explosion;
		BlesserJoueursDansZone(_zoneExplosion, DegatsExplosion);
		DesactiverCollisions();

		if (!JouerSiPresente("explosion"))
			QueueFree();
	}

	// Seule la boule de neige a prise sur lui, et elle ne lui retire pas de PV : elle
	// l'amorce à distance, ce qui est justement la façon sûre de le désamorcer.
	public override bool IsInvincibleToDamage(DamageSource source) => source is not DamageSource.Snowball;

	public override void TakeDamage(DamageSource source)
	{
		if (source == DamageSource.Snowball)
			Amorcer();
	}

	// L'animation est entièrement pilotée par la machine à états ci-dessus.
	protected override void MettreAJourAnimation(Vector2 velocite) { }

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/grotte_florale/bulbe_explosif";
		AjouterAnimation(frames, "idle", $"{b}/idle", 5f, true);
		AjouterAnimation(frames, "gonflement", $"{b}/gonflement", 7f, false);
		AjouterAnimation(frames, "explosion", $"{b}/explosion", 14f, false);
		return frames;
	}
}
