using Godot;

// Zone de déclenchement : exécute une action quand le joueur entre dans l'Area2D.
// Deux usages : héritage (override SurEntreeJoueur) ou composition (connecter le
// signal JoueurEntre depuis un parent). UneSeuleFois limite à un seul déclenchement.
public partial class DeclencheurZone : Area2D
{
	[Signal] public delegate void JoueurEntreEventHandler(Player joueur);

	[Export] public bool UneSeuleFois;

	private bool _declenche;

	public override void _Ready()
	{
		if (!PreparerDeclencheur())   // permet à une sous-classe d'annuler (ex. QueueFree)
			return;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player joueur)
			return;
		if (UneSeuleFois && _declenche)
			return;

		_declenche = true;
		SurEntreeJoueur(joueur);
		EmitSignal(SignalName.JoueurEntre, joueur);
	}

	// Init avant branchement ; retourner false pour ne pas s'activer.
	protected virtual bool PreparerDeclencheur() => true;

	// Hook d'héritage ; par défaut ne fait rien (usage signal pur).
	protected virtual void SurEntreeJoueur(Player joueur) { }
}
