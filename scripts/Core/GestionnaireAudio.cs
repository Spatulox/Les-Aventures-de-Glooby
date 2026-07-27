using Godot;
using Godot.Collections;

// Musique et ambiance de fond, par lieu et par état météo. Autoload (et non nœud
// de 01-monde1.tscn) pour survivre au passage menu -> monde -> écran de fin.
//
// Le gestionnaire ne décide RIEN, comme GestionnaireMeteo : les zones viennent
// lui demander une ambiance par son nom (CameraZone/ZoneBoss via
// AppliquerCommeSalle) et la météo vient lui annoncer l'état courant. Lui se
// contente de résoudre le couple (ambiance, état) en deux playlists et de faire
// les fondus croisés.
//
// Deux canaux indépendants, traités par le MÊME code (BasculerCanal) :
// - Musique  : une piste tirée au sort SELON SES PROBABILITÉS (PisteMusicale),
//   la suivante enchaîne en fin de morceau ;
// - Ambiance : lit sonore continu, tirage uniforme (le bouclage se règle à
//   l'import du .ogg).
//
// Cas particulier du blizzard : la musique de blizzard ne REMPLACE pas la
// musique normale, elle la MET EN PAUSE (position conservée) et celle-ci REPREND
// là où elle en était à la fin du blizzard (cf. _musiqueSuspendue).
public partial class GestionnaireAudio : Node
{
	public static GestionnaireAudio Instance { get; private set; }

	// Volume d'un lecteur éteint. Assez bas pour être inaudible avant le QueueFree.
	private const float VolumeMuet = -60f;

	// Fondu plus long que celui du fond (0.5s) et que celui de la météo (1s) :
	// une coupure musicale s'entend bien plus qu'un fondu d'image.
	[Export] public float DureeFondu = 1.5f;

	// Les ambiances sont DÉCOUVERTES dans ce dossier au démarrage : y déposer un
	// .tres suffit à enregistrer un nouveau lieu, sans toucher à ce script (même
	// esprit que le parcours des enfants de BackgroundManager). Un export sur un
	// autoload en script nu n'étant pas éditable dans l'inspecteur, le dossier
	// remplace la liste à remplir à la main.
	[Export] public string DossierAmbiances = "res://assets/audio/ambiances";

	private readonly System.Collections.Generic.Dictionary<string, AmbianceSonore> _ambiances = new();

	private readonly Canal _musique = new() { Bus = "Musique" };
	private readonly Canal _ambiance = new() { Bus = "Ambiance" };

	private string _ambianceActive = "";
	private string _etatActuel = AmbianceSonore.EtatNormal;
	private VarianteAmbiance _varianteActive;

	// Lecteur de la musique NORMALE mis en pause pendant un blizzard, à reprendre
	// (à sa position) une fois le blizzard terminé. Null en dehors d'un blizzard.
	private AudioStreamPlayer _musiqueSuspendue;

	// Un canal = une famille de sons qui ne joue qu'une piste à la fois. Regrouper
	// son état ici permet à BasculerCanal de servir la musique ET l'ambiance sans
	// dupliquer la mécanique de fondu.
	private class Canal
	{
		public string Bus;
		public AudioStreamPlayer Lecteur;
		public AudioStream Piste;
		public int DernierIndex = -1;
	}

	public override void _Ready()
	{
		Instance = this;

		// La musique continue pendant la pause (MenuPause met GetTree().Paused à
		// true, ce qui suspendrait les lecteurs). Always se propage aux lecteurs
		// créés ici, qui héritent du mode de leur parent - et aux tweens de fondu.
		ProcessMode = ProcessModeEnum.Always;

		ChargerAmbiances();
	}

	// Lit toutes les ressources d'ambiance du dossier et les indexe par leur Nom.
	private void ChargerAmbiances()
	{
		using var dossier = DirAccess.Open(DossierAmbiances);
		if (dossier == null)
			return;   // pas encore d'assets audio : le jeu tourne en silence.

		foreach (string fichier in dossier.GetFiles())
		{
			// À l'export, les ressources peuvent être converties en binaire et
			// suffixées .remap - on retrouve le chemin réel en retirant le suffixe.
			string nomFichier = fichier.EndsWith(".remap") ? fichier.GetBaseName() : fichier;
			if (!nomFichier.EndsWith(".tres") && !nomFichier.EndsWith(".res"))
				continue;

			if (GD.Load($"{DossierAmbiances}/{nomFichier}") is AmbianceSonore ambiance
				&& !string.IsNullOrEmpty(ambiance.Nom))
				_ambiances[ambiance.Nom] = ambiance;
		}
	}

	// Entrée des zones. L'early-return est essentiel : le Player sonde sa zone à
	// chaque frame et un respawn ou un aller-retour ne doit PAS relancer la musique.
	public void JouerAmbiance(string nom)
	{
		if (nom == _ambianceActive)
			return;

		if (!_ambiances.TryGetValue(nom, out var ambiance))
		{
			// Silence tant qu'aucun asset n'est posé ; une fois les ambiances
			// chargées, une clé inconnue est en revanche une erreur d'auteur.
			if (_ambiances.Count > 0)
				GD.PushWarning($"Ambiance sonore '{nom}' introuvable dans {DossierAmbiances}.");
			return;
		}

		// Changement de LIEU : une éventuelle musique suspendue appartenait à
		// l'ancien lieu, elle ne doit pas ressusciter ici (ex. blizzard puis
		// entrée en grotte). On la libère avant d'appliquer la nouvelle ambiance.
		LibererMusiqueSuspendue();

		_ambianceActive = nom;
		AppliquerVariante(ambiance.Trouver(_etatActuel), changementDeLieu: true);
	}

	// Entrée de la météo (GestionnaireMeteo.AfficherBlizzard) : rejoue le lieu
	// courant dans son nouvel état. La musique NORMALE est mise en pause pour la
	// durée du blizzard, puis reprise à sa position.
	public void DefinirEtat(string etat)
	{
		if (etat == _etatActuel)
			return;

		_etatActuel = etat;

		if (!_ambiances.TryGetValue(_ambianceActive, out var ambiance))
			return;

		var variante = ambiance.Trouver(_etatActuel);
		_varianteActive = variante;

		// Le lit de fond (vent, gouttes) bascule normalement : un simple coup de
		// vent ne coupe jamais une ambiance en cours (couperSiVide = false).
		BasculerCanal(_ambiance, couperSiVide: false);

		bool varianteADesMusiques = NombrePistes(_musique) > 0;
		bool entreeBlizzard = _etatActuel != AmbianceSonore.EtatNormal;

		if (entreeBlizzard && varianteADesMusiques && _musiqueSuspendue == null && _musique.Lecteur != null)
		{
			// Début de blizzard AVEC sa propre musique : on met la musique normale
			// en pause (position conservée) et on lance la musique de blizzard.
			SuspendreMusique();
			BasculerCanal(_musique, couperSiVide: false);
		}
		else if (!entreeBlizzard && _musiqueSuspendue != null)
		{
			// Fin du blizzard : on reprend la musique normale là où elle en était.
			ReprendreMusiqueSuspendue();
		}
		else
		{
			// Autres cas (blizzard qui ne change que le lit sonore, ou pas de
			// musique normale à reprendre) : bascule musicale ordinaire.
			BasculerCanal(_musique, couperSiVide: false);
		}
	}

	// Thème hors playlist (musique de boss "one-shot"). La piste unique se rejoue
	// en fin de morceau via l'enchaînement du canal Musique. Pour une musique de
	// boss tirée au sort/pondérée, préférer une ambiance dédiée (NomAmbiance).
	public void JouerMusiquePonctuelle(AudioStream musique, float volumeDb = 0f)
	{
		if (musique == null)
			return;

		LibererMusiqueSuspendue();
		_varianteActive = null;

		if (musique == _musique.Piste)
			return;   // déjà en train de jouer : idempotence.

		EteindreLecteur(_musique.Lecteur);
		_musique.Piste = musique;
		_musique.DernierIndex = 0;
		_musique.Lecteur = DemarrerLecteur(_musique, musique, volumeDb);
	}

	// changementDeLieu : voir le paramètre couperSiVide de BasculerCanal. Une
	// variante absente vaut playlists vides - un lieu sans son est silencieux.
	private void AppliquerVariante(VarianteAmbiance variante, bool changementDeLieu)
	{
		_varianteActive = variante;

		BasculerCanal(_musique, changementDeLieu);
		BasculerCanal(_ambiance, changementDeLieu);
	}

	// Fait passer un canal à une nouvelle playlist, en fondu croisé.
	//
	// couperSiVide distingue les DEUX raisons de basculer :
	// - changement de LIEU (true) : une zone sans musique est une zone silencieuse,
	//   la piste précédente s'arrête en fondu ;
	// - changement de MÉTÉO (false) : une variante blizzard qui ne renseigne que
	//   Ambiances laisse au contraire la musique du lieu continuer - sinon le
	//   moindre coup de vent couperait le morceau en cours.
	//
	// Non-action volontaire dans les deux cas : si la piste tirée est déjà celle
	// qui joue, on ne la relance pas (idempotence - utile quand une entrée de zone
	// et une bascule météo s'enchaînent dans la même frame, et c'est ce qui rend
	// le passage menu -> village continu, les deux partageant la même piste).
	private void BasculerCanal(Canal canal, bool couperSiVide)
	{
		if (NombrePistes(canal) == 0)
		{
			if (couperSiVide)
				EteindreCanal(canal);
			return;
		}

		var piste = ProchainePiste(canal, out int index);
		if (piste == null)
			return;

		if (piste == canal.Piste)
		{
			canal.DernierIndex = index;   // mémorise le tirage même sans relance.
			return;
		}

		EteindreLecteur(canal.Lecteur);
		canal.Piste = piste;
		canal.DernierIndex = index;
		canal.Lecteur = DemarrerLecteur(canal, piste, VolumeCanal(canal));
	}

	// Nombre de pistes JOUABLES du canal dans la variante active : pondérées pour
	// la musique (une PisteMusicale sans flux ne compte pas), toutes pour l'ambiance.
	private int NombrePistes(Canal canal)
	{
		if (_varianteActive == null)
			return 0;

		if (canal != _musique)
			return _varianteActive.Ambiances.Count;

		int jouables = 0;
		foreach (var piste in _varianteActive.Musiques)
			if (piste?.Musique != null && piste.Probabilite > 0f)
				jouables++;
		return jouables;
	}

	// Tire la prochaine piste du canal : tirage PONDÉRÉ pour la musique (via
	// PisteMusicale), uniforme pour l'ambiance. Les deux évitent la répétition
	// immédiate quand il y a le choix.
	private AudioStream ProchainePiste(Canal canal, out int index)
	{
		index = -1;
		if (_varianteActive == null)
			return null;

		if (canal == _musique)
			return _varianteActive.TirerMusique(canal.DernierIndex, out index);

		return TirerUniforme(_varianteActive.Ambiances, canal.DernierIndex, out index);
	}

	private float VolumeCanal(Canal canal) =>
		canal == _musique
			? (_varianteActive?.VolumeMusiqueDb ?? 0f)
			: (_varianteActive?.VolumeAmbianceDb ?? 0f);

	// Rend le canal silencieux : la piste courante s'éteint et rien ne la remplace.
	private void EteindreCanal(Canal canal)
	{
		EteindreLecteur(canal.Lecteur);
		canal.Lecteur = null;
		canal.Piste = null;
		canal.DernierIndex = -1;
	}

	// Détache le lecteur de musique normale et le met EN PAUSE (position conservée)
	// après un fondu descendant, au lieu de le libérer : c'est ce lecteur qui
	// reprendra à la fin du blizzard. Le canal est laissé vide pour que la musique
	// de blizzard démarre proprement par-dessus.
	private void SuspendreMusique()
	{
		var lecteur = _musique.Lecteur;
		if (lecteur == null)
			return;

		// Détaché du canal AVANT la pause : son signal Finished éventuel est ignoré
		// par le garde d'identité de DemarrerLecteur.
		_musique.Lecteur = null;
		_musique.Piste = null;

		var tween = lecteur.CreateTween();
		tween.TweenProperty(lecteur, "volume_db", VolumeMuet, DureeFondu);
		tween.TweenCallback(Callable.From(() => lecteur.StreamPaused = true));

		_musiqueSuspendue = lecteur;
	}

	// Fin du blizzard : coupe la musique de blizzard et reprend la musique normale
	// suspendue là où elle en était (StreamPaused = false), en fondu montant.
	private void ReprendreMusiqueSuspendue()
	{
		EteindreLecteur(_musique.Lecteur);

		var lecteur = _musiqueSuspendue;
		_musiqueSuspendue = null;

		_musique.Lecteur = lecteur;
		_musique.Piste = lecteur.Stream;
		lecteur.StreamPaused = false;

		var tween = lecteur.CreateTween();
		tween.TweenProperty(lecteur, "volume_db", VolumeCanal(_musique), DureeFondu);
	}

	// Libère la musique suspendue sans la reprendre (changement de lieu, thème de
	// boss...). Déjà en pause et muette : un QueueFree direct suffit.
	private void LibererMusiqueSuspendue()
	{
		if (_musiqueSuspendue == null)
			return;

		_musiqueSuspendue.QueueFree();
		_musiqueSuspendue = null;
	}

	// Fondu descendant puis libération. Le lecteur est détaché du canal AVANT
	// (l'appelant le remplace juste après), donc son signal Finished éventuel est
	// ignoré par le garde d'identité de DemarrerLecteur.
	private void EteindreLecteur(AudioStreamPlayer lecteur)
	{
		if (lecteur == null)
			return;

		var tween = lecteur.CreateTween();
		tween.TweenProperty(lecteur, "volume_db", VolumeMuet, DureeFondu);
		tween.TweenCallback(Callable.From(lecteur.QueueFree));
	}

	private AudioStreamPlayer DemarrerLecteur(Canal canal, AudioStream piste, float volumeDb)
	{
		var lecteur = new AudioStreamPlayer
		{
			Stream = piste,
			Bus = canal.Bus,
			VolumeDb = VolumeMuet
		};

		// Enchaînement : en fin de morceau on tire la piste suivante. Le garde
		// d'identité évite qu'un ancien lecteur encore en fondu ne déclenche
		// l'enchaînement du canal à la place du lecteur courant.
		lecteur.Finished += () =>
		{
			if (canal.Lecteur == lecteur)
				Enchainer(canal);
		};

		AddChild(lecteur);
		lecteur.Play();

		var tween = lecteur.CreateTween();
		tween.TweenProperty(lecteur, "volume_db", volumeDb, DureeFondu);
		return lecteur;
	}

	// Fin de morceau : on enchaîne sans fondu. C'est CE mécanisme qui fait le
	// bouclage - une zone à une seule piste la rejoue indéfiniment, une zone à
	// plusieurs pistes passe à une autre. Les pistes de musique sont donc importées
	// en loop = false : un flux bouclé par l'import n'émettrait jamais Finished
	// et resterait coincé sur la même piste.
	private void Enchainer(Canal canal)
	{
		if (NombrePistes(canal) == 0)
		{
			canal.Lecteur.Play();   // thème ponctuel ou variante disparue : on reboucle.
			return;
		}

		var piste = ProchainePiste(canal, out int index);
		if (piste == null)
		{
			canal.Lecteur.Play();
			return;
		}

		canal.Piste = piste;
		canal.DernierIndex = index;
		canal.Lecteur.Stream = piste;
		canal.Lecteur.Play();
	}

	// Tirage au sort UNIFORME qui évite de resservir la piste précédente dès qu'il
	// y a le choix (le canal Ambiance) : un tirage strictement aléatoire répète,
	// et ça s'entend. La musique, elle, passe par le tirage pondéré de VarianteAmbiance.
	private static AudioStream TirerUniforme(Array<AudioStream> pistes, int dernierIndex, out int index)
	{
		if (pistes.Count == 0)
		{
			index = -1;
			return null;
		}

		if (pistes.Count == 1)
		{
			index = 0;
			return pistes[0];
		}

		do
		{
			index = GD.RandRange(0, pistes.Count - 1);
		} while (index == dernierIndex);

		return pistes[index];
	}
}
