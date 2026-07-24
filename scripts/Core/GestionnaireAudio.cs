using Godot;
using Godot.Collections;

// Musique et ambiance de fond, par lieu et par état météo. Autoload (et non nœud
// de monde1.tscn) pour survivre au passage menu -> monde -> écran de fin.
//
// Le gestionnaire ne décide RIEN, comme GestionnaireMeteo : les zones viennent
// lui demander une ambiance par son nom (CameraZone/ZoneBoss via
// AppliquerCommeSalle) et la météo vient lui annoncer l'état courant. Lui se
// contente de résoudre le couple (ambiance, état) en deux playlists et de faire
// les fondus croisés.
//
// Deux canaux indépendants, traités par le MÊME code (BasculerCanal) :
// - Musique  : une piste tirée au sort, la suivante enchaîne en fin de morceau ;
// - Ambiance : lit sonore continu (le bouclage se règle à l'import du .ogg).
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

		_ambianceActive = nom;
		AppliquerVariante(ambiance.Trouver(_etatActuel), changementDeLieu: true);
	}

	// Entrée de la météo (GestionnaireMeteo.AfficherBlizzard) : rejoue le lieu
	// courant dans son nouvel état.
	public void DefinirEtat(string etat)
	{
		if (etat == _etatActuel)
			return;

		_etatActuel = etat;

		if (_ambiances.TryGetValue(_ambianceActive, out var ambiance))
			AppliquerVariante(ambiance.Trouver(_etatActuel), changementDeLieu: false);
	}

	// Thème hors playlist (musique de boss). La piste unique se rejoue en fin de
	// morceau via l'enchaînement du canal Musique.
	public void JouerMusiquePonctuelle(AudioStream musique, float volumeDb = 0f)
	{
		if (musique == null)
			return;

		_varianteActive = null;
		BasculerCanal(_musique, new Array<AudioStream> { musique }, volumeDb, couperSiVide: true);
	}

	// changementDeLieu : voir le paramètre couperSiVide de BasculerCanal. Une
	// variante absente vaut playlists vides - un lieu sans son est silencieux.
	private void AppliquerVariante(VarianteAmbiance variante, bool changementDeLieu)
	{
		_varianteActive = variante;

		BasculerCanal(_musique, variante?.Musiques, variante?.VolumeMusiqueDb ?? 0f, changementDeLieu);
		BasculerCanal(_ambiance, variante?.Ambiances, variante?.VolumeAmbianceDb ?? 0f, changementDeLieu);
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
	private void BasculerCanal(Canal canal, Array<AudioStream> pistes, float volumeDb, bool couperSiVide)
	{
		if (pistes == null || pistes.Count == 0)
		{
			if (couperSiVide)
				EteindreCanal(canal);
			return;
		}

		var piste = TirerPiste(canal, pistes);
		if (piste == null || piste == canal.Piste)
			return;

		EteindreLecteur(canal.Lecteur);
		canal.Piste = piste;
		canal.Lecteur = DemarrerLecteur(canal, piste, volumeDb);
	}

	// Rend le canal silencieux : la piste courante s'éteint et rien ne la remplace.
	private void EteindreCanal(Canal canal)
	{
		EteindreLecteur(canal.Lecteur);
		canal.Lecteur = null;
		canal.Piste = null;
		canal.DernierIndex = -1;
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
	// plusieurs pistes passe à une autre. Les .ogg de musique sont donc importés
	// en loop = false : un .ogg bouclé par l'import n'émettrait jamais Finished
	// et resterait coincé sur la même piste.
	private void Enchainer(Canal canal)
	{
		var pistes = canal == _musique ? _varianteActive?.Musiques : _varianteActive?.Ambiances;
		if (pistes == null || pistes.Count == 0)
		{
			canal.Lecteur.Play();   // thème ponctuel ou variante disparue : on reboucle.
			return;
		}

		canal.Piste = TirerPiste(canal, pistes);
		canal.Lecteur.Stream = canal.Piste;
		canal.Lecteur.Play();
	}

	// Tirage au sort qui ÉVITE de resservir la piste précédente dès qu'il y a le
	// choix : un tirage strictement aléatoire répète, et ça s'entend.
	private static AudioStream TirerPiste(Canal canal, Array<AudioStream> pistes)
	{
		if (pistes.Count == 1)
		{
			canal.DernierIndex = 0;
			return pistes[0];
		}

		int index;
		do
		{
			index = GD.RandRange(0, pistes.Count - 1);
		} while (index == canal.DernierIndex);

		canal.DernierIndex = index;
		return pistes[index];
	}
}
