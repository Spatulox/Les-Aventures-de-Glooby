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

	private const string SectionMeta = "meta";
	private const string SectionTouches = "touches";

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
}
