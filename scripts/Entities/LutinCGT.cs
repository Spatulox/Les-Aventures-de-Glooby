using Godot;
using System.Collections.Generic;

// Lutin de Noël gréviste (gag visuel assumé) : même modèle que PNJPingouin
// (idle permanent, bulle de dialogue à l'approche du joueur), plus un
// slogan affiché en Label Godot sur l'aplat vide de sa pancarte — jamais
// dessiné dans le sprite, donc changeable par instance ("En grève",
// "Non à la hotte 35h", ...).
public partial class LutinCGT : Node2D
{
	public enum PoseLutin { BrasCroises, PancarteLevee, AssisCaisse }

	[Export] public PoseLutin Pose = PoseLutin.PancarteLevee;
	[Export(PropertyHint.MultilineText)] public string DialogueTexte = "On lâche rien. Même pas les jouets.";
	[Export(PropertyHint.MultilineText)] public string Slogan = "EN GRÈVE";

	private record Config(string Dossier, Vector2 ZoneSlogan, Vector2 TailleSlogan);

	// Zone du slogan : rectangle de l'aplat clair de la pancarte, mesuré sur
	// chaque pose (coordonnées locales, sprite x2 centré).
	private static readonly Dictionary<PoseLutin, Config> Configs = new()
	{
		[PoseLutin.BrasCroises] = new("res://assets/pnj/lutin_cgt/bras_croises", new Vector2(16, -38), new Vector2(34, 40)),
		[PoseLutin.PancarteLevee] = new("res://assets/pnj/lutin_cgt/pancarte_levee", new Vector2(-40, -52), new Vector2(48, 38)),
		[PoseLutin.AssisCaisse] = new("res://assets/pnj/lutin_cgt/assis_caisse", new Vector2(12, -52), new Vector2(46, 42)),
	};

	private AnimatedSprite2D _sprite;
	private BulleDialogue _bulle;
	private Label _slogan;

	public bool BulleVisible => _bulle != null && _bulle.Visible;

	public override void _Ready()
	{
		var config = Configs[Pose];

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		ChargerAnimations(config.Dossier);

		_slogan = GetNode<Label>("Slogan");
		_slogan.Text = Slogan;
		_slogan.Position = config.ZoneSlogan;
		_slogan.Size = config.TailleSlogan;

		// Ancrée au-dessus de la pancarte pour ne pas la recouvrir.
		_bulle = new BulleDialogue { Position = new Vector2(0, -66) };
		AddChild(_bulle);

		var zone = GetNode<Area2D>("ZoneDetection");
		zone.BodyEntered += corps => { if (corps is Player) _bulle.AfficherDialogue(DialogueTexte); };
		zone.BodyExited += corps => { if (corps is Player) _bulle.Cacher(); };
	}

	private void ChargerAnimations(string dossier)
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		frames.AddAnimation("idle");
		frames.SetAnimationSpeed("idle", 4f);
		frames.SetAnimationLoop("idle", true);

		var fichiers = new List<string>();
		foreach (var fichier in DirAccess.GetFilesAt(dossier))
		{
			if (fichier.EndsWith(".png"))
				fichiers.Add(fichier);
		}
		fichiers.Sort();
		foreach (var fichier in fichiers)
			frames.AddFrame("idle", GD.Load<Texture2D>($"{dossier}/{fichier}"));

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}
}
