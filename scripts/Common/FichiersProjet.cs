using Godot;
using System.Collections.Generic;

// Listage de fichiers du système de fichiers virtuel de Godot, robuste à l'export.
// Dans le projet source un dossier contient « 0.png » ; dans le JEU EXPORTÉ il ne
// contient plus que « 0.png.import » (l'image réelle est un .ctex de .godot/imported/)
// ou « banquise.tres.remap » (ressource texte convertie en binaire). Le chemin
// d'origine reste chargeable tel quel par GD.Load — seule l'ÉNUMÉRATION doit retirer
// ces suffixes. Sans ça, tout code qui découvre ses assets en scannant un dossier
// (animations, ambiances, niveaux) marche dans l'éditeur et ne trouve plus rien une
// fois le jeu exporté. Tout scan de dossier du projet doit passer par ici.
public static class FichiersProjet
{
	// Suffixes que l'export ajoute au nom d'origine.
	private static readonly string[] SuffixesExport = { ".import", ".remap" };

	// Noms des fichiers d'un dossier portant l'une des extensions demandées, triés par
	// nom. Les noms sont ramenés à leur forme d'origine et dédoublonnés : dans l'éditeur
	// un même asset apparaît deux fois (« 0.png » ET « 0.png.import »).
	public static List<string> Lister(string dossier, params string[] extensions)
	{
		var noms = new List<string>();
		foreach (string fichier in DirAccess.GetFilesAt(dossier))
		{
			string nom = NomOrigine(fichier);
			if (PorteUneExtension(nom, extensions) && !noms.Contains(nom))
				noms.Add(nom);
		}
		noms.Sort();
		return noms;
	}

	// Nom d'origine d'un fichier listé : sans le suffixe ajouté par l'export.
	public static string NomOrigine(string fichier)
	{
		foreach (string suffixe in SuffixesExport)
		{
			if (fichier.EndsWith(suffixe))
				return fichier.TrimSuffix(suffixe);
		}
		return fichier;
	}

	private static bool PorteUneExtension(string nom, string[] extensions)
	{
		foreach (string extension in extensions)
		{
			if (nom.EndsWith(extension))
				return true;
		}
		return false;
	}
}
