using Godot;

// Sol d'usine en bois : rangée de plancher réutilisable et extensible, composée
// de segments posés côte à côte pour former un sol continu de n'importe quelle
// longueur. Un embout gauche, N segments centraux (variantes A/B/C alternées,
// strictement raccordables — joint invisible sur une jointure de planche) puis
// un embout droit (planche cassée + poutre en coupe). Régler NombreSegments dans
// l'inspecteur allonge le sol ; l'aperçu se reconstruit dans l'éditeur ([Tool]).
//
// La rangée n'invente aucune géométrie : elle instancie les scènes de segment de
// scenes/sol/usine/ (SegmentSolUsineBois), seule source de vérité du sprite et de
// la collision — les mêmes scènes qu'on peut poser une par une à la main dans un
// niveau. Même logique et mêmes dimensions que SolBanquise (172px natif ×2,
// surface de marche calée sur le dessus dessiné).
[Tool]
public partial class SolUsineBois : Node2D
{
	// Largeur affichée d'un emplacement, définie par le segment lui-même.
	public const float LargeurSegment = SegmentSolUsineBois.LargeurSegment;

	private const string DossierScenes = "res://scenes/sol/usine/";
	private static readonly string[] Centres =
	{
		DossierScenes + "SolUsineBoisCentreA.tscn",
		DossierScenes + "SolUsineBoisCentreB.tscn",
		DossierScenes + "SolUsineBoisCentreC.tscn",
	};
	private const string CheminEmboutGauche = DossierScenes + "SolUsineBoisEmboutGauche.tscn";
	private const string CheminEmboutDroit = DossierScenes + "SolUsineBoisEmboutDroit.tscn";

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

	// Un segment = une instance de la scène correspondante, posée à l'emplacement
	// voulu (son origine est son bord gauche, sa surface de marche y = 8).
	private void PoserSegment(string chemin, float x)
	{
		var segment = GD.Load<PackedScene>(chemin).Instantiate<Node2D>();
		segment.Position = new Vector2(x, 0f);
		AddChild(segment);
	}
}
