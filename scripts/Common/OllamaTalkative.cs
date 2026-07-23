// Contrat des éléments « parlants » dont la réplique est générée à la volée par un LLM
// local (Ollama), plutôt que piochée dans des Lignes statiques. Étend Talkative : si le
// dialogue dynamique n'est pas actif (opt-in décoché OU Ollama indisponible), le moteur
// DeclencheurDialogue retombe automatiquement sur le chemin statique via Dialogue.
public interface OllamaTalkative : Talkative
{
	// Vrai seulement si l'élément a activé le mode dynamique ET qu'Ollama est prêt à
	// générer. Faux ⇒ repli silencieux sur les Lignes statiques (aucune erreur bloquante).
	bool DialogueDynamiqueActif { get; }

	// Contexte/personnalité PROPRE à cet élément (rôle, ton, marotte…). Il est combiné
	// au contexte global partagé (nom du joueur, univers) par OllamaService.ConstruireContexte.
	string Contexte { get; }

	// Amorce fixe donnée au modèle (ex. « Salue Glooby en une phrase. ») : le joueur ne
	// saisit pas de texte, l'invite lance simplement la génération.
	string Invite { get; }
}
