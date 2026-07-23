using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// Lève l'ambiguïté avec Godot.HttpClient : on veut le client HTTP du BCL.
using HttpClient = System.Net.Http.HttpClient;

// Autoload (singleton) : dialogues dynamiques via un LLM local (Ollama). Il gère trois
// choses : le provisionnement embarqué du serveur (délégué à ProvisionneurOllama : premier
// lancement = téléchargement du binaire + du modèle dans user://ollama/, ensuite cache
// hors ligne), la composition du prompt système (contexte global partagé + contexte propre
// au PNJ) et la génération de la réplique en streaming (token par token) renvoyée sur le
// thread principal. Tout se fait en tâche de fond au boot pour ne pas bloquer le jeu ;
// tant que Disponible est faux (provisionnement en cours ou échec, ex. pas de réseau), les
// PNJ retombent silencieusement sur leurs Lignes statiques — jamais d'erreur bloquante.
public partial class OllamaService : Node
{
	public static OllamaService Instance { get; private set; }

	// ---- Réglages ----
	// Modèle courant (tag Ollama). La valeur d'export sert de défaut (palier « Petit ») ; elle
	// est écrasée au boot par le choix persistant (voir ChargerConfig / DefinirModele).
	[Export] public string Modele = "llama3.2:3b";
	[Export] public string UrlBase = "http://127.0.0.1:11434";
	[Export] public int MaxTokens = 80; // borne la longueur de réponse (options.num_predict)

	// Durée pendant laquelle Ollama garde le modèle EN MÉMOIRE après une requête (défaut Ollama :
	// 5 min). On l'allonge pour éviter que le modèle ne se décharge entre deux PNJ : sinon le
	// dialogue suivant repaie le coût de chargement (le fameux « … » interminable). "-1" = jamais.
	[Export] public string KeepAlive = "30m";

	// Sources d'installation officielles par OS (ProvisionneurOllama lance la bonne procédure) :
	// Linux = archive extraite par tar (amd64) ; Windows = installeur silencieux OllamaSetup.exe ;
	// macOS = image disque Ollama.dmg. Adapter au besoin depuis ollama.com/download.
	[Export] public string UrlBinaireLinux = "https://ollama.com/download/ollama-linux-amd64.tar.zst";
	[Export] public string UrlBinaireWindows = "https://ollama.com/download/OllamaSetup.exe";
	[Export] public string UrlBinaireMacos = "https://ollama.com/download/Ollama.dmg";

	// ---- Contexte global partagé (injecté dans le prompt système de TOUS les PNJ IA) ----
	[Export] public string NomJoueur = "Glooby";

	[Export(PropertyHint.MultilineText)]
	public string ContexteGlobal =
		"Univers : un jeu de plateforme 2D goofy et bon enfant sur la banquise. " +
		"Le héros est un petit pingouin nommé Glooby. Réponds TOUJOURS en français en tutoyant, " +
		"en une seule phrase courte et adaptée aux enfants, en gardant le ton de ton " +
		"personnage (il peut être ronchon, timide, farceur…).";

	// Faits ajoutables PAR CODE au fil de la partie (progression, pouvoir obtenu…), sans
	// toucher aux exports. Composés dans le prompt système par ConstruireContexte.
	public Dictionary<string, string> FaitsGlobaux { get; } = new();

	// Faux tant que le serveur n'est pas prêt à générer (provisionnement, ou échec/skip).
	public bool Disponible { get; private set; }

	// Activation par l'utilisateur (persistée dans user://ollama.cfg). Quand false, on ne
	// démarre NI le serveur NI les appels : les PNJ restent en dialogues statiques. Réglé
	// dans Paramètres > Avancé.
	public bool Actif { get; private set; } = true;
	private const string CheminConfig = "user://ollama.cfg";

	// Palier de modèle proposé au joueur : un libellé lisible et le tag Ollama correspondant.
	public readonly record struct PaletteModele(string Libelle, string Tag);

	// Catalogue des tailles sélectionnables (Paramètres > Avancé). Source UNIQUE, réutilisée
	// par l'UI : plus le modèle est gros, meilleures sont les répliques mais plus lourd est le
	// téléchargement. Les tags doivent exister sur ollama.com (pull automatique au choix).
	public static readonly PaletteModele[] Modeles =
	{
		new("Minuscule (1.3 Go)", "llama3.2:1b"), // ~1.3 Go
		new("Petit (2.0 Go)", "llama3.2:3b"),     // ~2.0 Go (défaut)
		new("Moyen (4.1 Go)", "mistral:7b"),      // ~4.1 Go, français natif
		new("Lourd (9.0 Go)", "qwen2.5:14b"),     // ~9 Go
	};

	// Progression du provisionnement (pour l'écran de chargement) : phase lisible + ratio 0→1.
	[Signal] public delegate void ProvisionnementProgresseEventHandler(string phase, float ratio);
	// Échec d'une étape (téléchargement, installation, modèle…) avec sa raison lisible.
	[Signal] public delegate void ProvisionnementErreurEventHandler(string message);
	[Signal] public delegate void ProvisionnementTermineEventHandler(bool succes);

	// Un seul HttpClient (partagé avec le provisionneur), timeout géré par jeton par requête.
	private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };
	private System.Diagnostics.Process _serveur;
	private CancellationTokenSource _ctsGeneration;
	private bool _ecranDemande; // l'écran de chargement n'est instancié qu'une fois, au 1er téléchargement

	public override void _Ready()
	{
		Instance = this;
		ChargerConfig();
		if (Actif)
			_ = Task.Run(DemarrerAsync);
	}

	private void ChargerConfig()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(CheminConfig) != Error.Ok)
			return;
		Actif = cfg.GetValue("ollama", "actif", true).AsBool();
		Modele = cfg.GetValue("ollama", "modele", Modele).AsString();
	}

	private void SauverConfig()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("ollama", "actif", Actif);
		cfg.SetValue("ollama", "modele", Modele);
		cfg.Save(CheminConfig);
	}

	// Change le modèle (tag Ollama) et persiste le choix. Si Ollama est actif : relance le
	// provisionnement, qui réutilise le serveur déjà lancé, télécharge le nouveau modèle s'il
	// est absent (barre de progression) puis le préchauffe. Inactif : on persiste seulement,
	// le choix s'appliquera à la prochaine activation. Un tag déjà courant ne fait rien.
	public void DefinirModele(string tag)
	{
		if (string.IsNullOrEmpty(tag) || tag == Modele)
			return;
		Modele = tag;
		SauverConfig();

		if (Actif)
		{
			Disponible = false;
			_ecranDemande = false;
			_ = Task.Run(DemarrerAsync);
		}
	}

	// Active/désactive l'usage d'Ollama et persiste le choix. À l'activation : (re)démarre le
	// serveur en réutilisant le cache s'il existe (téléchargement seulement s'il manque). À la
	// désactivation : arrête le serveur qu'on a lancé et coupe les appels (Disponible=false).
	public void DefinirActif(bool actif)
	{
		if (actif == Actif)
			return;
		Actif = actif;
		SauverConfig();

		if (actif)
		{
			_ecranDemande = false;
			_ = Task.Run(DemarrerAsync);
		}
		else
		{
			ArreterServeur();
			Disponible = false;
		}
	}

	// Provisionnement en tâche de fond : garantit un Ollama utilisable puis bascule
	// Disponible. Toute exception est absorbée (repli statique), jamais remontée au jeu.
	private async Task DemarrerAsync()
	{
		try
		{
			var provisionneur = new ProvisionneurOllama(this, _http);
			bool ok = await provisionneur.Garantir(SignalerProgres);
			_serveur = provisionneur.ProcessusLance;
			if (!ok && !string.IsNullOrEmpty(provisionneur.DerniereErreur))
				SignalerErreur(provisionneur.DerniereErreur);
			Terminer(ok);

			// Préchauffage : charge le modèle en mémoire pendant que le joueur est encore au
			// menu, pour que le PREMIER dialogue ne paie pas le coût de chargement (le « … »
			// qui traîne). Best-effort, en fond ; ne change pas Disponible.
			if (ok)
				await PrechaufferAsync();
		}
		catch (Exception e)
		{
			GD.PushWarning($"OllamaService : provisionnement impossible ({e.Message}).");
			SignalerErreur(e.Message);
			Terminer(false);
		}
	}

	// Assemble le prompt système. Ordre IMPORTANT : contexte global, puis le rôle du PNJ
	// SANS l'interrompre, puis les faits, et ENFIN le cadre d'énonciation (à qui il parle).
	// On ne glisse plus « le héros s'appelle X » au milieu du rôle : le petit modèle attachait
	// le backstory du PNJ au nom le plus proche (le joueur). La consigne finale lève la
	// confusion des identités (le PNJ n'est pas le joueur ; il parle à la 1re personne).
	public string ConstruireContexte(string contextePnj)
	{
		var sb = new StringBuilder();
		
		sb.AppendLine($"Cadre : Tu discutes avec {NomJoueur}, le héros du jeu. {NomJoueur} n'est PAS toi. "
		              + $"Parle uniquement de toi, à la première personne (je/moi) ; n'attribue jamais ton histoire à {NomJoueur}. "
		              + $"Si tu n'a pas de nom, ne mentionne jamais ton nom, ni comment tu t'appelle"
		              + $"Fait toujours une seule phrase simple et courte. Jamais plusieurs phrases"
		              + $"Ne parle jamais du fait que tu es un PNJ dans un jeu."
		              + $"Tu n'es pas obligé de saluer le jouer : tu peux entrer directement dans le vif du sujet."
		              + $"Ne mentionne JAMAIS quel type de PNJ tu es (pingouin, lutin, père noel, etc...)"
		              + $"[TRES IMPORTANT] Fini toujours ta phrase par un point."
		);
		
		if (!string.IsNullOrWhiteSpace(ContexteGlobal))
			sb.AppendLine(ContexteGlobal.Trim());
		if (!string.IsNullOrWhiteSpace(contextePnj))
			sb.AppendLine(contextePnj.Trim());
		foreach (var (cle, valeur) in FaitsGlobaux)
			sb.AppendLine($"{cle} : {valeur}");
		return sb.ToString().Trim();
	}

	// Génère une réplique en streaming. Les callbacks sont TOUJOURS rejoués sur le thread
	// Godot (via CallDeferred) : jamais de mutation de scène hors thread principal.
	//   surChunk : texte CUMULÉ reçu jusqu'ici (rendu incrémental de la bulle).
	//   surFin   : génération terminée proprement.
	//   surErreur: échec (réseau, modèle…) ⇒ l'appelant retombe sur les Lignes statiques.
	public void GenererFlux(string contexte, string invite, int motMoyenParReponse, Action<string> surChunk, Action surFin, Action surErreur)
	{
		if (!Disponible)
		{
			surErreur?.Invoke();
			return;
		}

		_ctsGeneration?.Cancel();
		_ctsGeneration = new CancellationTokenSource();
		var jeton = _ctsGeneration.Token;
		_ = Task.Run(() => FluxAsync(contexte, invite, motMoyenParReponse, surChunk, surFin, surErreur, jeton));
	}

	// Charge le modèle en mémoire sans rien générer (prompt vide) et fixe keep_alive : le
	// premier vrai dialogue démarre alors instantanément. Best-effort — un échec est ignoré.
	private async Task PrechaufferAsync()
	{
		try
		{
			var corps = JsonSerializer.Serialize(new
			{
				model = Modele,
				prompt = "",
				stream = false,
				keep_alive = KeepAlive,
			});
			using var requete = new HttpRequestMessage(HttpMethod.Post, $"{UrlBase}/api/generate")
			{
				Content = new StringContent(corps, Encoding.UTF8, "application/json"),
			};
			using var reponse = await _http.SendAsync(requete);
		}
		catch { /* préchauffage best-effort : sans effet sur la disponibilité */ }
	}

	// Annule la génération en cours (fin de conversation / sortie de zone).
	public void AnnulerGeneration() => _ctsGeneration?.Cancel();

	// Chemin disque du cache Ollama géré par le jeu (binaire extrait/app copiée + modèles).
	private static string RacineCacheDisque => ProjectSettings.GlobalizePath("user://ollama");

	// Supprime l'installation/cache Ollama gérés par le jeu (binaire, app, modèles téléchargés)
	// et arrête le serveur qu'on a lancé. Best-effort : les fichiers verrouillés par un serveur
	// encore vivant sont libérés par l'arrêt du process avant la suppression. Utilisé par la
	// section « Avancé » des paramètres (bouton « Supprimer Ollama »).
	public void SupprimerOllama()
	{
		ArreterServeur();
		Disponible = false;
		try
		{
			if (System.IO.Directory.Exists(RacineCacheDisque))
				System.IO.Directory.Delete(RacineCacheDisque, recursive: true);
		}
		catch (Exception e)
		{
			GD.PushWarning($"OllamaService : suppression partielle du cache ({e.Message}).");
		}
	}

	// Supprime puis relance tout le provisionnement (téléchargement + modèle), avec la barre
	// de chargement. Utilisé par la section « Avancé » (bouton « Retélécharger Ollama »).
	public void Reprovisionner()
	{
		SupprimerOllama();
		_ecranDemande = false; // la barre pourra réapparaître au prochain téléchargement
		_ = Task.Run(DemarrerAsync);
	}

	private async Task FluxAsync(string contexte, string invite, int motMoyenParReponse, Action<string> surChunk, Action surFin, Action surErreur, CancellationToken jeton)
	{
		try
		{
			// Longueur : on STEER en douceur via une consigne « en ~N mots » (plus fiable que
			// couper net), et on borne quand même num_predict comme filet de sécurité (marge de
			// ~2.5 tokens/mot pour ne pas tronquer la phrase cible). MaxTokens reste le plafond dur.
			int mots = Mathf.Max(1, motMoyenParReponse);
			//int budgetTokens = Mathf.Clamp(Mathf.RoundToInt(mots * 2.5f), 12, MaxTokens);
			string inviteAvecLongueur = $"{invite}\nLa réponse courte doit avoir environ {mots} mots.";//" Fini ta phrase et ne coupe pas ta réponse. Ne coupe jamais un mot. Ne commence JAMAIS de nouvelle phrase, la réponse doit avoir seulement une phrase";

			var corps = JsonSerializer.Serialize(new
			{
				model = Modele,
				system = contexte,
				prompt = inviteAvecLongueur,
				stream = true,
				keep_alive = KeepAlive, // garde le modèle chaud pour le PNJ suivant
				//options = new { num_predict = budgetTokens },
			});

			using var requete = new HttpRequestMessage(HttpMethod.Post, $"{UrlBase}/api/generate")
			{
				Content = new StringContent(corps, Encoding.UTF8, "application/json"),
			};
			using var reponse = await _http.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, jeton);
			reponse.EnsureSuccessStatusCode();

			using var flux = await reponse.Content.ReadAsStreamAsync(jeton);
			using var lecteur = new StreamReader(flux);

			var cumule = new StringBuilder();
			string ligne;
			while ((ligne = await lecteur.ReadLineAsync()) != null)
			{
				jeton.ThrowIfCancellationRequested();
				if (string.IsNullOrWhiteSpace(ligne))
					continue;

				using var doc = JsonDocument.Parse(ligne);
				var racine = doc.RootElement;
				if (racine.TryGetProperty("response", out var fragment))
				{
					cumule.Append(fragment.GetString());
					string texte = cumule.ToString();
					Callable.From(() => surChunk?.Invoke(texte)).CallDeferred();
				}
				if (racine.TryGetProperty("done", out var fini) && fini.GetBoolean())
					break;
			}

			Callable.From(() => surFin?.Invoke()).CallDeferred();
		}
		catch (OperationCanceledException)
		{
			// Annulation volontaire : rien à signaler.
		}
		catch (Exception e)
		{
			GD.PushWarning($"OllamaService : échec de génération ({e.Message}).");
			Callable.From(() => surErreur?.Invoke()).CallDeferred();
		}
	}

	// Rapporte la progression (appelé depuis le thread de fond) : rejoué sur le thread
	// principal, il instancie l'écran de chargement au 1er téléchargement puis émet le signal.
	private void SignalerProgres(string phase, float ratio)
	{
		Callable.From(() =>
		{
			if (!_ecranDemande)
			{
				_ecranDemande = true;
				AfficherEcranChargement();
			}
			EmitSignal(SignalName.ProvisionnementProgresse, phase, ratio);
		}).CallDeferred();
	}

	// Rapporte un échec (depuis le thread de fond) : rejoué sur le thread principal ; instancie
	// la barre si besoin (une erreur peut survenir avant toute progression, ex. pas de réseau).
	private void SignalerErreur(string message)
	{
		Callable.From(() =>
		{
			if (!_ecranDemande)
			{
				_ecranDemande = true;
				AfficherEcranChargement();
			}
			EmitSignal(SignalName.ProvisionnementErreur, message);
		}).CallDeferred();
	}

	private void AfficherEcranChargement()
	{
		var scene = GD.Load<PackedScene>("res://scenes/ui/ecran_chargement_ollama.tscn");
		if (scene == null)
			return;
		GetTree().Root.AddChild(scene.Instantiate());
	}

	private void Terminer(bool succes)
	{
		Disponible = succes;
		Callable.From(() => EmitSignal(SignalName.ProvisionnementTermine, succes)).CallDeferred();
	}

	// Arrête le serveur qu'on a lancé (le nôtre uniquement : jamais un serveur externe) et
	// annule toute génération. Réutilisé par la fermeture du jeu et la suppression/réinstallation.
	private void ArreterServeur()
	{
		AnnulerGeneration();
		if (_serveur is { HasExited: false })
		{
			try { _serveur.Kill(true); }
			catch { /* déjà mort : rien à faire */ }
		}
		_serveur = null;
	}

	// Arrêt propre : tuer le serveur qu'on a lancé, libérer HttpClient.
	public override void _ExitTree()
	{
		ArreterServeur();
		_http.Dispose();
	}
}
