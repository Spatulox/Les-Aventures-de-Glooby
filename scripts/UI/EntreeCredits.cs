using Godot;

// Un bloc du générique de fin : un rôle et les personnes (ou outils) qui l'ont
// tenu. C'est l'unité que l'on ajoute/retire dans l'inspecteur pour changer les
// crédits sans toucher au code — voir CreditsGenerique.
[GlobalClass]
public partial class EntreeCredits : Resource
{
	// Intitulé du rôle, affiché au-dessus des noms (ex. "Développement").
	[Export] public string Categorie = "";

	// Les noms rattachés à ce rôle, un par ligne à l'écran. Une entrée sans nom
	// reste valable : elle sert alors de simple intertitre.
	[Export] public string[] Noms = System.Array.Empty<string>();
}
