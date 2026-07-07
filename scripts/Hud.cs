using Godot;

// HUD minimal : cœurs de vie (icône unique, teinte grisée pour les PV manquants
// afin d'économiser une génération) + compteur de poissons.
public partial class Hud : CanvasLayer
{
	private HBoxContainer _coeurs;
	private Label _labelPoissons;

	public override void _Ready()
	{
		_coeurs = GetNode<HBoxContainer>("Coeurs");
		_labelPoissons = GetNode<Label>("Poissons/LabelPoissons");

		var etat = GameState.Instance;
		ConstruirePv(etat.Pv, etat.PvMax);
		MettreAJourPoissons(etat.Poissons);

		etat.PvChanges += OnPvChanges;
		etat.PoissonsChanges += OnPoissonsChanges;
	}

	private void OnPvChanges(int pv, int pvMax)
	{
		if (_coeurs.GetChildCount() != pvMax)
			ConstruirePv(pv, pvMax);
		else
			MettreAJourPv(pv);
	}

	private void OnPoissonsChanges(int total) => MettreAJourPoissons(total);

	private void ConstruirePv(int pv, int pvMax)
	{
		foreach (var enfant in _coeurs.GetChildren())
			enfant.QueueFree();

		var texture = GD.Load<Texture2D>("res://assets/ui/coeur.png");
		for (int i = 0; i < pvMax; i++)
		{
			var coeur = new TextureRect
			{
				Texture = texture,
				CustomMinimumSize = new Vector2(24, 24),
				ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional
			};
			_coeurs.AddChild(coeur);
		}

		MettreAJourPv(pv);
	}

	private void MettreAJourPv(int pv)
	{
		var enfants = _coeurs.GetChildren();
		for (int i = 0; i < enfants.Count; i++)
		{
			if (enfants[i] is TextureRect coeur)
				coeur.Modulate = i < pv ? Colors.White : new Color(0.25f, 0.25f, 0.3f);
		}
	}

	private void MettreAJourPoissons(int total) => _labelPoissons.Text = $"x {total}";
}
