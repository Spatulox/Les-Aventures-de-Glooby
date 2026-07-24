using Godot;

// Une piste musicale ET sa probabilité de tirage, éditables ensemble dans
// l'inspecteur. C'est l'unité d'une playlist pondérée (VarianteAmbiance.Musiques) :
// mettre la musique et sa proba sur la MÊME entrée évite les tableaux parallèles
// (une liste de musiques + une liste de probas) qu'il faudrait garder alignés.
//
// La somme des Probabilite d'une playlist est censée faire 100, mais le tirage
// normalise sur la somme réelle (cf. VarianteAmbiance.TirerMusique) : une somme
// différente marche quand même, avec seulement un avertissement.
[GlobalClass]
public partial class PisteMusicale : Resource
{
	// Le morceau joué si cette entrée est tirée. Importer le .ogg/.mp3 en
	// loop = false pour que l'enchaînement de fin de piste fonctionne.
	[Export] public AudioStream Musique;

	// Poids relatif du tirage, en pourcentage (total visé : 100 pour une playlist).
	// Slider 0-100 dans l'inspecteur pour rendre le réglage évident.
	[Export(PropertyHint.Range, "0,100,1")] public float Probabilite = 100f;
}
