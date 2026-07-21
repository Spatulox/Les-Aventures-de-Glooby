using Godot;
using System.Collections.Generic;

// Menu principal (scène séparée, écran de lancement du jeu) : créer une
// partie, continuer (grisé tant qu'aucune sauvegarde n'existe), paramètres
// (rappel des touches) et quitter. Le layout (titre, colonne de boutons et
// BoiteMob) est authoré dans menu_principal.tscn et donc éditable à la souris
// dans l'éditeur ; ce script ne porte que le comportement : branchement des
// boutons, fond aléatoire, mob décoratif et panneau Paramètres (dont les
// lignes se déduisent de l'InputMap, donc restent générées via MenuFabrique).
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

	// Dossiers où chercher les mobs affichables sur l'écran-titre : le joueur et
	// chaque PNJ. Un mob est retenu s'il possède un sous-dossier d'animation « idle ».
	private const string DossierJoueur = "res://assets/player";
	private const string DossierPnj = "res://assets/pnj";
	private const string NomAnimationIdle = "idle";

	// Ambiance sonore de l'écran-titre. Le menu n'a pas de CameraZone pour la
	// demander à sa place : il s'adresse directement au gestionnaire, qui est un
	// autoload et survit donc au changement de scène vers le monde.
	private const string NomAmbiance = "menu";

	private Control _panneauParametres;

	// Boîte de la scène dans laquelle le mob décoratif est cantonné, et le sprite
	// qu'on y a monté (null si aucun mob n'était affichable).
	private Control _boiteMob;
	private AnimatedSprite2D _mob;

	public override void _Ready()
	{
		// Le village partage cette piste : en lançant une partie, la musique
		// enchaîne sans coupure (le gestionnaire ne relance pas une piste déjà
		// en cours).
		GestionnaireAudio.Instance?.JouerAmbiance(NomAmbiance);

		// Le HUD (autoload) ne doit pas s'afficher par-dessus le menu.
		GetNodeOrNull<Hud>("/root/Hud")?.Masquer();

		// Image de fond aléatoire, rejouée à chaque affichage (la scène du menu se
		// recharge en y revenant). Le voile sombre qui la rend lisible et toute la
		// mise en page viennent, eux, de la scène.
		AjouterFondAleatoire();

		GetNode<Button>("Colonne/BoutonNouvelle").Pressed += DemarrerNouvellePartie;
		GetNode<Button>("Colonne/BoutonDebug").Pressed += DemarrerPartieDebug;
		GetNode<Button>("Colonne/BoutonParametres").Pressed += () => AfficherParametres(true);
		GetNode<Button>("Colonne/BoutonQuitter").Pressed += () => GetTree().Quit();

		var boutonContinuer = GetNode<Button>("Colonne/BoutonContinuer");
		boutonContinuer.Pressed += ContinuerPartie;
		boutonContinuer.Disabled = !GameState.Instance.SauvegardeExiste;

		_boiteMob = GetNode<Control>("BoiteMob");
		// La taille d'un Control ancré n'est pas encore définitive dans _Ready : on
		// recalcule le cadrage du mob à chaque redimensionnement de la boîte (donc
		// aussi au premier layout et quand la fenêtre change de taille).
		_boiteMob.Resized += AjusterMob;

		AjouterMobAleatoire();
		AjusterMob();

		ConstruireParametres();
	}

	private void DemarrerNouvellePartie()
	{
		GameState.Instance.NouvellePartie();
		ChargerMonde();
	}

	// Partie de test : tous les pouvoirs débloqués et les mobs tués d'un coup, pour
	// parcourir le monde rapidement sans refaire la progression.
	private void DemarrerPartieDebug()
	{
		GameState.Instance.NouvellePartieDebug();
		ChargerMonde();
	}

	private void ContinuerPartie()
	{
		// Restaure la progression sauvegardée avant de charger le monde : le joueur
		// se replace ensuite à son checkpoint (voir Player._Ready).
		GameState.Instance.Charger();
		ChargerMonde();
	}

	private void ChargerMonde() => GetTree().ChangeSceneToFile("res://scenes/niveaux/monde.tscn");

	// Affiche dans la BoiteMob de la scène un mob tiré au hasard (joueur ou PNJ) en
	// animation « idle », purement décoratif (non contrôlable) : on ne réutilise pas les
	// scènes d'entités (qui embarquent physique, collisions et scripts de comportement)
	// mais uniquement leurs frames idle, montées sur un AnimatedSprite2D. Le tirage est
	// rejoué à chaque affichage du menu.
	private void AjouterMobAleatoire()
	{
		var dossier = MobAleatoire();
		if (dossier == null)
			return;

		var frames = ChargerFramesIdle(dossier);
		if (frames == null)
			return;

		// Les sprites idle regardent vers la droite : on les retourne pour qu'ils
		// fassent face au menu, situé à leur gauche. Échelle et position sont posées
		// par AjusterMob, qui a besoin de la taille réelle de la boîte.
		_mob = new AnimatedSprite2D { SpriteFrames = frames, FlipH = true };
		_boiteMob.AddChild(_mob);
		_mob.Play(NomAnimationIdle);
	}

	// Cadre le mob dans la boîte dessinée dans l'éditeur : c'est la boîte qui commande
	// sa taille, jamais l'inverse. Les frames ne font pas toutes la même taille (64 à
	// 96 px) et certaines sont plus larges que hautes (le boss cerf) : on retient
	// l'échelle uniforme la plus grande qui fasse tenir la frame entière, en largeur
	// comme en hauteur, ce qui interdit tout débordement sur la colonne de boutons
	// (clip_contents sur la boîte n'est qu'un filet de sécurité).
	private void AjusterMob()
	{
		if (_mob == null)
			return;

		var frame = _mob.SpriteFrames.GetFrameTexture(NomAnimationIdle, 0);
		var boite = _boiteMob.Size;
		float echelle = Mathf.Min(boite.X / frame.GetWidth(), boite.Y / frame.GetHeight());

		_mob.Scale = new Vector2(echelle, echelle);
		// Le sprite est centré sur sa frame : le poser au centre de la boîte le centre.
		_mob.Position = boite / 2f;
	}

	// Dossier d'un mob tiré au hasard parmi ceux qui ont une animation « idle »
	// exploitable, ou null s'il n'y en a aucun.
	private static string MobAleatoire()
	{
		var mobs = MobsDisponibles();
		if (mobs.Count == 0)
			return null;

		GD.Randomize();
		return mobs[(int)(GD.Randi() % (uint)mobs.Count)];
	}

	// Dossiers des mobs affichables : le joueur et chaque PNJ possédant un sous-dossier
	// « idle » d'au moins deux frames. La liste est déduite du disque plutôt que codée
	// en dur — ajouter un PNJ animé suffit à le faire apparaître dans le tirage. Le
	// seuil de deux frames écarte les PNJ dont l'idle n'est qu'un placeholder figé.
	private static List<string> MobsDisponibles()
	{
		var candidats = new List<string> { DossierJoueur };

		using var pnj = DirAccess.Open(DossierPnj);
		if (pnj != null)
			foreach (var nom in pnj.GetDirectories())
				candidats.Add($"{DossierPnj}/{nom}");

		// On vérifie l'existence du dossier avant d'appeler ChargerFrames : celui-ci
		// journalise une erreur Godot sur un chemin absent (plusieurs PNJ n'ont qu'un
		// placeholder à plat, sans dossier d'animation).
		var mobs = new List<string>();
		foreach (var dossier in candidats)
		{
			var idle = $"{dossier}/{NomAnimationIdle}";
			if (DirAccess.DirExistsAbsolute(idle) && AnimationsSprite.ChargerFrames(idle).Length >= 2)
				mobs.Add(dossier);
		}
		return mobs;
	}

	// Frames de l'animation « idle » d'un mob (via le helper partagé AnimationsSprite),
	// ou null si le dossier est vide. Bouclées à la même cadence (6 fps) que dans le jeu.
	private static SpriteFrames ChargerFramesIdle(string dossierMob)
	{
		var idle = AnimationsSprite.ChargerFrames($"{dossierMob}/{NomAnimationIdle}");
		if (idle.Length == 0)
			return null;

		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		AnimationsSprite.EnregistrerAnimation(frames, NomAnimationIdle, idle, 6f, true);
		return frames;
	}

	// Place derrière tout le reste une image de fond tirée au hasard parmi celles
	// de assets/backgrounds, étirée pour couvrir l'écran sans déformation. Le tirage
	// étant fait au lancement, ce nœud ne peut pas venir de la scène : il est ajouté
	// ici puis renvoyé au fond de la pile, sous les nœuds authorés dans le .tscn.
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
		MoveChild(fond, 0);
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
