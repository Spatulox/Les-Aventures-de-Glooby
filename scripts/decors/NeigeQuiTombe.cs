using Godot;

// Neige qui tombe : effet d'ambiance instanciable dans n'importe quel niveau
// extérieur. Trois GpuParticles2D (un par texture de flocon, car un émetteur
// n'accepte qu'une texture) sont construits et réglés depuis les [Export] :
// le grain domine le fond, le flocon simple au milieu, l'ornementé rare et
// plus gros au premier plan. Dérive latérale douce, tailles et vitesses
// variées, rotation lente. Poser le nœud là où couvrir l'écran (souvent
// enfant de la caméra, comme la poussière ambiante).
public partial class NeigeQuiTombe : Node2D
{
	// Densité globale : multiplie le nombre de particules des trois couches.
	[Export] public float Densite = 1.0f;
	// Largeur de la bande d'émission (au-dessus de l'écran). Hauteur de chute.
	[Export] public float LargeurEmission = 800f;
	[Export] public float HauteurChute = 460f;
	// Vitesse de chute (px/s) : bornes min/max pour varier les flocons.
	[Export] public float VitesseMin = 30f;
	[Export] public float VitesseMax = 70f;
	// Dérive latérale (vent) : décalage horizontal moyen pendant la chute.
	[Export] public float DeriveLaterale = 25f;

	private record Couche(string Texture, int Base, float EchelleMin, float EchelleMax, float FacteurVitesse);

	// grain nombreux et lent (fond) -> ornementé rare, gros et rapide (premier plan)
	private static readonly Couche[] Couches =
	{
		new("res://assets/props/flocons/flocon_grain.png", 90, 0.6f, 1.1f, 0.75f),
		new("res://assets/props/flocons/flocon_simple.png", 45, 0.8f, 1.3f, 1.0f),
		new("res://assets/props/flocons/flocon_ornemente.png", 18, 1.0f, 1.6f, 1.25f),
	};

	public override void _Ready()
	{
		foreach (var couche in Couches)
			AjouterEmetteur(couche);
	}

	private void AjouterEmetteur(Couche couche)
	{
		var mat = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(LargeurEmission / 2f, 4f, 1f),
			Direction = new Vector3(0, 1, 0),
			Spread = 8f,
			// Chute + vent latéral (composante X = dérive constante).
			Gravity = new Vector3(DeriveLaterale * couche.FacteurVitesse, 12f, 0f),
			InitialVelocityMin = VitesseMin * couche.FacteurVitesse,
			InitialVelocityMax = VitesseMax * couche.FacteurVitesse,
			ScaleMin = couche.EchelleMin,
			ScaleMax = couche.EchelleMax,
			// Orientation de départ et rotation lente aléatoires.
			AngleMin = 0f,
			AngleMax = 360f,
			AngularVelocityMin = -40f,
			AngularVelocityMax = 40f,
			ParticleFlagDisableZ = true,
		};

		var particules = new GpuParticles2D
		{
			Amount = Mathf.Max(1, (int)(couche.Base * Densite)),
			Texture = GD.Load<Texture2D>(couche.Texture),
			ProcessMaterial = mat,
			Lifetime = HauteurChute / Mathf.Max(1f, VitesseMin * couche.FacteurVitesse) + 2f,
			Preprocess = 6f,
			LocalCoords = false,
			Position = new Vector2(0, -HauteurChute / 2f),
		};
		AddChild(particules);
	}
}
