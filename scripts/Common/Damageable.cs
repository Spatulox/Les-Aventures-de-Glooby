// Contrat commun aux entités capables de subir des dégâts (boss, joueur...).
// Permet aux sources de dégâts (boule de neige, feu...) d'infliger des dégâts
// sans connaître le type concret de leur cible.
public interface Damageable
{
	// Inflige à la cible les dégâts associés à la source. L'implémentation doit
	// ignorer le coup si elle est actuellement invincible à cette source.
	void TakeDamage(DamageSource source);

	// Indique si la cible est actuellement insensible à cette source de dégâts.
	bool IsInvincibleToDamage(DamageSource source);
}
