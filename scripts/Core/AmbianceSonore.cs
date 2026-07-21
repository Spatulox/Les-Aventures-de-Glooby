using Godot;
using Godot.Collections;

// Le son d'UN lieu, toutes météos confondues : un nom (la clé demandée par les
// zones, ex. "village") et ses variantes d'état.
//
// La clé sonore est volontairement DISTINCTE de la région visuelle : le village
// et la banquise partagent le fond "banquise" mais n'ont pas la même musique.
// C'est CameraZone.NomAmbiance qui porte cette clé, avec repli sur NomRegion.
[GlobalClass]
public partial class AmbianceSonore : Resource
{
	public const string EtatNormal = "normal";

	[Export] public string Nom = "";

	[Export] public Array<VarianteAmbiance> Variantes = new();

	// Variante correspondant à l'état demandé, avec REPLI sur "normal" : c'est ce
	// repli qui permet qu'un blizzard se déclenche n'importe où sans obliger
	// chaque ambiance à définir sa variante tempête. Retourne null si le lieu n'a
	// même pas de variante normale (ambiance muette).
	public VarianteAmbiance Trouver(string etat)
	{
		VarianteAmbiance normale = null;

		foreach (var variante in Variantes)
		{
			if (variante == null)
				continue;
			if (variante.Etat == etat)
				return variante;
			if (variante.Etat == EtatNormal)
				normale = variante;
		}

		return normale;
	}
}
