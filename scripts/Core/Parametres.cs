using Godot;
using System.Collections.Generic;

// Autoload (singleton) : gestionnaire des paramètres du jeu — touches (remapping
// clavier + manette), affichage (mode fenêtré/plein écran, résolution, VSync) et audio
// (volume des bus Master/Musique/Ambiance). Au démarrage il pose dans l'InputMap les
// liaisons par défaut du CatalogueActions, puis écrase avec les personnalisations lues
// dans user://parametres.cfg, et applique l'affichage puis l'audio. Reprend à GameState
// la responsabilité de configurer les entrées, centralisée ici. Point d'entrée unique
// des réglages, extensible (accessibilité…).
public partial class Parametres : Node
{
	public static Parametres Instance { get; private set; }

	// Noms des bus audio (default_bus_layout.tres). Centralisés ici pour que l'UI n'ait
	// pas à répéter les chaînes littérales.
	public const string BusMaster = "Master";
	public const string BusMusique = "Musique";
	public const string BusAmbiance = "Ambiance";

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

	// Volumes audio par bus, en linéaire 0 → 1. Comme l'affichage, la mémoire fait foi :
	// on les applique à l'AudioServer au démarrage et à chaque modification.
	private float _volumeMaster = 1f;
	private float _volumeMusique = 1f;
	private float _volumeAmbiance = 1f;

	public ModeAffichage ModeAffichageCourant => _modeAffichage;
	public Vector2I TailleFenetreCourante => _tailleFenetre;
	public bool VsyncActif => _vsync;

	// Faux quand le moteur refuse nos ordres de fenêtre — cas de la fenêtre EMBARQUÉE dans
	// l'éditeur (onglet « Game », Godot 4.4+), où DisplayServer rejette mode, taille et
	// position (« Embedded window can't be resized. »…). Ce mode n'est pas exposé au script
	// et se détecte différemment selon l'OS : plutôt que de le deviner, on applique, on
	// relit, et on retient le refus. L'UI s'en sert pour annoncer un effet différé ; le
	// choix de l'utilisateur reste mémorisé et sauvegardé, donc appliqué au prochain
	// lancement non embarqué.
	public bool FenetrePilotable { get; private set; } = true;

	public float VolumeMasterCourant => _volumeMaster;
	public float VolumeMusiqueCourant => _volumeMusique;
	public float VolumeAmbianceCourant => _volumeAmbiance;

	public override void _Ready()
	{
		Instance = this;
		AppliquerDefautsAuMap();
		Charger();
		AppliquerAffichage();
		AppliquerAudio();
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

		_volumeMaster = donnees.VolumeMaster;
		_volumeMusique = donnees.VolumeMusique;
		_volumeAmbiance = donnees.VolumeAmbiance;
		return true;
	}

	// Écrit sur disque TOUTES les sections : instantané de l'InputMap (touches) + état
	// d'affichage + volumes audio. Tout réécrire à chaque fois évite qu'un remap de touche
	// n'efface la section [affichage] ou [audio] (et inversement).
	public void Sauver()
	{
		var donnees = new DonneesParametres
		{
			Mode = _modeAffichage,
			TailleFenetre = _tailleFenetre,
			Vsync = _vsync,
			VolumeMaster = _volumeMaster,
			VolumeMusique = _volumeMusique,
			VolumeAmbiance = _volumeAmbiance,
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
			AppliquerTailleFenetre();
		Sauver();
	}

	// Active/désactive la synchronisation verticale (effet immédiat) et persiste.
	public void DefinirVsync(bool actif)
	{
		_vsync = actif;
		DefinirVsyncMoteur(actif);
		Sauver();
	}

	// --- Audio ---

	// Applique les trois volumes mémorisés aux bus. Appelé au démarrage.
	public void AppliquerAudio()
	{
		AppliquerVolumeBus(BusMaster, _volumeMaster);
		AppliquerVolumeBus(BusMusique, _volumeMusique);
		AppliquerVolumeBus(BusAmbiance, _volumeAmbiance);
	}

	// Volume courant du bus demandé (1 si le nom est inconnu) : pour initialiser l'UI.
	public float VolumeCourant(string bus) => bus switch
	{
		BusMusique => _volumeMusique,
		BusAmbiance => _volumeAmbiance,
		BusMaster => _volumeMaster,
		_ => 1f,
	};

	// Change le volume d'un bus (effet immédiat) et persiste. La valeur est bornée à
	// [0,1] : l'appelant (slider) n'a pas à s'en soucier.
	public void DefinirVolume(string bus, float valeur)
	{
		valeur = Mathf.Clamp(valeur, 0f, 1f);
		switch (bus)
		{
			case BusMaster: _volumeMaster = valeur; break;
			case BusMusique: _volumeMusique = valeur; break;
			case BusAmbiance: _volumeAmbiance = valeur; break;
			default: return;
		}

		AppliquerVolumeBus(bus, valeur);
		Sauver();
	}

	// Pose le volume sur le bus de l'AudioServer. Le volume utilisateur vit sur le BUS,
	// alors que GestionnaireAudio tweene le volume_db de ses LECTEURS pour ses fondus :
	// les deux se composent sans se marcher dessus. À 0, LinearToDb vaut -inf : on coupe
	// explicitement le bus plutôt que de compter sur ce cas limite. Un bus absent
	// (renommé dans le layout) est ignoré silencieusement plutôt que de planter.
	private static void AppliquerVolumeBus(string nom, float valeur)
	{
		int index = AudioServer.GetBusIndex(nom);
		if (index < 0)
			return;

		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(valeur));
		AudioServer.SetBusMute(index, valeur <= 0f);
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

	// Pose le mode moteur correspondant, puis vérifie que la fenêtre a obtempéré. Au retour
	// en fenêtré on redéfinit taille + position (contourne un bug connu où la fenêtre reste
	// mal placée après un plein écran) — c'est cette étape qui fait office de vérification
	// dans ce cas, le mode fenêtré étant déjà celui d'une fenêtre embarquée.
	private void AppliquerMode(ModeAffichage mode)
	{
		if (!FenetrePilotable)
			return;

		var attendu = ModeMoteur(mode);
		DisplayServer.WindowSetMode(attendu);

		if (mode == ModeAffichage.Fenetre)
			AppliquerTailleFenetre();
		else
			VerifierPilotage(EstPleinEcran(DisplayServer.WindowGetMode()));
	}

	// Applique la taille mémorisée puis recentre. La relecture immédiate sert de test :
	// une fenêtre embarquée ignore le redimensionnement (après l'avoir loggé) et renvoie
	// toujours la taille de son conteneur.
	private void AppliquerTailleFenetre()
	{
		if (!FenetrePilotable)
			return;

		DisplayServer.WindowSetSize(_tailleFenetre);
		VerifierPilotage(DisplayServer.WindowGetSize() == _tailleFenetre);

		if (FenetrePilotable)
			CentrerFenetre(_tailleFenetre);
	}

	// Retient un refus du moteur. Sens unique : l'embarquement ne peut pas être levé en
	// cours d'exécution, donc on ne repasse jamais à « pilotable ».
	private void VerifierPilotage(bool applique)
	{
		if (!applique)
			FenetrePilotable = false;
	}

	// Traduit le mode « métier » en DisplayServer.WindowMode.
	private static DisplayServer.WindowMode ModeMoteur(ModeAffichage mode) => mode switch
	{
		ModeAffichage.PleinEcran => DisplayServer.WindowMode.ExclusiveFullscreen,
		ModeAffichage.PleinEcranFenetre => DisplayServer.WindowMode.Fullscreen,
		_ => DisplayServer.WindowMode.Windowed,
	};

	// Nos deux modes plein écran sont interchangeables pour ce test : selon la plateforme
	// (Wayland notamment) le moteur retombe de l'exclusif vers le plein écran fenêtré. On
	// vérifie donc « est-on en plein écran ? » plutôt qu'une égalité stricte, qui
	// signalerait à tort une fenêtre non pilotable.
	private static bool EstPleinEcran(DisplayServer.WindowMode mode) =>
		mode == DisplayServer.WindowMode.Fullscreen || mode == DisplayServer.WindowMode.ExclusiveFullscreen;

	private static void DefinirVsyncMoteur(bool actif) =>
		DisplayServer.WindowSetVsyncMode(actif ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

	// Recentre la fenêtre de taille donnée sur l'écran courant (multi-écran géré : on
	// part de l'origine de cet écran). On calcule à partir de la taille cible plutôt que
	// de WindowGetSize(), qui peut être encore périmée juste après un redimensionnement.
	// Sous Wayland, une application ne peut pas positionner sa propre fenêtre (c'est le
	// compositeur qui décide) : on n'essaie pas, ça éviterait un appel sans effet. La
	// comparaison ignore la casse — le moteur nomme ses pilotes en minuscules (« wayland »)
	// mais les DisplayServer se présentent capitalisés, et se tromper de casse ici
	// désarmerait silencieusement le garde.
	private static void CentrerFenetre(Vector2I tailleFenetre)
	{
		if (DisplayServer.GetName().ToLowerInvariant() == "wayland")
			return;

		int ecran = DisplayServer.WindowGetCurrentScreen();
		var origine = DisplayServer.ScreenGetPosition(ecran);
		var tailleEcran = DisplayServer.ScreenGetSize(ecran);
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
