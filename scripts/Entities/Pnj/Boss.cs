using Godot;
using System.Collections.Generic;

// Base commune à tous les boss (CharacterBody2D). Gère le générique — PV, signaux,
// encaissement des dégâts, séquence de mort et chargement d'animations par dossier —
// via des hooks. L'IA concrète, les patterns et le contenu (animations, nombres de
// PV/dégâts) sont fournis par chaque sous-classe (ex. BossCerf).
public abstract partial class Boss : CharacterBody2D, Damageable
{
	[Signal] public delegate void PvChangesEventHandler(int pv, int pvMax);
	[Signal] public delegate void VaincuEventHandler();

	[Export] public int PvMax = 1;

	public int Pv { get; protected set; }
	public bool EstVaincu { get; private set; }

	protected AnimatedSprite2D Sprite;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Sprite.SpriteFrames = ConstruireAnimations();
		Pv = PvMax;
		Initialiser();
	}

	// (Re)définit les PV max et remet le boss à pleine vie. Sert à une ZoneBoss qui
	// arme le combat à l'entrée du joueur ; émet PvChanges pour rafraîchir la barre.
	public void DefinirPvMax(int pvMax)
	{
		PvMax = Mathf.Max(1, pvMax);
		Pv = PvMax;
		EmitSignal(SignalName.PvChanges, Pv, PvMax);
	}

	// Implémentation de Damageable : traduit la source en montant de dégâts.
	public void TakeDamage(DamageSource source) => SubirDegats(source.MontantDegats());

	// Un boss vaincu n'encaisse plus aucun dégât, quelle qu'en soit la source.
	public bool IsInvincibleToDamage(DamageSource source) => EstVaincu;

	public void SubirDegats(int quantite)
	{
		if (EstVaincu)
			return;

		int total = Mathf.Max(0, AjusterDegats(quantite));
		Pv = Mathf.Max(0, Pv - total);
		EmitSignal(SignalName.PvChanges, Pv, PvMax);

		if (Pv <= 0)
		{
			Mourir();
			return;
		}

		ApresDegats(total);
	}

	// Hook d'init des sous-classes (récupération de nœuds, RNG, état/anim de départ...).
	protected virtual void Initialiser() { }

	// Chaque boss fournit son jeu d'animations (doit inclure AnimationMort).
	protected abstract SpriteFrames ConstruireAnimations();

	// Nom de l'animation jouée à la mort (surchargeable).
	protected virtual string AnimationMort => "vaincu";

	// Modifie les dégâts encaissés (ex. ×N en état vulnérable). Par défaut, aucun bonus.
	protected virtual int AjusterDegats(int brut) => brut;

	// Réaction après un coup non fatal (ex. transition de phase). Par défaut, rien.
	protected virtual void ApresDegats(int degats) { }

	// Séquence de mort générique : fige le boss, joue l'anim de mort, coupe la
	// physique et la collision, puis signale la défaite.
	protected virtual void Mourir()
	{
		EstVaincu = true;
		Velocity = Vector2.Zero;
		Sprite.Play(AnimationMort);
		SetPhysicsProcess(false);
		GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		EmitSignal(SignalName.Vaincu);
	}

	// Helper générique : ajoute à un SpriteFrames une animation depuis un dossier de
	// PNG (triés par nom). Réutilisable par tous les boss.
	protected static void AjouterAnimation(SpriteFrames frames, string nom, string dossier, float fps, bool boucle)
	{
		frames.AddAnimation(nom);
		frames.SetAnimationSpeed(nom, fps);
		frames.SetAnimationLoop(nom, boucle);

		var fichiers = new List<string>();
		foreach (var fichier in DirAccess.GetFilesAt(dossier))
		{
			if (fichier.EndsWith(".png"))
				fichiers.Add(fichier);
		}
		fichiers.Sort();

		foreach (var fichier in fichiers)
			frames.AddFrame(nom, GD.Load<Texture2D>($"{dossier}/{fichier}"));
	}
}
