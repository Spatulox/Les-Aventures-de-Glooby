using Godot;

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

// Point d'entrée unique pour appliquer des dégâts à un nœud quelconque. Centralise
// les règles communes à toutes les sources : une entité amicale (FriendlyLivingEntity)
// n'encaisse jamais rien, et une cible momentanément insensible ignore le coup. Toute
// source de dégâts (boule de neige, chaleur...) doit passer par ici.
public static class Degats
{
	public static void Infliger(Node cible, DamageSource source)
	{
		if (cible is FriendlyLivingEntity)
			return;

		if (cible is Damageable damageable && !damageable.IsInvincibleToDamage(source))
			damageable.TakeDamage(source);
	}
}
