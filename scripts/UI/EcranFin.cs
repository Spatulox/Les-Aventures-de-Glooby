using Godot;

// Générique de fin : les crédits montent tout seuls du bas de l'écran, puis la
// partie repart. Le script ne connaît AUCUN nom — il déroule ce que contient la
// ressource Credits (assets/credits/generique.tres), qui est le seul endroit à
// éditer pour changer les rôles, l'ordre ou la vitesse.
//
// Les Label sont donc construits au runtime : la colonne est vide dans
// l'éditeur, comme les lignes de touches d'EcranParametres.
public partial class EcranFin : Control
{
	// Le contenu du générique. Sans elle l'écran reste passable à la touche,
	// pour ne jamais bloquer le joueur sur un fichier oublié.
	[Export] public CreditsGenerique Credits;

	// Scène chargée quand le générique est fini (ou passé).
	[Export(PropertyHint.File, "*.tscn")] public string CheminSuite = "res://scenes/niveaux/01-monde1.tscn";

	// Ambiance sonore du générique, par son nom (cf. GestionnaireAudio). Le thème
	// du boss revient ici : l'arène le coupe dès que le boss tombe (pour que
	// l'épilogue se joue au calme), et il fait la musique de fin. Vide = on garde
	// ce qui jouait déjà, sans coupure.
	[Export] public string NomAmbiance = "boss_cerf";

	private VBoxContainer _colonne;

	// Hauteur totale du texte, mesurée une fois construite : c'est elle qui dit
	// quand le générique est entièrement sorti par le haut.
	private float _hauteurTexte;

	// Le changement de scène n'est pas instantané : sans ce verrou, le
	// défilement et une touche pourraient le déclencher deux fois.
	private bool _termine;

	public override void _Ready()
	{
		// Le HUD est un autoload : sans ça, les cœurs et les poissons restent
		// affichés par-dessus les crédits (même geste que MenuPrincipal).
		GetNodeOrNull<Hud>("/root/Hud")?.Masquer();

		if (!string.IsNullOrEmpty(NomAmbiance))
			GestionnaireAudio.Instance?.JouerAmbiance(NomAmbiance);

		_colonne = GetNode<VBoxContainer>("Zone/Colonne");

		if (Credits == null)
			GD.PushWarning("EcranFin : aucune ressource Credits assignée, générique vide.");
		else
			RemplirColonne();

		// Le texte démarre juste sous le bas de l'écran et monte de là.
		_hauteurTexte = _colonne.GetCombinedMinimumSize().Y;
		_colonne.Position = new Vector2(_colonne.Position.X, GetViewportRect().Size.Y);
	}

	// Construit les Label du générique dans l'ordre : titre, puis chaque rôle
	// (intitulé + ses noms) séparé par un blanc, puis le mot de la fin.
	private void RemplirColonne()
	{
		if (!string.IsNullOrEmpty(Credits.Titre))
			AjouterTexte(Credits.Titre, Credits.TailleTitre);

		foreach (var entree in Credits.Entrees)
		{
			if (entree == null)
				continue;

			AjouterEspace(Credits.EspaceEntreBlocs);

			if (!string.IsNullOrEmpty(entree.Categorie))
				AjouterTexte(entree.Categorie, Credits.TailleCategorie);

			foreach (var nom in entree.Noms)
				AjouterTexte(nom, Credits.TailleNom);
		}

		if (string.IsNullOrEmpty(Credits.Remerciements))
			return;

		AjouterEspace(Credits.EspaceEntreBlocs * 2f);
		AjouterTexte(Credits.Remerciements, Credits.TailleNom).AutowrapMode = TextServer.AutowrapMode.Word;
	}

	// Une ligne centrée à la taille demandée (le Label centré vient de
	// MenuFabrique, on ne surcharge que la police).
	private Label AjouterTexte(string texte, int taille)
	{
		var label = MenuFabrique.AjouterLigne(_colonne, texte);
		label.AddThemeFontSizeOverride("font_size", taille);
		return label;
	}

	private void AjouterEspace(float hauteur)
	{
		_colonne.AddChild(new Control { CustomMinimumSize = new Vector2(0f, hauteur) });
	}

	public override void _Process(double delta)
	{
		if (_termine || Credits == null)
			return;

		_colonne.Position -= new Vector2(0f, Credits.VitesseDefilement * (float)delta);

		// Dernière ligne passée au-dessus du bord haut : le générique est lu.
		if (_colonne.Position.Y + _hauteurTexte < 0f)
			Terminer();
	}

	public override void _UnhandledInput(InputEvent evenement)
	{
		// N'importe quelle touche, et aussi les boutons de manette liés à
		// "action"/"menu" : le générique doit être passable sans clavier.
		if (evenement is InputEventKey { Pressed: true }
			|| evenement.IsActionPressed("action")
			|| evenement.IsActionPressed("menu"))
			Terminer();
	}

	private void Terminer()
	{
		if (_termine)
			return;

		_termine = true;
		GetTree().ChangeSceneToFile(CheminSuite);
	}
}
