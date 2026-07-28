using Godot;

// Campement « cadeau mécanique » de l'usine du Père Noël : même point de sauvegarde que le
// trou de pêche (toute la logique vient de Checkpoint), seul le skin change — un cadeau qui
// s'ouvre en s'activant, avec un calque de confettis en boucle par-dessus.
// L'ouverture n'est jouée qu'à une VRAIE activation en jeu : le premier affichage (restauration
// de la sauvegarde au _Ready, re-diffusion du signal par GameState.Charger) se cale directement
// sur l'état final, sinon un campement déjà acquis se rouvrirait à chaque chargement.
// La fermeture, elle, est instantanée : elle concerne les campements qu'on vient de quitter,
// souvent hors écran, une transition inverse n'y serait jamais vue.
public partial class CheckpointCadeau : Checkpoint
{
	private AnimatedSprite2D _cadeau;
	private AnimatedSprite2D _confettis;
	private bool _premierAffichageFait;

	protected override void PreparerVisuel()
	{
		_cadeau = GetNode<AnimatedSprite2D>("Cadeau");
		_confettis = GetNode<AnimatedSprite2D>("Confettis");
	}

	protected override void AfficherEtat(bool actif)
	{
		_confettis.Visible = actif;
		if (actif)
			_confettis.Play("boucle");
		else
			_confettis.Stop();

		// « ouverture » n'est pas bouclée et sa dernière frame EST l'état ouvert : Godot la
		// maintient à la fin, pas besoin d'enchaîner sur « ouvert » à la main.
		if (actif && _premierAffichageFait)
			_cadeau.Play("ouverture");
		else
			_cadeau.Play(actif ? "ouvert" : "ferme");

		_premierAffichageFait = true;
	}
}
