using Godot;

// Petites aides partagées par les constructeurs de salles du monde continu.
public static class Outils
{
	public static void AjouterDecor(Node2D racine, string chemin, Vector2 position)
	{
		var sprite = new Sprite2D
		{
			Texture = GD.Load<Texture2D>(chemin),
			Position = position,
			ZIndex = -1,
		};
		racine.AddChild(sprite);
		// Owner requis pour qu'un nœud ajouté par code soit inclus si la scène
		// est capturée/sauvegardée (sans ça, PackedScene.Pack() l'ignore).
		sprite.Owner = racine;
	}

	// Les propriétés doivent être fixées AVANT AddChild : _Ready() s'exécute
	// immédiatement à l'ajout dans un arbre actif, donc un .Set(...) après
	// coup arriverait trop tard (le nœud aurait déjà lu sa valeur par défaut).
	public static Node2D Instancier(Node2D racine, string cheminScene, Vector2 position, System.Action<Node2D> avantAjout = null)
	{
		var instance = GD.Load<PackedScene>(cheminScene).Instantiate<Node2D>();
		instance.Position = position;
		avantAjout?.Invoke(instance);
		racine.AddChild(instance);
		instance.Owner = racine;
		return instance;
	}
}
