using Godot;

// Boule de neige : projectile du joueur qui éclate (fondu + agrandissement) au contact,
// plutôt que d'utiliser des frames dédiées.
public partial class Snowball : Projectile
{
	protected override DamageSource Source => DamageSource.Snowball;

	// Éclatement visuel : fondu + léger agrandissement (Effets.Disparaitre libère le nœud).
	protected override void Disparaitre() => Effets.Disparaitre(this, Scale * 1.6f, 0.12f);
}
