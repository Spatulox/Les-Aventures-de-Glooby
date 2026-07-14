using Godot;
using System.Collections.Generic;

// PNJ générique : une animation idle chargée depuis un dossier de frames et
// une bulle de dialogue à l'approche du joueur (BulleDialogue, la bulle
// procédurale partagée du projet). Sert de base aux PNJ sans logique
// spécifique (Père Noël, lutins d'usine...) — le dossier d'idle étant
// exporté, une même scène couvre plusieurs poses.
public partial class PNJSimple : Node2D
{
	[Export(PropertyHint.Dir)] public string DossierIdle = "";
	[Export] public float ImagesParSeconde = 5f;
	[Export(PropertyHint.MultilineText)] public string DialogueTexte = "";
	// Point d'ancrage de la queue de la bulle (au-dessus de la tête).
	[Export] public Vector2 DecalageBulle = new(0, -40);

	private AnimatedSprite2D _sprite;
	private BulleDialogue _bulle;

	public bool BulleVisible => _bulle != null && _bulle.Visible;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		ChargerAnimation();

		_bulle = new BulleDialogue { Position = DecalageBulle };
		AddChild(_bulle);

		var zone = GetNode<Area2D>("ZoneDetection");
		zone.BodyEntered += corps => { if (corps is Player) _bulle.AfficherDialogue(DialogueTexte); };
		zone.BodyExited += corps => { if (corps is Player) _bulle.Cacher(); };
	}

	private void ChargerAnimation()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		frames.AddAnimation("idle");
		frames.SetAnimationSpeed("idle", ImagesParSeconde);
		frames.SetAnimationLoop("idle", true);

		var fichiers = new List<string>();
		foreach (var fichier in DirAccess.GetFilesAt(DossierIdle))
		{
			if (fichier.EndsWith(".png"))
				fichiers.Add(fichier);
		}
		fichiers.Sort();
		foreach (var fichier in fichiers)
			frames.AddFrame("idle", GD.Load<Texture2D>($"{DossierIdle}/{fichier}"));

		_sprite.SpriteFrames = frames;
		_sprite.Play("idle");
	}
}
