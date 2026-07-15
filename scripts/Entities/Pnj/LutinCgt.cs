using Godot;
using System.Collections.Generic;

// PNJ amical « Lutin gréviste » (gag visuel assumé) : un lutin du Père Noël en grève,
// planté sur place (statique, DistancePatrouille = 0) avec une pose au choix. Comme tout
// PnjAmical il déambulerait, mais ici il reste immobile ; le dialogue passe par le moteur
// partagé Talkative/DeclencheurDialogue. En plus, un slogan est affiché en Label Godot sur
// l'aplat vide de sa pancarte — jamais dessiné dans le sprite, donc changeable par instance
// ("EN GRÈVE", "Non à la hotte 35h"...). Visuel via l'AnimatedSprite2D de la scène, chargé
// depuis le dossier de la pose choisie (invisible si ce dossier est vide).
public partial class LutinCgt : PnjAmical
{
	public enum PoseLutin { BrasCroises, PancarteLevee, AssisCaisse }

	[Export] public PoseLutin Pose = PoseLutin.PancarteLevee;
	[Export(PropertyHint.MultilineText)] public string Slogan = "EN GRÈVE";

	private record Config(string Dossier, Vector2 ZoneSlogan, Vector2 TailleSlogan);

	// Zone du slogan : rectangle de l'aplat clair de la pancarte, mesuré sur chaque pose
	// (coordonnées locales, sprite x2 centré). Chaque pose a aussi son dossier de frames.
	private static readonly Dictionary<PoseLutin, Config> Configs = new()
	{
		[PoseLutin.BrasCroises] = new("res://assets/pnj/lutin_cgt/bras_croises", new Vector2(16, -38), new Vector2(34, 40)),
		[PoseLutin.PancarteLevee] = new("res://assets/pnj/lutin_cgt/pancarte_levee", new Vector2(-40, -52), new Vector2(48, 38)),
		[PoseLutin.AssisCaisse] = new("res://assets/pnj/lutin_cgt/assis_caisse", new Vector2(12, -52), new Vector2(46, 42)),
	};

	private Config _config;

	// Init (avant ConstruireAnimations) : fige le lutin sur place, résout la pose et câble
	// le Label du slogan sur l'aplat de la pancarte.
	protected override void Initialiser()
	{
		DistancePatrouille = 0f;   // gréviste immobile
		_config = Configs[Pose];

		var slogan = GetNode<Label>("Slogan");
		slogan.Text = Slogan;
		slogan.Position = _config.ZoneSlogan;
		slogan.Size = _config.TailleSlogan;
	}

	// Une seule animation « idle » depuis le dossier de la pose (lutin statique, pas de marche).
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AjouterAnimation(frames, "idle", _config.Dossier, 4f, true);
		return frames;
	}
}
