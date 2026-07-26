using System.Collections.Generic;
using Godot;
using Godot.Collections;

// Une ÉTAPE de conversation : ce que le PNJ dit, puis les réponses proposées au
// joueur. Chaque choix pointe (ou non) vers le nœud suivant, si bien qu'un arbre
// de dialogue entier tient dans un seul .tres — même patron que AmbianceSonore et
// ses VarianteAmbiance, et même endroit d'édition (dock FileSystem, inspecteur).
//
// Les fichiers vivent dans assets/dialogues/ et sont nommés « <lieu>_<pnj>.tres »
// (ex. banquise_fin_lutin_cgt.tres) : le nom dit OÙ se trouve le PNJ, pour
// retrouver la bonne conversation sans ouvrir monde1.tscn.
//
// RÈGLE D'ÉCRITURE : pendant les choix le joueur est figé (dialogue modal). Un nœud
// SANS choix referme donc la conversation (c'est la fin naturelle d'une branche), et
// un nœud QUI en propose doit toujours en garder un qui termine (Suite vide) — sinon
// le joueur n'a pas de porte de sortie. Un nœud dont tous les choix sont épuisés
// referme aussi, par sécurité. Éviter enfin les cycles (a → b → a) : une
// sous-ressource cyclique ne se sérialise pas dans un .tres (un choix peut en
// revanche être partagé par plusieurs nœuds, tant que le graphe reste acyclique).
[GlobalClass]
public partial class NoeudDialogue : Resource
{
	// Ce que dit le PNJ AVANT d'ouvrir la liste. Ce n'est PAS forcément le texte
	// affiché : quand le PNJ est en dialogue dynamique, ces lignes servent
	// d'INTENTION au modèle (« fais passer cette idée avec tes mots »), et ne sont
	// jouées telles quelles qu'en repli — Ollama désactivé, indisponible ou en
	// échec. Laisser vide = le PNJ improvise à partir de son seul contexte.
	[Export] public string[] Repliques = System.Array.Empty<string>();

	// Les réponses proposées au joueur. Vide = le nœud se contente de parler puis
	// referme la conversation.
	[Export] public Array<ChoixDialogue> Choix = new();

	// Les choix réellement proposables maintenant (les usages uniques déjà retenus
	// sont écartés). Le moteur ne manipule que cette liste filtrée.
	public List<ChoixDialogue> ChoixDisponibles()
	{
		var disponibles = new List<ChoixDialogue>();
		foreach (var choix in Choix)
		{
			if (choix != null && choix.EstDisponible())
				disponibles.Add(choix);
		}
		return disponibles;
	}
}
