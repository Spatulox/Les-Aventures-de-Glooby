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

	// ---- Collisions ----
	// Applique la convention de collision d'un PNJ : il voit le terrain et les
	// plateformes traversables, mais ni le joueur ni les autres PNJ (personne ne
	// masque les layers joueur/PNJ). Posé ici, en code, plutôt que laissé à chaque
	// scène : un nouveau PNJ est correct même si son .tscn est mal réglé, et
	// l'invariant « aucune collision entre PNJ » ne peut plus se perdre en route.
	protected void AppliquerCollisionsPnj()
	{
		CollisionLayer = Constantes.LayerPnj;
		CollisionMask = Constantes.MasqueMarcheur;
	}

	// ---- Aperçu éditeur ----
	// Chaque scène d'entité porte un Sprite2D « Apercu » figé sur la 1re frame de son idle,
	// uniquement pour que l'entité soit visible/positionnable dans l'éditeur Godot. En jeu,
	// c'est l'AnimatedSprite2D qui rend : on masque donc l'aperçu au démarrage (facultatif,
	// une scène sans nœud « Apercu » est simplement ignorée). La règle est partagée avec les
	// projectiles, d'où le helper commun ApercuEditeur.
	protected void MasquerApercuEditeur() => ApercuEditeur.Masquer(this);

	// ---- Détection du joueur (portée) ----
	// Joueur actuellement présent dans la ZoneDetection (Area2D enfant facultative), ou null.
	private Player _joueurDansZone;

	// Vrai si la scène porte une Area2D « ZoneDetection » câblée : la portée est alors pilotée
	// par la taille de sa CollisionShape2D (réglable par instance) et non par une distance codée.
	protected bool ZoneDetectionPresente { get; private set; }

	// Câble la zone de détection facultative « ZoneDetection » : au chevauchement du joueur, il
	// devient la cible « à portée » ; à sa sortie, la portée redevient vide. Sans nœud
	// « ZoneDetection », ne fait rien : la sous-classe retombe sur la distance flottante (repli).
	protected void CablerZoneDetection(string nom = "ZoneDetection")
	{
		var zone = GetNodeOrNull<Area2D>(nom);
		if (zone == null)
			return;

		ZoneDetectionPresente = true;
		zone.BodyEntered += corps => { if (corps is Player j) _joueurDansZone = j; };
		zone.BodyExited += corps => { if (corps == _joueurDansZone) _joueurDansZone = null; };
	}

	// Joueur « à portée » : si la scène fournit une ZoneDetection, c'est le joueur présent dans la
	// zone (distance 0) ou null (distance MaxValue) ; sinon, le joueur le plus proche à sa distance
	// réelle. Contrat : distance == 0 signifie « dans la zone », MaxValue « hors de portée » — les
	// IA gardent ainsi leur test « joueur == null || distance > Portee » sans le moindre changement.
	protected Player JoueurAPortee(out float distance)
	{
		if (ZoneDetectionPresente)
		{
			distance = _joueurDansZone != null ? 0f : float.MaxValue;
			return _joueurDansZone;
		}
		return JoueurLePlusProche(out distance);
	}

	// Renvoie le joueur le plus proche (groupe « joueur ») et sa distance, ou null s'il n'y en a pas.
	protected Player JoueurLePlusProche(out float distance)
	{
		distance = float.MaxValue;
		Player plusProche = null;
		foreach (var noeud in GetTree().GetNodesInGroup("joueur"))
		{
			if (noeud is not Player joueur)
				continue;
			float d = GlobalPosition.DistanceTo(joueur.GlobalPosition);
			if (d < distance)
			{
				distance = d;
				plusProche = joueur;
			}
		}
		return plusProche;
	}

	// ---- PV & dégâts (Damageable) ----
	[Signal] public delegate void PvChangesEventHandler(int pv, int pvMax);
	[Signal] public delegate void VaincuEventHandler();

	// Les PV d'entité se comptent au TIERS DE POINT (voir DamageSourceExtensions) : 3 est
	// donc le « une seule vie » de référence, celui que la boule de neige abat d'un coup.
	[Export] public int PvMax = 3;

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

		// Option de test « ennemis tués en un coup » : tout coup porté par le joueur
		// emporte la cible d'un seul impact, quels que soient ses PV. Le joueur n'est pas
		// concerné : il surcharge TakeDamage pour router vers GameState, et ne s'inflige
		// de toute façon aucune source « du joueur ».
		bool debug = GameState.Instance?.OptionDebugActive(CatalogueOptionsDebug.OneShot) == true
			&& source.EstDuJoueur();
		int total = debug ? PvMax : Mathf.Max(0, AjusterDegats(source.MontantDegats()));
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
