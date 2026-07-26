using Godot;
using System.Collections.Generic;

// Catégorie d'action, pour regrouper les lignes dans le menu Paramètres.
public enum CategorieAction
{
	Deplacement,
	Actions,
	Systeme,
}

// Fiche d'une action jouable : son nom InputMap, son libellé français, sa catégorie
// (regroupement dans le menu) et ses liaisons par défaut (clavier + manette :
// boutons et axes). Immuable ; sait fabriquer ses InputEvent par défaut.
public class ActionJeu
{
	public string Nom { get; }
	public string Libelle { get; }
	public CategorieAction Categorie { get; }
	public float ZoneMorte { get; }

	private readonly Key[] _touches;
	private readonly JoyButton[] _boutons;
	private readonly (JoyAxis Axe, int Signe)[] _axes;

	public ActionJeu(string nom, string libelle, CategorieAction categorie,
		Key[] touches, JoyButton[] boutons = null,
		(JoyAxis, int)[] axes = null, float zoneMorte = 0.5f)
	{
		Nom = nom;
		Libelle = libelle;
		Categorie = categorie;
		ZoneMorte = zoneMorte;
		_touches = touches ?? System.Array.Empty<Key>();
		_boutons = boutons ?? System.Array.Empty<JoyButton>();
		_axes = axes ?? System.Array.Empty<(JoyAxis, int)>();
	}

	// Construit les InputEvent par défaut de l'action (clavier, puis boutons manette,
	// puis axes manette) : consommé par Parametres pour (re)poser les défauts.
	public IEnumerable<InputEvent> EvenementsDefaut()
	{
		foreach (var touche in _touches)
			yield return new InputEventKey { PhysicalKeycode = touche };
		foreach (var bouton in _boutons)
			yield return new InputEventJoypadButton { ButtonIndex = bouton };
		foreach (var (axe, signe) in _axes)
			yield return new InputEventJoypadMotion { Axis = axe, AxisValue = signe };
	}
}

// Catalogue central de toutes les actions du jeu : source unique de vérité pour
// leurs libellés et leurs liaisons par défaut (clavier + manette). Remplace la
// config autrefois codée en dur dans GameState.ConfigurerActionsParDefaut() et le
// tableau d'affichage de MenuPrincipal. Ajouter/retirer une action = éditer cette
// seule liste ; Parametres et l'UI en découlent automatiquement.
public static class CatalogueActions
{
	public static readonly IReadOnlyList<ActionJeu> Toutes = new List<ActionJeu>
	{
		// Déplacement
		new("move_left", "Aller à gauche", CategorieAction.Deplacement,
			new[] { Key.Left }, new[] { JoyButton.DpadLeft }, new[] { (JoyAxis.LeftX, -1) }),
		new("move_right", "Aller à droite", CategorieAction.Deplacement,
			new[] { Key.Right }, new[] { JoyButton.DpadRight }, new[] { (JoyAxis.LeftX, 1) }),
		new("jump", "Sauter", CategorieAction.Deplacement,
			new[] { Key.Space }, new[] { JoyButton.A }),
		new("slide", "Glisser (glissade ventrale)", CategorieAction.Deplacement,
			new[] { Key.Shift }, new[] { JoyButton.X }),
		new("bas", "Descendre / traverser une plateforme", CategorieAction.Deplacement,
			new[] { Key.Down }, new[] { JoyButton.DpadDown }, new[] { (JoyAxis.LeftY, 1) }),
		// Symétrique de "bas", utilisé aujourd'hui par la liste de réponses des
		// dialogues à choix (haut/bas naviguent, "action" valide).
		new("haut", "Monter / choix précédent", CategorieAction.Deplacement,
			new[] { Key.Up }, new[] { JoyButton.DpadUp }, new[] { (JoyAxis.LeftY, -1) }),

		// Actions
		new("lancer", "Lancer une boule de neige", CategorieAction.Actions,
			new[] { Key.D }, new[] { JoyButton.B }),
		new("manger", "Manger un poisson", CategorieAction.Actions,
			new[] { Key.W }, new[] { JoyButton.Y }),
		new("pouvoir_chaleur", "Pouvoir de chaleur", CategorieAction.Actions,
			new[] { Key.A }, new[] { JoyButton.LeftShoulder }),
		new("pouvoir_glace", "Pouvoir de glace", CategorieAction.Actions,
			new[] { Key.S }, new[] { JoyButton.RightShoulder }),

		// Système
		new("action", "Interagir / valider", CategorieAction.Systeme,
			new[] { Key.Enter, Key.Space }, new[] { JoyButton.A }),
		new("menu", "Menu / Pause", CategorieAction.Systeme,
			new[] { Key.Escape }, new[] { JoyButton.Start }),
	};

	// Retrouve une fiche d'action par son nom InputMap (null si inconnue).
	public static ActionJeu Trouver(string nom)
	{
		foreach (var action in Toutes)
			if (action.Nom == nom)
				return action;
		return null;
	}
}
