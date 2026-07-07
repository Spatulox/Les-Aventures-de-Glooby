using Godot;

// Monde continu façon Hollow Knight : une seule scène contient tout le
// niveau. Chaque salle est peinte à un décalage (en tuiles) dans le même
// TileMapLayer ; des zones de caméra (Area2D) ajustent les limites de la
// Camera2D du joueur en entrant dans une salle, sans jamais recharger de scène.
public partial class Monde : Node2D
{
	private const int TailleTuile = 32;

	// Décalages (colonnes, rangées) de chaque salle dans le monde partagé.
	private static readonly Vector2I DecalageDepart = new(0, 0);
	private static readonly Vector2I DecalageBanquise02 = new(86, 0);
	private static readonly Vector2I DecalageCrevasse = new(127, 6);
	private static readonly Vector2I DecalageCarrefour = new(125, 18);
	private static readonly Vector2I DecalageCheminPouvoir = new(140, 32);
	private static readonly Vector2I DecalageChemin1 = new(154, 20);
	private static readonly Vector2I DecalageBoss = new(181, 20);

	public override void _Ready()
	{
		var couche = GetNode<TileMapLayer>("Terrain");
		var tileSet = TileSetFabrique.CreerMonde();
		couche.TileSet = tileSet;
		couche.AddToGroup("sol");

		var parallaxe = GetNode<Node2D>("FondParallaxe");

		SalleDepart.Construire(couche, tileSet, this, parallaxe, DecalageDepart);
		SalleBanquise02.Construire(couche, tileSet, this, parallaxe, DecalageBanquise02);
		SalleCrevasse.Construire(couche, tileSet, this, DecalageCrevasse);
		SalleCarrefour.Construire(couche, tileSet, this, DecalageCarrefour);
		SalleCheminPouvoir.Construire(couche, tileSet, this, DecalageCheminPouvoir);
		SalleChemin1.Construire(couche, tileSet, this, DecalageChemin1);
		SalleBoss.Construire(couche, tileSet, this, DecalageBoss);

		CreerZonesCamera();

		var joueur = GetNode<Player>("Joueur");
		var camera = joueur.GetNode<Camera2D>("Camera2D");
		var zoneDepart = ZoneDe(DecalageDepart, 86, 0, 400);
		camera.LimitLeft = (int)zoneDepart.gauche;
		camera.LimitRight = (int)zoneDepart.droite;
		camera.LimitTop = (int)zoneDepart.haut;
		camera.LimitBottom = (int)zoneDepart.bas;
	}

	private void CreerZonesCamera()
	{
		var zones = new[]
		{
			ZoneDe(DecalageDepart, 86, 0, 400),
			ZoneDe(DecalageBanquise02, 41, 0, 500),
			ZoneDe(DecalageCrevasse, 8, 0, 768),
			ZoneDe(DecalageCarrefour, 30, 0, 640),
			ZoneDe(DecalageCheminPouvoir, 25, 0, 400),
			ZoneDe(DecalageChemin1, 27, 0, 400),
			ZoneDe(DecalageBoss, 90, 0, 400),
		};

		var racine = GetNode<Node2D>("ZonesCamera");
		foreach (var zone in zones)
		{
			var aire = new CameraZone
			{
				LimGauche = (int)zone.gauche,
				LimDroite = (int)zone.droite,
				LimHaut = (int)zone.haut,
				LimBas = (int)zone.bas,
			};
			var forme = new CollisionShape2D
			{
				Shape = new RectangleShape2D { Size = new Vector2(zone.droite - zone.gauche, zone.bas - zone.haut) },
			};
			aire.AddChild(forme);
			aire.Position = new Vector2((zone.gauche + zone.droite) / 2f, (zone.haut + zone.bas) / 2f);
			racine.AddChild(aire);
		}
	}

	private static (float gauche, float droite, float haut, float bas) ZoneDe(Vector2I decalage, int largeurTuiles, int hautLocal, int basLocal)
	{
		float decX = decalage.X * TailleTuile;
		float decY = decalage.Y * TailleTuile;
		return (decX, decX + largeurTuiles * TailleTuile, decY + hautLocal, decY + basLocal);
	}
}
