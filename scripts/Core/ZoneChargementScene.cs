using Godot;

// Zone de transition entre deux scènes de niveau. Quand le joueur entre dans
// l'Area2D (typiquement placée à la sortie d'un lieu, ex. la fin de la banquise),
// elle remplace complètement la scène courante par SceneSuivante
// (GetTree().ChangeSceneToPacked) — contrairement à ZoneBoss qui, lui, instancie
// son contenu DANS le monde. Le changement est optionnellement précédé d'un fondu
// au noir (DureeFondu). SceneSuivante est laissée vide par défaut : on l'assigne
// par instance dans la scène (le « xxx.tscn » à charger).
public partial class ZoneChargementScene : DeclencheurZone
{
	// Scène à charger à l'entrée du joueur (vide = zone inerte, avertissement).
	// À assigner par instance dans l'éditeur.
	[Export] public PackedScene SceneSuivante;

	// Durée du fondu au noir avant le changement (0 = bascule immédiate, sans voile).
	[Export] public float DureeFondu = 0.5f;

	// Une transition ne doit se déclencher qu'une fois, même si le joueur oscille
	// sur le bord de la zone pendant le fondu.
	public override void _Ready()
	{
		UneSeuleFois = true;
		base._Ready();
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		if (SceneSuivante == null)
		{
			GD.PushWarning($"ZoneChargementScene '{Name}' : SceneSuivante non assignée, transition ignorée.");
			return;
		}

		if (DureeFondu <= 0f)
		{
			ChangerScene();
			return;
		}

		FondreAuNoirPuisCharger();
	}

	// Voile noir plein écran (CanvasLayer au-dessus du jeu) dont l'alpha monte de 0
	// à 1, puis bascule vers SceneSuivante une fois le fondu terminé.
	private void FondreAuNoirPuisCharger()
	{
		var couche = new CanvasLayer { Layer = 128 };
		var voile = new ColorRect
		{
			Color = Colors.Black,
			Modulate = new Color(1f, 1f, 1f, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		voile.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		couche.AddChild(voile);
		GetTree().Root.AddChild(couche);

		var tween = voile.CreateTween();
		tween.TweenProperty(voile, "modulate:a", 1f, DureeFondu);
		tween.TweenCallback(Callable.From(ChangerScene));
	}

	// Bascule effective vers la scène suivante (différée pour rester hors du
	// traitement de signal/physique en cours).
	private void ChangerScene()
	{
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToPacked, SceneSuivante);
	}
}
