using Godot;
using System.Collections.Generic;

// Ligne de sol de banquise : instancie automatiquement embout gauche +
// N segments centraux (variantes A/B/C alternées pour casser la répétition)
// + embout droit. Poser le nœud à l'endroit voulu et régler NombreSegments
// dans l'inspecteur ; la surface de marche est à y = -50 en local.
// Chaque type a sa propre scène (scenes/sol/SolBanquiseXxx.tscn) : le visuel y
// est figé, donc juste dans l'éditeur comme au runtime. On instancie la scène
// du type voulu plutôt que d'écraser un export après coup.
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

	private static readonly Dictionary<SolBanquise.TypeSegment, string> Scenes = new()
	{
		[SolBanquise.TypeSegment.CentreA] = "res://scenes/sol/SolBanquiseCentreA.tscn",
		[SolBanquise.TypeSegment.CentreB] = "res://scenes/sol/SolBanquiseCentreB.tscn",
		[SolBanquise.TypeSegment.CentreC] = "res://scenes/sol/SolBanquiseCentreC.tscn",
		[SolBanquise.TypeSegment.EmboutGauche] = "res://scenes/sol/SolBanquiseEmboutGauche.tscn",
		[SolBanquise.TypeSegment.EmboutDroit] = "res://scenes/sol/SolBanquiseEmboutDroit.tscn",
	};

	public override void _Ready()
	{
		float x = 0f;

		if (AvecEmbouts)
		{
			AjouterSegment(SolBanquise.TypeSegment.EmboutGauche, x);
			x += SolBanquise.LargeurSegment;
		}

		for (int i = 0; i < NombreSegments; i++)
		{
			AjouterSegment(Alternance[i % Alternance.Length], x);
			x += SolBanquise.LargeurSegment;
		}

		if (AvecEmbouts)
			AjouterSegment(SolBanquise.TypeSegment.EmboutDroit, x);
	}

	private void AjouterSegment(SolBanquise.TypeSegment type, float x)
	{
		var segment = GD.Load<PackedScene>(Scenes[type]).Instantiate<SolBanquise>();
		segment.Position = new Vector2(x, 0);
		AddChild(segment);
	}
}
