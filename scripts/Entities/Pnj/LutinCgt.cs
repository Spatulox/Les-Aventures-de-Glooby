using Godot;
using System.Collections.Generic;

// PNJ amical « Lutin gréviste » (gag visuel assumé) : un lutin du Père Noël en grève,
// planté sur place (statique, DistancePatrouille = 0) avec une pose au choix. Comme tout
// PnjAmical il déambulerait, mais ici il reste immobile ; le dialogue passe par le moteur
// partagé Talkative/DeclencheurDialogue. En plus, un slogan est affiché en Label Godot sur
// l'aplat vide de sa pancarte — jamais dessiné dans le sprite, donc changeable par instance
// ("EN GRÈVE", "Non à la hotte 35h"...). Visuel via l'AnimatedSprite2D de la scène, chargé
// depuis le dossier de la pose choisie (invisible si ce dossier est vide).
//
// Le script est [Tool] (comme PanneauBois) : changer Pose ou Slogan dans l'inspecteur réapplique
// aussitôt la 1re frame de la pose sur le Sprite2D « Apercu » ET replace le Label sur l'aplat de la
// nouvelle pancarte, si bien que monde1.tscn montre le lutin — texte compris — tel qu'il sera en
// jeu (l'aperçu éditeur n'est plus figé sur une seule pose, et le centrage se règle à vue).
[Tool]
public partial class LutinCgt : PnjAmical
{
	public enum PoseLutin { BrasCroises, PancarteLevee, AssisCaisse }

	private PoseLutin _pose = PoseLutin.PancarteLevee;
	private string _texteSlogan = "EN GRÈVE";

	[Export]
	public PoseLutin Pose
	{
		get => _pose;
		set { _pose = value; AppliquerApercu(); }
	}

	// Texte peint sur la pancarte. Propriété (et non champ) comme Pose : le setter replace et
	// recentre aussitôt le Label, dans l'éditeur comme en jeu — d'où l'aperçu fidèle dans monde1.tscn
	// et le recentrage automatique quand le slogan change en cours de partie (SurChoixRetenu).
	[Export(PropertyHint.MultilineText)]
	public string Slogan
	{
		get => _texteSlogan;
		set { _texteSlogan = value; AppliquerSlogan(); }
	}

	private record Config(string Dossier, Vector2 CentreSlogan, Vector2 TailleSlogan);

	// Zone du slogan : rectangle de l'aplat clair de la pancarte, mesuré au pixel sur chaque pose
	// puis exprimé par son CENTRE (le coin haut-gauche se déduit, cf. AppliquerSlogan). Coordonnées
	// LOCALES du sprite 64x64 centré, affiché à l'échelle 1 : local = pixel - 32.
	//
	// Attention au piège hérité de PanneauBois, dont ce script est décalqué : PanneauBois mesure ses
	// rectangles sur un sprite affiché x2 (il fait sprite.Scale = (2,2)). Le lutin, lui, reste à
	// l'échelle 1 comme tous les PNJ 64x64 du jeu — ses rectangles sont donc en pixels d'art bruts,
	// jamais doublés. Chaque pose a aussi son dossier de frames.
	private static readonly Dictionary<PoseLutin, Config> Configs = new()
	{
		[PoseLutin.BrasCroises] = new("res://assets/pnj/lutin_cgt/bras_croises", new Vector2(15f, -8.5f), new Vector2(12, 17)),
		[PoseLutin.PancarteLevee] = new("res://assets/pnj/lutin_cgt/pancarte_levee", new Vector2(-6.5f, -16f), new Vector2(27, 22)),
		[PoseLutin.AssisCaisse] = new("res://assets/pnj/lutin_cgt/assis_caisse", new Vector2(10.5f, -16.5f), new Vector2(19, 17)),
	};

	// Bornes de la police du slogan : on part de la taille posée dans lutin_cgt.tscn et on descend
	// jusqu'à ce que le texte tienne dans l'aplat (voir AjusterPolice).
	private const int PoliceMin = 4;

	private Config _config;
	private Label _slogan;
	private int _policeOrigine;   // taille posée dans la scène, mémorisée avant tout ajustement

	// Identifiant du choix « je te donne mes poissons » dans l'arbre de dialogue
	// (assets/dialogues/banquise_fin_lutin_cgt.tres) : c'est la clé qui relie la
	// réponse écrite dans l'éditeur à son effet en jeu.
	public const string IdDonPoissons = "lutin_cgt_don_poissons";

	// En éditeur ([Tool]) : met à jour l'aperçu selon la pose et n'exécute PAS le pipeline runtime
	// de la base — base._Ready() masquerait justement le Sprite2D « Apercu » qu'on veut voir ici.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			AppliquerApercu();
			return;
		}
		base._Ready();
	}

	// Charge la 1re frame du dossier de la pose sur le Sprite2D « Apercu » (aperçu éditeur), et
	// replace le slogan puisque l'aplat de la pancarte change avec la pose.
	private void AppliquerApercu()
	{
		var apercu = GetNodeOrNull<Sprite2D>("Apercu");
		if (apercu != null)
			apercu.Texture = GD.Load<Texture2D>($"{Configs[_pose].Dossier}/00.png");

		AppliquerSlogan();
	}

	// Pose le Label du slogan sur l'aplat clair de la pancarte de la pose courante. Point d'entrée
	// unique du placement, partagé par le runtime (Initialiser) et l'éditeur (AppliquerApercu).
	private void AppliquerSlogan()
	{
		// GetNodeOrNull (et non IsNodeReady) : les setters d'export tournent avant que les enfants
		// existent — le nœud est alors introuvable — mais IsNodeReady() est encore false PENDANT
		// _Ready(), si bien qu'un tel garde bloquerait aussi le placement au démarrage du jeu.
		_slogan = GetNodeOrNull<Label>("Slogan");
		if (_slogan == null)
			return;

		var config = Configs[_pose];
		_slogan.Text = _texteSlogan;

		// ClipText ramène la taille minimale du Label à ~(1,1). Sans lui, Godot regonfle Size à la
		// hauteur du texte replié (autowrap) et le centrage vertical se ferait dans cette boîte
		// gonflée : le slogan baverait sous la pancarte. C'est le « layout paresseux d'un Control »
		// que BulleDialogue contourne en dessinant en _Draw ; ici il suffit de le neutraliser.
		_slogan.ClipText = true;
		AjusterPolice(config.TailleSlogan);

		_slogan.Size = config.TailleSlogan;
		_slogan.Position = config.CentreSlogan - config.TailleSlogan / 2f;
	}

	// Réduit la police jusqu'à ce que le slogan tienne dans l'aplat (les pancartes font de 12x17 à
	// 27x22 pixels : la taille d'origine ne convient pas à toutes). On garde la première taille qui
	// rentre, sinon le plancher — ClipText rattrape alors le débordement.
	private void AjusterPolice(Vector2 zone)
	{
		if (_slogan.LabelSettings == null)
			return;

		// Une seule fois par lutin : on retient la taille d'auteur, puis on détache le LabelSettings.
		// C'est une sous-ressource de lutin_cgt.tscn, donc PARTAGÉE par toutes les instances — sans
		// Duplicate, ajuster la police d'un lutin changerait celle de tous les autres.
		if (_policeOrigine == 0)
		{
			_policeOrigine = _slogan.LabelSettings.FontSize;
			_slogan.LabelSettings = (LabelSettings)_slogan.LabelSettings.Duplicate();
		}

		// On repart TOUJOURS de la taille d'origine, jamais de la dernière taille réduite : sinon un
		// slogan long rapetisserait la police définitivement pour tous les slogans suivants.
		_slogan.LabelSettings.FontSize = _policeOrigine;

		if (string.IsNullOrEmpty(_texteSlogan))
			return;

		// LabelSettings.Font est nul ici (seuls taille et couleur sont réglés) : la police réellement
		// utilisée est celle du thème.
		var police = _slogan.LabelSettings.Font ?? _slogan.GetThemeFont("font");
		if (police == null)
			return;

		for (int taille = _policeOrigine; taille > PoliceMin; taille--)
		{
			var mesure = police.GetMultilineStringSize(_texteSlogan, HorizontalAlignment.Center, zone.X, taille);
			// Le repli ne coupe pas un mot plus large que la boîte : on teste aussi la largeur.
			if (mesure.X <= zone.X && mesure.Y <= zone.Y)
			{
				_slogan.LabelSettings.FontSize = taille;
				return;
			}
		}

		_slogan.LabelSettings.FontSize = PoliceMin;
	}

	// Init (avant ConstruireAnimations) : fige le lutin sur place, résout la pose et câble
	// le Label du slogan sur l'aplat de la pancarte.
	protected override void Initialiser()
	{
		DistancePatrouille = 0f;   // gréviste immobile
		_config = Configs[Pose];

		AppliquerSlogan();
	}

	// Le joueur a retenu une réponse : le ravitaillement du piquet de grève se voit
	// tout de suite sur la pancarte. La dépense des poissons, elle, est portée par le
	// choix lui-même (ChoixDialogue.CoutPoissons) — ici on ne gère que la réaction.
	public override void SurChoixRetenu(ChoixDialogue choix)
	{
		if (choix.IdMemoire != IdDonPoissons || _slogan == null)
			return;

		Slogan = "MERCI CAMARADE";   // le setter replace et recentre le Label
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
