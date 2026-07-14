using Godot;
using System.Collections.Generic;

// PNJ pingouin : personnage statique qui affiche une bulle de dialogue
// (BulleDialogue, la bulle procédurale partagée du projet) quand le joueur
// s'approche. Animations "idle" et "parler" chargées depuis
// assets/pnj/pingouin_ancien/, même mécanique de chargement par dossier que
// Player.
public partial class PNJPingouin : Node2D
{
	[Export(PropertyHint.MultilineText)] public string DialogueTexte = "Brrr... quel froid, pas vrai ?";

	private AnimatedSprite2D _sprite;
	private BulleDialogue _bulle;

	public bool BulleVisible => _bulle != null && _bulle.Visible;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		ChargerAnimations();

		_bulle = new BulleDialogue { Position = new Vector2(0, -40) };
		AddChild(_bulle);

		var zone = GetNode<Area2D>("ZoneDetection");
		zone.BodyEntered += OnJoueurEntre;
		zone.BodyExited += OnJoueurSorti;
	}

	private void OnJoueurEntre(Node2D corps)
	{
		if (corps is not Player)
			return;

		_bulle.AfficherDialogue(DialogueTexte);
		_sprite.Play("parler");
	}

	private void OnJoueurSorti(Node2D corps)
	{
		if (corps is not Player)
			return;

		_bulle.Cacher();
		_sprite.Play("idle");
	}

	private void ChargerAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");

		EnregistrerAnimation(frames, "idle", "res://assets/pnj/pingouin_ancien/idle", 5f);
		EnregistrerAnimation(frames, "parler", "res://assets/pnj/pingouin_ancien/parler", 7f);

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}

	private static void EnregistrerAnimation(SpriteFrames frames, string nom, string dossier, float fps)
	{
		frames.AddAnimation(nom);
		frames.SetAnimationSpeed(nom, fps);
		frames.SetAnimationLoop(nom, true);

		var fichiers = new List<string>();
		foreach (var fichier in DirAccess.GetFilesAt(dossier))
		{
			if (fichier.EndsWith(".png"))
				fichiers.Add(fichier);
		}
		fichiers.Sort();

		foreach (var fichier in fichiers)
			frames.AddFrame(nom, GD.Load<Texture2D>($"{dossier}/{fichier}"));
	}
}
