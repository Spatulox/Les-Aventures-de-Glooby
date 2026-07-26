using Godot;

// Recale les Parallax2D des salles instanciées ailleurs qu'à l'origine du niveau.
//
// Un Parallax2D recalcule sa position chaque frame depuis la caméra, en coordonnées
// MONDE : position = screen_offset × (1 − ScrollScale), où screen_offset est le coin
// de la caméra. Ce terme ignore l'endroit où la salle a été instanciée, alors que la
// transform du parent, elle, s'applique : une salle posée à x = 8128 voyait donc ses
// couches décalées de 8128 × (1 − ScrollScale), soit des milliers de pixels à côté du
// décor qu'elles sont censées habiller.
//
// D'où la correction : −ancrage × (1 − ScrollScale). Une couche pinnée à la caméra
// (ScrollScale = 0) se voit retirer tout l'ancrage ; une couche qui suit le monde
// (ScrollScale = 1) n'est pas touchée.
//
// Elle est appliquée ICI, à l'exécution, et non écrite dans le .tscn du niveau :
// l'éditeur n'a pas de caméra et afficherait le décalage brut, envoyant les props très
// loin à droite. Le niveau reste donc lisible et éditable, et rien n'est à recalculer
// à la main quand une salle bouge.
//
// Usage : déposer scenes/core/recalage_parallaxe.tscn sous le nœud racine du niveau
// (il traite tous les Parallax2D de son parent, salles instanciées comprises).
public partial class RecalageParallaxe : Node
{
	public override void _Ready()
	{
		var racine = GetParent();
		if (racine == null)
			return;

		Recaler(racine);
	}

	private static void Recaler(Node noeud)
	{
		foreach (var enfant in noeud.GetChildren())
		{
			if (enfant is Parallax2D couche)
			{
				// Ancrage = position monde du porteur de la couche (la salle) ; la
				// couche, elle, n'a pas de position propre exploitable.
				var ancrage = (couche.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero;
				if (ancrage != Vector2.Zero)
					Decaler(couche, -ancrage * (Vector2.One - couche.ScrollScale));

				VerifierContenu(couche);
				continue;   // une couche de parallaxe ne contient pas d'autre couche
			}

			Recaler(enfant);
		}
	}

	// Le décalage est appliqué aux ENFANTS de la couche, pas à la couche : Parallax2D
	// réécrit sa propre position ET son ScrollOffset à chaque frame depuis la caméra
	// (position = screen_offset × (1 − ScrollScale)), donc tout réglage posé sur elle
	// - dans le .tscn comme au _Ready - est effacé. Les sprites enfants, eux, ne sont
	// touchés par personne.
	private static void Decaler(Parallax2D couche, Vector2 decalage)
	{
		foreach (var enfant in couche.GetChildren())
			if (enfant is Node2D visuel)
				visuel.Position += decalage;
	}

	// Une couche de parallaxe ne contient QUE du décor peint : elle défile à une autre
	// vitesse que le monde, donc tout ce qui doit rester solidaire du sol (plateforme,
	// panneau, piège...) y « vole ». Le cas est invisible dans l'éditeur, où rien ne
	// défile - d'où cet avertissement au lancement, qui nomme le fautif.
	private static void VerifierContenu(Parallax2D couche)
	{
		foreach (var enfant in couche.GetChildren())
			if (enfant is CollisionObject2D)
				GD.PushWarning($"RecalageParallaxe : '{enfant.Name}' porte une collision mais vit dans "
					+ $"la couche de parallaxe '{couche.GetPath()}' - il défilera à côté du sol. "
					+ "À déplacer sous le nœud de lieu (ou sous Salle/DecorBord).");
	}
}
