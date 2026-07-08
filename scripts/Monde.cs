using Godot;

// Générateur d'origine du monde continu (façon Hollow Knight) : peint chaque
// salle à un décalage (en tuiles) dans un même TileMapLayer et crée les
// zones de caméra. N'EST PLUS BRANCHÉ à scenes/monde.tscn - cette scène est
// désormais éditable à la main dans Godot (le résultat de ce générateur y a
// été capturé une fois, tuiles et objets compris). Ce script reste comme
// outil de référence si on veut regénérer une salle par code puis la
// recapturer, mais il ne tourne plus au lancement du jeu.
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
	private static readonly Vector2I DecalagePrototypeGlace = new(141, 16);

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
		SallePrototypeGlace.Construire(couche, tileSet, this, DecalagePrototypeGlace);

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
			ZoneDe(DecalagePrototypeGlace, SallePrototypeGlace.Largeur, 0, SallePrototypeGlace.Hauteur * TailleTuile),
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
			// L'Owner ne peut être fixé qu'une fois le nœud effectivement dans
			// l'arbre (l'Owner doit être un ancêtre réel au moment de l'appel).
			aire.Owner = this;
			forme.Owner = this;
		}
	}

	private static (float gauche, float droite, float haut, float bas) ZoneDe(Vector2I decalage, int largeurTuiles, int hautLocal, int basLocal)
	{
		float decX = decalage.X * TailleTuile;
		float decY = decalage.Y * TailleTuile;
		return (decX, decX + largeurTuiles * TailleTuile, decY + hautLocal, decY + basLocal);
	}
}
