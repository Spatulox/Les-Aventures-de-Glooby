using Godot;

// Salle de boss : Area2D couvrant l'arène d'un boss. La barre de vie reste
// masquée tant que le joueur n'y est pas entré, puis s'affiche à l'entrée.
// Réutilisable pour tout boss : on lui confie simplement la barre à révéler
// (point d'extension : combat, musique, portes... peuvent se brancher ici).
public partial class ZoneBoss : DeclencheurZone
{
	// Barre à révéler quand le joueur pénètre dans l'arène. À fixer AVANT
	// l'ajout dans l'arbre (cf. Outils : _Ready lit la valeur immédiatement).
	public BossHudBarre Barre;

	protected override bool PreparerDeclencheur()
	{
		Barre?.Masquer();
		return true;
	}

	protected override void SurEntreeJoueur(Player joueur)
	{
		Barre?.Afficher();
	}
}
