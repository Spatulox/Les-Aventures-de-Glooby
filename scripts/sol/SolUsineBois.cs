using Godot;

// Sol d'usine en bois : rangée de plancher réutilisable et extensible, composée
// de segments posés côte à côte pour former un sol continu de n'importe quelle
// longueur. Un embout gauche, N segments centraux (variantes A/B/C alternées,
// strictement raccordables — joint invisible sur une jointure de planche) puis
// un embout droit (planche cassée + poutre en coupe). Chaque segment porte son
// Sprite2D et sa collision (StaticBody2D). Régler NombreSegments dans
// l'inspecteur allonge le sol ; l'aperçu se reconstruit dans l'éditeur ([Tool]).
// Même logique et mêmes dimensions que SolBanquise (172px natif ×2, surface de
// marche calée sur le dessus dessiné).
[Tool]
public partial class SolUsineBois : Node2D
{
	// Largeur affichée d'un emplacement (172px natif ×2), identique au sol banquise.
	public const float LargeurSegment = 344f;

	private const string DossierAssets = "res://assets/sol_usine/";
	private static readonly string[] Centres =
	{
		DossierAssets + "sol_centre_a.png",
		DossierAssets + "sol_centre_b.png",
		DossierAssets + "sol_centre_c.png",
	};
	private const string CheminEmboutGauche = DossierAssets + "sol_embout_gauche.png";
	private const string CheminEmboutDroit = DossierAssets + "sol_embout_droit.png";

	private int _nombreSegments = 3;
	private bool _emboutGauche = true;
	private bool _emboutDroit = true;

	[Export(PropertyHint.Range, "1,60,1")]
	public int NombreSegments
	{
		get => _nombreSegments;
		set { _nombreSegments = Mathf.Max(1, value); Reconstruire(); }
	}

	[Export]
	public bool EmboutGauche
	{
		get => _emboutGauche;
		set { _emboutGauche = value; Reconstruire(); }
	}

	[Export]
	public bool EmboutDroit
	{
		get => _emboutDroit;
		set { _emboutDroit = value; Reconstruire(); }
	}

	public override void _Ready() => Reconstruire();

	// (Re)pose tous les segments de gauche à droite. Appelé au chargement et à
	// chaque changement d'export (dans l'éditeur comme au runtime).
	private void Reconstruire()
	{
		if (!IsInsideTree())
			return;

		foreach (var enfant in GetChildren())
			enfant.Free();

		float x = 0f;
		if (EmboutGauche)
		{
			PoserSegment(CheminEmboutGauche, x);
			x += LargeurSegment;
		}
		for (int i = 0; i < NombreSegments; i++)
		{
			PoserSegment(Centres[i % Centres.Length], x);
			x += LargeurSegment;
		}
		if (EmboutDroit)
			PoserSegment(CheminEmboutDroit, x);
	}

	// Un segment = un StaticBody2D à la position voulue, avec son sprite (ancré en
	// haut-gauche, ×2) et une collision calée sous la surface de marche.
	private void PoserSegment(string chemin, float x)
	{
		var corps = new StaticBody2D { Position = new Vector2(x, 0f) };
		corps.AddChild(new Sprite2D
		{
			Texture = GD.Load<Texture2D>(chemin),
			Scale = new Vector2(2f, 2f),
			Centered = false,
		});

		// Surface de marche = dessus dessiné (pixel 4 → local y = 8 en ×2) ;
		// la collision descend jusqu'au bas du sprite (pixel 90 → local y = 180).
		var collision = new CollisionShape2D
		{
			Position = new Vector2(LargeurSegment / 2f, 94f),
			Shape = new RectangleShape2D { Size = new Vector2(LargeurSegment, 172f) },
		};
		corps.AddChild(collision);

		AddChild(corps);
	}
}
