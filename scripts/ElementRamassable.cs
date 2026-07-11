using Godot;

// Base des objets ramassés au contact du joueur (poisson, pickup de pouvoir).
// Factorise le squelette commun : si l'objet est déjà consommé, il se retire
// au chargement ; sinon il se connecte au contact et se ramasse une seule fois.
// Les sous-classes ne fournissent que la condition « déjà consommé » et l'effet
// du ramassage.
public abstract partial class ElementRamassable : Area2D
{
	public override void _Ready()
	{
		Initialiser();

		if (EstDejaConsomme())
		{
			QueueFree();
			return;
		}

		PreparerVisuel();
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
			return;

		Ramasser();
		QueueFree();
	}

	// Réglages faits avant la vérification de consommation (ex. défaut de l'id).
	protected virtual void Initialiser() { }

	// Ambiance optionnelle une fois l'objet confirmé présent (ex. flottaison).
	protected virtual void PreparerVisuel() { }

	protected abstract bool EstDejaConsomme();

	protected abstract void Ramasser();
}
