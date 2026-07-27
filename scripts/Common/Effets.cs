using Godot;
using System;

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

	// Fonte : le nœud s'affaisse verticalement en gardant sa base fixe —
	// contrairement à Disparaitre, dont la mise à l'échelle part du centre et
	// décollerait donc l'objet du sol — avec fondu de l'alpha, puis libère le
	// nœud voulu. Suppose un visuel centré sur son origine.
	// demiHauteurLocale = distance origine → base du visuel, AVANT mise à l'échelle.
	public static void FondreVersLeBas(Node2D anime, float facteur, float demiHauteurLocale, float duree, Node aLiberer = null)
	{
		float echelleDepart = anime.Scale.Y;
		float echelleFin = echelleDepart * facteur;
		// En descendant l'origine d'autant que le visuel perd en demi-hauteur, la
		// base reste exactement où elle était : l'objet s'écrase au lieu de rétrécir.
		float compensation = demiHauteurLocale * (echelleDepart - echelleFin);

		var tween = anime.CreateTween();
		tween.TweenProperty(anime, "scale:y", echelleFin, duree);
		tween.Parallel().TweenProperty(anime, "position:y", anime.Position.Y + compensation, duree);
		tween.Parallel().TweenProperty(anime, "modulate:a", 0f, duree);
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

	// Fondu de l'alpha vers une cible SANS libérer le nœud : contrairement à
	// Disparaitre, l'effet est réversible (voile de météo qui apparaît puis
	// repart, élément qu'on masque temporairement).
	public static void Fondu(CanvasItem cible, float alphaCible, float duree)
	{
		var tween = cible.CreateTween();
		tween.TweenProperty(cible, "modulate:a", alphaCible, duree);
	}

	// Voile noir plein écran : fondu d'entrée (alpha 0→1), exécution de l'action au
	// noir, puis fondu de sortie (1→0) et nettoyage du voile. Sert aux transitions
	// qui coupent la vue le temps d'un changement brutal — chargement d'une autre
	// scène (ZoneChargementScene) comme téléportation d'une salle à l'autre
	// (PorteInterne).
	//
	// Le voile est rattaché à Root, PAS à l'appelant : un ChangeSceneToFile libère la
	// scène courante (et donc l'appelant) au milieu du tween. Pour la même raison le
	// tween est créé SUR le voile — sans quoi le fondu de sortie ne jouerait jamais et
	// l'écran resterait noir par-dessus la nouvelle scène.
	public static void FondreAuNoirPuis(Node source, float duree, Action action)
	{
		if (duree <= 0f)
		{
			action();
			return;
		}

		var couche = new CanvasLayer { Layer = 128 };
		var voile = new ColorRect
		{
			Color = Colors.Black,
			Modulate = new Color(1f, 1f, 1f, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		voile.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		couche.AddChild(voile);
		source.GetTree().Root.AddChild(couche);

		var tween = voile.CreateTween();
		tween.TweenProperty(voile, "modulate:a", 1f, duree);
		tween.TweenCallback(Callable.From(action));
		// Laisse la destination s'instancier/s'afficher avant de révéler.
		tween.TweenInterval(0.1);
		tween.TweenProperty(voile, "modulate:a", 0f, duree);
		tween.TweenCallback(Callable.From(couche.QueueFree));
	}

	// Balancement en boucle (pendule) : oscillation douce de la rotation autour de
	// l'angle de repos actuel. Pendant angulaire de Flottaison — sert à tout ce qui
	// pend ou dérive (jouet suspendu à son parachute, enseigne, lampion).
	public static void Balancement(Node2D cible, float angleDegres, float duree)
	{
		float repos = cible.Rotation;
		float amplitude = Mathf.DegToRad(angleDegres);
		var tween = cible.CreateTween().SetLoops();
		tween.TweenProperty(cible, "rotation", repos - amplitude, duree).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(cible, "rotation", repos + amplitude, duree).SetTrans(Tween.TransitionType.Sine);
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

	// Rotation continue en boucle, à vitesse constante (halo, cristal, roue).
	// Contrairement à Balancement, elle ne fait pas d'aller-retour : elle boucle sur un
	// tour complet, donc l'image doit être à peu près symétrique pour que la reprise
	// soit invisible.
	public static void RotationContinue(Node2D cible, float dureeTour)
	{
		float repos = cible.Rotation;
		var tween = cible.CreateTween().SetLoops();
		tween.TweenProperty(cible, "rotation", repos + Mathf.Tau, dureeTour)
			.From(repos)
			.SetTrans(Tween.TransitionType.Linear);
	}

	// Respiration en boucle : l'échelle enfle et retombe autour de l'échelle actuelle.
	// `ampleur` est une FRACTION de cette échelle (0.08 = ±8 %), pour que l'effet soit
	// identique quelle que soit la taille à laquelle l'élément a été réglé dans sa scène.
	public static void Pulsation(Node2D cible, float ampleur, float duree)
	{
		var repos = cible.Scale;
		var tween = cible.CreateTween().SetLoops();
		tween.TweenProperty(cible, "scale", repos * (1f - ampleur), duree).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(cible, "scale", repos * (1f + ampleur), duree).SetTrans(Tween.TransitionType.Sine);
	}
}
