using Godot;

// Helper d'I/O du fichier de paramètres : isole le chemin et le format (ConfigFile
// INI dans user://). Réutilisable ; ne connaît que le fichier, pas le contenu métier
// (c'est DonneesParametres qui sait se sérialiser). Miroir de Sauvegarde, mais
// distinct : les paramètres ne sont pas de la progression et survivent à une
// « nouvelle partie ».
public static class ConfigFichier
{
	private const string Chemin = "user://parametres.cfg";

	// Vrai si un fichier de paramètres existe sur disque.
	public static bool Existe() => FileAccess.FileExists(Chemin);

	// Écrit le ConfigFile sur disque (écrase l'unique emplacement).
	public static void Ecrire(ConfigFile cfg)
	{
		var err = cfg.Save(Chemin);
		if (err != Error.Ok)
			GD.PushError($"ConfigFichier : impossible d'écrire {Chemin} ({err})");
	}

	// Charge le ConfigFile. Retourne null si absent ou illisible (les défauts
	// restent alors en place).
	public static ConfigFile Lire()
	{
		if (!Existe())
			return null;

		var cfg = new ConfigFile();
		return cfg.Load(Chemin) == Error.Ok ? cfg : null;
	}
}
