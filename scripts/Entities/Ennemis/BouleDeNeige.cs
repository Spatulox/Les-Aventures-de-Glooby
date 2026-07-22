using Godot;

// Boule de neige lancée par le Bonhomme de neige : même base animée que celle du
// joueur (vol + éclat de neige à l'impact, via BouleDeNeigeProjectile), avec une
// trajectoire en cloche (surcharge Initialiser à vecteur vitesse : la boule monte
// puis retombe) et des dégâts de contact de méchant.
public partial class BouleDeNeige : BouleDeNeigeProjectile
{
	// Blesse le joueur comme le contact d'un méchant (1 PV).
	protected override DamageSource Source => DamageSource.ContactMechant;
}
