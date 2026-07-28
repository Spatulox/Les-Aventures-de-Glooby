using Godot;
using System.Collections.Generic;

// Autoload (singleton) : progression du joueur entre les écrans.
public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	// Toute la progression sauvegardable vit dans cette structure : GameState en
	// est le gestionnaire (il lit/écrit ses données), pas le propriétaire des
	// champs. Charger une partie = remplacer cette instance (voir Charger).
	private DonneesSauvegarde _donnees = new();

	[Signal]
	public delegate void PoissonsChangesEventHandler(int total);

	[Signal]
	public delegate void PvChangesEventHandler(int pv, int pvMax);

	[Signal]
	public delegate void JoueurMortEventHandler();

	[Signal]
	public delegate void CheckpointActifEventHandler(string idCheckpoint);

	[Signal]
	public delegate void PouvoirChaleurObtenuEventHandler();

	[Signal]
	public delegate void PouvoirGlaceObtenuEventHandler();

	[Signal]
	public delegate void ManaGlaceChangesEventHandler(float mana, float max);

	[Export] public int PvMax = 5;

	// Pouvoir de Glace : jauge de mana transient (non sauvegardée, régénérée à
	// chaque session). Chaque plateforme de glace posée coûte du mana ; après la
	// dernière pose, la régénération ne reprend qu'après DelaiRegenGlace, puis
	// remonte progressivement (0 -> ManaGlaceMax en DureeRegenGlace secondes).
	[Export] public float ManaGlaceMax = 100f;
	[Export] public float DureeRegenGlace = 30f;
	[Export] public float DelaiRegenGlace = 5f;
	[Export] public float CoutPlateformeGlace = 12f;
	public float ManaGlace { get; private set; }
	private float _delaiRegenTimer;

	// Réserve fixe de poissons donnée en début de partie : ils ne se ramassent
	// pas dans le monde, ils se consomment seulement (soin via ManagerPoisson).
	public const int PoissonsDepart = 50;

	public int Poissons { get => _donnees.Poissons; private set => _donnees.Poissons = value; }
	public int Pv { get => _donnees.Pv; private set => _donnees.Pv = value; }

	// "Continuer" n'est actif que si un fichier de sauvegarde existe sur disque.
	public bool SauvegardeExiste => Sauvegarde.Existe();

	// Mode debug : la partie en cours est une partie de TEST, lancée depuis l'écran de
	// debug du menu principal. À ce titre elle n'écrit jamais sur disque (Sauvegarder)
	// et ne déplace pas le campement actif (Checkpoint). Volontairement hors
	// DonneesSauvegarde : c'est un état de session, il ne doit pas contaminer un
	// fichier de sauvegarde.
	public bool ModeDebug { get; private set; }

	// Facilités cochées pour cette partie de test (clés du CatalogueOptionsDebug).
	// Elles ne sont plus liées en bloc à ModeDebug : chacune s'active séparément dans
	// l'écran de debug, ce qui permet par exemple de tester un niveau avec les pouvoirs
	// mais sans invincibilité. Hors sauvegarde, comme ModeDebug.
	private readonly HashSet<string> _optionsDebug = new();

	// Flags de progression (débloqués une fois pour toute la partie).
	public bool PouvoirChaleurActif { get => _donnees.PouvoirChaleurActif; private set => _donnees.PouvoirChaleurActif = value; }
	public bool PouvoirGlaceActif { get => _donnees.PouvoirGlaceActif; private set => _donnees.PouvoirGlaceActif = value; }

	// Un seul monde continu (façon Hollow Knight) : plus de scène à recharger,
	// juste une position où replacer le joueur.
	public Vector2 CheckpointPosition { get => _donnees.CheckpointPosition; private set => _donnees.CheckpointPosition = value; }
	public string CheckpointIdActif { get => _donnees.CheckpointIdActif; private set => _donnees.CheckpointIdActif = value; }

	// Scène de niveau où la partie a été sauvegardée : lue par « Continuer » du menu
	// pour rouvrir le bon monde (le monde est découpé en plusieurs .tscn).
	public string CheminScene { get => _donnees.CheminScene; private set => _donnees.CheminScene = value; }

	// Porte par laquelle le joueur arrive dans la PROCHAINE scène : posée par la
	// ZoneChargementScene juste avant la bascule, consommée par Player._Ready qui
	// spawn sur le PointEntree d'Id correspondant. État de session (non sauvegardé) :
	// c'est le trajet en cours, pas de la progression. Vide = spawn à la position
	// authorée du nœud Joueur (comportement d'origine, ex. tout premier lancement).
	public string PointEntreeDemande { get; set; } = "";

	// Vrai quand le joueur est à portée de QUELQUE CHOSE avec quoi interagir : un élément
	// parlant (Talkative) ou un mécanisme manœuvrable (PorteBois). La touche de saut,
	// partagée avec l'action "action", est alors captée par cet élément et ne fait pas
	// sauter le joueur — mais seulement s'il est immobile, voir Player._PhysicsProcess.
	public bool InteractionDisponible { get; set; }

	// Vrai pendant une conversation à choix : le joueur ne joue plus, ses touches
	// pilotent la liste de réponses (haut/bas pour naviguer, "action" pour valider).
	// Player._PhysicsProcess s'arrête net dessus, ce qui neutralise d'un coup saut,
	// glissade, traversée de plateforme et pouvoirs — donc tout conflit de touche.
	public bool DialogueModal { get; set; }

	public override void _Ready()
	{
		Instance = this;
		Pv = PvMax;
		ManaGlace = ManaGlaceMax;
		// La configuration des actions d'entrée (défauts + remapping persistant) est
		// désormais gérée par l'autoload Parametres (scripts/Core/Parametres.cs).
	}

	// Régénération du mana de glace : rien pendant DelaiRegenGlace après la
	// dernière pose, puis remontée progressive jusqu'au plein. Le signal n'est
	// émis qu'en cas de changement effectif (pas une émission par frame à plein).
	public override void _Process(double delta)
	{
		if (_delaiRegenTimer > 0f)
		{
			_delaiRegenTimer -= (float)delta;
			return;
		}

		if (ManaGlace >= ManaGlaceMax)
			return;

		ManaGlace = Mathf.Min(ManaGlaceMax, ManaGlace + ManaGlaceMax / DureeRegenGlace * (float)delta);
		EmitSignal(SignalName.ManaGlaceChanges, ManaGlace, ManaGlaceMax);
	}

	// Réinitialise toute la progression pour une nouvelle partie. Le monde ne
	// se recharge pas seul : à appeler avant de charger scenes/01-monde1.tscn.
	public void NouvellePartie()
	{
		_donnees = new DonneesSauvegarde { Pv = PvMax };
		ManaGlace = ManaGlaceMax;
		QuitterModeDebug();
	}

	// Fin d'une partie de test : on retombe dans une vraie partie. À appeler sur TOUT
	// chemin qui sort d'une session de debug — sinon ModeDebug, qui survit au changement
	// de scène (autoload), reste vrai dans la partie suivante et la sabote en silence :
	// Sauvegarder() n'écrit plus rien, Checkpoint ne déplace plus le campement actif
	// (donc les campements ne s'allument même plus) et les facilités cochées (invincible,
	// one-shot, mana infini) restent accordées.
	public void QuitterModeDebug()
	{
		ModeDebug = false;
		_optionsDebug.Clear();
	}

	// Nouvelle partie de test : seules les facilités COCHÉES dans l'écran de debug sont
	// accordées, pour atteindre vite n'importe quel point du monde sans rejouer la
	// progression — mais en gardant la possibilité de tester un niveau presque normal.
	// Réutilise NouvellePartie plutôt que de dupliquer la remise à zéro ; les effets
	// ponctuels (déblocages, mémoires) viennent du catalogue et passent par les méthodes
	// métier, pour que le HUD reçoive bien les signaux.
	// options null = les cases cochées par défaut (appels hors écran de debug : sondes
	// headless, scripts de test).
	public void NouvellePartieDebug(ICollection<string> options = null)
	{
		NouvellePartie();
		ModeDebug = true;

		foreach (var cle in options ?? CatalogueOptionsDebug.ClesParDefaut())
			_optionsDebug.Add(cle);

		foreach (var option in CatalogueOptionsDebug.Toutes)
			if (option.Appliquer != null && _optionsDebug.Contains(option.Cle))
				option.Appliquer(this);
	}

	// Vrai si la facilité de test est active. Le test sur ModeDebug garantit qu'aucune
	// option ne peut fuir dans une vraie partie, même si le set n'était pas vidé.
	public bool OptionDebugActive(string cle) => ModeDebug && _optionsDebug.Contains(cle);

	public void Degats(int quantite = 1)
	{
		Pv = Mathf.Max(0, Pv - quantite);
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
		if (Pv <= 0)
			EmitSignal(SignalName.JoueurMort);
	}

	public void Soigner(int quantite)
	{
		Pv = Mathf.Min(PvMax, Pv + quantite);
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
	}

	// Active un campement de pêche : un seul actif à la fois, les autres
	// basculent en inactif via le signal CheckpointActif.
	public void ActiverCheckpoint(string idCheckpoint, Vector2 position)
	{
		CheckpointIdActif = idCheckpoint;
		CheckpointPosition = position;
		// La SCÈNE fait partie du checkpoint : une position seule ne veut rien dire
		// depuis qu'un niveau peut en appeler un autre (arènes de boss). C'est elle que
		// le respawn rechargera si le joueur meurt ailleurs. Écrite ici et non dans
		// Sauvegarder(), pour que position et scène ne puissent jamais diverger.
		var scene = GetTree()?.CurrentScene?.SceneFilePath;
		if (!string.IsNullOrEmpty(scene))
			CheminScene = scene;
		Soigner(PvMax);
		EmitSignal(SignalName.CheckpointActif, idCheckpoint);
	}

	// Point d'apparition de secours, posé à l'arrivée dans un niveau quand le joueur n'a
	// ENCORE ACTIVÉ AUCUN campement (tout début de partie) : sans lui il n'y aurait aucun
	// point de respawn. Ni soin (les PV traversent la transition) ni écriture disque (une
	// transition n'est pas une sauvegarde).
	//
	// Il ne remplace JAMAIS un vrai campement : depuis que le respawn sait changer de
	// scène, un campement resté dans le niveau précédent est toujours atteignable et doit
	// primer — c'est ce qui renvoie le joueur au dernier feu de camp quand il tombe face
	// à un boss, au lieu de le relancer à l'entrée de l'arène.
	public void DefinirPointEntree(string chemin, Vector2 position)
	{
		if (!string.IsNullOrEmpty(CheckpointIdActif))
			return;

		CheckpointIdActif = "entree";
		CheckpointPosition = position;
		CheminScene = chemin;
	}

	public bool ManagerPoisson()
	{
		if (Poissons <= 0 || Pv >= PvMax)
			return false;

		Poissons--;
		EmitSignal(SignalName.PoissonsChanges, Poissons);
		Soigner(1);
		return true;
	}

	// Prélève des poissons pour autre chose que se soigner (don à un PNJ, troc...).
	// Rien n'est prélevé si la réserve est insuffisante : l'appelant sait alors que
	// l'échange n'a pas eu lieu. Pendant générique de ManagerPoisson, réutilisable
	// par tout élément qui « coûte » des poissons.
	public bool DepenserPoissons(int nombre)
	{
		if (nombre <= 0 || Poissons < nombre)
			return false;

		Poissons -= nombre;
		EmitSignal(SignalName.PoissonsChanges, Poissons);
		return true;
	}

	public void ObtenirPouvoirChaleur()
	{
		if (PouvoirChaleurActif)
			return;

		PouvoirChaleurActif = true;
		EmitSignal(SignalName.PouvoirChaleurObtenu);
	}

	public void ObtenirPouvoirGlace()
	{
		if (PouvoirGlaceActif)
			return;

		PouvoirGlaceActif = true;
		EmitSignal(SignalName.PouvoirGlaceObtenu);
	}

	// Le pouvoir de glace ne se déclenche que débloqué et avec assez de mana.
	public bool PeutUtiliserPouvoirGlace(float cout) => PouvoirGlaceActif && ManaGlace >= cout;

	// Consomme du mana (pose d'une plateforme) et relance le délai avant régen.
	// Avec l'option de test « mana infini » la jauge ne descend pas, donc elle reste
	// pleine et PeutUtiliserPouvoirGlace laisse toujours passer. Bloquer ici plutôt que
	// dans PeutUtiliserPouvoirGlace garde la jauge du HUD cohérente avec ce qu'elle
	// affiche.
	public void ConsommerManaGlace(float cout)
	{
		if (OptionDebugActive(CatalogueOptionsDebug.ManaInfini))
			return;

		ManaGlace = Mathf.Max(0f, ManaGlace - cout);
		_delaiRegenTimer = DelaiRegenGlace;
		EmitSignal(SignalName.ManaGlaceChanges, ManaGlace, ManaGlaceMax);
	}

	// API générique des éléments persistants (un seul ensemble de stockage).
	public bool EstConsomme(string id) => _donnees.ElementsConsommes.Contains(id);

	public void MarquerConsomme(string id) => _donnees.ElementsConsommes.Add(id);

	// Wrappers nommés : gardent des appels métier lisibles côté nœuds.
	public bool EstMurFondu(string idMur) => EstConsomme(idMur);

	public void MarquerMurFondu(string idMur) => MarquerConsomme(idMur);

	// API générique des boss vaincus (miroir des éléments consommés) : prépare
	// de futurs boss et évite qu'un boss battu ne réapparaisse après chargement.
	public bool EstBossVaincu(string id) => _donnees.BossVaincus.Contains(id);

	public void MarquerBossVaincu(string id) => _donnees.BossVaincus.Add(id);

	// Écrit toute la progression courante sur disque (emplacement unique). En mode
	// debug on n'écrit jamais : une partie de test ne doit pas contaminer le fichier
	// de sauvegarde (voir ModeDebug), sinon Continuer reprendrait un demi-état debug
	// (pouvoirs débloqués mais ni mana infini ni oneshot). Continuer reprend donc
	// toujours une vraie partie.
	public void Sauvegarder()
	{
		if (ModeDebug)
			return;

		// CheminScene n'est PAS remis à la scène courante ici : il désigne la scène du
		// checkpoint, et c'est ActiverCheckpoint qui l'écrit. Sauvegarder est aussi appelé
		// hors campement (défaite d'un boss, dans une arène sans feu de camp) — l'écraser
		// ferait pointer la scène d'arène avec la position d'un campement d'ailleurs, et
		// « Continuer » comme le respawn téléporteraient dans le vide.
		Sauvegarde.Ecrire(_donnees.VersDictionnaire());
	}

	// Recharge la progression depuis le disque : remplace l'instance de données
	// puis ré-émet les signaux pour resynchroniser HUD et sprites de checkpoint
	// (dont les pouvoirs débloqués : le HUD, autoload, a lu son état une seule fois
	// au boot et doit être renotifié pour ré-afficher la jauge de mana).
	// Retourne false si aucune sauvegarde n'existe.
	public bool Charger()
	{
		var dict = Sauvegarde.Lire();
		if (dict == null)
			return false;

		// Reprendre une sauvegarde, c'est par définition reprendre une VRAIE partie :
		// on sort du mode test ici plutôt que de compter sur le passage par le menu.
		QuitterModeDebug();

		_donnees = DonneesSauvegarde.DepuisDictionnaire(dict);
		Pv = Mathf.Min(Pv, PvMax);

		EmitSignal(SignalName.PvChanges, Pv, PvMax);
		EmitSignal(SignalName.PoissonsChanges, Poissons);
		EmitSignal(SignalName.CheckpointActif, CheckpointIdActif);

		// Resynchronise les pouvoirs déjà débloqués dans la sauvegarde : sans ça la
		// jauge de mana (masquée au boot) ne réapparaîtrait jamais après Continuer.
		if (PouvoirChaleurActif)
			EmitSignal(SignalName.PouvoirChaleurObtenu);
		if (PouvoirGlaceActif)
		{
			EmitSignal(SignalName.PouvoirGlaceObtenu);
			EmitSignal(SignalName.ManaGlaceChanges, ManaGlace, ManaGlaceMax);
		}

		return true;
	}

	// Ne change plus de scène (monde continu) : Player se téléporte lui-même
	// à CheckpointPosition après cet appel.
	public void RespawnAuCheckpoint()
	{
		Pv = PvMax;
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
	}

}
