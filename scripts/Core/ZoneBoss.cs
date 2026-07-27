using Godot;

// Salle de boss générique (Area2D). À l'entrée du joueur : fait apparaître (spawn)
// le boss dans l'arène, révèle et lie sa barre de vie, arme ses PV et lance la
// musique. C'est AUSSI une salle caméra (IZoneCamera) : son rectangle verrouille la
// Camera2D du joueur sur l'arène (aucun défilement une fois entré) et sert de bornes
// au boss (il ne peut pas en sortir) - plus aucune taille à saisir à la main.
// Base RÉUTILISABLE ET HÉRITABLE : une sous-classe par boss (ZoneBossCerf) fournit
// le contenu spécifique via ConfigurerBoss/DemarrerCombat.
public partial class ZoneBoss : DeclencheurZone, IZoneCamera
{
	// Le boss : scène à instancier + nom lisible.
	[Export] public PackedScene SceneBoss;
	[Export] public string NomBoss = "";
	[Export] public Vector2 PositionApparition;

	// Salle caméra : fond de région à afficher dans l'arène (ex. "grotte", "banquise")
	// et marge sous le sol pour le filet anti-chute - exactement comme une CameraZone.
	[Export] public string NomRegion = "";
	[Export] public float MargeChuteVide = 300f;

	// Ambiance sonore de l'arène. À renseigner ici ET sur la CameraZone qui la
	// recouvre : les deux sont dans le groupe zones_camera, et laquelle s'applique
	// dépend de l'ordre de parcours - sans quoi la musique de l'arène serait tirée
	// au hasard entre les deux. Vide = reprendre NomRegion.
	[Export] public string NomAmbiance = "";

	// Barre de vie à révéler et lier au boss spawné.
	[Export] public NodePath CheminBarre;

	// PV du boss pour ce combat (0 = garder le PvMax par défaut de la scène du boss).
	[Export] public int PvBoss;

	// Musique de combat (optionnelle) jouée à l'entrée du joueur.
	[Export] public AudioStream Musique;

	protected Boss Boss;
	protected BossHudBarre Barre;

	protected override bool PreparerDeclencheur()
	{
		Barre = GetNodeOrNull<BossHudBarre>(CheminBarre);
		Barre?.Masquer();
		// Aussi salle caméra : le Player la trouve par sondage (comme une CameraZone)
		// et verrouille la caméra sur l'arène. On garde BodyEntered (retour true) pour
		// déclencher l'apparition du boss à l'entrée du joueur.
		AddToGroup(CameraZone.Groupe);
		return true;
	}

	// IZoneCamera : verrouille la caméra du joueur sur l'arène (limites = rectangle de
	// la zone) et affiche le fond de région. Appelé par le Player par sondage.
	public void Appliquer(Player joueur) => AppliquerCommeSalle(joueur, NomRegion, NomAmbiance, MargeChuteVide);

	protected override void SurEntreeJoueur(Player joueur)
	{
		// Boss déjà vaincu (partie chargée) : ne pas le faire réapparaître, barre masquée.
		if (GameState.Instance.EstBossVaincu(NomBoss))
			return;

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
	//
	// L'ajout est DIFFÉRÉ : on arrive ici depuis BodyEntered, donc en plein flush des
	// requêtes physiques, où le serveur refuse qu'on ajoute un corps avec ses formes de
	// collision. Le reste de la séquence (PV, barre, musique) n'a pas besoin que le boss
	// soit déjà dans l'arbre, et ConfigurerBoss passe toujours avant son _Ready.
	protected virtual Boss FaireApparaitreBoss()
	{
		if (SceneBoss == null)
			return null;

		var boss = SceneBoss.Instantiate<Boss>();
		boss.Position = PositionApparition;
		ConfigurerBoss(boss);
		GetParent().CallDeferred(Node.MethodName.AddChild, boss);
		return boss;
	}

	// Hook d'héritage : réglages du boss avant son ajout à l'arbre (limites, tuning...).
	protected virtual void ConfigurerBoss(Boss boss) { }

	// Hook d'héritage : appelé une fois le boss apparu et le combat lancé
	// (ex. connecter Vaincu à la fin de partie).
	protected virtual void DemarrerCombat(Player joueur) { }

	// Thème de combat optionnel, en plus de l'ambiance de l'arène. Délégué au
	// GestionnaireAudio : lui seul tient le fondu et le lecteur unique par canal
	// (cette méthode empilait auparavant un lecteur par entrée, sans jamais les
	// libérer).
	private void JouerMusique() => GestionnaireAudio.Instance?.JouerMusiquePonctuelle(Musique);
}
