using Godot;
using System.Collections.Generic;

// Écran Paramètres réutilisable, partagé par le menu principal et le menu pause.
// Construit par code (comme MenuFabrique) et organisé en SECTIONS : « Touches »,
// « Affichage », « Audio » et « Dialogue IA » (gestion d'Ollama : activation, choix du
// modèle, (re)téléchargement, et liste des modèles installés supprimables un par un).
//
// La section Touches liste chaque action (regroupée par catégorie) avec sa liaison
// clavier et sa liaison manette, chacune remappable : un clic arme la capture (overlay
// « Appuyez sur une touche… »), Échap annule, un conflit ouvre un dialogue de
// confirmation (réassigner en libérant l'autre action, ou annuler). Boutons de
// réinitialisation par action et global. Se resynchronise via le signal
// Parametres.LiaisonsChangees. Démarre caché : le menu parent gère sa visibilité et sa
// fermeture (touche « menu »).
public partial class EcranParametres : Control
{
	// Références des deux boutons de liaison d'une action, pour rafraîchir leur libellé.
	private readonly Dictionary<string, (Button Clavier, Button Manette)> _lignes = new();

	private Control _overlayCapture;
	private Label _labelCapture;
	private CaptureEntree _capture;
	private ConfirmationDialog _dialogueConflit;
	private ConfirmationDialog _dialogueReset;
	private ConfirmationDialog _dialogueSupprOllama;
	private ConfirmationDialog _dialogueReDlOllama;
	private ConfirmationDialog _dialogueSupprModele;

	// Contrôles de la section Dialogue IA (gestion d'Ollama).
	private CheckButton _checkOllama;
	private OptionButton _optionModele;
	private Label _statutOllama;
	private Button _boutonReDlOllama;
	private Button _boutonSupprOllama;
	// Liste dynamique des modèles installés sur le disque (une ligne + bouton Supprimer chacun).
	private VBoxContainer _listeModeles;
	// Tag du modèle en attente de confirmation de suppression (dialogue partagé).
	private string _tagModeleASupprimer;

	// Contexte de la capture / résolution de conflit en cours.
	private string _actionEnCapture;
	private InputEvent _evtEnAttente;
	private string _actionConflit;

	// Sections empilées (une visible à la fois), indexées par titre.
	private readonly Dictionary<string, Control> _sections = new();

	// Contrôles de la section Affichage.
	private OptionButton _optionMode;
	private OptionButton _optionResolution;
	private CheckButton _checkVsync;
	private Label _avertissementAffichage;
	private List<Vector2I> _resolutions = new();

	// Éléments dont la dimension/police/marge suit la taille de la fenêtre. On mémorise la
	// valeur de base de chacun et on la réapplique × facteur à chaque redimensionnement (voir
	// AppliquerEchelle) : aucune dimension en dur n'est dispersée dans le code de construction.
	private readonly List<(Control Ctrl, Vector2 Base)> _taillesMin = new();
	private readonly List<(Label Lbl, int Base)> _polices = new();
	private readonly List<(Control Noeud, string[] Cotes, int Base)> _marges = new();
	private readonly List<(BoxContainer Boite, int Base)> _separations = new();

	// Taille de police de base des contrôles qui n'ont PAS de libellé passé par Police() —
	// boutons, listes déroulantes, cases à cocher, dialogues de confirmation : ceux-là lisent
	// la taille du thème. Sans thème mis à l'échelle, leur texte garderait la taille par
	// défaut du moteur pendant que leurs gabarits, eux, suivent le facteur — texte débordant
	// des boutons. Un Theme (et non un override local) parce qu'il se propage à tout l'écran,
	// fenêtres de dialogue comprises.
	private const int PoliceParDefaut = 16;
	private readonly Theme _theme = new();

	public bool EnCapture => _capture != null && _capture.EnCours;

	// --- Helpers d'enregistrement pour le redimensionnement proportionnel ---
	// Chacun pose la valeur de base et l'inscrit au registre correspondant, puis renvoie le
	// contrôle pour un usage fluide (var b = Min(new Button(), 120, 32)).

	private T Min<T>(T controle, float x, float y) where T : Control
	{
		controle.CustomMinimumSize = new Vector2(x, y) * FacteurEchelle();
		_taillesMin.Add((controle, new Vector2(x, y)));
		return controle;
	}

	private Label Police(Label label, int px)
	{
		label.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(px * FacteurEchelle()));
		_polices.Add((label, px));
		return label;
	}

	private void Marge(Control noeud, int px, params string[] cotes)
	{
		int valeur = Mathf.RoundToInt(px * FacteurEchelle());
		foreach (var cote in cotes)
			noeud.AddThemeConstantOverride(cote, valeur);
		_marges.Add((noeud, cotes, px));
	}

	private T Sep<T>(T boite, int px) where T : BoxContainer
	{
		boite.AddThemeConstantOverride("separation", Mathf.RoundToInt(px * FacteurEchelle()));
		_separations.Add((boite, px));
		return boite;
	}

	// Facteur d'échelle courant, dérivé de la hauteur du VIEWPORT — l'espace de mise en page
	// réellement disponible — et non de la fenêtre système. Le projet est en stretch
	// « viewport » : l'interface est dessinée dans un canvas fixe de 640×360 que le moteur
	// agrandit ensuite pour remplir la fenêtre. Mesurer la fenêtre (1280×720 → 1.0) revenait
	// donc à dessiner à taille 720 p dans un canvas deux fois plus petit, puis à tout
	// réagrandir ×2 : texte énorme et colonnes qui débordaient à droite — et c'était pire en
	// grande fenêtre, l'ancien facteur montant jusqu'à 1.5 alors que le canvas, lui, ne
	// grandit jamais. Réf. = 720 p → 1.0, donc 0.5 dans le canvas 640×360 : les dimensions de
	// base restent écrites en pixels « écran » et retombent sur la bonne taille à l'affichage,
	// × EchelleConfort pour le confort de lecture.
	private float FacteurEchelle() =>
		Mathf.Clamp(GetViewportRect().Size.Y / 720f, 0.4f, 1.5f) * EchelleConfort;

	// Grossissement de confort appliqué par-dessus le facteur de mise à l'échelle : à taille
	// « exacte » l'écran est correct mais un peu petit. Réglage unique et volontairement
	// modeste — c'est la marge horizontale qui borne la valeur : à ×1.2 la plus large des
	// rangées (les deux boutons Ollama) occupe ~271 px des ~612 px utiles du canvas 640, donc
	// on a encore de l'air, mais au-delà de ~×2 les colonnes finiraient par déborder.
	private const float EchelleConfort = 1.2f;

	// Réapplique la taille de base × facteur à tous les éléments enregistrés. Appelée après la
	// construction (différée) et à chaque changement de taille de la fenêtre. On purge d'abord les
	// entrées libérées : la liste des modèles installés recrée ses lignes à chaque rafraîchissement.
	private void AppliquerEchelle()
	{
		float k = FacteurEchelle();
		_theme.DefaultFontSize = Mathf.RoundToInt(PoliceParDefaut * k);
		_taillesMin.RemoveAll(e => !IsInstanceValid(e.Ctrl));
		foreach (var (ctrl, b) in _taillesMin)
			ctrl.CustomMinimumSize = b * k;
		_polices.RemoveAll(e => !IsInstanceValid(e.Lbl));
		foreach (var (lbl, px) in _polices)
			lbl.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(px * k));
		_marges.RemoveAll(e => !IsInstanceValid(e.Noeud));
		foreach (var (noeud, cotes, b) in _marges)
			foreach (var cote in cotes)
				noeud.AddThemeConstantOverride(cote, Mathf.RoundToInt(b * k));
		_separations.RemoveAll(e => !IsInstanceValid(e.Boite));
		foreach (var (boite, b) in _separations)
			boite.AddThemeConstantOverride("separation", Mathf.RoundToInt(b * k));
	}

	// Enveloppe le contenu d'une section dans un défilement vertical (jamais horizontal) : si la
	// section dépasse la hauteur disponible (grande police, longue liste de modèles/touches), on la
	// fait défiler au lieu de couper le texte. Le contenu suit la largeur du conteneur.
	private static ScrollContainer EnvelopperDefilement(Control contenu)
	{
		var defilement = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		contenu.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		defilement.AddChild(contenu);
		return defilement;
	}

	public override void _Ready()
	{
		// Doit fonctionner même arbre en pause (ouvert depuis le menu pause).
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsPreset(LayoutPreset.FullRect);

		// Posé avant la construction : les contrôles créés ensuite héritent directement de la
		// bonne taille de police (AppliquerEchelle la réajuste à chaque redimensionnement).
		_theme.DefaultFontSize = Mathf.RoundToInt(PoliceParDefaut * FacteurEchelle());
		Theme = _theme;

		MenuFabrique.AjouterFond(this, new Color(0.06f, 0.08f, 0.14f));

		var marge = new MarginContainer();
		marge.SetAnchorsPreset(LayoutPreset.FullRect);
		Marge(marge, 28, "margin_left", "margin_right", "margin_top", "margin_bottom");
		AddChild(marge);

		var colonne = Sep(new VBoxContainer(), 10);
		marge.AddChild(colonne);

		var titre = Police(new Label { Text = "Paramètres", HorizontalAlignment = HorizontalAlignment.Center }, 28);
		colonne.AddChild(titre);

		ConstruireOnglets(colonne);

		// Hôte des sections : prend tout l'espace vertical restant.
		var hote = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
		colonne.AddChild(hote);
		ConstruireSections(hote);
		AfficherSection("Touches");

		ConstruireBasDePage(colonne);
		ConstruireOverlayEtDialogues();

		// Resynchronisation quand une liaison change (remap, reset).
		Parametres.Instance.LiaisonsChangees += OnLiaisonsChangees;

		// Rafraîchit l'état Ollama de la section Avancé à la fin d'un (re)provisionnement.
		if (OllamaService.Instance != null)
			OllamaService.Instance.ProvisionnementTermine += OnProvisionnementTermine;

		// Redimensionnement dynamique : l'écran suit la taille de l'espace de dessin. Celle-ci
		// n'étant pas garantie finale en _Ready, on applique une première fois en différé, puis à
		// chaque changement de taille de la fenêtre (même motif que MenuPrincipal sur Resized).
		GetTree().Root.SizeChanged += AppliquerEchelle;
		Callable.From(AppliquerEchelle).CallDeferred();

		Visible = false;
	}

	public override void _ExitTree()
	{
		// L'autoload Parametres survit à cet écran : se désabonner pour ne pas rappeler
		// une instance libérée (menu principal rechargé, retour au jeu…).
		if (Parametres.Instance != null)
			Parametres.Instance.LiaisonsChangees -= OnLiaisonsChangees;
		if (OllamaService.Instance != null)
			OllamaService.Instance.ProvisionnementTermine -= OnProvisionnementTermine;
		if (IsInstanceValid(GetTree()?.Root))
			GetTree().Root.SizeChanged -= AppliquerEchelle;
	}

	// Barre d'onglets de sections. Seule « Touches » est active ; les autres sont
	// visibles mais désactivées pour annoncer l'extensibilité à venir.
	private void ConstruireOnglets(VBoxContainer colonne)
	{
		var onglets = Sep(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 8);
		colonne.AddChild(onglets);

		AjouterOnglet(onglets, "Touches", actif: true);
		AjouterOnglet(onglets, "Affichage", actif: true);
		AjouterOnglet(onglets, "Audio", actif: true);
		AjouterOnglet(onglets, "Dialogue IA", actif: true);
	}

	private void AjouterOnglet(HBoxContainer onglets, string titre, bool actif)
	{
		var bouton = Min(new Button { Text = titre, Disabled = !actif }, 120, 32);
		if (actif)
			bouton.Pressed += () => AfficherSection(titre);
		onglets.AddChild(bouton);
	}

	// Construit toutes les sections (empilées, full rect) puis les cache. Chaque contenu est
	// enveloppé dans un défilement vertical : quand une section dépasse la hauteur disponible
	// (grande police à haute résolution, longue liste), elle défile au lieu de couper le texte.
	private void ConstruireSections(Control hote)
	{
		_sections["Touches"] = EnvelopperDefilement(ConstruireSectionTouches());
		_sections["Affichage"] = EnvelopperDefilement(ConstruireSectionAffichage());
		_sections["Audio"] = EnvelopperDefilement(ConstruireSectionAudio());
		_sections["Dialogue IA"] = EnvelopperDefilement(ConstruireSectionAvance());

		foreach (var section in _sections.Values)
		{
			section.SetAnchorsPreset(LayoutPreset.FullRect);
			section.Visible = false;
			hote.AddChild(section);
		}
	}

	private void AfficherSection(string titre)
	{
		foreach (var (nom, section) in _sections)
			section.Visible = nom == titre;

		// La liste des modèles peut avoir changé (pull, suppression) depuis la dernière visite :
		// on la rafraîchit à chaque ouverture de la section Dialogue IA.
		if (titre == "Dialogue IA")
			RafraichirListeModeles();
	}

	// Section Touches : liste des actions regroupées par catégorie. Le défilement vertical est
	// ajouté par l'enveloppe commune (EnvelopperDefilement) ; la liste suit la largeur du conteneur.
	private Control ConstruireSectionTouches()
	{
		var liste = Sep(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }, 4);

		foreach (var categorie in new[] { CategorieAction.Deplacement, CategorieAction.Actions, CategorieAction.Systeme })
		{
			AjouterEntete(liste, LibelleCategorie(categorie));
			foreach (var action in CatalogueActions.Toutes)
				if (action.Categorie == categorie)
					AjouterLigneAction(liste, action);
		}
		return liste;
	}

	private void AjouterEntete(VBoxContainer liste, string texte)
	{
		liste.AddChild(Police(new Label { Text = texte }, 18));
	}

	// Une ligne : libellé + bouton clavier + bouton manette + réinitialisation.
	private void AjouterLigneAction(VBoxContainer liste, ActionJeu action)
	{
		var ligne = Sep(new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }, 8);

		// Le libellé prend l'espace restant et se replie sur plusieurs lignes si besoin
		// (autowrap) au lieu d'imposer sa largeur et de forcer un défilement horizontal.
		var libelle = Min(Police(new Label
		{
			Text = action.Libelle,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		}, 16), 0, 32);
		ligne.AddChild(libelle);

		var boutonClavier = BoutonLiaison(action.Nom, clavier: true);
		var boutonManette = BoutonLiaison(action.Nom, clavier: false);
		ligne.AddChild(boutonClavier);
		ligne.AddChild(boutonManette);

		var reset = Min(new Button { Text = "↺" }, 36, 32);
		reset.TooltipText = "Réinitialiser cette action";
		reset.Pressed += () => Parametres.Instance.ReinitialiserAction(action.Nom);
		ligne.AddChild(reset);

		_lignes[action.Nom] = (boutonClavier, boutonManette);
		liste.AddChild(ligne);
	}

	private Button BoutonLiaison(string action, bool clavier)
	{
		var p = Parametres.Instance;
		var bouton = Min(new Button
		{
			Text = TexteLiaison(clavier ? p.LiaisonClavier(action) : p.LiaisonManette(action)),
			ClipText = true,
		}, 124, 32);
		bouton.Pressed += () => DemarrerCapture(action, clavier);
		return bouton;
	}

	// Section Affichage : mode (fenêtré / plein écran / plein écran fenêtré), résolution
	// (mode fenêtré uniquement) et VSync. Tous les changements sont immédiats.
	private Control ConstruireSectionAffichage()
	{
		var marge = new MarginContainer();
		Marge(marge, 8, "margin_left", "margin_right", "margin_top");

		var colonne = Sep(new VBoxContainer(), 14);
		marge.AddChild(colonne);

		var p = Parametres.Instance;

		_optionMode = new OptionButton();
		_optionMode.AddItem("Fenêtré", (int)ModeAffichage.Fenetre);
		_optionMode.AddItem("Plein écran", (int)ModeAffichage.PleinEcran);
		_optionMode.AddItem("Plein écran fenêtré", (int)ModeAffichage.PleinEcranFenetre);
		_optionMode.Selected = (int)p.ModeAffichageCourant;
		_optionMode.ItemSelected += OnModeChoisi;
		colonne.AddChild(LigneReglage("Mode d'affichage", _optionMode));

		_optionResolution = new OptionButton();
		RemplirResolutions();
		_optionResolution.ItemSelected += OnResolutionChoisie;
		colonne.AddChild(LigneReglage("Résolution (fenêtré)", _optionResolution));

		_checkVsync = new CheckButton { ButtonPressed = p.VsyncActif };
		_checkVsync.Toggled += OnVsyncBascule;
		colonne.AddChild(LigneReglage("Synchronisation verticale (VSync)", _checkVsync));

		// Effet différé : quand le moteur refuse de piloter la fenêtre (jeu lancé dans la
		// fenêtre embarquée de l'éditeur), les réglages sont bien mémorisés et sauvegardés,
		// simplement pas appliqués tout de suite. On le dit plutôt que de laisser croire à
		// une panne — les listes restent utilisables.
		_avertissementAffichage = Police(new Label
		{
			Text = "⚠ Sera appliqué au prochain lancement (fenêtre embarquée dans l'éditeur).",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(1f, 1f, 1f, 0.6f),
			Visible = false,
		}, 16);
		colonne.AddChild(_avertissementAffichage);

		MettreAJourEtatAffichage();
		return marge;
	}

	// Ligne « libellé … contrôle », alignée comme les lignes de touches.
	private HBoxContainer LigneReglage(string libelle, Control controle)
	{
		var ligne = Sep(new HBoxContainer(), 12);

		var label = Police(new Label
		{
			Text = libelle,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		}, 16);
		ligne.AddChild(label);

		controle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		Min(controle, 220, 32);
		ligne.AddChild(controle);
		return ligne;
	}

	// (Re)remplit la liste déroulante des résolutions et sélectionne la taille courante.
	private void RemplirResolutions()
	{
		_optionResolution.Clear();
		_resolutions = Parametres.Instance.ResolutionsDisponibles();
		var courante = Parametres.Instance.TailleFenetreCourante;
		for (int i = 0; i < _resolutions.Count; i++)
		{
			var r = _resolutions[i];
			_optionResolution.AddItem($"{r.X} × {r.Y}", i);
			if (r == courante)
				_optionResolution.Selected = i;
		}
	}

	private void OnModeChoisi(long index)
	{
		Parametres.Instance.DefinirMode((ModeAffichage)_optionMode.GetItemId((int)index));
		MettreAJourEtatAffichage();
	}

	private void OnResolutionChoisie(long index)
	{
		if (index >= 0 && index < _resolutions.Count)
			Parametres.Instance.DefinirResolution(_resolutions[(int)index]);
		MettreAJourEtatAffichage();
	}

	private void OnVsyncBascule(bool actif) => Parametres.Instance.DefinirVsync(actif);

	// Point unique de synchronisation de la section : la résolution ne se règle qu'en mode
	// fenêtré (en plein écran, la taille suit l'écran), et l'avertissement d'effet différé
	// apparaît dès que le moteur a refusé un ordre de fenêtre. Appelé à la construction et
	// après chaque changement, le refus n'étant constaté qu'à la première tentative.
	private void MettreAJourEtatAffichage()
	{
		var p = Parametres.Instance;
		_optionResolution.Disabled = p.ModeAffichageCourant != ModeAffichage.Fenetre;
		_avertissementAffichage.Visible = !p.FenetrePilotable;
	}

	// Section Audio : un curseur de volume par bus (général, musique, ambiance). Tous les
	// changements sont immédiats et persistés par Parametres.
	private Control ConstruireSectionAudio()
	{
		var marge = new MarginContainer();
		Marge(marge, 8, "margin_left", "margin_right", "margin_top");

		var colonne = Sep(new VBoxContainer(), 14);
		marge.AddChild(colonne);

		colonne.AddChild(LigneVolume("Volume général", Parametres.BusMaster));
		colonne.AddChild(LigneVolume("Musique", Parametres.BusMusique));
		colonne.AddChild(LigneVolume("Ambiance", Parametres.BusAmbiance));
		return marge;
	}

	// Ligne « libellé … curseur + pourcentage » pour un bus audio. Le curseur et son
	// pourcentage voyagent ensemble dans une boîte, que LigneReglage traite comme un
	// contrôle unique (même gabarit que les listes déroulantes de la section Affichage).
	private HBoxContainer LigneVolume(string libelle, string bus)
	{
		var boite = Sep(new HBoxContainer(), 8);

		float valeur = Parametres.Instance.VolumeCourant(bus);

		var curseur = new HSlider
		{
			MinValue = 0,
			MaxValue = 1,
			Step = 0.01,
			Value = valeur,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		boite.AddChild(curseur);

		// Largeur fixe : le pourcentage change de longueur (5 % → 100 %) et ferait
		// autrement bouger le curseur pendant qu'on le glisse.
		var pourcentage = Min(Police(new Label
		{
			Text = TextePourcentage(valeur),
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		}, 16), 48, 0);
		boite.AddChild(pourcentage);

		curseur.ValueChanged += valeurNouvelle =>
		{
			Parametres.Instance.DefinirVolume(bus, (float)valeurNouvelle);
			pourcentage.Text = TextePourcentage((float)valeurNouvelle);
		};

		return LigneReglage(libelle, boite);
	}

	private static string TextePourcentage(float valeur) => $"{Mathf.RoundToInt(valeur * 100f)} %";

	// Section Avancé : gestion d'Ollama (moteur des dialogues IA locaux). Une case pour
	// activer/désactiver l'usage d'Ollama (démarrage du serveur + appels), puis deux actions —
	// retélécharger (supprime puis réinstalle, barre de progression en bas) et supprimer
	// (efface binaire + modèle du dossier du jeu). Chaque action passe par une confirmation.
	private Control ConstruireSectionAvance()
	{
		var marge = new MarginContainer();
		Marge(marge, 8, "margin_left", "margin_right", "margin_top");

		var colonne = Sep(new VBoxContainer(), 12);
		marge.AddChild(colonne);

		AjouterEntete(colonne, "Dialogues IA (Ollama)");

		var info = Police(new Label
		{
			Text = "Ollama génère les dialogues des PNJ par IA. Il est téléchargé au premier "
				+ "lancement dans le dossier du jeu, puis réutilisé hors ligne.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(1f, 1f, 1f, 0.7f),
		}, 16);
		colonne.AddChild(info);

		_checkOllama = new CheckButton { ButtonPressed = OllamaService.Instance is { Actif: true } };
		_checkOllama.Toggled += OnOllamaActifBascule;
		colonne.AddChild(LigneReglage("Activer les dialogues IA (Ollama)", _checkOllama));

		// Sélecteur de taille de modèle : plus gros = meilleures répliques mais plus lourd à
		// télécharger. Rempli depuis le catalogue unique OllamaService.Modeles.
		_optionModele = new OptionButton();
		string modeleCourant = OllamaService.Instance?.Modele;
		for (int i = 0; i < OllamaService.Modeles.Length; i++)
		{
			_optionModele.AddItem(OllamaService.Modeles[i].Libelle, i);
			if (OllamaService.Modeles[i].Tag == modeleCourant)
				_optionModele.Selected = i;
		}
		_optionModele.ItemSelected += OnModeleChoisi;
		colonne.AddChild(LigneReglage("Modèle", _optionModele));

		_statutOllama = Police(new Label(), 16);
		colonne.AddChild(_statutOllama);

		var boutons = Sep(new HBoxContainer(), 12);
		colonne.AddChild(boutons);

		_boutonReDlOllama = Min(new Button { Text = "Retélécharger Ollama" }, 220, 36);
		_boutonReDlOllama.Pressed += () => _dialogueReDlOllama.PopupCentered();
		boutons.AddChild(_boutonReDlOllama);

		_boutonSupprOllama = Min(new Button { Text = "Supprimer Ollama" }, 220, 36);
		_boutonSupprOllama.Pressed += () => _dialogueSupprOllama.PopupCentered();
		boutons.AddChild(_boutonSupprOllama);

		// Modèles réellement téléchargés sur le disque : une ligne par modèle avec son propre
		// bouton Supprimer (libère l'espace disque sans toucher au binaire Ollama). Rempli à la
		// volée par RafraichirListeModeles (interrogation du serveur), rafraîchi à l'affichage
		// de la section et après un (re)provisionnement.
		AjouterEntete(colonne, "Modèles installés");
		_listeModeles = Sep(new VBoxContainer(), 4);
		colonne.AddChild(_listeModeles);

		MettreAJourStatutOllama();
		RafraichirListeModeles();
		return marge;
	}

	// Interroge Ollama pour la liste des modèles présents sur le disque et reconstruit l'affichage
	// (une ligne « nom … Supprimer » par modèle). Serveur éteint/indisponible ⇒ message d'attente.
	private void RafraichirListeModeles()
	{
		if (_listeModeles == null)
			return;

		foreach (var enfant in _listeModeles.GetChildren())
			enfant.QueueFree();

		var svc = OllamaService.Instance;
		if (svc is not { Actif: true, Disponible: true })
		{
			_listeModeles.AddChild(Police(new Label
			{
				Text = "Modèles indisponibles (Ollama désactivé ou pas encore prêt).",
				Modulate = new Color(1f, 1f, 1f, 0.6f),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
			}, 16));
			return;
		}

		var attente = Police(new Label { Text = "Chargement de la liste…", Modulate = new Color(1f, 1f, 1f, 0.6f) }, 16);
		_listeModeles.AddChild(attente);

		svc.ListerModelesInstalles(tags =>
		{
			// La section a pu être libérée entre-temps (écran fermé/rechargé).
			if (_listeModeles == null || !IsInstanceValid(_listeModeles))
				return;
			foreach (var enfant in _listeModeles.GetChildren())
				enfant.QueueFree();

			if (tags.Length == 0)
			{
				_listeModeles.AddChild(Police(new Label
				{
					Text = "Aucun modèle installé.",
					Modulate = new Color(1f, 1f, 1f, 0.6f),
				}, 16));
				return;
			}

			foreach (var tag in tags)
				_listeModeles.AddChild(LigneModele(tag));
		});
	}

	// Une ligne de la liste des modèles installés : le tag du modèle + un bouton Supprimer qui
	// ouvre la confirmation partagée (_dialogueSupprModele) en mémorisant le tag ciblé.
	private HBoxContainer LigneModele(string tag)
	{
		var ligne = Sep(new HBoxContainer(), 12);

		ligne.AddChild(Police(new Label
		{
			Text = tag,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		}, 16));

		// Palier de taille (Minuscule / Petit / Moyen / Lourd) déduit du catalogue, en repère discret.
		var palier = PalierModele(tag);
		if (palier != null)
			ligne.AddChild(Police(new Label
			{
				Text = palier,
				SizeFlagsVertical = SizeFlags.ShrinkCenter,
				Modulate = new Color(1f, 1f, 1f, 0.6f),
			}, 16));

		var suppr = Min(new Button { Text = "Supprimer" }, 120, 32);
		suppr.Pressed += () =>
		{
			_tagModeleASupprimer = tag;
			_dialogueSupprModele.DialogText = $"Supprimer le modèle « {tag} » du disque ?";
			_dialogueSupprModele.PopupCentered();
		};
		ligne.AddChild(suppr);
		return ligne;
	}

	// Palier de taille lisible d'un modèle installé (« Minuscule », « Petit », « Moyen »,
	// « Lourd »), déduit du catalogue unique OllamaService.Modeles via son tag — le premier mot du
	// libellé (« Minuscule (2.0 Go) » → « Minuscule »). Null si le tag n'est pas au catalogue.
	private static string PalierModele(string tag)
	{
		foreach (var modele in OllamaService.Modeles)
			if (modele.Tag == tag)
				return modele.Libelle.Split(' ')[0];
		return null;
	}

	// Reflète l'état courant d'Ollama : désactivé (grisé), disponible (vert) ou indisponible
	// (orange). Les boutons retélécharger/supprimer n'ont de sens que si l'usage est activé.
	private void MettreAJourStatutOllama()
	{
		if (_statutOllama == null)
			return;

		var svc = OllamaService.Instance;
		bool actif = svc is { Actif: true };
		bool dispo = svc is { Disponible: true };

		if (!actif)
		{
			_statutOllama.Text = "État : désactivé.";
			_statutOllama.Modulate = new Color(1f, 1f, 1f, 0.6f);
		}
		else
		{
			_statutOllama.Text = dispo
				? "État : disponible ✓"
				: "État : indisponible (téléchargement/serveur en cours, ou échec).";
			_statutOllama.Modulate = dispo ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.5f);
		}

		if (_optionModele != null)
			_optionModele.Disabled = !actif;
		if (_boutonReDlOllama != null)
			_boutonReDlOllama.Disabled = !actif;
		if (_boutonSupprOllama != null)
			_boutonSupprOllama.Disabled = !actif;
	}

	private void OnOllamaActifBascule(bool actif)
	{
		OllamaService.Instance?.DefinirActif(actif);
		MettreAJourStatutOllama();
	}

	private void OnModeleChoisi(long index)
	{
		if (index >= 0 && index < OllamaService.Modeles.Length)
			OllamaService.Instance?.DefinirModele(OllamaService.Modeles[(int)index].Tag);
		MettreAJourStatutOllama();
	}

	private void OnSupprimerOllama()
	{
		OllamaService.Instance?.SupprimerOllama();
		MettreAJourStatutOllama();
	}

	private void OnRetelechargerOllama()
	{
		OllamaService.Instance?.Reprovisionner();
		MettreAJourStatutOllama();
	}

	// Rafraîchit l'état ET la liste des modèles à la fin d'un (re)provisionnement (un modèle a pu
	// être téléchargé, l'installation nettoyée…), qu'il soit lancé d'ici ou au démarrage du jeu.
	private void OnProvisionnementTermine(bool succes)
	{
		MettreAJourStatutOllama();
		RafraichirListeModeles();
	}

	private void ConstruireBasDePage(VBoxContainer colonne)
	{
		var bas = Sep(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }, 16);
		colonne.AddChild(bas);

		var toutReset = Min(new Button { Text = "Tout réinitialiser" }, 200, 36);
		toutReset.Pressed += () => _dialogueReset.PopupCentered();
		bas.AddChild(toutReset);

		var retour = Min(new Button { Text = "Retour" }, 200, 36);
		retour.Pressed += () => Visible = false;
		bas.AddChild(retour);
	}

	// Overlay d'attente de capture + dialogues de conflit et de reset global.
	private void ConstruireOverlayEtDialogues()
	{
		_overlayCapture = new Control { Visible = false };
		_overlayCapture.SetAnchorsPreset(LayoutPreset.FullRect);
		MenuFabrique.AjouterFond(_overlayCapture, new Color(0f, 0f, 0f, 0.75f));
		var centre = new CenterContainer();
		centre.SetAnchorsPreset(LayoutPreset.FullRect);
		_overlayCapture.AddChild(centre);
		_labelCapture = Police(new Label { HorizontalAlignment = HorizontalAlignment.Center }, 22);
		centre.AddChild(_labelCapture);
		AddChild(_overlayCapture);

		_capture = new CaptureEntree();
		_capture.Capturee += OnCapturee;
		_capture.Annulee += OnCaptureAnnulee;
		AddChild(_capture);

		_dialogueConflit = new ConfirmationDialog { Title = "Touche déjà utilisée" };
		_dialogueConflit.GetOkButton().Text = "Réassigner";
		_dialogueConflit.CancelButtonText = "Annuler";
		_dialogueConflit.Confirmed += OnConflitConfirme;
		AddChild(_dialogueConflit);

		_dialogueReset = new ConfirmationDialog { Title = "Réinitialiser" };
		_dialogueReset.DialogText = "Réinitialiser toutes les touches à leurs valeurs par défaut ?";
		_dialogueReset.GetOkButton().Text = "Réinitialiser";
		_dialogueReset.CancelButtonText = "Annuler";
		_dialogueReset.Confirmed += () => Parametres.Instance.ReinitialiserTout();
		AddChild(_dialogueReset);

		_dialogueSupprOllama = new ConfirmationDialog { Title = "Supprimer Ollama" };
		_dialogueSupprOllama.DialogText =
			"Supprimer Ollama et le modèle téléchargés ? Les dialogues IA seront désactivés "
			+ "jusqu'au prochain téléchargement.";
		_dialogueSupprOllama.GetOkButton().Text = "Supprimer";
		_dialogueSupprOllama.CancelButtonText = "Annuler";
		_dialogueSupprOllama.Confirmed += OnSupprimerOllama;
		AddChild(_dialogueSupprOllama);

		_dialogueReDlOllama = new ConfirmationDialog { Title = "Retélécharger Ollama" };
		_dialogueReDlOllama.DialogText =
			"Supprimer puis retélécharger Ollama et le modèle ? Une barre de progression "
			+ "apparaîtra en bas de l'écran.";
		_dialogueReDlOllama.GetOkButton().Text = "Retélécharger";
		_dialogueReDlOllama.CancelButtonText = "Annuler";
		_dialogueReDlOllama.Confirmed += OnRetelechargerOllama;
		AddChild(_dialogueReDlOllama);

		_dialogueSupprModele = new ConfirmationDialog { Title = "Supprimer un modèle" };
		_dialogueSupprModele.GetOkButton().Text = "Supprimer";
		_dialogueSupprModele.CancelButtonText = "Annuler";
		_dialogueSupprModele.Confirmed += OnSupprimerModele;
		AddChild(_dialogueSupprModele);
	}

	// Suppression confirmée d'un modèle précis : on l'efface via Ollama puis on rafraîchit la liste
	// (et le statut, car supprimer le modèle courant rend Ollama indisponible tant qu'aucun n'est prêt).
	private void OnSupprimerModele()
	{
		if (string.IsNullOrEmpty(_tagModeleASupprimer))
			return;
		OllamaService.Instance?.SupprimerModele(_tagModeleASupprimer, _ =>
		{
			RafraichirListeModeles();
			MettreAJourStatutOllama();
		});
	}

	private void DemarrerCapture(string action, bool clavier)
	{
		_actionEnCapture = action;
		_labelCapture.Text = clavier
			? "Appuyez sur une touche…\n(Échap pour annuler)"
			: "Appuyez sur un bouton ou inclinez un stick…\n(Échap pour annuler)";
		_overlayCapture.Visible = true;
		_capture.Demarrer(clavier);
	}

	private void OnCaptureAnnulee() => _overlayCapture.Visible = false;

	private void OnCapturee(InputEvent evenement)
	{
		_overlayCapture.Visible = false;
		var action = _actionEnCapture;

		var conflit = Parametres.Instance.TrouverConflit(evenement, action);
		if (conflit != null)
		{
			// On diffère l'application : l'utilisateur choisit de réassigner ou d'annuler.
			_evtEnAttente = evenement;
			_actionConflit = conflit;
			_dialogueConflit.DialogText =
				$"« {EvenementEntree.Libelle(evenement)} » est déjà utilisée par « {CatalogueActions.Trouver(conflit)?.Libelle} ».\n" +
				$"La réassigner à « {CatalogueActions.Trouver(action)?.Libelle} » ?";
			_dialogueConflit.PopupCentered();
			return;
		}

		Parametres.Instance.Remapper(action, evenement);
	}

	private void OnConflitConfirme()
	{
		// Libère la touche de l'action qui la détenait, puis l'assigne à la nouvelle.
		Parametres.Instance.RetirerCorrespondance(_actionConflit, _evtEnAttente);
		Parametres.Instance.Remapper(_actionEnCapture, _evtEnAttente);
	}

	private void OnLiaisonsChangees(string action)
	{
		if (string.IsNullOrEmpty(action))
		{
			foreach (var nom in _lignes.Keys)
				RafraichirLigne(nom);
			return;
		}
		RafraichirLigne(action);
	}

	private void RafraichirLigne(string action)
	{
		if (!_lignes.TryGetValue(action, out var ligne))
			return;
		var p = Parametres.Instance;
		ligne.Clavier.Text = TexteLiaison(p.LiaisonClavier(action));
		ligne.Manette.Text = TexteLiaison(p.LiaisonManette(action));
	}

	private static string TexteLiaison(InputEvent evenement) =>
		evenement == null ? "—" : EvenementEntree.Libelle(evenement);

	private static string LibelleCategorie(CategorieAction categorie) => categorie switch
	{
		CategorieAction.Deplacement => "Déplacement",
		CategorieAction.Actions => "Actions",
		CategorieAction.Systeme => "Système",
		_ => "Autres",
	};
}
