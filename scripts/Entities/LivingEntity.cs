using Godot;

// Base commune à toutes les entités vivantes du jeu (joueur, boss, futurs PNJ).
// Mutualise ce que partage tout ce qui « vit » : des PV avec encaissement de dégâts
// (via Damageable) et des aides de déplacement (gravité, friction, saut). Le contrôle
// concret (IA, entrées) et le contenu (animations, valeurs) restent aux sous-classes.
public abstract partial class LivingEntity : CharacterBody2D, Damageable
{
	// ---- Déplacement (réglages + aides partagés) ----
	[Export] public float Gravity = 1200f;
	[Export] public float MaxFallSpeed = 900f;
	[Export] public float Friction = 1400f;
	[Export] public float JumpVelocity = -420f;

	// Applique la gravité en plafonnant la vitesse de chute.
	protected void AppliquerGravite(ref Vector2 velocite, float dt)
		=> velocite.Y = Mathf.Min(velocite.Y + Gravity * dt, MaxFallSpeed);

	// Ramène la vitesse horizontale vers 0 (facteur < 1 sur sol glissant, 1 = normal).
	protected void AppliquerFriction(ref Vector2 velocite, float dt, float facteur = 1f)
		=> velocite.X = Mathf.MoveToward(velocite.X, 0f, Friction * facteur * dt);

	// Impulsion de saut (vers le haut).
	protected void Sauter(ref Vector2 velocite) => velocite.Y = JumpVelocity;

	// ---- PV & dégâts (Damageable) ----
	[Signal] public delegate void PvChangesEventHandler(int pv, int pvMax);
	[Signal] public delegate void VaincuEventHandler();

	[Export] public int PvMax = 1;

	public int Pv { get; protected set; }
	public bool EstVaincu { get; protected set; }

	// (Re)définit les PV max et remet à pleine vie ; émet PvChanges (rafraîchit une barre).
	public void DefinirPvMax(int pvMax)
	{
		PvMax = Mathf.Max(1, pvMax);
		Pv = PvMax;
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
	}

	// Insensible aux dégâts une fois vaincue. Surchargeable (ex. invincibilité du joueur).
	public virtual bool IsInvincibleToDamage(DamageSource source) => EstVaincu;

	// Damageable : unique point d'encaissement. Applique les hooks, met à jour les PV et
	// déclenche la mort. Surchargeable (ex. le joueur route vers GameState avec un recul).
	public virtual void TakeDamage(DamageSource source)
	{
		if (EstVaincu)
			return;

		int total = Mathf.Max(0, AjusterDegats(source.MontantDegats()));
		Pv = Mathf.Max(0, Pv - total);
		EmitSignal(SignalName.PvChanges, Pv, PvMax);

		if (Pv <= 0)
		{
			Mourir();
			return;
		}

		ApresDegats(total);
	}

	// Modifie les dégâts encaissés (ex. ×N en état vulnérable). Par défaut, aucun bonus.
	protected virtual int AjusterDegats(int brut) => brut;

	// Réaction après un coup non fatal (ex. transition de phase). Par défaut, rien.
	protected virtual void ApresDegats(int degats) { }

	// Séquence de mort générique : marque l'entité vaincue, la stoppe et signale la
	// défaite. Les sous-classes enrichissent (animation, désactivation de collision...).
	protected virtual void Mourir()
	{
		EstVaincu = true;
		Velocity = Vector2.Zero;
		EmitSignal(SignalName.Vaincu);
	}
}
