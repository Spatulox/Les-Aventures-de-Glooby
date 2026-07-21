using Godot;
using System.Collections.Generic;

// Structure des réglages persistés (miroir de DonneesSauvegarde, côté paramètres) :
// liaisons de touches par action, affichage et volumes audio ; extensible plus tard
// (accessibilité = une section supplémentaire de plus). Sait se
// (dé)sérialiser vers un ConfigFile sectionné. Séparer les données de leur I/O
// (ConfigFichier) et de leur application à l'InputMap (Parametres) garde le
// round-trip trivial, comme pour la sauvegarde de progression.
public class DonneesParametres
{
	// Version du format sérialisé, pour d'éventuelles migrations futures.
	public const int VersionActuelle = 1;
	public int Version = VersionActuelle;

	// action -> événements liés (clavier + manette mélangés, comme dans l'InputMap).
	public readonly Dictionary<string, Godot.Collections.Array<InputEvent>> Touches = new();

	// Réglages d'affichage (section [affichage]). Défauts = état du projet au lancement
	// (fenêtré 1280×720, VSync active).
	public ModeAffichage Mode = ModeAffichage.Fenetre;
	public Vector2I TailleFenetre = new(1280, 720);
	public bool Vsync = true;

	// Volumes audio par bus (section [audio]), en linéaire 0 → 1. Défaut = 1 (volume
	// plein) : un fichier sans cette section laisse le jeu sonner comme avant.
	public float VolumeMaster = 1f;
	public float VolumeMusique = 1f;
	public float VolumeAmbiance = 1f;

	private const string SectionMeta = "meta";
	private const string SectionTouches = "touches";
	private const string SectionAffichage = "affichage";
	private const string SectionAudio = "audio";

	// Écrit toutes les liaisons dans un ConfigFile : une clé par action, valeur =
	// tableau de descripteurs sérialisables (voir EvenementEntree.Serialiser).
	public ConfigFile VersConfig()
	{
		var cfg = new ConfigFile();
		cfg.SetValue(SectionMeta, "version", Version);

		foreach (var (action, evenements) in Touches)
		{
			var tableau = new Godot.Collections.Array();
			foreach (var evenement in evenements)
			{
				var descripteur = EvenementEntree.Serialiser(evenement);
				if (descripteur != null)
					tableau.Add(descripteur);
			}
			cfg.SetValue(SectionTouches, action, tableau);
		}

		cfg.SetValue(SectionAffichage, "mode", (int)Mode);
		cfg.SetValue(SectionAffichage, "largeur", TailleFenetre.X);
		cfg.SetValue(SectionAffichage, "hauteur", TailleFenetre.Y);
		cfg.SetValue(SectionAffichage, "vsync", Vsync);

		cfg.SetValue(SectionAudio, "master", VolumeMaster);
		cfg.SetValue(SectionAudio, "musique", VolumeMusique);
		cfg.SetValue(SectionAudio, "ambiance", VolumeAmbiance);
		return cfg;
	}

	// Reconstruit depuis un ConfigFile. Tolérant : descripteurs inconnus ignorés,
	// section absente => aucune touche (les défauts posés par Parametres restent).
	public static DonneesParametres DepuisConfig(ConfigFile cfg)
	{
		var donnees = new DonneesParametres();
		if (cfg == null)
			return donnees;

		if (cfg.HasSectionKey(SectionMeta, "version"))
			donnees.Version = (int)cfg.GetValue(SectionMeta, "version");

		LireAffichage(cfg, donnees);
		LireAudio(cfg, donnees);

		if (!cfg.HasSection(SectionTouches))
			return donnees;

		foreach (var action in cfg.GetSectionKeys(SectionTouches))
		{
			var liste = new Godot.Collections.Array<InputEvent>();
			foreach (var element in cfg.GetValue(SectionTouches, action).AsGodotArray())
			{
				var evenement = EvenementEntree.Deserialiser(element.AsGodotDictionary());
				if (evenement != null)
					liste.Add(evenement);
			}
			donnees.Touches[action] = liste;
		}
		return donnees;
	}

	// Lit la section [affichage] si présente ; chaque champ absent garde son défaut
	// (compat ascendante : un ancien fichier sans cette section reste valide).
	private static void LireAffichage(ConfigFile cfg, DonneesParametres donnees)
	{
		if (!cfg.HasSection(SectionAffichage))
			return;

		if (cfg.HasSectionKey(SectionAffichage, "mode"))
			donnees.Mode = (ModeAffichage)(int)cfg.GetValue(SectionAffichage, "mode");

		int largeur = cfg.HasSectionKey(SectionAffichage, "largeur")
			? (int)cfg.GetValue(SectionAffichage, "largeur") : donnees.TailleFenetre.X;
		int hauteur = cfg.HasSectionKey(SectionAffichage, "hauteur")
			? (int)cfg.GetValue(SectionAffichage, "hauteur") : donnees.TailleFenetre.Y;
		donnees.TailleFenetre = new Vector2I(largeur, hauteur);

		if (cfg.HasSectionKey(SectionAffichage, "vsync"))
			donnees.Vsync = (bool)cfg.GetValue(SectionAffichage, "vsync");
	}

	// Lit la section [audio] si présente ; même tolérance que LireAffichage. Les valeurs
	// sont bornées à [0,1] : le fichier est éditable à la main, on ne lui fait pas confiance.
	private static void LireAudio(ConfigFile cfg, DonneesParametres donnees)
	{
		if (!cfg.HasSection(SectionAudio))
			return;

		donnees.VolumeMaster = LireVolume(cfg, "master", donnees.VolumeMaster);
		donnees.VolumeMusique = LireVolume(cfg, "musique", donnees.VolumeMusique);
		donnees.VolumeAmbiance = LireVolume(cfg, "ambiance", donnees.VolumeAmbiance);
	}

	private static float LireVolume(ConfigFile cfg, string cle, float defaut) =>
		cfg.HasSectionKey(SectionAudio, cle)
			? Mathf.Clamp((float)cfg.GetValue(SectionAudio, cle), 0f, 1f)
			: defaut;
}
