using Godot;
using System.Collections.Generic;

// Panneau en bois affichant un message : le texte est un Label Godot posé
// sur la zone d'écriture vide du sprite (jamais dessiné dans l'image), donc
// modifiable par instance dans l'inspecteur. La variante Droite réutilise
// le sprite de la flèche gauche en miroir. Si AfficherSeulementProche est
// actif, le texte n'apparaît que quand le joueur est dans la zone.
public partial class PanneauBois : Node2D
{
	public enum TypePanneau { Poteau, Accroche, FlecheGauche, FlecheDroite }

	[Export] public TypePanneau Type = TypePanneau.Poteau;
	[Export(PropertyHint.MultilineText)] public string Texte = "";
	[Export] public bool AfficherSeulementProche = false;

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
}
