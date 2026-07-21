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

	// Première liaison clavier de l'action (null si aucune) : pour l'affichage UI.
	public InputEvent LiaisonClavier(string action) => PremiereDeType(action, clavier: true);

	// Première liaison manette de l'action (null si aucune) : pour l'affichage UI.
	public InputEvent LiaisonManette(string action) => PremiereDeType(action, clavier: false);

	// Cherche une action (autre que celle exclue) dont une liaison correspond à
	// l'événement fourni. Retourne son nom, ou null s'il n'y a pas de conflit.
	public string TrouverConflit(InputEvent evenement, string actionExclue)
	{
		foreach (var action in CatalogueActions.Toutes)
		{
			if (action.Nom == actionExclue)
				continue;
			foreach (var existant in InputMap.ActionGetEvents(action.Nom))
				if (EvenementEntree.Correspond(existant, evenement))
					return action.Nom;
		}
		return null;
	}

	// Remplace, pour l'action, la liaison du même périphérique que l'événement fourni
	// (clavier OU manette) en conservant l'autre périphérique, puis persiste et signale.
	// La résolution d'un éventuel conflit est à faire en amont (RetirerCorrespondance).
	public void Remapper(string action, InputEvent nouvel)
	{
		if (nouvel == null || !InputMap.HasAction(action))
			return;

		bool clavier = EvenementEntree.EstClavier(nouvel);
		foreach (var existant in InputMap.ActionGetEvents(action))
			if (clavier ? EvenementEntree.EstClavier(existant) : EvenementEntree.EstManette(existant))
				InputMap.ActionEraseEvent(action, existant);

		InputMap.ActionAddEvent(action, nouvel);
		Sauver();
		EmitSignal(SignalName.LiaisonsChangees, action);
	}

	// Retire de l'action toute liaison correspondant à l'événement (résolution de
	// conflit : on libère l'action qui détenait la touche). Persiste et signale.
	public void RetirerCorrespondance(string action, InputEvent evenement)
	{
		if (!InputMap.HasAction(action))
			return;

		bool change = false;
		foreach (var existant in InputMap.ActionGetEvents(action))
			if (EvenementEntree.Correspond(existant, evenement))
			{
				InputMap.ActionEraseEvent(action, existant);
				change = true;
			}

		if (change)
		{
			Sauver();
			EmitSignal(SignalName.LiaisonsChangees, action);
		}
	}

	// Réinitialise une action à ses liaisons par défaut (catalogue). Persiste et signale.
	public void ReinitialiserAction(string action)
	{
		var fiche = CatalogueActions.Trouver(action);
		if (fiche == null || !InputMap.HasAction(action))
			return;

		InputMap.ActionEraseEvents(action);
		foreach (var evenement in fiche.EvenementsDefaut())
			InputMap.ActionAddEvent(action, evenement);
		Sauver();
		EmitSignal(SignalName.LiaisonsChangees, action);
	}

	// Réinitialise TOUTES les actions aux défauts. Persiste et signale (action = "",
	// convention « tout a changé » pour que l'UI se reconstruise entièrement).
	public void ReinitialiserTout()
	{
		AppliquerDefautsAuMap();
		Sauver();
		EmitSignal(SignalName.LiaisonsChangees, "");
	}

	// Première liaison de l'action du périphérique demandé (clavier ou manette).
	private static InputEvent PremiereDeType(string action, bool clavier)
	{
		if (!InputMap.HasAction(action))
			return null;
		foreach (var evenement in InputMap.ActionGetEvents(action))
			if (clavier ? EvenementEntree.EstClavier(evenement) : EvenementEntree.EstManette(evenement))
				return evenement;
		return null;
	}

	// Remplace toutes les liaisons d'une action par la liste fournie.
	private static void AppliquerListe(string action, Godot.Collections.Array<InputEvent> evenements)
	{
		InputMap.ActionEraseEvents(action);
		foreach (var evenement in evenements)
			InputMap.ActionAddEvent(action, evenement);
	}
}
