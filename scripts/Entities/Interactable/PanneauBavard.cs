using System;
using System.Collections.Generic;
using Godot;

// Exemple concret de Talkative : un panneau (ou PNJ) bavard. Il porte seulement son
// contenu — lignes de dialogue, ancrage de la bulle, mode de déclenchement — et délègue
// toute la mécanique d'interaction à un DeclencheurDialogue enfant. Sert de patron pour
// tout élément parlant : implémenter Talkative et ajouter un DeclencheurDialogue en enfant.
public partial class PanneauBavard : Node2D, Talkative
{
	// Répliques affichées l'une après l'autre à chaque appui sur la touche d'action.
	[Export] public string[] Lignes = Array.Empty<string>();

	// Ancrage (local) de la bulle par rapport à l'origine du nœud : au-dessus par défaut.
	[Export] public Vector2 AncrageBulle = new(0f, -40f);

	// Vrai : afficher UNE seule réplique tirée au hasard au lieu de tout faire défiler.
	[Export] public bool Aleatoire { get; set; }

	// Vrai : le dialogue démarre au simple passage du joueur (sinon : sur la touche).
	[Export] public bool AuPassage;

	// Vrai : dialogue à usage unique pour toute la partie (mémorisé via GameState).
	[Export] public bool UneSeuleFois;

	// Identifiant persistant du dialogue (requis si UneSeuleFois ; unique dans le jeu).
	[Export] public string IdDialogue = "";

	public IReadOnlyList<string> Dialogue => Lignes;

	public Vector2 PointBulle => ToGlobal(AncrageBulle);

	public bool DeclencheAuPassage => AuPassage;

	public bool PeutParler()
	{
		if (UneSeuleFois && !string.IsNullOrEmpty(IdDialogue))
			return !GameState.Instance.EstConsomme(IdDialogue);
		return true;
	}

	public void SurDebutDialogue() { }

	public void SurFinDialogue()
	{
		if (UneSeuleFois && !string.IsNullOrEmpty(IdDialogue))
			GameState.Instance.MarquerConsomme(IdDialogue);
	}
}
