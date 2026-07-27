using Godot;

// Salle de boss générique (Area2D). À l'entrée du joueur : fait apparaître (spawn)
// le boss dans l'arène, révèle et lie sa barre de vie, arme ses PV et lance la
// musique. C'est AUSSI une salle caméra (IZoneCamera) : son rectangle verrouille la
// Camera2D du joueur sur l'arène (aucun défilement une fois entré) et sert de bornes
// au boss (il ne peut pas en sortir) - plus aucune taille à saisir à la main.
// Base RÉUTILISABLE ET HÉRITABLE : une sous-classe par boss (ZoneBossCerf) fournit
// le contenu spécifique via ConfigurerBoss/DemarrerCombat.
// Une même arène peut porter DEUX boss et choisir lequel apparaît selon la progression
// (MemoireRequise/SceneBossAlternative) : c'est ainsi que BossEnd sert de fin normale
// ou de fin cachée sans dupliquer la scène.
public partial class ZoneBoss : DeclencheurZone, IZoneCamera
{
	// Le boss : scène à instancier + nom lisible.
	[Export] public PackedScene SceneBoss;
	[Export] public string NomBoss = "";
	// Point d'apparition. Deux façons de le donner, dans cet ordre de priorité :
	//   MarqueurApparition — un Marker2D posé dans la scène, qu'on déplace à la souris et
	//                        qui reste visible dans l'éditeur : c'est la forme à préférer,
	//                        des coordonnées recopiées à la main se désynchronisent du
	//                        décor dès qu'on retouche l'arène ;
	//   PositionApparition — repli historique, coordonnées locales au parent de la zone.
	[Export] public NodePath MarqueurApparition;
	[Export] public Vector2 PositionApparition;

	// Boss CACHÉ : si MemoireRequise est renseignée ET déjà consommée (GameState),
	// c'est SceneBossAlternative qui apparaît à la place de SceneBoss, avec son propre
	// nom et ses propres PV. C'est ainsi qu'une même arène sert de fin normale ou de fin
	// secrète — ex. donner ses 50 poissons au lutin CGT (LutinCgt.IdDonPoissons) fait
	// spawner l'autre boss. Tout se règle par instance dans l'inspecteur.
	// Vide (ou alternative non assignée) = arène à un seul boss, comportement d'origine.
	[Export] public string MemoireRequise = "";
	[Export] public PackedScene SceneBossAlternative;
	// Vides/zéro = on garde ceux du boss normal (NomBoss / PvBoss).
	[Export] public string NomBossAlternatif = "";
	[Export] public int PvBossAlternatif;

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

	// Vrai quand la variante cachée est débloquée. Les deux conditions sont exigées
	// ensemble : une mémoire sans scène alternative (ou l'inverse) est un câblage
	// incomplet et doit rester sans effet plutôt que faire apparaître un boss nul.
	protected bool VariantePrise =>
		!string.IsNullOrEmpty(MemoireRequise)
		&& SceneBossAlternative != null
		&& GameState.Instance?.EstConsomme(MemoireRequise) == true;

	// Boss effectivement en jeu, une fois l'embranchement résolu. Tout le reste de la
	// classe (et les sous-classes) passe par ces trois-là, jamais par les exports bruts.
	protected PackedScene SceneChoisie => VariantePrise ? SceneBossAlternative : SceneBoss;

	// Public : c'est ce nom que la sous-classe passe à GameState.MarquerBossVaincu, et
	// donc celui qu'une PorteInterne doit citer dans son BossRequis.
	public string NomChoisi =>
		VariantePrise && !string.IsNullOrEmpty(NomBossAlternatif) ? NomBossAlternatif : NomBoss;

	protected int PvChoisis => VariantePrise && PvBossAlternatif > 0 ? PvBossAlternatif : PvBoss;

	protected override bool PreparerDeclencheur()
	{
		Barre = GetNodeOrNull<BossHudBarre>(CheminBarre);
		Barre?.Masquer();
		// Aussi salle caméra : le Player la trouve par sondage (comme une CameraZone)
		// et verrouille la caméra sur l'arène. On garde BodyEntered (retour true) pour
		// déclencher l'apparition du boss à l'entrée du joueur.
		AddToGroup(CameraZone.Groupe);
		// Le combat ne s'arme qu'UNE fois. BodyEntered se réémet à chaque nouvelle
		// entrée du joueur - retour après un respawn, recul qui le fait sortir puis
		// rentrer, téléportation - et chacune faisait apparaître un boss de plus.
		UneSeuleFois = true;
		return true;
	}

	// IZoneCamera : verrouille la caméra du joueur sur l'arène (limites = rectangle de
	// la zone) et affiche le fond de région. Appelé par le Player par sondage.
	public void Appliquer(Player joueur) => AppliquerCommeSalle(joueur, NomRegion, NomAmbiance, MargeChuteVide);

	protected override void SurEntreeJoueur(Player joueur)
	{
		// Boss déjà vaincu (partie chargée) : ne pas le faire réapparaître, barre masquée.
		if (GameState.Instance.EstBossVaincu(NomChoisi))
			return;

		// Ceinture et bretelles avec UneSeuleFois : un boss encore en vie dans l'arène
		// interdit d'en faire apparaître un second, même si la zone était réarmée.
		if (Boss != null && IsInstanceValid(Boss))
			return;

		Boss = FaireApparaitreBoss();
		if (Boss != null)
		{
			if (PvChoisis > 0)
				Boss.DefinirPvMax(PvChoisis);
			// Le nom vient de la zone et non de la barre : une arène à deux boss doit
			// afficher celui qui est réellement apparu.
			if (!string.IsNullOrEmpty(NomChoisi))
				Barre?.DefinirNom(NomChoisi);
			Barre?.Lier(Boss);
		}

		Barre?.Afficher();
		JouerMusique();
		DemarrerCombat(joueur);
	}

	// Fait apparaître le boss : instancie SceneBoss à son point d'apparition, en frère de
	// la zone. Les réglages spécifiques passent par ConfigurerBoss AVANT l'ajout à
	// l'arbre (règle Outils : _Ready lit ses valeurs dès l'ajout).
	//
	// L'ajout est DIFFÉRÉ : on arrive ici depuis BodyEntered, donc en plein flush des
	// requêtes physiques, où le serveur refuse qu'on ajoute un corps avec ses formes de
	// collision. Le reste de la séquence (PV, barre, musique) n'a pas besoin que le boss
	// soit déjà dans l'arbre, et ConfigurerBoss passe toujours avant son _Ready.
	protected virtual Boss FaireApparaitreBoss()
	{
		if (SceneChoisie == null)
			return null;

		var boss = SceneChoisie.Instantiate<Boss>();
		boss.Position = CalculerApparition();
		ConfigurerBoss(boss);
		// Ajout DIFFÉRÉ : on est appelé depuis BodyEntered, donc en plein flush des
		// requêtes physiques, où Godot refuse toute modification de forme de collision
		// (« Can't change this state while flushing queries »). Les _Ready du boss qui
		// activent/désactivent une CollisionShape2D ou sa ZoneDetection échouaient
		// silencieusement, laissant des formes dans le mauvais état.
		GetParent().CallDeferred(Node.MethodName.AddChild, boss);
		return boss;
	}

	// Position d'apparition, exprimée dans l'espace du PARENT de la zone — c'est là que
	// le boss est ajouté, et il n'est pas encore dans l'arbre au moment où on la pose
	// (donc pas de GlobalPosition, qui n'aurait pas encore de sens). Le marqueur, lui,
	// est posé n'importe où dans la scène : on repasse par ToLocal pour que sa position
	// reste juste même si le parent est décalé (cas des arènes translatées).
	private Vector2 CalculerApparition()
	{
		var marqueur = GetNodeOrNull<Node2D>(MarqueurApparition);
		if (marqueur == null)
			return PositionApparition;

		return GetParent() is Node2D parent
			? parent.ToLocal(marqueur.GlobalPosition)
			: marqueur.GlobalPosition;
	}

	// Réglages du boss avant son ajout à l'arbre. La base couvre le seul réglage commun
	// à tous : les bornes de déplacement, dérivées du rectangle dessiné dans l'éditeur
	// exactement comme les limites caméra. Elles passent par le contrat BossBorne et non
	// par un cast vers un boss précis — c'est ce qui permet à une arène d'héberger DEUX
	// boss de classes différentes (fin normale / fin cachée) et de les borner tous les
	// deux. Une sous-classe qui surcharge doit appeler base.ConfigurerBoss(boss).
	protected virtual void ConfigurerBoss(Boss boss)
	{
		if (boss is BossBorne borne && CalculerLimitesDepuisForme(out int g, out int d, out int _, out int _))
		{
			borne.LimiteGauche = g;
			borne.LimiteDroite = d;
		}
	}

	// Hook d'héritage : appelé une fois le boss apparu et le combat lancé
	// (ex. connecter Vaincu à la fin de partie).
	protected virtual void DemarrerCombat(Player joueur) { }

	// Thème de combat optionnel, en plus de l'ambiance de l'arène. Délégué au
	// GestionnaireAudio : lui seul tient le fondu et le lecteur unique par canal
	// (cette méthode empilait auparavant un lecteur par entrée, sans jamais les
	// libérer).
	private void JouerMusique() => GestionnaireAudio.Instance?.JouerMusiquePonctuelle(Musique);
}
