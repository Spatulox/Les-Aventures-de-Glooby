using Godot;

// Effets visuels procéduraux partagés (tweens) : réutilisés à la place de
// frames dédiées, pour économiser le budget de génération et centraliser les
// petites animations recopiées dans plusieurs nœuds.
public static class Effets
{
	// Fait disparaître un nœud : fondu de l'alpha + variation d'échelle en
	// parallèle, puis QueueFree. Par défaut libère le nœud animé lui-même ;
	// aLiberer permet d'animer un enfant (ex. le sprite) tout en libérant le
	// parent (ex. le mur entier).
	public static void Disparaitre(Node2D anime, Vector2 echelleFinale, float duree, Node aLiberer = null)
	{
		var tween = anime.CreateTween();
		tween.TweenProperty(anime, "modulate:a", 0f, duree);
		tween.Parallel().TweenProperty(anime, "scale", echelleFinale, duree);
		tween.TweenCallback(Callable.From((aLiberer ?? anime).QueueFree));
	}

	// Flash de couleur bref puis retour au blanc (retour visuel de soin, de
	// pouvoir, etc.) sans animation dédiée.
	public static void FlashCouleur(CanvasItem cible, Color couleur, float dureeAllee, float dureeRetour)
	{
		var tween = cible.CreateTween();
		tween.TweenProperty(cible, "modulate", couleur, dureeAllee);
		tween.TweenProperty(cible, "modulate", Colors.White, dureeRetour);
	}

	// Flottaison verticale en boucle (objets à ramasser) : oscillation douce
	// autour de la position de repos actuelle.
	public static void Flottaison(Node2D cible, float amplitude, float duree)
	{
		float repos = cible.Position.Y;
		var tween = cible.CreateTween().SetLoops();
		tween.TweenProperty(cible, "position:y", repos - amplitude, duree).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(cible, "position:y", repos + amplitude, duree).SetTrans(Tween.TransitionType.Sine);
	}
}
