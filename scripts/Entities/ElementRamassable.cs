using Godot;

// Base des objets ramassés au contact du joueur (poisson, pickup de pouvoir).
// Étend DeclencheurZone : si l'objet est déjà consommé, il se retire au
// chargement ; sinon il se ramasse une seule fois à l'entrée du joueur. Les
// sous-classes ne fournissent que la condition « déjà consommé » et l'effet du
// ramassage.
public abstract partial class ElementRamassable : DeclencheurZone
{
	protected override bool PreparerDeclencheur()
	{
		Initialiser();

		if (EstDejaConsomme())
		{
			QueueFree();
			return false;
		}

		PreparerVisuel();
		return true;
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
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
