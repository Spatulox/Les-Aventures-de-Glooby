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
// Le chargement passe par NouvellePartieDebug : pouvoirs débloqués, réserve pleine, et
// surtout aucune écriture dans le fichier de sauvegarde (voir GameState.ModeDebug) —
// une session de test ne doit pas polluer la vraie partie.
public partial class EcranScenesDebug : Control
{
	private const string DossierNiveaux = "res://scenes/niveaux";

	// Boutons de la liste, gardés pour le filtrage (chemin -> bouton), et titres de
	// sous-dossier, masqués quand le filtre les vide.
	private readonly List<(string Chemin, Button Bouton)> _entrees = new();
	private readonly List<(Label Titre, List<Button> Boutons)> _groupes = new();

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
		GameState.Instance.NouvellePartieDebug();
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

		foreach (var fichier in acces.GetFiles())
		{
			if (!fichier.EndsWith(".tscn"))
				continue;

			if (!parDossier.TryGetValue(dossier, out var niveaux))
				parDossier[dossier] = niveaux = new List<string>();
			niveaux.Add($"{dossier}/{fichier}");
		}

		foreach (var sous in acces.GetDirectories())
			Parcourir($"{dossier}/{sous}", parDossier);
	}
}
