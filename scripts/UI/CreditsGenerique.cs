using Godot;
using Godot.Collections;

// Tout le contenu du générique de fin, réuni dans UNE ressource éditable à la
// souris (assets/credits/generique.tres) : le titre, la liste des rôles et leurs
// noms, le mot de la fin, plus le réglage du défilement et des tailles de texte.
//
// Le but est qu'ajouter quelqu'un aux crédits soit une manipulation d'inspecteur
// (Entrees → +) et jamais une modification de C# ni de scène : EcranFin se
// contente de dérouler ce que cette ressource contient.
[GlobalClass]
public partial class CreditsGenerique : Resource
{
	// Titre affiché en tête du générique (le nom du jeu).
	[Export] public string Titre = "";

	// Les rôles, dans l'ordre d'apparition à l'écran.
	[Export] public Array<EntreeCredits> Entrees = new();

	// Mot de la fin, affiché après les rôles. Peut tenir sur plusieurs lignes :
	// EcranFin le fait passer à la ligne tout seul s'il déborde.
	[Export(PropertyHint.MultilineText)] public string Remerciements = "";

	// Vitesse de montée du texte, en pixels du canvas 640x360 par seconde. Plus
	// bas = plus lent à lire. 25 correspond à environ 15 secondes de générique.
	[Export(PropertyHint.Range, "5,150,1")] public float VitesseDefilement = 25f;

	// Tailles de police des trois niveaux de texte. Elles sont exprimées dans le
	// canvas 640x360 (agrandi ensuite en entier), donc de petites valeurs
	// suffisent : au-delà d'une vingtaine de pixels un nom long déborde.
	[Export] public int TailleTitre = 32;
	[Export] public int TailleCategorie = 16;
	[Export] public int TailleNom = 12;

	// Blanc laissé entre deux rôles, pour que les blocs se lisent séparément.
	[Export] public float EspaceEntreBlocs = 18f;
}
