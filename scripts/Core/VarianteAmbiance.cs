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

	// Playlist musicale PONDÉRÉE : chaque entrée porte sa piste ET sa probabilité
	// de tirage (cf. PisteMusicale). Une piste est tirée au sort selon ces poids,
	// et une autre lui succède à la fin du morceau. Importer ces .ogg/.mp3 en
	// loop = false, sinon la fin de piste n'émet jamais Finished.
	[Export] public Array<PisteMusicale> Musiques = new();

	// Lit sonore de fond, joué en continu. Importer ces .ogg en loop = true.
	[Export] public Array<AudioStream> Ambiances = new();

	[Export] public float VolumeMusiqueDb;
	[Export] public float VolumeAmbianceDb;

	// Tire une piste au sort proportionnellement aux Probabilite, en ÉVITANT
	// l'index précédent quand il y a le choix (un tirage strictement aléatoire
	// répète, et ça s'entend). Retourne le flux à jouer et l'index tiré (mémorisé
	// par le canal pour le tirage suivant). Retourne null si la playlist est vide
	// ou ne contient que des entrées sans flux.
	//
	// La somme des probas est censée faire 100 : on NORMALISE sur la somme réelle
	// (donc 90 ou 120 marchent aussi) et on avertit seulement si l'auteur s'en
	// écarte, conformément au choix "normaliser + avertir".
	public AudioStream TirerMusique(int dernierIndex, out int index)
	{
		index = -1;

		// Poids effectifs : une entrée sans flux, ou de proba <= 0, ne peut pas
		// sortir. On exclut aussi l'index précédent tant qu'une autre piste
		// jouable reste disponible.
		float sommeTotale = 0f;
		float sommeEligible = 0f;
		int nbJouables = 0;

		for (int i = 0; i < Musiques.Count; i++)
		{
			var piste = Musiques[i];
			if (piste?.Musique == null || piste.Probabilite <= 0f)
				continue;

			sommeTotale += piste.Probabilite;
			nbJouables++;
			if (i != dernierIndex)
				sommeEligible += piste.Probabilite;
		}

		if (nbJouables == 0)
			return null;

		if (Mathf.Abs(sommeTotale - 100f) > 0.01f)
			GD.PushWarning(
				$"Ambiance '{Etat}' : la somme des probabilités des musiques vaut " +
				$"{sommeTotale}, pas 100. Le tirage normalise sur cette somme.");

		// S'il ne reste qu'une seule piste jouable, on la ressert forcément ;
		// sinon on tire dans les pistes AUTRES que la précédente.
		bool eviterPrecedent = nbJouables > 1 && sommeEligible > 0f;
		float cible = (float)GD.RandRange(0.0, eviterPrecedent ? sommeEligible : sommeTotale);

		float cumul = 0f;
		for (int i = 0; i < Musiques.Count; i++)
		{
			var piste = Musiques[i];
			if (piste?.Musique == null || piste.Probabilite <= 0f)
				continue;
			if (eviterPrecedent && i == dernierIndex)
				continue;

			cumul += piste.Probabilite;
			if (cible <= cumul)
			{
				index = i;
				return piste.Musique;
			}
		}

		// Repli (arrondis flottants) : dernière piste jouable rencontrée.
		for (int i = Musiques.Count - 1; i >= 0; i--)
		{
			var piste = Musiques[i];
			if (piste?.Musique != null && piste.Probabilite > 0f
				&& !(eviterPrecedent && i == dernierIndex))
			{
				index = i;
				return piste.Musique;
			}
		}

		return null;
	}
}
