// Talkative auquel le joueur peut RÉPONDRE : au lieu de subir un défilement de
// lignes, il choisit sa réplique dans une liste écrite à l'avance (un arbre
// NoeudDialogue/ChoixDialogue, cf. assets/dialogues/). Le moteur DeclencheurDialogue
// détecte ce type et bascule sur le parcours de l'arbre.
//
// Comme TalkativeAutomatique et OllamaTalkative, c'est une extension OPTIONNELLE :
// un Talkative qui ne l'implémente pas — ou dont la Conversation est vide — garde
// exactement le comportement actuel. Les deux mondes se combinent : si un choix
// n'a pas de Reponse écrite et que le PNJ est aussi OllamaTalkative, c'est le LLM
// qui répond à la réplique choisie.
public interface TalkativeAChoix : Talkative
{
	// Racine de l'arbre de dialogue. Null = pas de choix (comportement d'origine).
	// Côté implémentation, exporter la ressource en `Resource` + filtre d'inspecteur
	// (voir PnjAmical) plutôt qu'en `NoeudDialogue` : un export fortement typé casse
	// dans l'éditeur sur les scripts [Tool]. La conversion se fait dans l'implémentation.
	NoeudDialogue Conversation { get; }

	// Hook « le joueur a retenu ce choix » : le moteur l'appelle juste après avoir
	// mémorisé l'IdMemoire et avant d'afficher la réponse. C'est là que le PNJ agit
	// sur le jeu (prendre des poissons, changer de pancarte...) ; la bulle, elle,
	// reste possédée par le moteur.
	void SurChoixRetenu(ChoixDialogue choix);
}
