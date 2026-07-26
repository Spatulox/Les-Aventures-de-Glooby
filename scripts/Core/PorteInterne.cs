using Godot;

// Porte entre deux salles d'UNE MÊME scène : à l'entrée du joueur, elle le téléporte
// sur le PointEntree d'Id IdDestination, sous un fondu au noir. C'est le pendant
// intra-scène de ZoneChargementScene (qui, lui, remplace toute la scène) : les salles
// de la grotte florale sont des boîtes fermées posées côte à côte dans le même niveau,
// on passe de l'une à l'autre sans rechargement ni perte d'état.
//
// Aucun marqueur nouveau : la destination réutilise PointEntree, déjà employé pour les
// arrivées inter-scènes. Rien à câbler côté caméra non plus — Player.MettreAJourZoneCamera
// sonde la position chaque frame (et non BodyEntered), donc la CameraZone d'arrivée
// s'applique d'elle-même : limites caméra, fond de région et ambiance sonore.
public partial class PorteInterne : DeclencheurZone
{
	// Id du PointEntree visé, présent dans la même scène (ex. "galerie").
	[Export] public string IdDestination = "";

	// Durée d'un demi-fondu au noir (0 = téléportation sèche).
	[Export] public float DureeFondu = 0.4f;

	// Verrou de progression : tant que ce boss n'est pas vaincu (GameState), la porte
	// reste inerte. Vide = porte toujours ouverte. Le nom doit être celui du
	// ZoneBoss.NomBoss correspondant (ex. "Rodolphe"), puisque c'est lui que
	// ZoneBossCerf.SurVictoire passe à GameState.MarquerBossVaincu.
	[Export] public string BossRequis = "";

	// Temps mort après un passage : empêche une porte de se redéclencher tant que le
	// joueur n'a pas quitté sa zone, et un aller-retour immédiat si la porte de retour
	// recouvre le point d'arrivée.
	[Export] public float DelaiReactivation = 0.8f;

	private float _repos;

	public override void _Process(double delta)
	{
		if (_repos > 0f)
			_repos -= (float)delta;
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		if (_repos > 0f)
			return;

		// Porte verrouillée par la progression : on ne téléporte pas, et on ne
		// consomme pas le temps mort — le joueur peut repasser dès qu'il a gagné.
		if (!string.IsNullOrEmpty(BossRequis) && GameState.Instance?.EstBossVaincu(BossRequis) != true)
			return;

		var cible = PointEntree.Trouver(GetTree(), IdDestination);
		if (cible == null)
		{
			GD.PushWarning($"PorteInterne '{Name}' : aucun PointEntree d'Id '{IdDestination}' dans la scène, passage ignoré.");
			return;
		}

		_repos = DelaiReactivation;
		// La téléportation a lieu AU NOIR (callback de mi-fondu) : ni le saut de
		// caméra ni le changement de décor ne sont visibles.
		Effets.FondreAuNoirPuis(this, DureeFondu, () => Teleporter(joueur, cible));
	}

	private void Teleporter(Player joueur, PointEntree cible)
	{
		// Le fondu court sur plusieurs frames : le joueur a pu mourir/respawner entre-temps.
		if (!IsInstanceValid(joueur) || !IsInstanceValid(cible))
			return;

		joueur.GlobalPosition = cible.GlobalPosition;
		joueur.Velocity = Vector2.Zero;
	}
}
