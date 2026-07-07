using Godot;

// Chemin du Pouvoir : défi de plateforme (stalactite-piège + glace glissante)
// menant à la salle du Pouvoir de Chaleur. Un mur de glace fondable bloque un
// raccourci visible dès l'entrée - inutilisable au premier passage, mémorisé
// pour plus tard.
public partial class CheminPouvoir : Node2D
{
	private const int TailleTuile = 32;

	private record Segment(int ColDebut, int ColFin, int Rangee, string Source, int Profondeur);

	private static readonly Segment[] Segments =
	{
		new(0, 1, 2, "grotte_plein", 1), // perch d'entrée
		// trou colonne 2 : force la chute vers le couloir principal
		new(3, 4, 2, "grotte_plein", 1), // alcôve du raccourci (derrière le mur fondable)
		new(0, 16, 8, "grotte_plein", 2), // couloir principal du défi (s'arrête avant l'escalade)
		// Escalade en escalier décalé (jamais empilée au-dessus du couloir,
		// sinon la garde au plafond tombe pile à la hauteur du joueur et il
		// se coince - repéré en testant, pas en relisant le code).
		new(17, 18, 6, "grotte_plein", 0),
		new(19, 20, 4, "grotte_plein", 0),
		new(19, 24, 2, "grotte_plein", 0), // salle de récompense
	};

	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		var tileSet = TileSetFabrique.CreerMonde();
		couche.TileSet = tileSet;
		couche.AddToGroup("sol");

		foreach (var segment in Segments)
		{
			int sourceId = (int)tileSet.GetMeta(segment.Source);
			TerrainPeintre.PeindreBandeSol(couche, sourceId, segment.ColDebut, segment.ColFin, segment.Rangee, segment.Profondeur);
		}

		AjouterDecor("res://assets/props/cristal_petit.png", new Vector2(20 * TailleTuile, 4 * TailleTuile - 20));

		var camera = GetNode<Camera2D>("Joueur/Camera2D");
		camera.LimitRight = 25 * TailleTuile;
		camera.LimitBottom = 400;
	}

	private void AjouterDecor(string chemin, Vector2 position)
	{
		var sprite = new Sprite2D
		{
			Texture = GD.Load<Texture2D>(chemin),
			Position = position,
			ZIndex = -1
		};
		AddChild(sprite);
	}
}
