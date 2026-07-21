using Godot;
using System.Collections.Generic;

// Ennemi « Bonhomme de neige » (malicieux) : ennemi lent/statique qui, dès que
// le joueur entre dans sa portée, arme un tir (télégraphe), lance une boule de
// neige en cloche, puis recharge. Encaisse les boules du joueur (LivingEntity =
// Damageable) et se désagrège à la mort. Fichier neuf, indépendant du
// LanceurBouleNeige existant. Machine à états : Idle / Armer / Lancer / Mort.
public partial class BonhommeDeNeige : LivingEntity
{
	private enum Etat { Idle, Armer, Lancer, Mort }

	// Scène du projectile en cloche (scenes/ennemis/BouleDeNeige.tscn).
	[Export] public PackedScene SceneBoule;
	// Distance de détection du joueur.
	[Export] public float Portee = 220f;
	// Délai entre deux salves quand le joueur reste à portée.
	[Export] public float CadenceTir = 2.0f;
	// Durée du télégraphe « il forme la boule » avant le lancer.
	[Export] public float DureeArmer = 0.5f;
	// Vitesse horizontale et hauteur d'arc du projectile (trajectoire en cloche).
	[Export] public float VitesseProjectile = 170f;
	[Export] public float ArcProjectile = 200f;

	private Etat _etat = Etat.Idle;
	private float _minuteur;
	private float _rechargeMinuteur;
	private int _dirTir = 1;
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		MasquerApercuEditeur();
		_sprite.SpriteFrames = ConstruireAnimations();
		Pv = PvMax;
		AddToGroup("pnj");
		JouerSiPresente("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_etat == Etat.Mort)
			return;

		float dt = (float)delta;
		var v = Velocity;
		AppliquerGravite(ref v, dt);   // reste au sol
		v.X = 0f;                       // statique
		if (_rechargeMinuteur > 0f)
			_rechargeMinuteur -= dt;

		var joueur = JoueurLePlusProche(out float distance);

		switch (_etat)
		{
			case Etat.Idle:
				if (joueur != null && distance <= Portee && _rechargeMinuteur <= 0f)
					EntrerArmer(joueur);
				break;

			case Etat.Armer:
				_minuteur -= dt;
				if (_minuteur <= 0f)
					EntrerLancer();
				break;

			case Etat.Lancer:
				// Le lancer effectif se fait à la fin de l'animation (voir OnLancerFini).
				break;
		}

		Velocity = v;
		MoveAndSlide();
	}

	private void EntrerArmer(Player joueur)
	{
		_etat = Etat.Armer;
		_minuteur = DureeArmer;
		_dirTir = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		if (_dirTir == 0)
			_dirTir = 1;
		_sprite.FlipH = _dirTir < 0;
		JouerSiPresente("armer");
	}

	private void EntrerLancer()
	{
		_etat = Etat.Lancer;
		if (JouerSiPresente("lancer"))
			_sprite.AnimationFinished += OnLancerFini;
		else
			OnLancerFini();   // pas d'anim : on lance tout de suite
	}

	// Fin de l'animation de lancer : instancie la boule et repart en recharge.
	private void OnLancerFini()
	{
		_sprite.AnimationFinished -= OnLancerFini;
		Tirer();
		_rechargeMinuteur = CadenceTir;
		_etat = Etat.Idle;
		JouerSiPresente("idle");
	}

	private void Tirer()
	{
		if (SceneBoule == null)
			return;

		var boule = SceneBoule.Instantiate<BouleDeNeige>();
		// Cloche : vitesse horizontale vers le joueur + poussée vers le haut.
		boule.Lancer(new Vector2(_dirTir * VitesseProjectile, -ArcProjectile));
		GetParent().AddChild(boule);
		boule.GlobalPosition = GlobalPosition + new Vector2(_dirTir * 16f, -18f);
	}

	// Mort : joue la désagrégation puis disparaît.
	protected override void Mourir()
	{
		base.Mourir();
		_etat = Etat.Mort;
		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col != null)
			col.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		if (JouerSiPresente("mort"))
			_sprite.AnimationFinished += QueueFree;
		else
			QueueFree();
	}

	// Joue une animation si elle a au moins une frame ; renvoie vrai si jouée.
	private bool JouerSiPresente(string nom)
	{
		if (_sprite.SpriteFrames.HasAnimation(nom) && _sprite.SpriteFrames.GetFrameCount(nom) > 0)
		{
			_sprite.Play(nom);
			return true;
		}
		return false;
	}

	// Joueur le plus proche (groupe "joueur") et sa distance.
	private Player JoueurLePlusProche(out float distance)
	{
		distance = float.MaxValue;
		Player proche = null;
		foreach (var n in GetTree().GetNodesInGroup("joueur"))
		{
			if (n is not Player j)
				continue;
			float d = GlobalPosition.DistanceTo(j.GlobalPosition);
			if (d < distance) { distance = d; proche = j; }
		}
		return proche;
	}

	private SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/pnj/bonhomme_neige";
		AnimationsSprite.EnregistrerAnimation(frames, "idle", AnimationsSprite.ChargerFrames($"{b}/idle"), 5f, true);
		AnimationsSprite.EnregistrerAnimation(frames, "armer", AnimationsSprite.ChargerFrames($"{b}/armer"), 8f, false);
		AnimationsSprite.EnregistrerAnimation(frames, "lancer", AnimationsSprite.ChargerFrames($"{b}/lancer"), 12f, false);
		AnimationsSprite.EnregistrerAnimation(frames, "mort", AnimationsSprite.ChargerFrames($"{b}/mort"), 8f, false);
		return frames;
	}
}
