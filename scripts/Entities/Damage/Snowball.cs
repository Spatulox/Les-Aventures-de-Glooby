using Godot;

// Boule de neige lancée par le joueur : trajectoire à plat (Projectile) et éclat de
// neige animé à l'impact, partagé avec la boule du bonhomme via BouleDeNeigeProjectile.
public partial class Snowball : BouleDeNeigeProjectile
{
	protected override DamageSource Source => DamageSource.Snowball;
}
