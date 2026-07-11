using Godot;
using System.Collections.Generic;

// Rodolphe, le Cerf-boss : sous-classe de Boss qui fournit l'IA spécifique —
// machine à états complète (intro, patterns, 2 phases, sonné, défaite). Économie
// de génération assumée (voir DECISIONS.md) : le piétinement réutilise "idle"
// (pas de pose de cabrage dédiée) et le souffle de givre réutilise "charge" comme
// télégraphe - seul le résultat gameplay (stalactites, cône de givre) est nouveau.
public partial class BossCerf : Boss
{
	private enum Etat { Intro, Idle, Telegraphe, Charge, Etourdi, Pietinement, SouffleGivre, Vaincu }
	private enum Pattern { Charge, Pietinement, SouffleGivre }

	[Export] public float VitesseCharge = 260f;
	[Export] public float DelaiTelegraphe = 0.8f;
	[Export] public float DureeEtourdi = 2f;
	[Export] public int MultiplicateurDegatsEtourdi = 3;
	[Export] public float LimiteGauche = 80f;
	[Export] public float LimiteDroite = 2800f;

	public int Phase { get; private set; } = 1;

	private Area2D _zoneChargeDegats;
	private Etat _etat = Etat.Intro;
	private float _timerEtat = 1.6f;
	private int _direction = 1;
	private bool _dejaToucheCetteCharge;
	private bool _dejaToucheCeSouffle;
	private int _chargesRestantesEnchainement;
	private readonly RandomNumberGenerator _rng = new();
	private bool _vulnerableEtourdi;
	private Pattern _patternChoisi;

	protected override void Initialiser()
	{
		_zoneChargeDegats = GetNode<Area2D>("ZoneChargeDegats");
		_zoneChargeDegats.BodyEntered += OnZoneChargeDegatsEntered;
		_rng.Randomize();
		Sprite.Play("idle");
	}

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");

		AjouterAnimation(frames, "idle", "res://assets/boss_cerf/idle", 6f, true);
		AjouterAnimation(frames, "patrouille", "res://assets/boss_cerf/patrouille", 8f, true);
		AjouterAnimation(frames, "charge", "res://assets/boss_cerf/charge", 14f, true);
		AjouterAnimation(frames, "etourdi", "res://assets/boss_cerf/etourdi", 8f, true);
		AjouterAnimation(frames, "vaincu", "res://assets/boss_cerf/vaincu", 8f, false);

		return frames;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		_timerEtat -= dt;

		switch (_etat)
		{
			case Etat.Intro:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.Idle:
				Sprite.FlipH = _direction < 0;
				if (_timerEtat <= 0f)
					ChoisirPattern();
				break;

			case Etat.Telegraphe:
				if (_timerEtat <= 0f)
					LancerPatternTelegraphie();
				break;

			case Etat.Charge:
				AvancerCharge(dt);
				break;

			case Etat.Etourdi:
				if (_timerEtat <= 0f)
					FinDeEtourdissement();
				break;

			case Etat.Pietinement:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;

			case Etat.SouffleGivre:
				if (_timerEtat <= 0f)
					PasserEnIdle();
				break;
		}

		MoveAndSlide();
	}

	// Coup ×3 pendant la fenêtre de vulnérabilité (boss sonné contre un mur).
	protected override int AjusterDegats(int brut) => _vulnerableEtourdi ? brut * MultiplicateurDegatsEtourdi : brut;

	// Bascule en phase 2 à mi-vie.
	protected override void ApresDegats(int degats)
	{
		if (Phase == 1 && Pv <= PvMax / 2)
			DeclencherTransitionPhase2();
	}

	protected override void Mourir()
	{
		_etat = Etat.Vaincu;
		base.Mourir();
	}

	private void PasserEnIdle()
	{
		_etat = Etat.Idle;
		_timerEtat = _rng.RandfRange(1.0f, 1.8f);
		Sprite.Play("idle");
	}

	private void ChoisirPattern()
	{
		Pattern pattern;
		if (Phase == 1)
			pattern = _rng.Randf() < 0.5f ? Pattern.Charge : Pattern.Pietinement;
		else
			pattern = _rng.Randf() < 0.65f ? Pattern.Charge : (_rng.Randf() < 0.5f ? Pattern.Pietinement : Pattern.SouffleGivre);

		_patternChoisi = pattern;
		_etat = Etat.Telegraphe;
		_timerEtat = DelaiTelegraphe;

		// Se tourne vers le joueur pour le télégraphe.
		var joueur = GetTree().GetFirstNodeInGroup("joueur") as Node2D;
		if (joueur != null)
			_direction = joueur.GlobalPosition.X >= GlobalPosition.X ? 1 : -1;

		Sprite.Play(pattern == Pattern.SouffleGivre ? "charge" : "idle");
		Sprite.FlipH = _direction < 0;
	}

	private void LancerPatternTelegraphie()
	{
		switch (_patternChoisi)
		{
			case Pattern.Charge:
				DemarrerCharge();
				break;
			case Pattern.Pietinement:
				DemarrerPietinement();
				break;
			case Pattern.SouffleGivre:
				DemarrerSouffleGivre();
				break;
		}
	}

	private void DemarrerCharge()
	{
		_etat = Etat.Charge;
		_dejaToucheCetteCharge = false;
		Sprite.Play("charge");
		Sprite.FlipH = _direction < 0;
		Velocity = new Vector2(_direction * VitesseCharge, 0f);
	}

	private void AvancerCharge(float dt)
	{
		Velocity = new Vector2(_direction * VitesseCharge, 0f);

		if ((_direction > 0 && GlobalPosition.X >= LimiteDroite) || (_direction < 0 && GlobalPosition.X <= LimiteGauche))
		{
			PasserEnEtourdi();
		}
	}

	private void PasserEnEtourdi()
	{
		_etat = Etat.Etourdi;
		_vulnerableEtourdi = true;
		_timerEtat = DureeEtourdi;
		Velocity = Vector2.Zero;
		Sprite.Play("etourdi");

		if (Phase == 2 && _chargesRestantesEnchainement > 0)
			_chargesRestantesEnchainement--;
	}

	private void FinDeEtourdissement()
	{
		_vulnerableEtourdi = false;

		if (Phase == 2 && _chargesRestantesEnchainement > 0)
		{
			_direction *= -1;
			DemarrerCharge();
			return;
		}

		PasserEnIdle();
	}

	private void DemarrerPietinement()
	{
		_etat = Etat.Pietinement;
		_timerEtat = 1.6f;
		Sprite.Play("idle");
		DeclencherStalactites(Phase == 1 ? 3 : 5);
	}

	private void DeclencherStalactites(int nombre)
	{
		var stalactites = new List<Node>(GetTree().GetNodesInGroup("stalactites_boss"));
		_rng.Randomize();
		for (int i = 0; i < nombre && stalactites.Count > 0; i++)
		{
			int index = _rng.RandiRange(0, stalactites.Count - 1);
			if (stalactites[index] is StalactitePiege stalactite)
				stalactite.DeclencherImmediatement();
			stalactites.RemoveAt(index);
		}
	}

	private void DemarrerSouffleGivre()
	{
		_etat = Etat.SouffleGivre;
		_timerEtat = 1.0f;
		_dejaToucheCeSouffle = false;
		Sprite.Play("charge");
		CreerConeDeGivre();
	}

	private void CreerConeDeGivre()
	{
		var zone = new Area2D { CollisionLayer = 0, CollisionMask = 1 };
		var forme = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(10, 24) } };
		zone.AddChild(forme);
		AddChild(zone);
		zone.Position = new Vector2(_direction * 20f, -12);

		var visuel = new ColorRect
		{
			Color = new Color(0.7f, 0.9f, 1f, 0.55f),
			Size = new Vector2(10, 24),
			Position = new Vector2(-5, -12)
		};
		zone.AddChild(visuel);

		zone.BodyEntered += (Node2D body) =>
		{
			if (_dejaToucheCeSouffle || body is not Player joueur)
				return;
			_dejaToucheCeSouffle = true;
			joueur.SubirDegats(_direction, 2);
		};

		var tween = CreateTween();
		float largeurCible = 150f;
		tween.TweenProperty(forme.Shape, "size", new Vector2(largeurCible, 24), 0.5f);
		tween.Parallel().TweenProperty(visuel, "size", new Vector2(largeurCible, 24), 0.5f);
		tween.Parallel().TweenProperty(zone, "position:x", _direction * (20f + largeurCible / 2f), 0.5f);
		tween.Parallel().TweenProperty(visuel, "position:x", -largeurCible / 2f, 0.5f);
		tween.TweenInterval(0.4f);
		tween.TweenCallback(Callable.From(zone.QueueFree));
	}

	private void OnZoneChargeDegatsEntered(Node2D body)
	{
		if (_etat != Etat.Charge || _dejaToucheCetteCharge)
			return;
		if (body is not Player joueur || joueur.EstEnGlissade)
			return;

		_dejaToucheCetteCharge = true;
		joueur.SubirDegats(_direction);
	}

	private void DeclencherTransitionPhase2()
	{
		Phase = 2;
		_chargesRestantesEnchainement = 1;

		var tween = CreateTween();
		tween.TweenProperty(Sprite, "modulate", new Color(1.3f, 1.3f, 1.6f), 0.3f);
		tween.TweenProperty(Sprite, "modulate", Colors.White, 0.3f);
	}
}
