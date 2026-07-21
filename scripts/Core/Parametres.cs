using Godot;
using System.Collections.Generic;

// Autoload (singleton) : gestionnaire des paramètres du jeu — touches (remapping
// clavier + manette) et affichage (mode fenêtré/plein écran, résolution, VSync). Au
// démarrage il pose dans l'InputMap les liaisons par défaut du CatalogueActions, puis
// écrase avec les personnalisations lues dans user://parametres.cfg, et applique
// l'affichage. Reprend à GameState la responsabilité de configurer les entrées,
// centralisée ici. Point d'entrée unique des réglages, extensible (audio…).
public partial class Parametres : Node
{
	public static Parametres Instance { get; private set; }

	// Émis quand les liaisons d'une action changent (chaîne vide = tout a changé,
	// ex. réinitialisation globale) : l'UI s'y abonne pour se resynchroniser.
	[Signal]
	public delegate void LiaisonsChangeesEventHandler(string action);

	// État d'affichage courant, conservé en mémoire (la source de vérité, contrairement
	// aux touches qui vivent dans l'InputMap) : appliqué au démarrage, modifié par les
	// setters, et réécrit à chaque Sauver() pour ne jamais perdre la section [affichage].
	private ModeAffichage _modeAffichage = ModeAffichage.Fenetre;
	private Vector2I _tailleFenetre = new(1280, 720);
	private bool _vsync = true;

	public ModeAffichage ModeAffichageCourant => _modeAffichage;
	public Vector2I TailleFenetreCourante => _tailleFenetre;
	public bool VsyncActif => _vsync;

	public override void _Ready()
	{
		Instance = this;
		AppliquerDefautsAuMap();
		Charger();
		AppliquerAffichage();
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

		// Charge aussi l'état d'affichage en mémoire (appliqué ensuite par _Ready).
		_modeAffichage = donnees.Mode;
		_tailleFenetre = donnees.TailleFenetre;
		_vsync = donnees.Vsync;
		return true;
	}

	// Écrit sur disque TOUTES les sections : instantané de l'InputMap (touches) + état
	// d'affichage courant. Écrire les deux à chaque fois évite qu'un remap de touche
	// n'efface la section [affichage] (et inversement).
	public void Sauver()
	{
		var donnees = new DonneesParametres
		{
			Mode = _modeAffichage,
			TailleFenetre = _tailleFenetre,
			Vsync = _vsync,
		};
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

	// --- Affichage ---

	// Applique l'état d'affichage mémorisé (mode + VSync). Appelé au démarrage.
	public void AppliquerAffichage()
	{
		DefinirVsyncMoteur(_vsync);
		AppliquerMode(_modeAffichage);
	}

	// Change le mode d'affichage (effet immédiat) et persiste.
	public void DefinirMode(ModeAffichage mode)
	{
		_modeAffichage = mode;
		AppliquerMode(mode);
		Sauver();
	}

	// Change la taille de fenêtre. Effet immédiat en mode fenêtré uniquement ; en plein
	// écran la valeur est mémorisée et reprise au prochain retour en fenêtré. Persiste.
	public void DefinirResolution(Vector2I taille)
	{
		_tailleFenetre = taille;
		if (_modeAffichage == ModeAffichage.Fenetre)
		{
			DisplayServer.WindowSetSize(taille);
			CentrerFenetre();
		}
		Sauver();
	}

	// Active/désactive la synchronisation verticale (effet immédiat) et persiste.
	public void DefinirVsync(bool actif)
	{
		_vsync = actif;
		DefinirVsyncMoteur(actif);
		Sauver();
	}

	// Résolutions proposées = multiples entiers de la résolution de base du jeu
	// (viewport 640×360, cf. project.godot) qui tiennent dans l'écran courant. L'échelle
	// entière du projet (stretch « viewport ») garantit alors des pixels nets sans
	// étirement ni coupure. Toujours au moins la résolution de base.
	public List<Vector2I> ResolutionsDisponibles()
	{
		int largeurBase = (int)ProjectSettings.GetSetting("display/window/size/viewport_width", 640);
		int hauteurBase = (int)ProjectSettings.GetSetting("display/window/size/viewport_height", 360);

		int ecran = DisplayServer.WindowGetCurrentScreen();
		var tailleEcran = DisplayServer.ScreenGetSize(ecran);

		var liste = new List<Vector2I>();
		for (int facteur = 1; facteur <= 8; facteur++)
		{
			var taille = new Vector2I(largeurBase * facteur, hauteurBase * facteur);
			if (taille.X <= tailleEcran.X && taille.Y <= tailleEcran.Y)
				liste.Add(taille);
		}
		if (liste.Count == 0)
			liste.Add(new Vector2I(largeurBase, hauteurBase));
		return liste;
	}

	// Traduit le mode « métier » en DisplayServer.WindowMode. Au retour en fenêtré on
	// redéfinit taille + position (contourne un bug connu où la fenêtre reste mal placée
	// après un plein écran).
	private void AppliquerMode(ModeAffichage mode)
	{
		switch (mode)
		{
			case ModeAffichage.Fenetre:
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				DisplayServer.WindowSetSize(_tailleFenetre);
				CentrerFenetre();
				break;
			case ModeAffichage.PleinEcran:
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
				break;
			case ModeAffichage.PleinEcranFenetre:
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
				break;
		}
	}

	private static void DefinirVsyncMoteur(bool actif) =>
		DisplayServer.WindowSetVsyncMode(actif ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

	// Recentre la fenêtre sur l'écran courant (multi-écran géré : on part de l'origine
	// de cet écran).
	private static void CentrerFenetre()
	{
		int ecran = DisplayServer.WindowGetCurrentScreen();
		var origine = DisplayServer.ScreenGetPosition(ecran);
		var tailleEcran = DisplayServer.ScreenGetSize(ecran);
		var tailleFenetre = DisplayServer.WindowGetSize();
		DisplayServer.WindowSetPosition(origine + (tailleEcran - tailleFenetre) / 2);
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
