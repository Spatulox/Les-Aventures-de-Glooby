// Boss dont le déplacement est borné par son arène : il ne doit jamais sortir du
// rectangle dessiné dans l'éditeur (charge du Cerf, marche du Mecha, téléportation du
// Père Noël). Implémenté par chaque boss concerné, il est posé génériquement par
// ZoneBoss.ConfigurerBoss.
//
// C'est ce qui permet à UNE arène d'héberger DEUX boss de classes différentes (fin
// normale / fin cachée) et de les borner tous les deux : sans ce contrat, la zone
// devrait connaître le type exact du boss, et seul celui du cast était borné —
// l'autre gardait ses valeurs par défaut et débordait du décor.
public interface BossBorne
{
	float LimiteGauche { set; }
	float LimiteDroite { set; }
}
