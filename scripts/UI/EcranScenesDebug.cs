using Godot;
using System.Collections.Generic;

// Écran de debug : liste les scènes JOUABLES du jeu — celles de res://scenes/niveaux —
// et en charge une d'un clic, pour tester un niveau sans traverser le reste. Superposé
// et masqué au départ, comme EcranParametres : le menu parent gère sa visibilité et sa
// fermeture (touche « menu »).
//
// La liste est DÉDUITE DU DISQUE (parcours récursif du dossier) et non écrite à la main :
// déposer un niveau suffit à l'y faire apparaître, il n'y a rien à tenir à jour. Le
// dossier fait donc office de déclaration : ce qui est jouable y vit, le reste (props,
// entités, écrans, plateformes) n'a pas à encombrer ce menu.
//
// Le chargement passe par NouvellePartieDebug : réserve pleine, les facilités COCHÉES
// dans la rangée d'options, et surtout aucune écriture dans le fichier de sauvegarde
// (voir GameState.ModeDebug) — une session de test ne doit pas polluer la vraie partie.
//
// Les cases à cocher sont déduites du CatalogueOptionsDebug, comme la liste des niveaux
// l'est du disque : ajouter une facilité de test ne demande rien ici.
public partial class EcranScenesDebug : Control
{
	private const string DossierNiveaux = "res://scenes/niveaux";

	// Boutons de la liste, gardés pour le filtrage (chemin -> bouton), et titres de
	// sous-dossier, masqués quand le filtre les vide.
	private readonly List<(string Chemin, Button Bouton)> _entrees = new();
	private readonly List<(Label Titre, List<Button> Boutons)> _groupes = new();

	// Facilités cochées, transmises au lancement du niveau. Elles vivent dans l'écran :
	// fermer puis rouvrir le panneau garde les choix, revenir au menu les remet aux
	// défauts du catalogue (l'écran est réinstancié avec le menu principal).
	private readonly HashSet<string> _optionsActives = new();

	public override void _Ready()
	{
		Visible = false;
		MenuFabrique.AjouterFond(this, new Color(0f, 0f, 0f, 0.88f));
		Construire();
	}

	private void Construire()
	{
		var colonne = new VBoxContainer();
		colonne.SetAnchorsPreset(LayoutPreset.FullRect);
		colonne.AddThemeConstantOverride("separation", 8);
		colonne.OffsetLeft = 24;
		colonne.OffsetRight = -24;
		colonne.OffsetTop = 16;
		colonne.OffsetBottom = -16;
		AddChild(colonne);

		var titre = new Label { Text = "Debug — charger un niveau", HorizontalAlignment = HorizontalAlignment.Center };
		titre.AddThemeFontSizeOverride("font_size", 24);
		colonne.AddChild(titre);

		RemplirOptions(colonne);

		var filtre = new LineEdit { PlaceholderText = "Filtrer..." };
		filtre.TextChanged += Filtrer;
		colonne.AddChild(filtre);

		var defilement = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		colonne.AddChild(defilement);

		var liste = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		defilement.AddChild(liste);

		RemplirListe(liste);

		var retour = new Button { Text = "Retour" };
		retour.Pressed += () => Visible = false;
		colonne.AddChild(retour);
	}

	// Rangée des facilités de test, au-dessus de la liste des niveaux. Un HFlowContainer
	// (et non une colonne) pour que les cases s'étalent sur une ou deux lignes sans
	// manger la place de la liste, qui reste l'objet principal de l'écran.
	private void RemplirOptions(VBoxContainer colonne)
	{
		var titre = new Label { Text = "Options de la partie de test" };
		titre.AddThemeFontSizeOverride("font_size", 14);
		titre.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
		colonne.AddChild(titre);

		var rangee = new HFlowContainer();
		rangee.AddThemeConstantOverride("h_separation", 16);
		colonne.AddChild(rangee);

		foreach (var option in CatalogueOptionsDebug.Toutes)
		{
			if (option.ParDefaut)
				_optionsActives.Add(option.Cle);

			// La clé est capturée pour la fermeture : option est réutilisée à chaque tour.
			string cle = option.Cle;
			MenuFabrique.AjouterCase(rangee, option.Libelle, option.ParDefaut, actif =>
			{
				if (actif)
					_optionsActives.Add(cle);
				else
					_optionsActives.Remove(cle);
			});
		}
	}

	private void RemplirListe(VBoxContainer liste)
	{
		foreach (var (sousDossier, niveaux) in NiveauxParDossier())
		{
			// Un titre seulement pour les sous-dossiers : à la racine de niveaux/, il
			// ferait doublon avec le titre de l'écran.
			Label titre = null;
			if (sousDossier.Length > 0)
			{
				titre = new Label { Text = sousDossier };
				titre.AddThemeFontSizeOverride("font_size", 18);
				titre.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
				liste.AddChild(titre);
			}

			var boutons = new List<Button>();
			foreach (var chemin in niveaux)
			{
				var bouton = new Button
				{
					Text = chemin.GetFile().GetBaseName(),
					Alignment = HorizontalAlignment.Left
				};
				bouton.Pressed += () => Charger(chemin);
				liste.AddChild(bouton);

				boutons.Add(bouton);
				_entrees.Add((chemin, bouton));
			}

			if (titre != null)
				_groupes.Add((titre, boutons));
		}
	}

	// Ne montre que les entrées dont le chemin contient le texte saisi ; un sous-dossier
	// dont plus aucun niveau n'est visible disparaît avec son titre.
	private void Filtrer(string texte)
	{
		string recherche = texte.ToLower();

		foreach (var (chemin, bouton) in _entrees)
			bouton.Visible = recherche.Length == 0 || chemin.ToLower().Contains(recherche);

		foreach (var (titre, boutons) in _groupes)
		{
			titre.Visible = false;
			foreach (var bouton in boutons)
				if (bouton.Visible)
				{
					titre.Visible = true;
					break;
				}
		}
	}

	private void Charger(string chemin)
	{
		GameState.Instance.NouvellePartieDebug(_optionsActives);
		GetTree().ChangeSceneToFile(chemin);
	}

	// Les niveaux, regroupés par sous-dossier (chaîne vide pour la racine) et triés.
	private static List<(string SousDossier, List<string> Niveaux)> NiveauxParDossier()
	{
		var parDossier = new SortedDictionary<string, List<string>>();
		Parcourir(DossierNiveaux, parDossier);

		var groupes = new List<(string, List<string>)>();
		foreach (var (dossier, niveaux) in parDossier)
		{
			niveaux.Sort();
			groupes.Add((dossier.Replace(DossierNiveaux, "").TrimStart('/'), niveaux));
		}
		return groupes;
	}

	private static void Parcourir(string dossier, SortedDictionary<string, List<string>> parDossier)
	{
		using var acces = DirAccess.Open(dossier);
		if (acces == null)
			return;

		foreach (string fichier in FichiersProjet.Lister(dossier, ".tscn"))
		{
			if (!parDossier.TryGetValue(dossier, out var niveaux))
				parDossier[dossier] = niveaux = new List<string>();
			niveaux.Add($"{dossier}/{fichier}");
		}

		foreach (var sous in acces.GetDirectories())
			Parcourir($"{dossier}/{sous}", parDossier);
	}
}
