using Godot;
using System.Collections.Generic;

// Structure des réglages persistés (miroir de DonneesSauvegarde, côté paramètres) :
// pour l'instant les liaisons de touches par action ; extensible plus tard (audio,
// affichage, accessibilité = autant de sections supplémentaires). Sait se
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

	private const string SectionMeta = "meta";
	private const string SectionTouches = "touches";
	private const string SectionAffichage = "affichage";

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
}
