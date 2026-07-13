using Godot;
using System.Collections.Generic;

// Menu principal (scène séparée, écran de lancement du jeu) : créer une
// partie, continuer (grisé tant qu'aucune sauvegarde n'existe), paramètres
// (rappel des touches) et quitter. Construit son UI via MenuFabrique pour
// rester cohérent avec le menu pause.
public partial class MenuPrincipal : Control
{
	// Actions du jeu associées à un libellé lisible, pour l'écran Paramètres.
	private static readonly (string Action, string Libelle)[] Controles =
	{
		("move_left", "Aller à gauche"),
		("move_right", "Aller à droite"),
		("jump", "Sauter"),
		("slide", "Glisser"),
		("bas", "Descendre / traverser une plateforme"),
		("lancer", "Lancer une boule de neige"),
		("manger", "Manger un poisson"),
		("pouvoir_chaleur", "Pouvoir de chaleur"),
		("menu", "Menu / Pause"),
	};

	// Dossier des images de fond piochées au hasard pour l'arrière-plan du menu.
	private const string DossierFonds = "res://assets/backgrounds";

	private Control _panneauParametres;

	public override void _Ready()
	{
		SetAnchorsPreset(Control.LayoutPreset.FullRect);

		// Le HUD (autoload) ne doit pas s'afficher par-dessus le menu.
		GetNodeOrNull<Hud>("/root/Hud")?.Masquer();

		// Image de fond aléatoire (rejouée à chaque affichage : la scène du menu
		// se recharge en y revenant), puis un voile sombre semi-transparent
		// par-dessus pour garder titre et boutons lisibles.
		AjouterFondAleatoire();
		MenuFabrique.AjouterFond(this, new Color(0.06f, 0.08f, 0.14f, 0.5f));

		var colonne = MenuFabrique.AjouterColonne(this, "Les Aventures de Glooby");
		MenuFabrique.AjouterBouton(colonne, "Créer une partie", DemarrerNouvellePartie);
		MenuFabrique.AjouterBouton(colonne, "Continuer", ContinuerPartie, actif: GameState.Instance.SauvegardeExiste);
		MenuFabrique.AjouterBouton(colonne, "Paramètres", () => AfficherParametres(true));
		MenuFabrique.AjouterBouton(colonne, "Quitter", () => GetTree().Quit());

		ConstruireParametres();
	}

	private void DemarrerNouvellePartie()
	{
		GameState.Instance.NouvellePartie();
		GetTree().ChangeSceneToFile("res://scenes/niveaux/monde.tscn");
	}

	private void ContinuerPartie()
	{
		// Restaure la progression sauvegardée avant de charger le monde : le joueur
		// se replace ensuite à son checkpoint (voir Player._Ready).
		GameState.Instance.Charger();
		GetTree().ChangeSceneToFile("res://scenes/niveaux/monde.tscn");
	}

	// Place derrière tout le reste une image de fond tirée au hasard parmi celles
	// de assets/backgrounds, étirée pour couvrir l'écran sans déformation.
	private void AjouterFondAleatoire()
	{
		var chemin = FondAleatoire();
		if (chemin == null)
			return;

		var fond = new TextureRect
		{
			Texture = GD.Load<Texture2D>(chemin),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		fond.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(fond);
	}

	// Chemin d'un PNG au hasard dans DossierFonds, ou null si le dossier est vide
	// ou introuvable. GD.Randomize garantit un tirage différent à chaque ouverture.
	private static string FondAleatoire()
	{
		using var dossier = DirAccess.Open(DossierFonds);
		if (dossier == null)
			return null;

		var fichiers = new List<string>();
		foreach (var nom in dossier.GetFiles())
			if (nom.EndsWith(".png"))
				fichiers.Add(nom);

		if (fichiers.Count == 0)
			return null;

		GD.Randomize();
		return $"{DossierFonds}/{fichiers[(int)(GD.Randi() % (uint)fichiers.Count)]}";
	}

	// Échap ferme le sous-menu ouvert (retour au menu précédent). Au menu racine,
	// il n'y a pas de menu précédent : on ne fait rien.
	public override void _UnhandledInput(InputEvent evenement)
	{
		if (!evenement.IsActionPressed("menu"))
			return;

		if (_panneauParametres.Visible)
		{
			AfficherParametres(false);
			GetViewport().SetInputAsHandled();
		}
	}

	// Écran Paramètres : superposé et masqué au départ, il liste les touches.
	private void ConstruireParametres()
	{
		_panneauParametres = new Control { Visible = false };
		_panneauParametres.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_panneauParametres);

		MenuFabrique.AjouterFond(_panneauParametres, new Color(0.06f, 0.08f, 0.14f));

		var colonne = MenuFabrique.AjouterColonne(_panneauParametres, "Touches");
		foreach (var (action, libelle) in Controles)
			MenuFabrique.AjouterLigne(colonne, $"{libelle} : {ToucheDe(action)}");
		MenuFabrique.AjouterBouton(colonne, "Retour", () => AfficherParametres(false));
	}

	private void AfficherParametres(bool visible) => _panneauParametres.Visible = visible;

	// Texte des touches physiques associées à une action (ex. "A / Left").
	private static string ToucheDe(string action)
	{
		var touches = new List<string>();
		foreach (var evenement in InputMap.ActionGetEvents(action))
			if (evenement is InputEventKey touche)
			{
				// Les actions sont liées en touche physique (position QWERTY) pour rester
				// stables quel que soit le clavier. Pour l'affichage on traduit cette
				// position vers l'étiquette réelle du clavier de l'utilisateur
				// (ex. W physique → « Z » en AZERTY) ; sinon on retombe sur la position brute.
				var etiquette = DisplayServer.KeyboardGetLabelFromPhysical(touche.PhysicalKeycode);
				if (etiquette == Key.None)
					etiquette = touche.PhysicalKeycode;
				touches.Add(OS.GetKeycodeString(etiquette));
			}
		return touches.Count > 0 ? string.Join(" / ", touches) : "-";
	}
}
