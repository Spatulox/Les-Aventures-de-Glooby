// Contrat des entités que le joueur peut ÉTOURDIR (typiquement avec une boule de neige) :
// au lieu de perdre des PV, la cible fige son comportement le temps indiqué. Etourdir(duree)
// est appelé depuis la surcharge TakeDamage de chaque ennemi concerné (bonhomme de neige,
// ours de neige...) là où une DamageSource.Snowball est reçue.
public interface Etourdissable
{
	// Étourdit la cible pendant `duree` secondes.
	void Etourdir(float duree);
}
