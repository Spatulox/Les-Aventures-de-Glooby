using Godot;
using Godot.Collections;

// Une variante d'ambiance sonore, c'est-à-dire le son d'un lieu DANS UN ÉTAT
// donné (beau temps, blizzard...). Deux canaux indépendants : la musique
// (playlist tirée au sort, cf. GestionnaireAudio) et l'ambiance de fond (vent,
// gouttes) qui, elle, boucle.
//
// Laisser Musiques VIDE est un cas utile, pas un oubli : une variante blizzard
// qui ne renseigne que Ambiances laisse la musique normale continuer sans
// coupure et ne change que le lit sonore - c'est le moins cher à produire et
// souvent le plus juste (voir la règle de BasculerCanal).
[GlobalClass]
public partial class VarianteAmbiance : Resource
{
	// Nom de l'état auquel cette variante répond. Vocabulaire commun avec la
	// météo : "normal" ou "blizzard".
	[Export] public string Etat = "normal";

	// Playlist musicale : une piste est tirée au sort, et une autre lui succède
	// à la fin du morceau. Importer ces .ogg en loop = false.
	[Export] public Array<AudioStream> Musiques = new();

	// Lit sonore de fond, joué en continu. Importer ces .ogg en loop = true.
	[Export] public Array<AudioStream> Ambiances = new();

	[Export] public float VolumeMusiqueDb;
	[Export] public float VolumeAmbianceDb;
}
