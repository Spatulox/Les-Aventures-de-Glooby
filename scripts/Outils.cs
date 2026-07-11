using Godot;

// Petites aides partagées par les constructeurs de salles du monde continu.
public static class Outils
{
	// Rattache un nœud à la racine de la salle et fixe son Owner en un seul
	// endroit - évite le duo AddChild(...) + Owner = racine recopié partout.
	// Owner requis pour qu'un nœud ajouté par code soit inclus si la scène est
	// capturée/sauvegardée (sans ça, PackedScene.Pack() l'ignore).
	public static void Attacher(Node2D racine, Node enfant)
	{
		racine.AddChild(enfant);
		enfant.Owner = racine;
	}

	public static void AjouterDecor(Node2D racine, string chemin, Vector2 position)
	{
		var sprite = new Sprite2D
		{
			Texture = GD.Load<Texture2D>(chemin),
			Position = position,
			ZIndex = -1,
		};
		Attacher(racine, sprite);
	}

	// Les propriétés doivent être fixées AVANT AddChild : _Ready() s'exécute
	// immédiatement à l'ajout dans un arbre actif, donc un .Set(...) après
	// coup arriverait trop tard (le nœud aurait déjà lu sa valeur par défaut).
	public static Node2D Instancier(Node2D racine, string cheminScene, Vector2 position, System.Action<Node2D> avantAjout = null)
	{
		var instance = GD.Load<PackedScene>(cheminScene).Instantiate<Node2D>();
		instance.Position = position;
		avantAjout?.Invoke(instance);
		Attacher(racine, instance);
		return instance;
	}

	// Place une rangée horizontale de panneaux de fond répétés (parallaxe), en
	// alternant les textures fournies. Remplace la boucle Sprite2D recopiée dans
	// plusieurs salles. Si owner est fourni, chaque panneau y est rattaché.
	public static void PlacerFondRepete(Node2D parent, string[] textures, int nombrePanneaux, float largeurPanneau, float posY, float echelle, int zIndex, Vector2 dec, Node2D owner = null)
	{
		for (int i = 0; i < nombrePanneaux; i++)
		{
			var panneau = new Sprite2D
			{
				Texture = GD.Load<Texture2D>(textures[i % textures.Length]),
				Scale = new Vector2(echelle, echelle),
				ZIndex = zIndex,
				Position = new Vector2(i * largeurPanneau + largeurPanneau / 2f, posY) + dec,
			};
			parent.AddChild(panneau);
			if (owner != null)
				panneau.Owner = owner;
		}
	}
}
