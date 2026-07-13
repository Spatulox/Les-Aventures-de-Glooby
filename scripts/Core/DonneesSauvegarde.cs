using Godot;
using System.Collections.Generic;

// Structure de données de la progression persistée : regroupe en un seul objet
// tout l'état sauvegardable du jeu (PV, poissons, pouvoir, checkpoint, éléments
// consommés, boss vaincus). GameState en détient une instance et modifie ses
// données ; Sauvegarde l'écrit/relit sur disque. Séparer les données de leur
// gestionnaire rend le round-trip trivial : charger = remplacer l'instance.
public class DonneesSauvegarde
{
	// Version du format sérialisé, pour gérer d'éventuelles migrations futures.
	public const int VersionActuelle = 1;
	public int Version = VersionActuelle;

	// Valeurs par défaut = état d'une nouvelle partie.
	public int Pv;                                   // réglé à PvMax par GameState
	public int Poissons = GameState.PoissonsDepart;
	public bool PouvoirChaleurActif;
	public string CheckpointIdActif = "";
	public Vector2 CheckpointPosition = Vector2.Zero;

	// Identifiants uniques des éléments persistants consommés (murs fondus,
	// dialogues uniques) et des boss vaincus.
	public readonly HashSet<string> ElementsConsommes = new();
	public readonly HashSet<string> BossVaincus = new();

	// Sérialise en dictionnaire Godot (Json ne gère que collections Godot +
	// primitives : Vector2 éclaté en posX/posY, sets en Array de chaînes).
	public Godot.Collections.Dictionary VersDictionnaire()
	{
		return new Godot.Collections.Dictionary
		{
			["version"] = Version,
			["pv"] = Pv,
			["poissons"] = Poissons,
			["pouvoirChaleur"] = PouvoirChaleurActif,
			["checkpointId"] = CheckpointIdActif,
			["posX"] = CheckpointPosition.X,
			["posY"] = CheckpointPosition.Y,
			["elementsConsommes"] = VersArray(ElementsConsommes),
			["bossVaincus"] = VersArray(BossVaincus),
		};
	}

	// Reconstruit l'objet depuis un dictionnaire lu sur disque. Tolérant aux clés
	// absentes : chaque champ retombe sur sa valeur par défaut (compat ascendante).
	public static DonneesSauvegarde DepuisDictionnaire(Godot.Collections.Dictionary d)
	{
		var donnees = new DonneesSauvegarde();
		if (d == null)
			return donnees;

		if (d.TryGetValue("version", out var version)) donnees.Version = (int)version;
		if (d.TryGetValue("pv", out var pv)) donnees.Pv = (int)pv;
		if (d.TryGetValue("poissons", out var poissons)) donnees.Poissons = (int)poissons;
		if (d.TryGetValue("pouvoirChaleur", out var pouvoir)) donnees.PouvoirChaleurActif = (bool)pouvoir;
		if (d.TryGetValue("checkpointId", out var id)) donnees.CheckpointIdActif = (string)id;

		float x = d.TryGetValue("posX", out var px) ? (float)px : 0f;
		float y = d.TryGetValue("posY", out var py) ? (float)py : 0f;
		donnees.CheckpointPosition = new Vector2(x, y);

		if (d.TryGetValue("elementsConsommes", out var elems)) RemplirSet(donnees.ElementsConsommes, elems.AsGodotArray());
		if (d.TryGetValue("bossVaincus", out var boss)) RemplirSet(donnees.BossVaincus, boss.AsGodotArray());

		return donnees;
	}

	// Convertit un ensemble de chaînes en Array Godot (sérialisable en JSON).
	private static Godot.Collections.Array VersArray(HashSet<string> source)
	{
		var tableau = new Godot.Collections.Array();
		foreach (var element in source)
			tableau.Add(element);
		return tableau;
	}

	// Remplit un ensemble depuis un Array Godot lu sur disque.
	private static void RemplirSet(HashSet<string> cible, Godot.Collections.Array source)
	{
		cible.Clear();
		foreach (var element in source)
			cible.Add((string)element);
	}
}
