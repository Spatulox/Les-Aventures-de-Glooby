using Godot;

// Guirlande de Noël raccordable (même logique que SolBanquiseLigne, en pur
// décor) : embout gauche + N segments auto-tuilables + embout droit,
// instanciés en _Ready. Deux variantes de couleurs de boules (rouge/or et
// bleu/or, la bleue est une recoloration procédurale de la rouge).
public partial class GuirlandeNoel : Node2D
{
	public enum CouleurBoules { Rouge, Bleue }

	[Export] public int NombreSegments = 3;
	[Export] public bool AvecEmbouts = true;
	[Export] public CouleurBoules Couleur = CouleurBoules.Rouge;

	private const float LargeurSegment = 160f;
	private const float LargeurEmbout = 64f;

	public override void _Ready()
	{
		string suffixe = Couleur == CouleurBoules.Rouge ? "rouge" : "bleue";
		float x = 0f;

		if (AvecEmbouts)
		{
			AjouterSprite($"res://assets/props/noel/guirlande_embout_gauche_{suffixe}.png", x, LargeurEmbout);
			x += LargeurEmbout;
		}

		for (int i = 0; i < NombreSegments; i++)
		{
			AjouterSprite($"res://assets/props/noel/guirlande_segment_{suffixe}.png", x, LargeurSegment);
			x += LargeurSegment;
		}

		if (AvecEmbouts)
			AjouterSprite($"res://assets/props/noel/guirlande_embout_droit_{suffixe}.png", x, LargeurEmbout);
	}

	private void AjouterSprite(string chemin, float x, float largeur)
	{
		AddChild(new Sprite2D
		{
			Texture = GD.Load<Texture2D>(chemin),
			Centered = false,
			Position = new Vector2(x, 0),
			ZIndex = -1,
		});
	}
}
