using Godot;

// Aperçu éditeur : convention partagée par tout nœud dont le visuel est construit AU RUNTIME
// (AnimatedSprite2D dont les SpriteFrames sont chargées en code) et qui serait donc invisible
// — donc impossible à placer — dans l'éditeur Godot. La scène porte un Sprite2D enfant nommé
// « Apercu » figé sur une frame représentative ; on le masque au démarrage pour laisser la
// main à l'animation. Vaut pour les entités (LivingEntity) comme pour les projectiles.
public static class ApercuEditeur
{
	// Nom, imposé, du nœud d'aperçu dans les scènes.
	public const string NomNoeud = "Apercu";

	// Masque l'aperçu d'une scène. Facultatif : une scène sans nœud « Apercu » est ignorée.
	public static void Masquer(Node porteur)
	{
		var apercu = porteur.GetNodeOrNull<Sprite2D>(NomNoeud);
		if (apercu != null)
			apercu.Visible = false;
	}
}
