using Godot;

// Cadeau piégé lancé par le Père Noël : un Projectile en cloche (mèche allumée, il pique
// du nez en retombant grâce à l'orientation de la base) qui explose au premier contact —
// joueur, sol ou plafond — puis souffle sur un rayon avant de disparaître.
// Pendant d'EclatGlace pour la famille « explosif » : même idiome vol/impact, à ceci près
// que l'impact ne se contente pas d'être décoratif, il blesse.
public partial class CadeauExplosif : Projectile
{
	private const string Dossier = "res://assets/projectiles/cadeau_explosif";
	private const string PrefixeVol = "cadeau_explosif_vol";
	private const string PrefixeExplosion = "cadeau_explosif_explosion";

	// Rayon du souffle, mesuré sur l'ampleur de la dernière frame d'explosion (~45 px
	// à l'échelle 0,5 de la scène). Le contact direct, lui, est déjà géré par la base.
	[Export] public float RayonSouffle = 46f;

	protected override DamageSource Source => DamageSource.JouetExplosif;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		base._Ready();   // couches, durée de vie et détection des corps
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.SpriteFrames = ConstruireAnimations();
		_sprite.Play("vol");
	}

	// Les deux animations vivent dans le MÊME dossier, à plat : elles se distinguent par
	// leur préfixe de nom de fichier, pas par un sous-dossier (d'où le filtre de
	// ChargerFrames — sans lui, un tri alphabétique mélangerait explosion et vol).
	private static SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AnimationsSprite.EnregistrerAnimation(frames, "vol",
			AnimationsSprite.ChargerFrames(Dossier, PrefixeVol), 10f, true);
		AnimationsSprite.EnregistrerAnimation(frames, "explosion",
			AnimationsSprite.ChargerFrames(Dossier, PrefixeExplosion), 16f, false);
		return frames;
	}

	// Explosion : redresse le cadeau, souffle autour de lui, puis joue les flammes.
	protected override void Disparaitre()
	{
		Rotation = 0f;
		AppliquerSouffle();

		if (_sprite.SpriteFrames.GetFrameCount("explosion") > 0)
		{
			_sprite.Play("explosion");
			_sprite.AnimationFinished += QueueFree;
		}
		else
		{
			QueueFree();
		}
	}

	// Souffle de zone : testé À LA DISTANCE plutôt qu'en agrandissant une Area2D, parce
	// qu'on arrive ici depuis un signal de collision (flush physique en cours, où changer
	// une forme est interdit). Même parti pris que MiniJouetExplosif.AppliquerSouffle.
	// Un joueur touché de plein fouet a déjà encaissé le contact : son invincibilité
	// post-coup absorbe ce second appel, il n'y a pas de double dégât.
	private void AppliquerSouffle()
	{
		if (GetTree().GetFirstNodeInGroup("joueur") is not Player joueur)
			return;

		if (GlobalPosition.DistanceTo(joueur.GlobalPosition) > RayonSouffle)
			return;

		int recul = Mathf.Sign(joueur.GlobalPosition.X - GlobalPosition.X);
		joueur.Blesser(recul == 0 ? Direction : recul, Source);
	}
}
