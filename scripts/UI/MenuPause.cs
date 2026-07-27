using Godot;

// Menu pause en jeu : fond transparent (le jeu reste visible net derrière) avec
// un panneau semi-opaque derrière les boutons pour la lisibilité. Met la partie
// en pause et propose Continuer / Retour au menu principal. S'ouvre et se ferme
// avec la touche "menu" (Échap). Construit via MenuFabrique, comme le menu
// principal. Placé dans scenes/01-monde1.tscn (le monde ne recharge jamais de scène).
public partial class MenuPause : CanvasLayer
{
	private Control _racine;
	private EcranParametres _ecranParametres;
	private bool _ouvert;

	public override void _Ready()
	{
		// Doit continuer à tourner (entrée + affichage) alors que l'arbre est
		// en pause, et passer au-dessus du HUD.
		ProcessMode = ProcessModeEnum.Always;
		Layer = 100;

		// Le HUD (autoload) démarre masqué : on le réaffiche en entrant en jeu.
		GetNodeOrNull<Hud>("/root/Hud")?.Afficher();

		_racine = new Control { Visible = false };
		_racine.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_racine);

		// Pas de voile plein écran : le fond reste transparent, seul le panneau
		// des boutons assombrit ce qu'il recouvre.
		var colonne = MenuFabrique.AjouterColonne(_racine, "Pause", avecPanneau: true);
		MenuFabrique.AjouterBouton(colonne, "Continuer", Fermer);
		MenuFabrique.AjouterBouton(colonne, "Paramètres", () => _ecranParametres.Visible = true);
		MenuFabrique.AjouterBouton(colonne, "Retour au menu principal", RetourMenuPrincipal);

		// Écran Paramètres réutilisable superposé (masqué au départ), au-dessus du menu
		// pause ; sa touche « Retour » se contente de le masquer, le menu réapparaît.
		_ecranParametres = GD.Load<PackedScene>("res://scenes/ui/ecran_parametres.tscn")
			.Instantiate<EcranParametres>();
		AddChild(_ecranParametres);
	}

	public override void _UnhandledInput(InputEvent evenement)
	{
		if (!evenement.IsActionPressed("menu"))
			return;

		// Priorité à l'écran Paramètres ouvert : Échap le referme sans rouvrir/fermer la
		// pause (sauf pendant une capture de touche, gérée par CaptureEntree lui-même).
		if (_ecranParametres.Visible)
		{
			if (!_ecranParametres.EnCapture)
			{
				_ecranParametres.Visible = false;
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		Basculer();
		GetViewport().SetInputAsHandled();
	}

	private void Basculer()
	{
		if (_ouvert)
			Fermer();
		else
			Ouvrir();
	}

	private void Ouvrir()
	{
		_ouvert = true;
		_racine.Visible = true;
		GetTree().Paused = true;
	}

	private void Fermer()
	{
		_ouvert = false;
		_racine.Visible = false;
		GetTree().Paused = false;
	}

	private void RetourMenuPrincipal()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/ui/menu_principal.tscn");
	}
}
