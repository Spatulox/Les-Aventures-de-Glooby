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

		// La colonne, centrée par défaut, est ramenée vers la gauche pour laisser la
		// moitié droite de l'écran au pingouin (on réduit la zone de centrage à la
		// partie gauche de l'écran, ce qui recentre le menu dedans).
		if (colonne.GetParent() is Control zoneColonne)
			zoneColonne.AnchorRight = 0.72f;

		AjouterPingouinIdle();

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

	// Affiche à droite du menu le pingouin du joueur en animation « idle », purement
	// décoratif (non contrôlable) : on ne réutilise pas la scène player.tscn (qui
	// embarque physique, caméra et script de contrôle) mais uniquement ses frames
	// idle, montées sur un AnimatedSprite2D posé dans la moitié droite de l'écran.
	private void AjouterPingouinIdle()
	{
		var frames = ChargerFramesIdle();
		if (frames == null)
			return;

		// Ancré au centre-droit de l'écran : le pingouin suit ce coin quel que soit
		// le redimensionnement de la fenêtre. L'AnimatedSprite2D (nœud 2D) est posé
		// à l'origine de cette ancre, décalé vers la gauche du bord.
		var ancre = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		ancre.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
		AddChild(ancre);

		// Le sprite idle regarde vers la droite par défaut : on le retourne pour
		// qu'il fasse face au menu, situé à sa gauche.
		var sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Position = new Vector2(-160, 0),
			Scale = new Vector2(2.5f, 2.5f),
			FlipH = true
		};
		ancre.AddChild(sprite);
		sprite.Play("idle");
	}

	// Frames de l'animation « idle » du joueur (mêmes PNG que Player, via le helper
	// partagé AnimationsSprite), ou null si le dossier est vide. Bouclées à la même
	// cadence (6 fps) que dans le jeu.
	private static SpriteFrames ChargerFramesIdle()
	{
		var idle = AnimationsSprite.ChargerFrames("res://assets/player/idle");
		if (idle.Length == 0)
			return null;

		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AnimationsSprite.EnregistrerAnimation(frames, "idle", idle, 6f, true);
		return frames;
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

		// Le fond de l'arène du Boss Cerf est réservé au combat (voir la région
		// "boss_cerf" de monde.tscn) : on l'exclut du tirage du menu pour ne pas
		// déflorer le boss sur l'écran-titre.
		var fichiers = new List<string>();
		foreach (var nom in dossier.GetFiles())
			if (nom.EndsWith(".png") && !nom.Contains("boss"))
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
