using Godot;

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

	[Export] public int PvMax = 5;

	// Réserve fixe de poissons donnée en début de partie : ils ne se ramassent
	// pas dans le monde, ils se consomment seulement (soin via ManagerPoisson).
	public const int PoissonsDepart = 50;

	public int Poissons { get => _donnees.Poissons; private set => _donnees.Poissons = value; }
	public int Pv { get => _donnees.Pv; private set => _donnees.Pv = value; }

	// "Continuer" n'est actif que si un fichier de sauvegarde existe sur disque.
	public bool SauvegardeExiste => Sauvegarde.Existe();

	// Flags de progression (débloqués une fois pour toute la partie).
	public bool PouvoirChaleurActif { get => _donnees.PouvoirChaleurActif; private set => _donnees.PouvoirChaleurActif = value; }

	// Un seul monde continu (façon Hollow Knight) : plus de scène à recharger,
	// juste une position où replacer le joueur.
	public Vector2 CheckpointPosition { get => _donnees.CheckpointPosition; private set => _donnees.CheckpointPosition = value; }
	public string CheckpointIdActif { get => _donnees.CheckpointIdActif; private set => _donnees.CheckpointIdActif = value; }

	// Vrai quand le joueur est à portée d'un élément parlant (Talkative) : la touche
	// de saut, partagée avec l'action "action", est alors captée par le dialogue et
	// ne fait pas sauter le joueur (voir Player._PhysicsProcess et DeclencheurDialogue).
	public bool DialogueDisponible { get; set; }

	public override void _Ready()
	{
		Instance = this;
		Pv = PvMax;
		ConfigurerActionsParDefaut();
	}

	// Réinitialise toute la progression pour une nouvelle partie. Le monde ne
	// se recharge pas seul : à appeler avant de charger scenes/monde.tscn.
	public void NouvellePartie()
	{
		_donnees = new DonneesSauvegarde { Pv = PvMax };
	}

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
		Soigner(PvMax);
		EmitSignal(SignalName.CheckpointActif, idCheckpoint);
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

	public void ObtenirPouvoirChaleur()
	{
		if (PouvoirChaleurActif)
			return;

		PouvoirChaleurActif = true;
		EmitSignal(SignalName.PouvoirChaleurObtenu);
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

	// Écrit toute la progression courante sur disque (emplacement unique).
	public void Sauvegarder() => Sauvegarde.Ecrire(_donnees.VersDictionnaire());

	// Recharge la progression depuis le disque : remplace l'instance de données
	// puis ré-émet les signaux pour resynchroniser HUD et sprites de checkpoint.
	// Retourne false si aucune sauvegarde n'existe.
	public bool Charger()
	{
		var dict = Sauvegarde.Lire();
		if (dict == null)
			return false;

		_donnees = DonneesSauvegarde.DepuisDictionnaire(dict);
		Pv = Mathf.Min(Pv, PvMax);

		EmitSignal(SignalName.PvChanges, Pv, PvMax);
		EmitSignal(SignalName.PoissonsChanges, Poissons);
		EmitSignal(SignalName.CheckpointActif, CheckpointIdActif);
		return true;
	}

	// Ne change plus de scène (monde continu) : Player se téléporte lui-même
	// à CheckpointPosition après cet appel.
	public void RespawnAuCheckpoint()
	{
		Pv = PvMax;
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
	}

	// Enregistre les actions d'entrée (mouvement, saut, glissade) par code,
	// pour ne pas dépendre d'un mapping figé dans project.godot.
	private static void ConfigurerActionsParDefaut()
	{
		// Déplacement
		AjouterAction("move_left", Key.Left);
		AjouterAction("move_right", Key.Right);
		AjouterAction("jump", Key.Space);
		AjouterAction("slide", Key.Shift);
		AjouterAction("bas", Key.Down);

		// Actions
		AjouterAction("lancer", Key.D);
		AjouterAction("manger", Key.W);
		AjouterAction("pouvoir_chaleur", Key.A);
		
		// Interactions
		AjouterAction("action", Key.Enter, Key.Space);
		AjouterAction("menu", Key.Escape);
	}

	private static void AjouterAction(string nom, params Key[] touches)
	{
		if (InputMap.HasAction(nom))
			return;

		InputMap.AddAction(nom);
		foreach (var touche in touches)
		{
			InputMap.ActionAddEvent(nom, new InputEventKey { PhysicalKeycode = touche });
		}
	}
}
