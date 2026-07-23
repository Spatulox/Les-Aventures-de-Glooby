using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// Lève les ambiguïtés avec les types Godot homonymes : on veut ceux du BCL.
using HttpClient = System.Net.Http.HttpClient;
using FileAccess = System.IO.FileAccess;
using Environment = System.Environment;

// Provisionneur embarqué d'Ollama : garantit, sans intervention manuelle, la présence d'un
// serveur Ollama utilisable — en lançant tout seul la procédure d'installation OFFICIELLE de
// l'OS détecté à l'exécution (OS.GetName()) :
//   • Windows : télécharge OllamaSetup.exe et l'exécute en mode silencieux (/VERYSILENT) ;
//   • macOS   : télécharge Ollama.dmg, le monte (hdiutil) et copie Ollama.app ;
//   • Linux   : télécharge l'archive (ollama-linux-amd64.tgz / .tar.zst) et l'extrait avec tar
//               dans user://ollama/ (pas de sudo, pas d'écriture dans /usr).
// Ensuite : démarre le serveur local (si l'installeur ne l'a pas déjà fait) puis télécharge le
// modèle si absent. Les lancements suivants réutilisent l'installation (hors ligne). Chaque
// étape rapporte sa progression via un callback (phase, ratio 0→1), branché sur l'écran de
// chargement par OllamaService. Aucun échec n'est bloquant : on retourne « indisponible » et
// le jeu retombe sur les dialogues statiques.
public class ProvisionneurOllama
{
	private readonly OllamaService _service;
	private readonly HttpClient _http;

	// Processus serveur démarré par NOUS (à tuer à la fermeture du jeu). Reste null si le
	// serveur répondait déjà (installeur qui l'a lancé, ou run précédent) : on ne tue pas
	// un process qu'on n'a pas ouvert.
	public Process ProcessusLance { get; private set; }

	// Raison lisible du dernier échec (affichée par la barre de chargement). Null si tout va bien.
	public string DerniereErreur { get; private set; }

	private string _cheminBinaire;

	public ProvisionneurOllama(OllamaService service, HttpClient http)
	{
		_service = service;
		_http = http;
	}

	// Enregistre une raison d'échec et retourne false : sucre pour « return Echec("...") ».
	private bool Echec(string message)
	{
		DerniereErreur = message;
		return false;
	}

	// Chemins disque réels (exécutables + inscriptibles) dérivés de user://ollama/.
	private string RacineCache => ProjectSettings.GlobalizePath("user://ollama");
	private string DossierBin => Path.Combine(RacineCache, "bin");
	private string DossierModeles => Path.Combine(RacineCache, "models");
	private bool EstWindows => OS.GetName() == "Windows";
	private bool EstMacos => OS.GetName() == "macOS";
	private string NomBinaire => EstWindows ? "ollama.exe" : "ollama";

	// Garantit un Ollama prêt à générer. Retourne false (repli statique) au moindre échec. Le
	// jeton permet à une relance (changement de modèle) d'annuler un provisionnement en cours :
	// l'annulation remonte en OperationCanceledException jusqu'à OllamaService.DemarrerAsync.
	public async Task<bool> Garantir(Action<string, float> progres, CancellationToken jeton)
	{
		_cheminBinaire = TrouverBinaire();

		// 1. Serveur déjà en écoute (lancé à la main/par l'installeur, ou run précédent vivant) ?
		if (!await ServeurRepond(jeton))
		{
			jeton.ThrowIfCancellationRequested();

			// Pas de binaire installé : lancer la procédure d'installation de l'OS.
			if (_cheminBinaire == null)
			{
				if (!await Installer(progres, jeton))
					return false; // DerniereErreur déjà renseignée par Installer
				_cheminBinaire = TrouverBinaire();
				if (_cheminBinaire == null)
					return Echec("Ollama introuvable après l'installation.");
			}

			// L'installeur Windows/macOS a pu démarrer le serveur lui-même : sinon on le lance.
			if (!await ServeurRepond(jeton) && !await LancerServeur(jeton))
				return Echec("Le serveur Ollama n'a pas pu démarrer.");
		}

		jeton.ThrowIfCancellationRequested();

		// 2. Modèle présent ? sinon on le télécharge via /api/pull (le serveur gère le store).
		return await GarantirModele(progres, jeton);
	}

	// Localise le binaire ollama : emplacements d'installation standard de l'OS d'abord,
	// puis recherche récursive dans le cache. Null si rien n'est trouvé.
	private string TrouverBinaire()
	{
		foreach (var candidat in CheminsCandidats())
			if (File.Exists(candidat))
				return candidat;

		if (Directory.Exists(RacineCache))
			foreach (var fichier in Directory.EnumerateFiles(RacineCache, NomBinaire, SearchOption.AllDirectories))
				return fichier;

		return null;
	}

	// Emplacements où l'installeur officiel (ou notre extraction) pose le binaire, par OS.
	private IEnumerable<string> CheminsCandidats()
	{
		if (EstWindows)
		{
			string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			yield return Path.Combine(local, "Programs", "Ollama", "ollama.exe");
			yield return Path.Combine(RacineCache, "ollama.exe");
		}
		else if (EstMacos)
		{
			yield return "/Applications/Ollama.app/Contents/Resources/ollama";
			yield return Path.Combine(RacineCache, "Ollama.app", "Contents", "Resources", "ollama");
		}
		else
		{
			yield return Path.Combine(DossierBin, "ollama");
			yield return "/usr/local/bin/ollama";
			yield return "/usr/bin/ollama";
		}
	}

	// Le serveur local répond-il ? (test court, sans exception remontée.) Le timeout de 2 s est
	// lié au jeton de provisionnement : une annulation coupe l'attente (le retour false laisse
	// alors Garantir constater l'annulation via ThrowIfCancellationRequested).
	private async Task<bool> ServeurRepond(CancellationToken jeton)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(jeton);
			cts.CancelAfter(TimeSpan.FromSeconds(2));
			using var reponse = await _http.GetAsync($"{_service.UrlBase}/api/tags", cts.Token);
			return reponse.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	// Lance la procédure d'installation officielle de l'OS courant. Retourne false sur échec.
	private async Task<bool> Installer(Action<string, float> progres, CancellationToken jeton)
	{
		try
		{
			Directory.CreateDirectory(RacineCache);
			Directory.CreateDirectory(DossierModeles);

			return OS.GetName() switch
			{
				"Windows" => await InstallerWindows(progres, jeton),
				"macOS" => await InstallerMacos(progres, jeton),
				_ => await InstallerLinux(progres, jeton),
			};
		}
		catch (OperationCanceledException)
		{
			throw; // provisionnement supersédé : remonter l'annulation, pas d'erreur affichée
		}
		catch (Exception e)
		{
			GD.PushWarning($"ProvisionneurOllama : installation impossible ({e.Message}).");
			return Echec($"Installation impossible : {e.Message}");
		}
	}

	// Windows : télécharge OllamaSetup.exe et l'exécute en silencieux (installeur Inno Setup,
	// installation par-utilisateur sans droits admin). TrouverBinaire localisera ensuite l'exe.
	private async Task<bool> InstallerWindows(Action<string, float> progres, CancellationToken jeton)
	{
		string installeur = Path.Combine(RacineCache, "OllamaSetup.exe");
		await TelechargerFichier(_service.UrlBinaireWindows, installeur, "Téléchargement d'Ollama", progres, jeton);

		progres?.Invoke("Installation d'Ollama", 1f);
		int code = await ExecuterProcessus(installeur, "/VERYSILENT", "/NORESTART");
		if (code != 0)
			GD.PushWarning($"OllamaSetup.exe a renvoyé le code {code}.");

		try { File.Delete(installeur); } catch { /* peu importe, l'installation est faite */ }
		if (TrouverBinaire() == null)
			return Echec("Installation Windows : binaire introuvable après l'installeur.");
		return true;
	}

	// macOS : télécharge Ollama.dmg, le monte (hdiutil), copie Ollama.app dans le cache puis
	// démonte. Le binaire CLI vit dans Contents/Resources/ollama de l'app copiée.
	private async Task<bool> InstallerMacos(Action<string, float> progres, CancellationToken jeton)
	{
		string dmg = Path.Combine(RacineCache, "Ollama.dmg");
		await TelechargerFichier(_service.UrlBinaireMacos, dmg, "Téléchargement d'Ollama", progres, jeton);

		progres?.Invoke("Installation d'Ollama", 1f);
		string montage = Path.Combine(RacineCache, "montage");
		Directory.CreateDirectory(montage);

		if (await ExecuterProcessus("hdiutil", "attach", dmg, "-nobrowse", "-quiet", "-mountpoint", montage) != 0)
			throw new Exception("hdiutil attach a échoué.");
		try
		{
			await ExecuterProcessus("cp", "-R", Path.Combine(montage, "Ollama.app"), RacineCache);
		}
		finally
		{
			await ExecuterProcessus("hdiutil", "detach", montage, "-quiet");
		}

		try { File.Delete(dmg); } catch { /* le cache reste valide sans le .dmg */ }
		try { Directory.Delete(montage, true); } catch { /* démonté : le dossier disparaît */ }

		string binaire = Path.Combine(RacineCache, "Ollama.app", "Contents", "Resources", "ollama");
		if (!File.Exists(binaire))
			return Echec("Installation macOS : binaire introuvable dans Ollama.app.");
		await ExecuterProcessus("chmod", "+x", binaire);
		return true;
	}

	// Linux : télécharge l'archive et l'extrait dans user://ollama/ (bin/ + lib/) ; le flag de
	// compression est choisi d'après l'extension de l'URL (zstd pour .tar.zst — format officiel
	// actuel —, gzip pour .tgz/.tar.gz, sinon autodétection). Pas de sudo ni d'écriture dans
	// /usr : on garde tout dans le cache du jeu.
	private async Task<bool> InstallerLinux(Action<string, float> progres, CancellationToken jeton)
	{
		string url = _service.UrlBinaireLinux;
		string archive = Path.Combine(RacineCache, "ollama-linux.tar");
		await TelechargerFichier(url, archive, "Téléchargement d'Ollama", progres, jeton);

		progres?.Invoke("Installation d'Ollama", 1f);

		var args = new List<string> { "-x" };
		if (url.EndsWith(".zst"))
			args.Add("--zstd");
		else if (url.EndsWith(".gz") || url.EndsWith(".tgz"))
			args.Add("-z");
		args.AddRange(new[] { "-f", archive, "-C", RacineCache });

		int code = await ExecuterProcessus("tar", args.ToArray());
		if (code != 0)
			throw new Exception($"tar a échoué (code {code}).");

		try { File.Delete(archive); } catch { /* le cache reste valide sans l'archive */ }

		string binaire = Path.Combine(DossierBin, "ollama");
		if (!File.Exists(binaire))
			return Echec("Installation Linux : binaire absent de l'archive extraite.");
		await ExecuterProcessus("chmod", "+x", binaire);
		return true;
	}

	// Pose l'environnement (hôte + dossier des modèles) puis lance « ollama serve » et attend
	// qu'il réponde (jusqu'à ~30 s). L'enfant hérite de l'environnement du process.
	private async Task<bool> LancerServeur(CancellationToken jeton)
	{
		OS.SetEnvironment("OLLAMA_HOST", "127.0.0.1:11434");
		OS.SetEnvironment("OLLAMA_MODELS", DossierModeles);

		var psi = new ProcessStartInfo
		{
			FileName = _cheminBinaire,
			WorkingDirectory = RacineCache,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		psi.ArgumentList.Add("serve");
		psi.Environment["OLLAMA_HOST"] = "127.0.0.1:11434";
		psi.Environment["OLLAMA_MODELS"] = DossierModeles;

		ProcessusLance = Process.Start(psi);

		for (int i = 0; i < 60; i++)
		{
			if (await ServeurRepond(jeton))
				return true;
			await Task.Delay(500, jeton);
		}
		return false;
	}

	// Garantit le modèle demandé : présent ⇒ rien à faire, absent ⇒ /api/pull streamé.
	private async Task<bool> GarantirModele(Action<string, float> progres, CancellationToken jeton)
	{
		if (await ModelePresent(jeton))
			return true;
		return await TelechargerModele(progres, jeton);
	}

	// Le modèle configuré figure-t-il dans /api/tags ?
	private async Task<bool> ModelePresent(CancellationToken jeton)
	{
		try
		{
			using var reponse = await _http.GetAsync($"{_service.UrlBase}/api/tags", jeton);
			if (!reponse.IsSuccessStatusCode)
				return false;

			using var doc = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync());
			if (!doc.RootElement.TryGetProperty("models", out var modeles))
				return false;

			foreach (var m in modeles.EnumerateArray())
				if (m.TryGetProperty("name", out var nom) && NomsCorrespondent(nom.GetString(), _service.Modele))
					return true;
			return false;
		}
		catch
		{
			return false;
		}
	}

	// Deux noms de modèle désignent-ils la même chose ? Un nom sans tag vaut « :latest ».
	private static bool NomsCorrespondent(string a, string b)
	{
		string Normaliser(string s) => string.IsNullOrEmpty(s) || s.Contains(':') ? s : s + ":latest";
		return Normaliser(a) == Normaliser(b);
	}

	// Télécharge le modèle via /api/pull (NDJSON : total/completed par couche → ratio).
	private async Task<bool> TelechargerModele(Action<string, float> progres, CancellationToken jeton)
	{
		try
		{
			var corps = JsonSerializer.Serialize(new { name = _service.Modele, stream = true });
			using var requete = new HttpRequestMessage(HttpMethod.Post, $"{_service.UrlBase}/api/pull")
			{
				Content = new StringContent(corps, Encoding.UTF8, "application/json"),
			};
			using var reponse = await _http.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, jeton);
			reponse.EnsureSuccessStatusCode();

			using var flux = await reponse.Content.ReadAsStreamAsync(jeton);
			using var lecteur = new StreamReader(flux);

			float dernierRatio = -1f;
			string ligne;
			while ((ligne = await lecteur.ReadLineAsync()) != null)
			{
				jeton.ThrowIfCancellationRequested();
				if (string.IsNullOrWhiteSpace(ligne))
					continue;

				using var doc = JsonDocument.Parse(ligne);
				var racine = doc.RootElement;
				if (racine.TryGetProperty("error", out var erreur))
					throw new Exception(erreur.GetString());

				if (racine.TryGetProperty("total", out var t) && racine.TryGetProperty("completed", out var c))
				{
					long total = t.GetInt64();
					if (total > 0)
					{
						float ratio = (float)c.GetInt64() / total;
						if (ratio - dernierRatio >= 0.01f)
						{
							dernierRatio = ratio;
							progres?.Invoke("Téléchargement du modèle", ratio);
						}
					}
				}
			}

			progres?.Invoke("Téléchargement du modèle", 1f);
			return await ModelePresent(jeton);
		}
		catch (OperationCanceledException)
		{
			throw; // provisionnement supersédé : remonter l'annulation, pas d'erreur affichée
		}
		catch (Exception e)
		{
			GD.PushWarning($"ProvisionneurOllama : modèle indisponible ({e.Message}).");
			return Echec($"Téléchargement du modèle échoué : {e.Message}");
		}
	}

	// Télécharge une URL vers un fichier en rapportant la progression (débit borné par la
	// taille annoncée ; barre laissée à 0 si le serveur ne renvoie pas Content-Length).
	private async Task TelechargerFichier(string url, string destination, string phase, Action<string, float> progres, CancellationToken jeton)
	{
		using var reponse = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, jeton);
		reponse.EnsureSuccessStatusCode();

		long? total = reponse.Content.Headers.ContentLength;
		using var source = await reponse.Content.ReadAsStreamAsync(jeton);
		using var fichier = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

		var tampon = new byte[81920];
		long recu = 0;
		float dernierRatio = -1f;
		int lus;
		while ((lus = await source.ReadAsync(tampon, jeton)) > 0)
		{
			await fichier.WriteAsync(tampon.AsMemory(0, lus));
			recu += lus;

			if (total is > 0)
			{
				float ratio = (float)recu / total.Value;
				if (ratio - dernierRatio >= 0.01f)
				{
					dernierRatio = ratio;
					progres?.Invoke(phase, ratio);
				}
			}
		}
		progres?.Invoke(phase, 1f);
	}

	// Lance un processus externe (installeur, tar, hdiutil, chmod…) et attend sa fin ; renvoie
	// son code de sortie. ArgumentList évite tout souci de guillemets/espaces dans les chemins.
	private static async Task<int> ExecuterProcessus(string fichier, params string[] arguments)
	{
		var psi = new ProcessStartInfo
		{
			FileName = fichier,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		foreach (var a in arguments)
			psi.ArgumentList.Add(a);

		using var proc = Process.Start(psi);
		await proc.WaitForExitAsync();
		return proc.ExitCode;
	}
}
