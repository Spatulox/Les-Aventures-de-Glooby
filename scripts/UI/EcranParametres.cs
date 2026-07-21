using Godot;
using System.Collections.Generic;

// Écran Paramètres réutilisable, partagé par le menu principal et le menu pause.
// Construit par code (comme MenuFabrique) et organisé en SECTIONS : « Touches »,
// « Affichage » et « Audio » ont du contenu ; « Accessibilité » reste un emplacement
// réservé — le menu accueillera ces réglages plus tard sans réécriture.
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
	private List<Vector2I> _resolutions = new();

	public bool EnCapture => _capture != null && _capture.EnCours;

	public override void _Ready()
	{
		// Doit fonctionner même arbre en pause (ouvert depuis le menu pause).
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsPreset(LayoutPreset.FullRect);

		MenuFabrique.AjouterFond(this, new Color(0.06f, 0.08f, 0.14f));

		var marge = new MarginContainer();
		marge.SetAnchorsPreset(LayoutPreset.FullRect);
		foreach (var cote in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
			marge.AddThemeConstantOverride(cote, 28);
		AddChild(marge);

		var colonne = new VBoxContainer();
		colonne.AddThemeConstantOverride("separation", 10);
		marge.AddChild(colonne);

		var titre = new Label { Text = "Paramètres", HorizontalAlignment = HorizontalAlignment.Center };
		titre.AddThemeFontSizeOverride("font_size", 28);
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

		Visible = false;
	}

	public override void _ExitTree()
	{
		// L'autoload Parametres survit à cet écran : se désabonner pour ne pas rappeler
		// une instance libérée (menu principal rechargé, retour au jeu…).
		if (Parametres.Instance != null)
			Parametres.Instance.LiaisonsChangees -= OnLiaisonsChangees;
	}

	// Barre d'onglets de sections. Seule « Touches » est active ; les autres sont
	// visibles mais désactivées pour annoncer l'extensibilité à venir.
	private void ConstruireOnglets(VBoxContainer colonne)
	{
		var onglets = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		onglets.AddThemeConstantOverride("separation", 8);
		colonne.AddChild(onglets);

		AjouterOnglet(onglets, "Touches", actif: true);
		AjouterOnglet(onglets, "Affichage", actif: true);
		AjouterOnglet(onglets, "Audio", actif: true);
		AjouterOnglet(onglets, "Accessibilité", actif: false);
	}

	private void AjouterOnglet(HBoxContainer onglets, string titre, bool actif)
	{
		var bouton = new Button
		{
			Text = titre,
			Disabled = !actif,
			CustomMinimumSize = new Vector2(120, 32),
		};
		if (actif)
			bouton.Pressed += () => AfficherSection(titre);
		onglets.AddChild(bouton);
	}

	// Construit toutes les sections (empilées, full rect) puis les cache.
	private void ConstruireSections(Control hote)
	{
		_sections["Touches"] = ConstruireSectionTouches();
		_sections["Affichage"] = ConstruireSectionAffichage();
		_sections["Audio"] = ConstruireSectionAudio();
		_sections["Accessibilité"] = ConstruireSectionAVenir("Options d'accessibilité à venir.");

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
	}

	// Section Touches : liste défilante des actions regroupées par catégorie.
	private Control ConstruireSectionTouches()
	{
		// Défilement vertical seulement : les lignes s'ajustent à la largeur de l'écran
		// (jamais de scroll horizontal). La liste suit la largeur du conteneur.
		var defilement = new ScrollContainer
		{
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		var liste = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		liste.AddThemeConstantOverride("separation", 4);
		defilement.AddChild(liste);

		foreach (var categorie in new[] { CategorieAction.Deplacement, CategorieAction.Actions, CategorieAction.Systeme })
		{
			AjouterEntete(liste, LibelleCategorie(categorie));
			foreach (var action in CatalogueActions.Toutes)
				if (action.Categorie == categorie)
					AjouterLigneAction(liste, action);
		}
		return defilement;
	}

	private static void AjouterEntete(VBoxContainer liste, string texte)
	{
		var entete = new Label { Text = texte };
		entete.AddThemeFontSizeOverride("font_size", 18);
		liste.AddChild(entete);
	}

	// Une ligne : libellé + bouton clavier + bouton manette + réinitialisation.
	private void AjouterLigneAction(VBoxContainer liste, ActionJeu action)
	{
		var ligne = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		ligne.AddThemeConstantOverride("separation", 8);

		// Le libellé prend l'espace restant et se replie sur plusieurs lignes si besoin
		// (autowrap) au lieu d'imposer sa largeur et de forcer un défilement horizontal.
		var libelle = new Label
		{
			Text = action.Libelle,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(0, 32),
		};
		ligne.AddChild(libelle);

		var boutonClavier = BoutonLiaison(action.Nom, clavier: true);
		var boutonManette = BoutonLiaison(action.Nom, clavier: false);
		ligne.AddChild(boutonClavier);
		ligne.AddChild(boutonManette);

		var reset = new Button { Text = "↺", CustomMinimumSize = new Vector2(36, 32) };
		reset.TooltipText = "Réinitialiser cette action";
		reset.Pressed += () => Parametres.Instance.ReinitialiserAction(action.Nom);
		ligne.AddChild(reset);

		_lignes[action.Nom] = (boutonClavier, boutonManette);
		liste.AddChild(ligne);
	}

	private Button BoutonLiaison(string action, bool clavier)
	{
		var p = Parametres.Instance;
		var bouton = new Button
		{
			Text = TexteLiaison(clavier ? p.LiaisonClavier(action) : p.LiaisonManette(action)),
			CustomMinimumSize = new Vector2(124, 32),
			ClipText = true,
		};
		bouton.Pressed += () => DemarrerCapture(action, clavier);
		return bouton;
	}

	// Section Affichage : mode (fenêtré / plein écran / plein écran fenêtré), résolution
	// (mode fenêtré uniquement) et VSync. Tous les changements sont immédiats.
	private Control ConstruireSectionAffichage()
	{
		var marge = new MarginContainer();
		foreach (var cote in new[] { "margin_left", "margin_right", "margin_top" })
			marge.AddThemeConstantOverride(cote, 8);

		var colonne = new VBoxContainer();
		colonne.AddThemeConstantOverride("separation", 14);
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

		MettreAJourEtatResolution();
		return marge;
	}

	// Ligne « libellé … contrôle », alignée comme les lignes de touches.
	private static HBoxContainer LigneReglage(string libelle, Control controle)
	{
		var ligne = new HBoxContainer();
		ligne.AddThemeConstantOverride("separation", 12);

		var label = new Label
		{
			Text = libelle,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ligne.AddChild(label);

		controle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		controle.CustomMinimumSize = new Vector2(220, 32);
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
		MettreAJourEtatResolution();
	}

	private void OnResolutionChoisie(long index)
	{
		if (index >= 0 && index < _resolutions.Count)
			Parametres.Instance.DefinirResolution(_resolutions[(int)index]);
	}

	private void OnVsyncBascule(bool actif) => Parametres.Instance.DefinirVsync(actif);

	// La résolution ne se règle qu'en mode fenêtré (en plein écran, la taille suit l'écran).
	private void MettreAJourEtatResolution() =>
		_optionResolution.Disabled = Parametres.Instance.ModeAffichageCourant != ModeAffichage.Fenetre;

	// Section Audio : un curseur de volume par bus (général, musique, ambiance). Tous les
	// changements sont immédiats et persistés par Parametres.
	private Control ConstruireSectionAudio()
	{
		var marge = new MarginContainer();
		foreach (var cote in new[] { "margin_left", "margin_right", "margin_top" })
			marge.AddThemeConstantOverride(cote, 8);

		var colonne = new VBoxContainer();
		colonne.AddThemeConstantOverride("separation", 14);
		marge.AddChild(colonne);

		colonne.AddChild(LigneVolume("Volume général", Parametres.BusMaster));
		colonne.AddChild(LigneVolume("Musique", Parametres.BusMusique));
		colonne.AddChild(LigneVolume("Ambiance", Parametres.BusAmbiance));
		return marge;
	}

	// Ligne « libellé … curseur + pourcentage » pour un bus audio. Le curseur et son
	// pourcentage voyagent ensemble dans une boîte, que LigneReglage traite comme un
	// contrôle unique (même gabarit que les listes déroulantes de la section Affichage).
	private static HBoxContainer LigneVolume(string libelle, string bus)
	{
		var boite = new HBoxContainer();
		boite.AddThemeConstantOverride("separation", 8);

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
		var pourcentage = new Label
		{
			Text = TextePourcentage(valeur),
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(48, 0),
		};
		boite.AddChild(pourcentage);

		curseur.ValueChanged += valeurNouvelle =>
		{
			Parametres.Instance.DefinirVolume(bus, (float)valeurNouvelle);
			pourcentage.Text = TextePourcentage((float)valeurNouvelle);
		};

		return LigneReglage(libelle, boite);
	}

	private static string TextePourcentage(float valeur) => $"{Mathf.RoundToInt(valeur * 100f)} %";

	private static Control ConstruireSectionAVenir(string texte)
	{
		var centre = new CenterContainer();
		centre.AddChild(new Label { Text = texte, Modulate = new Color(1f, 1f, 1f, 0.6f) });
		return centre;
	}

	private void ConstruireBasDePage(VBoxContainer colonne)
	{
		var bas = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		bas.AddThemeConstantOverride("separation", 16);
		colonne.AddChild(bas);

		var toutReset = new Button { Text = "Tout réinitialiser", CustomMinimumSize = new Vector2(200, 36) };
		toutReset.Pressed += () => _dialogueReset.PopupCentered();
		bas.AddChild(toutReset);

		var retour = new Button { Text = "Retour", CustomMinimumSize = new Vector2(200, 36) };
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
		_labelCapture = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_labelCapture.AddThemeFontSizeOverride("font_size", 22);
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
