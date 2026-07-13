using Godot;

// Helper d'entrées/sorties disque pour la sauvegarde : isole le chemin et le
// format de fichier (JSON unique dans user://). Réutilisable ; ne connaît que le
// fichier, pas le contenu métier (c'est DonneesSauvegarde qui sait se sérialiser).
public static class Sauvegarde
{
	private const string Chemin = "user://sauvegarde.json";

	// Vrai si une sauvegarde existe sur disque.
	public static bool Existe() => FileAccess.FileExists(Chemin);

	// Écrit le dictionnaire de données au format JSON (écrase l'unique emplacement).
	public static void Ecrire(Godot.Collections.Dictionary donnees)
	{
		using var fichier = FileAccess.Open(Chemin, FileAccess.ModeFlags.Write);
		if (fichier == null)
		{
			GD.PushError($"Sauvegarde : impossible d'écrire {Chemin}");
			return;
		}
		fichier.StoreString(Json.Stringify(donnees));
	}

	// Lit et parse la sauvegarde. Retourne null si absente ou corrompue.
	public static Godot.Collections.Dictionary Lire()
	{
		if (!Existe())
			return null;

		using var fichier = FileAccess.Open(Chemin, FileAccess.ModeFlags.Read);
		if (fichier == null)
			return null;

		var resultat = Json.ParseString(fichier.GetAsText());
		return resultat.VariantType == Variant.Type.Dictionary ? resultat.AsGodotDictionary() : null;
	}
}
