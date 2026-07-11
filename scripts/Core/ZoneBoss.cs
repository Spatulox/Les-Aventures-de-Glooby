using Godot;

// Salle de boss : Area2D couvrant l'arène. Base RÉUTILISABLE ET HÉRITABLE de
// tout combat de boss — à l'entrée du joueur, elle révèle la barre de vie,
// arme les PV du boss et lance la musique de combat. Deux façons de s'en servir :
//   - directement dans la scène (renseigner les [Export]) ;
//   - par héritage (une sous-classe par boss) en surchargeant DemarrerCombat
//     pour un comportement propre (patterns, portes, cinématique, phases...).
public partial class ZoneBoss : DeclencheurZone
{
	// Le boss : par référence (CheminBoss) et/ou par nom lisible (NomBoss).
	[Export] public NodePath CheminBoss;
	[Export] public string NomBoss = "";

	// Barre de vie à révéler à l'entrée (masquée tant que le joueur n'est pas là).
	[Export] public NodePath CheminBarre;

	// PV du boss pour ce combat (0 = garder le PvMax par défaut du boss).
	[Export] public int PvBoss;

	// Musique de combat (optionnelle) jouée à l'entrée du joueur.
	[Export] public AudioStream Musique;

	protected BossCerf Boss;
	protected BossHudBarre Barre;
	private AudioStreamPlayer _lecteurMusique;

	protected override bool PreparerDeclencheur()
	{
		Boss = GetNodeOrNull<BossCerf>(CheminBoss);
		Barre = GetNodeOrNull<BossHudBarre>(CheminBarre);
		Barre?.Masquer();
		return true;
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		Barre?.Afficher();
		JouerMusique();
		DemarrerCombat(joueur);
	}

	// Hook d'héritage : appelé une fois le joueur entré dans l'arène. Par défaut,
	// arme les PV du boss ; une sous-classe peut l'étendre (base.DemarrerCombat).
	protected virtual void DemarrerCombat(Player joueur)
	{
		if (PvBoss > 0)
			Boss?.DefinirPvMax(PvBoss);
	}

	private void JouerMusique()
	{
		if (Musique == null)
			return;

		_lecteurMusique = new AudioStreamPlayer { Stream = Musique };
		AddChild(_lecteurMusique);
		_lecteurMusique.Play();
	}
}
