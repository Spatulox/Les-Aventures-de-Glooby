using System.Collections.Generic;
using Godot;

// Contrat d'interaction pour tout élément « parlant » (PNJ, panneau...). Le moteur
// réutilisable DeclencheurDialogue s'appuie dessus pour afficher une bulle au-dessus
// du model 2D et faire défiler les lignes, sans connaître le type concret de la cible.
public interface Talkative
{
	// Lignes affichées successivement, une par appui sur la touche d'action.
	IReadOnlyList<string> Dialogue { get; }

	// Point d'ancrage MONDE au-dessus duquel dessiner la bulle (souvent la tête).
	Vector2 PointBulle { get; }

	// Vrai : le dialogue démarre automatiquement au passage du joueur. Faux : il
	// démarre sur la touche d'action quand le joueur est proche (un rappel de touche
	// s'affiche par défaut pour signaler qu'on peut parler).
	bool DeclencheAuPassage { get; }

	// Verrou optionnel : peut-on (re)parler maintenant ? (ex. dialogue à usage unique).
	bool PeutParler();

	// Hooks début/fin de conversation (regarder le joueur, marquer comme lu...).
	void SurDebutDialogue();
	void SurFinDialogue();
}
