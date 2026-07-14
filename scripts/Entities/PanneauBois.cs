using System;
using System.Collections.Generic;
using Godot;

// Panneau en bois affichant un message de deux façons complémentaires :
//  - Texte : légende gravée sur le bois, un Label Godot posé sur la zone
//    d'écriture vide du sprite (jamais dessinée dans l'image), modifiable par
//    instance dans l'inspecteur ; masquable jusqu'à l'approche via AfficherSeulementProche.
//  - Lignes : dialogue plus riche affiché dans une bulle externe (Talkative). Le
//    panneau ne porte que son contenu et délègue toute l'interaction à un
//    DeclencheurDialogue enfant (bulle au-dessus, défilement des répliques).
// La variante Droite réutilise le sprite de la flèche gauche en miroir.
public partial class PanneauBois : Node2D, Talkative
{
	public enum TypePanneau { Poteau, Accroche, FlecheGauche, FlecheDroite }

	[Export] public TypePanneau Type = TypePanneau.Poteau;
	[Export(PropertyHint.MultilineText)] public string Texte = "";
	[Export] public bool AfficherSeulementProche = false;

	// --- Volet « bavard » (Talkative) : répliques affichées dans la bulle externe ---

	// Répliques affichées l'une après l'autre à chaque appui sur la touche d'action.
	[Export] public string[] Lignes = Array.Empty<string>();

	// Ancrage (local) de la bulle par rapport à l'origine du nœud : au-dessus par défaut.
	[Export] public Vector2 AncrageBulle = new(0f, -40f);

	// Vrai : afficher UNE seule réplique tirée au hasard au lieu de tout faire défiler.
	[Export] public bool Aleatoire { get; set; }

	// Vrai : le dialogue démarre au simple passage du joueur (sinon : sur la touche).
	[Export] public bool AuPassage;

	// Vrai : dialogue à usage unique pour toute la partie (mémorisé via GameState).
	[Export] public bool UneSeuleFois;

	// Identifiant persistant du dialogue (requis si UneSeuleFois ; unique dans le jeu).
	[Export] public string IdDialogue = "";

	private record Config(string Texture, bool Miroir, Vector2 ZoneEcriture, Vector2 TailleZone);

	// ZoneEcriture/TailleZone : rectangle de l'aplat clair mesuré sur chaque
	// sprite (coordonnées locales, sprite affiché à l'échelle x2, centré).
	// Le sprite de flèche généré pointe vers la droite : c'est donc la
	// variante Gauche qui est en miroir.
	private static readonly Dictionary<TypePanneau, Config> Configs = new()
	{
		[TypePanneau.Poteau] = new("res://assets/props/panneau_poteau.png", false, new Vector2(-32, -36), new Vector2(64, 40)),
		[TypePanneau.Accroche] = new("res://assets/props/panneau_accroche.png", false, new Vector2(-36, -16), new Vector2(72, 44)),
		[TypePanneau.FlecheGauche] = new("res://assets/props/panneau_fleche.png", true, new Vector2(-32, -24), new Vector2(64, 36)),
		[TypePanneau.FlecheDroite] = new("res://assets/props/panneau_fleche.png", false, new Vector2(-36, -24), new Vector2(64, 36)),
	};

	private Label _etiquette;

	public bool TexteVisible => _etiquette != null && _etiquette.Visible;

	public override void _Ready()
	{
		var config = Configs[Type];

		var sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.Texture = GD.Load<Texture2D>(config.Texture);
		sprite.Scale = new Vector2(2, 2);
		sprite.FlipH = config.Miroir;

		_etiquette = GetNode<Label>("Label");
		_etiquette.Text = Texte;
		_etiquette.Position = config.ZoneEcriture;
		_etiquette.Size = config.TailleZone;

		var zone = GetNode<Area2D>("ZoneDetection");
		if (AfficherSeulementProche)
		{
			_etiquette.Visible = false;
			zone.BodyEntered += corps => { if (corps is Player) _etiquette.Visible = true; };
			zone.BodyExited += corps => { if (corps is Player) _etiquette.Visible = false; };
		}
	}

	// --- Implémentation Talkative (bulle externe pilotée par le DeclencheurDialogue enfant) ---

	public IReadOnlyList<string> Dialogue => Lignes;

	public Vector2 PointBulle => ToGlobal(AncrageBulle);

	public bool DeclencheAuPassage => AuPassage;

	public bool PeutParler()
	{
		if (UneSeuleFois && !string.IsNullOrEmpty(IdDialogue))
			return !GameState.Instance.EstConsomme(IdDialogue);
		return true;
	}

	public void SurDebutDialogue() { }

	public void SurFinDialogue()
	{
		if (UneSeuleFois && !string.IsNullOrEmpty(IdDialogue))
			GameState.Instance.MarquerConsomme(IdDialogue);
	}
}
