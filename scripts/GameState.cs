using Godot;

// Autoload (singleton) : progression du joueur entre les écrans.
public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	[Signal]
	public delegate void PoissonsChangesEventHandler(int total);

	[Signal]
	public delegate void PvChangesEventHandler(int pv, int pvMax);

	[Signal]
	public delegate void JoueurMortEventHandler();

	[Export] public int PvMax = 5;

	public int Poissons { get; private set; } = 0;
	public int Pv { get; private set; }

	public string CheckpointScene { get; private set; } = "res://scenes/ecran01.tscn";
	public Vector2 CheckpointPosition { get; private set; } = Vector2.Zero;

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

	public void DefinirCheckpoint(string scenePath, Vector2 position)
	{
		CheckpointScene = scenePath;
		CheckpointPosition = position;
	}

	public void RespawnAuCheckpoint()
	{
		Pv = PvMax;
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
		GetTree().ChangeSceneToFile(CheckpointScene);
	}

	// Enregistre les actions d'entrée (mouvement, saut, glissade) par code,
	// pour ne pas dépendre d'un mapping figé dans project.godot.
	private static void ConfigurerActionsParDefaut()
	{
		AjouterAction("move_left", Key.A, Key.Left);
		AjouterAction("move_right", Key.D, Key.Right);
		AjouterAction("jump", Key.Space);
		AjouterAction("slide", Key.Shift);
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
