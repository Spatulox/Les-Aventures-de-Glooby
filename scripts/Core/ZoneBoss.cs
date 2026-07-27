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
// Elle sait aussi faire PARLER avant de faire cogner (ScenePnjPrologue) : un PNJ amical
// accueille le joueur, et sa conversation finie un fondu au noir l'échange contre le
// boss - deux entités bien distinctes, jamais visibles ensemble. ScenePnjEpilogue fait
// l'échange inverse une fois le boss tombé.
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

	// PROLOGUE : on parle avant de se battre. Un PNJ amical (PnjAmical + son
	// DeclencheurDialogue) accueille le joueur dans l'arène ; la conversation finie, un
	// fondu au noir l'échange contre le boss - le joueur ne voit jamais les deux à la
	// fois. Vide = arène sans prologue, le combat démarre à l'entrée comme avant.
	// L'interlocuteur suit le MÊME aiguillage que le boss (voir MemoireRequise) : une
	// arène à deux fins a un interlocuteur par fin.
	[Export] public PackedScene ScenePnjPrologue;
	[Export] public PackedScene ScenePnjPrologueAlternatif;

	// ÉPILOGUE : le pendant, après la chute du boss. Un dernier fondu au noir remplace le
	// vaincu par un PNJ amical qui relance le joueur (« vite, allons délivrer... ») - le
	// joueur ne voit jamais les deux, et l'arène ne garde pas une carcasse au sol. Même
	// aiguillage que partout : chaque fin a le sien, vide = le combat se termine sec.
	[Export] public PackedScene ScenePnjEpilogue;
	[Export] public PackedScene ScenePnjEpilogueAlternatif;

	// Battement entre la chute du boss et le fondu d'épilogue : le temps de le voir
	// s'affaisser (son animation de mort) avant de couper au noir.
	[Export] public float DelaiEpilogue = 2f;

	// Durée d'un demi-fondu au noir des échanges PNJ <-> boss (0 = bascule sèche).
	// Partagée par le prologue et l'épilogue : c'est le même effet, dans les deux sens.
	[Export] public float DureeFonduEchange = 0.5f;

	protected Boss Boss;
	protected BossHudBarre Barre;

	// Interlocuteur du prologue tant qu'il est en scène (null dès l'échange fait).
	protected Node2D PnjPrologue;
	private Player _joueurPrologue;
	private bool _epilogueLance;

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

	// Jumelle de SceneChoisie pour le prologue : l'interlocuteur de la fin cachée n'est
	// pris que s'il est réellement assigné, sinon on garde celui de la fin normale (une
	// arène peut n'avoir qu'un seul PNJ pour ses deux boss).
	protected PackedScene ScenePrologueChoisie =>
		VariantePrise && ScenePnjPrologueAlternatif != null ? ScenePnjPrologueAlternatif : ScenePnjPrologue;

	// Idem pour l'épilogue. Laisser ScenePnjEpilogue vide et ne renseigner que
	// l'alternative donne un épilogue à la seule fin cachée : la branche normale
	// retombe sur un champ vide, donc sur aucun épilogue.
	protected PackedScene SceneEpilogueChoisie =>
		VariantePrise && ScenePnjEpilogueAlternatif != null ? ScenePnjEpilogueAlternatif : ScenePnjEpilogue;

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
		// Le combat se rejoue depuis le début si le joueur meurt : voir ReinitialiserCombat.
		GameState.Instance.JoueurMort += ReinitialiserCombat;
		return true;
	}

	// Désabonnement obligatoire : GameState est un autoload qui survit aux changements de
	// scène, et un délégué C# ne se défait pas seul à la libération du nœud abonné (même
	// piège que Player._ExitTree — une zone d'une scène quittée finirait par lever
	// ObjectDisposedException et couperait la diffusion du signal aux autres abonnés).
	public override void _ExitTree()
	{
		if (GameState.Instance != null)
			GameState.Instance.JoueurMort -= ReinitialiserCombat;
	}

	// Mort du joueur : le combat repart de zéro. Le boss est retiré (le suivant
	// réapparaîtra à PvMax plein, sans quoi le joueur retrouverait un boss à moitié
	// entamé et flottant dans l'arène), la barre est masquée et le déclencheur réarmé.
	//
	// Si le joueur réapparaît HORS de l'arène, c'est son retour qui refera surgir le
	// boss. S'il réapparaît DEDANS (campement placé dans l'arène, ou respawn qui recharge
	// cette scène), aucun BodyEntered ne se produit puisqu'il n'a pas franchi la
	// frontière : on relance donc nous-mêmes. Le contrôle est DIFFÉRÉ, le temps que le
	// joueur ait été téléporté par son propre gestionnaire de mort.
	private void ReinitialiserCombat()
	{
		if (Boss != null && IsInstanceValid(Boss))
			Boss.QueueFree();
		Boss = null;

		// Mourir pendant le prologue (chute dans le vide en allant vers l'interlocuteur)
		// laisserait sinon un PNJ orphelin dans l'arène, et le retour du joueur en ferait
		// apparaître un second. Son dialogue n'ayant pas abouti, il sera reproposé.
		if (PnjPrologue != null && IsInstanceValid(PnjPrologue))
			PnjPrologue.QueueFree();
		PnjPrologue = null;

		Barre?.Masquer();
		RearmerDeclencheur();
		CallDeferred(MethodName.RelancerSiJoueurPresent);
	}

	private void RelancerSiJoueurPresent()
	{
		if (GetTree()?.GetFirstNodeInGroup("joueur") is Player joueur && Contient(joueur.GlobalPosition))
			SurEntreeJoueur(joueur);
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

		// Idem pour l'interlocuteur du prologue : tant qu'il est là, on discute.
		if (PnjPrologue != null && IsInstanceValid(PnjPrologue))
			return;

		// La parole d'abord, s'il y en a encore à prendre : c'est la fin de sa
		// conversation qui enchaînera sur LancerCombat.
		if (LancerPrologue(joueur))
			return;

		LancerCombat(joueur);
	}

	// Le combat proprement dit : boss à ses PV, barre liée et révélée, musique, hook
	// d'héritage. Appelé à l'entrée du joueur, ou après le prologue s'il y en a un.
	private void LancerCombat(Player joueur)
	{
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
			Boss.Vaincu += DeclencherEpilogue;
		}

		Barre?.Afficher();
		JouerMusique();
		DemarrerCombat(joueur);
	}

	// ---- Prologue ----

	// Fait apparaître l'interlocuteur du prologue et rend la main : c'est la fin de sa
	// conversation qui lancera le combat. Retourne FAUX - donc on enchaîne aussitôt sur
	// le combat - si l'arène n'a pas de prologue, ou s'il a déjà été joué.
	//
	// C'est le PNJ lui-même qui dit s'il a encore quelque chose à raconter, via le verrou
	// Talkative.PeutParler() (UneSeuleFois + IdDialogue, mémorisés dans GameState par
	// PnjAmical.SurFinDialogue). Aucun identifiant à recopier ici : la clé de mémoire vit
	// d'un seul côté, dans la scène du PNJ, et ne peut donc pas se désynchroniser.
	private bool LancerPrologue(Player joueur)
	{
		if (ScenePrologueChoisie == null)
			return false;

		var pnj = ScenePrologueChoisie.Instantiate<Node2D>();
		var declencheur = TrouverDeclencheur(pnj);

		if (declencheur == null || (pnj is Talkative parlant && !parlant.PeutParler()))
		{
			// Jamais entré dans l'arbre : Free() et non QueueFree(), rien ne le référence.
			pnj.Free();
			return false;
		}

		// Même point que le boss : sous le fondu au noir, l'échange est invisible.
		pnj.Position = CalculerApparition();
		declencheur.DialogueTermine += SurPrologueTermine;
		PnjPrologue = pnj;
		_joueurPrologue = joueur;
		// Ajout DIFFÉRÉ, pour la même raison que le boss : on arrive de BodyEntered, donc
		// en plein flush des requêtes physiques, où Godot refuse d'ajouter un corps avec
		// ses formes de collision.
		GetParent().CallDeferred(Node.MethodName.AddChild, pnj);
		return true;
	}

	// Le DeclencheurDialogue d'un PNJ est un enfant Area2D de sa scène (jamais sa racine).
	// Cherché par TYPE et non par nom : la zone n'a ainsi à connaître aucune convention de
	// nommage des scènes de PNJ.
	private static DeclencheurDialogue TrouverDeclencheur(Node racine)
	{
		foreach (var enfant in racine.GetChildren())
			if (enfant is DeclencheurDialogue declencheur)
				return declencheur;

		return null;
	}

	// Conversation terminée : fondu au noir, l'interlocuteur s'efface, le boss prend sa
	// place. `complet` faux = le joueur s'est éloigné en cours de route (possible pour un
	// PNJ sans arbre de choix, qui ne fige pas le joueur) : on ne déclenche alors rien,
	// la conversation pourra reprendre.
	private void SurPrologueTermine(bool complet)
	{
		if (!complet || PnjPrologue == null || !IsInstanceValid(PnjPrologue))
			return;

		var pnj = PnjPrologue;
		PnjPrologue = null;

		// Le joueur reste figé le temps du noir : sans ça il reprend la main pendant le
		// fondu et peut s'écarter avant même que le boss soit là.
		GameState.Instance.DialogueModal = true;
		// Le PNJ vient de se marquer consommé (dialogue à usage unique) : on écrit la
		// sauvegarde pour que le prologue reste joué même après un rechargement.
		GameState.Instance.Sauvegarder();

		Effets.FondreAuNoirPuis(this, DureeFonduEchange, () =>
		{
			if (IsInstanceValid(pnj))
				pnj.QueueFree();

			// Filet : si le PNJ pouvait encore parler, il a laissé le rappel de touche
			// armé en partant, et Espace resterait détourné du saut pour tout le combat.
			GameState.Instance.DialogueDisponible = false;
			GameState.Instance.DialogueModal = false;
			LancerCombat(_joueurPrologue);
		});
	}

	// ---- Épilogue ----

	// Chute du boss : on laisse un battement, le temps que son animation de mort se joue,
	// avant de couper au noir. Sans épilogue câblé, la victoire reste telle quelle.
	private void DeclencherEpilogue()
	{
		if (_epilogueLance || SceneEpilogueChoisie == null)
			return;

		_epilogueLance = true;
		GetTree().CreateTimer(DelaiEpilogue).Timeout += EchangerContreEpilogue;
	}

	// Échange le vaincu contre son PNJ d'épilogue, sous le même fondu que le prologue mais
	// dans l'autre sens. Le PNJ reprend la position exacte du boss tombé.
	private void EchangerContreEpilogue()
	{
		var scene = SceneEpilogueChoisie;
		if (scene == null)
			return;

		var position = Boss != null && IsInstanceValid(Boss) ? Boss.Position : CalculerApparition();

		// Le joueur reste figé pendant le noir, comme à l'aller.
		GameState.Instance.DialogueModal = true;

		Effets.FondreAuNoirPuis(this, DureeFonduEchange, () =>
		{
			if (Boss != null && IsInstanceValid(Boss))
				Boss.QueueFree();
			Boss = null;
			Barre?.Masquer();

			// Ajout DIRECT et non différé, contrairement au prologue : on est appelé
			// depuis un tween (étape de process), pas depuis BodyEntered — aucune requête
			// physique n'est en cours de flush ici.
			var pnj = scene.Instantiate<Node2D>();
			pnj.Position = position;
			GetParent().AddChild(pnj);

			GameState.Instance.DialogueModal = false;
		});
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
