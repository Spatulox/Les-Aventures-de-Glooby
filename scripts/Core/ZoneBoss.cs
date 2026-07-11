using Godot;

// Salle de boss générique (Area2D). À l'entrée du joueur : fait apparaître (spawn)
// le boss dans l'arène, révèle et lie sa barre de vie, arme ses PV et lance la
// musique. Base RÉUTILISABLE ET HÉRITABLE : une sous-classe par boss (ZoneBossCerf)
// fournit le contenu spécifique via ConfigurerBoss/DemarrerCombat.
public partial class ZoneBoss : DeclencheurZone
{
	// Le boss : scène à instancier + nom lisible.
	[Export] public PackedScene SceneBoss;
	[Export] public string NomBoss = "";
	[Export] public Vector2 PositionApparition;

	// Barre de vie à révéler et lier au boss spawné.
	[Export] public NodePath CheminBarre;

	// PV du boss pour ce combat (0 = garder le PvMax par défaut de la scène du boss).
	[Export] public int PvBoss;

	// Musique de combat (optionnelle) jouée à l'entrée du joueur.
	[Export] public AudioStream Musique;

	protected Boss Boss;
	protected BossHudBarre Barre;
	private AudioStreamPlayer _lecteurMusique;

	protected override bool PreparerDeclencheur()
	{
		Barre = GetNodeOrNull<BossHudBarre>(CheminBarre);
		Barre?.Masquer();
		return true;
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		Boss = FaireApparaitreBoss();
		if (Boss != null)
		{
			if (PvBoss > 0)
				Boss.DefinirPvMax(PvBoss);
			Barre?.Lier(Boss);
		}

		Barre?.Afficher();
		JouerMusique();
		DemarrerCombat(joueur);
	}

	// Fait apparaître le boss : instancie SceneBoss à PositionApparition, en frère de
	// la zone. Les réglages spécifiques passent par ConfigurerBoss AVANT l'ajout à
	// l'arbre (règle Outils : _Ready lit ses valeurs dès l'ajout).
	protected virtual Boss FaireApparaitreBoss()
	{
		if (SceneBoss == null)
			return null;

		var boss = SceneBoss.Instantiate<Boss>();
		boss.Position = PositionApparition;
		ConfigurerBoss(boss);
		GetParent().AddChild(boss);
		return boss;
	}

	// Hook d'héritage : réglages du boss avant son ajout à l'arbre (limites, tuning...).
	protected virtual void ConfigurerBoss(Boss boss) { }

	// Hook d'héritage : appelé une fois le boss apparu et le combat lancé
	// (ex. connecter Vaincu à la fin de partie).
	protected virtual void DemarrerCombat(Player joueur) { }

	private void JouerMusique()
	{
		if (Musique == null)
			return;

		_lecteurMusique = new AudioStreamPlayer { Stream = Musique };
		AddChild(_lecteurMusique);
		_lecteurMusique.Play();
	}
}
