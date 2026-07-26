using Godot;

// UNE réponse possible du joueur, avec tout ce qu'elle entraîne : ce que Glooby dit,
// ce que le PNJ répond, la suite de la conversation et la trace qu'elle laisse dans
// la partie. Mettre les quatre sur la MÊME entrée évite les tableaux parallèles
// (une liste de choix + une liste de réponses) qu'il faudrait garder alignés — même
// raisonnement que PisteMusicale (musique + probabilité ensemble).
//
// C'est l'unité d'un NoeudDialogue.Choix, éditée dans l'inspecteur depuis le .tres
// du PNJ (assets/dialogues/) : aucun code à toucher pour écrire une conversation.
[GlobalClass]
public partial class ChoixDialogue : Resource
{
	// La réplique du JOUEUR, telle qu'elle s'affiche dans la liste. Garder court :
	// la bulle de choix se dessine au-dessus de Glooby.
	[Export(PropertyHint.MultilineText)] public string Texte = "";

	// Ce que le PNJ répond. Même règle que NoeudDialogue.Repliques : en dialogue
	// dynamique, ce texte est l'INTENTION donnée au modèle (qui a aussi la réplique
	// choisie par Glooby sous les yeux) et n'est joué tel quel qu'en repli, si l'IA
	// est coupée ou échoue. Laisser vide = le PNJ réagit librement au choix ; vide
	// ET sans IA, on enchaîne directement sur la Suite.
	[Export] public string[] Reponse = System.Array.Empty<string>();

	// Nœud joué après la réponse. Vide = la conversation se termine — c'est la
	// sortie de l'état modal, donc tout nœud doit proposer au moins un choix
	// dont la Suite est vide (voir NoeudDialogue).
	[Export] public NoeudDialogue Suite;

	// Identifiant persistant du choix (unique dans le jeu). Renseigné, il est
	// mémorisé via GameState.MarquerConsomme au moment où le joueur valide, et
	// sert de clé au PNJ dans SurChoixRetenu (ex. « lutin_cgt_don_poissons »).
	[Export] public string IdMemoire = "";

	// Vrai : le choix disparaît de la liste une fois retenu (nécessite IdMemoire).
	[Export] public bool UneSeuleFois;

	// Poissons que ce choix coûte à Glooby (0 = gratuit). Le choix n'est PAS proposé
	// si la réserve est insuffisante — donc une réponse du type « tiens, prends mes
	// 50 poissons » ne peut jamais mentir — et la dépense est faite à la validation.
	// Data-driven exprès : n'importe quel PNJ marchand/quémandeur en profite sans
	// une ligne de code (voir DeclencheurDialogue.ValiderChoix).
	[Export] public int CoutPoissons;

	// Le choix doit-il encore être proposé ? Un choix à usage unique déjà retenu
	// dans cette partie est masqué, comme un choix que le joueur ne peut pas payer.
	public bool EstDisponible()
	{
		if (CoutPoissons > 0 && GameState.Instance.Poissons < CoutPoissons)
			return false;
		if (!UneSeuleFois || string.IsNullOrEmpty(IdMemoire))
			return true;
		return !GameState.Instance.EstConsomme(IdMemoire);
	}
}
