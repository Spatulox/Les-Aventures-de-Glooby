using Godot;

// Ligne de sol de banquise : instancie automatiquement embout gauche +
// N segments centraux (variantes A/B/C alternées pour casser la répétition)
// + embout droit. Poser le nœud à l'endroit voulu et régler NombreSegments
// dans l'inspecteur ; la surface de marche est à y = -50 en local.
public partial class SolBanquiseLigne : Node2D
{
	[Export] public int NombreSegments = 3;
	[Export] public bool AvecEmbouts = true;

	private static readonly SolBanquise.TypeSegment[] Alternance =
	{
		SolBanquise.TypeSegment.CentreA,
		SolBanquise.TypeSegment.CentreB,
		SolBanquise.TypeSegment.CentreC,
	};

	public override void _Ready()
	{
		var scene = GD.Load<PackedScene>("res://scenes/decors/SolBanquise.tscn");
		float x = 0f;

		if (AvecEmbouts)
		{
			AjouterSegment(scene, SolBanquise.TypeSegment.EmboutGauche, x);
			x += SolBanquise.LargeurSegment;
		}

		for (int i = 0; i < NombreSegments; i++)
		{
			AjouterSegment(scene, Alternance[i % Alternance.Length], x);
			x += SolBanquise.LargeurSegment;
		}

		if (AvecEmbouts)
			AjouterSegment(scene, SolBanquise.TypeSegment.EmboutDroit, x);
	}

	private void AjouterSegment(PackedScene scene, SolBanquise.TypeSegment type, float x)
	{
		var segment = scene.Instantiate<SolBanquise>();
		segment.Type = type;
		segment.Position = new Vector2(x, 0);
		AddChild(segment);
	}
}
