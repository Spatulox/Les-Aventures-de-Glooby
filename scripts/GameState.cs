using Godot;
using System.Collections.Generic;

// Autoload (singleton) : progression du joueur entre les écrans.
public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	private readonly HashSet<string> _mursFondus = new();
	private readonly HashSet<string> _poissonsRamasses = new();

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

	public int Poissons { get; private set; } = 0;
	public int Pv { get; private set; }

	// Flags de progression (débloqués une fois pour toute la partie).
	public bool PouvoirChaleurActif { get; private set; }

	// Un seul monde continu (façon Hollow Knight) : plus de scène à recharger,
	// juste une position où replacer le joueur.
	public Vector2 CheckpointPosition { get; private set; } = Vector2.Zero;
	public string CheckpointIdActif { get; private set; } = "";

	public override void _Ready()
	{
		Instance = this;
		Pv = PvMax;
		ConfigurerActionsParDefaut();
	}

	public void AjouterPoissons(int quantite = 1)
	{
		Poissons += quantite;
		EmitSignal(SignalName.PoissonsChanges, Poissons);
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

	public bool EstMurFondu(string idMur) => _mursFondus.Contains(idMur);

	public void MarquerMurFondu(string idMur) => _mursFondus.Add(idMur);

	public bool EstPoissonRamasse(string idPoisson) => _poissonsRamasses.Contains(idPoisson);

	public void RamasserPoisson(string idPoisson)
	{
		if (_poissonsRamasses.Contains(idPoisson))
			return;

		_poissonsRamasses.Add(idPoisson);
		AjouterPoissons(1);
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
		AjouterAction("move_left", Key.A, Key.Left);
		AjouterAction("move_right", Key.D, Key.Right);
		AjouterAction("jump", Key.Space);
		AjouterAction("slide", Key.Shift);
		AjouterAction("lancer", Key.J, Key.Ctrl);
		AjouterAction("manger", Key.E);
		AjouterAction("pouvoir_chaleur", Key.F);
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
