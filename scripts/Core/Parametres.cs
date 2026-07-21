using Godot;

// Autoload (singleton) : gestionnaire des paramètres du jeu. Au démarrage il pose
// dans l'InputMap toutes les actions du CatalogueActions avec leurs liaisons par
// défaut (clavier + manette), puis écrase avec les personnalisations lues dans
// user://parametres.cfg. Reprend à GameState la responsabilité de configurer les
// entrées, désormais centralisée ici. Le remapping, la détection de conflits et la
// réinitialisation viendront s'ajouter (itérations suivantes) ; cette classe reste
// le point d'entrée unique des réglages, extensible (audio, affichage…).
public partial class Parametres : Node
{
	public static Parametres Instance { get; private set; }

	// Émis quand les liaisons d'une action changent (chaîne vide = tout a changé,
	// ex. réinitialisation globale) : l'UI s'y abonne pour se resynchroniser.
	[Signal]
	public delegate void LiaisonsChangeesEventHandler(string action);

	public override void _Ready()
	{
		Instance = this;
		AppliquerDefautsAuMap();
		Charger();
	}

	// (Ré)installe toutes les actions du catalogue dans l'InputMap avec leurs
	// liaisons par défaut (clavier + manette). Base du démarrage et du reset global.
	public void AppliquerDefautsAuMap()
	{
		foreach (var action in CatalogueActions.Toutes)
		{
			if (!InputMap.HasAction(action.Nom))
				InputMap.AddAction(action.Nom, action.ZoneMorte);
			InputMap.ActionSetDeadzone(action.Nom, action.ZoneMorte);
			InputMap.ActionEraseEvents(action.Nom);
			foreach (var evenement in action.EvenementsDefaut())
				InputMap.ActionAddEvent(action.Nom, evenement);
		}
	}

	// Applique par-dessus les défauts les liaisons personnalisées du disque. Une
	// action absente du fichier garde son défaut (compat ascendante). Retourne false
	// si aucun fichier n'existe encore.
	public bool Charger()
	{
		var cfg = ConfigFichier.Lire();
		if (cfg == null)
			return false;

		var donnees = DonneesParametres.DepuisConfig(cfg);
		foreach (var (action, evenements) in donnees.Touches)
		{
			if (!InputMap.HasAction(action))
				continue;
			AppliquerListe(action, evenements);
		}
		return true;
	}

	// Écrit sur disque l'état courant de l'InputMap pour toutes les actions du
	// catalogue (instantané complet : le fichier reste la copie fidèle du mapping).
	public void Sauver()
	{
		var donnees = new DonneesParametres();
		foreach (var action in CatalogueActions.Toutes)
			donnees.Touches[action.Nom] = InputMap.ActionGetEvents(action.Nom);
		ConfigFichier.Ecrire(donnees.VersConfig());
	}

	// Remplace toutes les liaisons d'une action par la liste fournie.
	private static void AppliquerListe(string action, Godot.Collections.Array<InputEvent> evenements)
	{
		InputMap.ActionEraseEvents(action);
		foreach (var evenement in evenements)
			InputMap.ActionAddEvent(action, evenement);
	}
}
