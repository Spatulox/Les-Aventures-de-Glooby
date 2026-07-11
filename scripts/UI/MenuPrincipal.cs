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

	private Control _panneauParametres;

	public override void _Ready()
	{
		SetAnchorsPreset(Control.LayoutPreset.FullRect);

		// Le HUD (autoload) ne doit pas s'afficher par-dessus le menu.
		GetNodeOrNull<Hud>("/root/Hud")?.Masquer();

		MenuFabrique.AjouterFond(this, new Color(0.06f, 0.08f, 0.14f));

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
		GetTree().ChangeSceneToFile("res://scenes/monde.tscn");
	}

	private void ContinuerPartie()
	{
		// Sauvegarde à implémenter : on reprend simplement le monde pour l'instant.
		GetTree().ChangeSceneToFile("res://scenes/monde.tscn");
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
				touches.Add(OS.GetKeycodeString(touche.PhysicalKeycode));
		return touches.Count > 0 ? string.Join(" / ", touches) : "-";
	}
}
