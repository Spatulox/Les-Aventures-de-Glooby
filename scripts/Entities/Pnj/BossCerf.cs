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

	// Hauteur d'obstacle que Rodolphe accepte d'enjamber en pleine charge (marche du
	// sol, bloc, plateforme). Au-delà, c'est un mur : la charge s'y écrase. Le défaut
	// couvre les ressauts de l'arène florale (64 à 67 px) tout en restant loin de ses
	// murs (215 à 242 px), qui doivent continuer de l'arrêter.
	[Export] public float HauteurFranchissable = 96f;

	// Battement au-dessus de l'obstacle : l'impulsion de saut est CALCULÉE pour que
	// l'apex dépasse HauteurFranchissable + cette marge (v = √(2·g·h)). Un seul
	// nombre à régler - la hauteur d'obstacle - et le saut suit tout seul, au lieu
	// d'un JumpVelocity à retoucher en parallèle et à garder cohérent.
	[Export] public float MargeFranchissement = 16f;

	// Bas de sa boîte de collision, relevé sur la scène (aucun nombre en dur : un
	// autre gabarit de boss reste correct).
	private float _bordPieds;


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
		MesurerGabarit();
		_rng.Randomize();
		Sprite.Play("idle");
	}

	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");

		AjouterAnimation(frames, "idle", "res://assets/pnj/boss_cerf/idle", 6f, true);
		AjouterAnimation(frames, "patrouille", "res://assets/pnj/boss_cerf/patrouille", 8f, true);
		AjouterAnimation(frames, "charge", "res://assets/pnj/boss_cerf/charge", 14f, true);
		AjouterAnimation(frames, "etourdi", "res://assets/pnj/boss_cerf/etourdi", 8f, true);
		AjouterAnimation(frames, "vaincu", "res://assets/pnj/boss_cerf/vaincu", 8f, false);

		return frames;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		_timerEtat -= dt;

		// Rodolphe est soumis à la gravité comme tout le monde : sans elle il chargeait
		// en lévitation à sa hauteur d'apparition, et sauter n'aurait aucun sens.
		// Chaque état ne pilote donc que la composante horizontale.
		var chute = Velocity;
		AppliquerGravite(ref chute, dt);
		Velocity = chute;

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
		Velocity = new Vector2(_direction * VitesseCharge, Velocity.Y);
	}

	private void AvancerCharge(float dt)
	{
		Velocity = new Vector2(_direction * VitesseCharge, Velocity.Y);

		if ((_direction > 0 && GlobalPosition.X >= LimiteDroite) || (_direction < 0 && GlobalPosition.X <= LimiteGauche))
		{
			PasserEnEtourdi();
			return;
		}

		// En l'air (saut de franchissement en cours), on ne juge rien : il garde son
		// élan. Sans ce garde, les rayons perdent la marche dès qu'il s'élève et la
		// charge s'interrompait en plein saut.
		if (!IsOnFloor())
			return;

		// Rien devant : on continue.
		if (!IsOnWall())
			return;

		// Obstacle franchissable (marche du sol, bloc, plateforme) : Rodolphe
		// l'enjambe et poursuit sa course vers le joueur au lieu de s'y arrêter.
		if (ObstacleFranchissable())
		{
			// Impulsion taillée pour l'obstacle le plus haut qu'il accepte, et non le
			// JumpVelocity générique de LivingEntity (calibré pour le joueur, trop
			// court pour les ressauts de l'arène).
			Velocity = new Vector2(Velocity.X, -Mathf.Sqrt(2f * Gravity * (HauteurFranchissable + MargeFranchissement)));
			return;
		}

		// Vrai mur : la charge s'y écrase, ce qui ouvre la fenêtre de vulnérabilité.
		PasserEnEtourdi();
	}

	// Relève une fois pour toutes les bords de la boîte de collision : c'est de là que
	// partent les rayons de détection d'obstacle.
	private void MesurerGabarit()
	{
		if (GetNodeOrNull<CollisionShape2D>("CollisionShape2D") is not CollisionShape2D forme
			|| forme.Shape is not RectangleShape2D rect)
			return;

		_bordPieds = forme.Position.Y + rect.Size.Y / 2f;
	}

	// L'obstacle heurté est-il assez bas pour être enjambé ? On repart du point de
	// contact donné par MoveAndSlide, puis on cherche le SOMMET de l'obstacle avec un
	// rayon vertical descendant, lancé depuis la hauteur franchissable.
	//
	// Un rayon horizontal parti du museau ne convient pas : les ressauts du décor sont
	// des dalles dont le bas s'arrête au-dessus des sabots (7 px de vide sous la face
	// dans l'arène florale), si bien que le rayon passait dessous sans rien voir. Le
	// rayon vertical, lui, part toujours du ciel libre - et s'il ne touche rien, c'est
	// que l'obstacle monte plus haut que ce que Rodolphe sait franchir : un vrai mur.
	private bool ObstacleFranchissable()
	{
		float pieds = GlobalPosition.Y + _bordPieds;

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var contact = GetSlideCollision(i);
			var normale = contact.GetNormal();
			if (Mathf.Abs(normale.X) < 0.7f)
				continue;   // sol ou plafond, pas un obstacle devant lui

			// Deux pixels au-delà de la face heurtée, donc au-dessus de l'obstacle.
			var sommet = new Vector2(contact.GetPosition().X - normale.X * 2f, pieds - HauteurFranchissable);
			if (RayonTouche(sommet, new Vector2(0f, HauteurFranchissable)))
				return true;
		}

		return false;
	}

	private bool RayonTouche(Vector2 depart, Vector2 avant)
	{
		var requete = PhysicsRayQueryParameters2D.Create(depart, depart + avant, Constantes.MasqueMarcheur);
		requete.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		// Pas de HitFromInside : un rayon qui DÉMARRE dans le solide signifie que
		// l'obstacle dépasse la hauteur franchissable — donc « mur », pas « marche ».
		return GetWorld2D().DirectSpaceState.IntersectRay(requete).Count > 0;
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
		var zone = new Area2D { CollisionLayer = 0, CollisionMask = Constantes.LayerJoueur };
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
			joueur.Blesser(_direction, DamageSource.SouffleGivre);
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
		joueur.Blesser(_direction, DamageSource.ChargeBoss);
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
