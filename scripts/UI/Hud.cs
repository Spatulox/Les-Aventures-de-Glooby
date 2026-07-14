using Godot;

// HUD minimal : cœurs de vie (icône unique, teinte grisée pour les PV manquants
// afin d'économiser une génération) + compteur de poissons.
public partial class Hud : CanvasLayer
{
	private HBoxContainer _coeurs;
	private Label _labelPoissons;
	private Control _manaGlace;
	private ColorRect _manaRemplissage;
	private float _manaLargeurMax;

	public override void _Ready()
	{
		// Autoload persistant : masqué par défaut, il ne s'affiche qu'en jeu
		// (MenuPause.Afficher à l'entrée du monde, MenuPrincipal.Masquer au menu).
		Visible = false;

		_coeurs = GetNode<HBoxContainer>("Coeurs");
		_labelPoissons = GetNode<Label>("Poissons/LabelPoissons");
		_manaGlace = GetNode<Control>("ManaGlace");
		_manaRemplissage = GetNode<ColorRect>("ManaGlace/Remplissage");
		_manaLargeurMax = _manaRemplissage.Size.X;

		var etat = GameState.Instance;
		ConstruirePv(etat.Pv, etat.PvMax);
		MettreAJourPoissons(etat.Poissons);

		// La jauge de mana ne s'affiche qu'une fois le pouvoir de glace débloqué.
		_manaGlace.Visible = etat.PouvoirGlaceActif;
		MettreAJourManaGlace(etat.ManaGlace, etat.ManaGlaceMax);

		etat.PvChanges += OnPvChanges;
		etat.PoissonsChanges += OnPoissonsChanges;
		etat.PouvoirGlaceObtenu += OnPouvoirGlaceObtenu;
		etat.ManaGlaceChanges += MettreAJourManaGlace;
	}

	private void OnPouvoirGlaceObtenu() => _manaGlace.Visible = true;

	private void MettreAJourManaGlace(float mana, float max)
	{
		float ratio = max > 0f ? Mathf.Clamp(mana / max, 0f, 1f) : 0f;
		_manaRemplissage.Size = new Vector2(_manaLargeurMax * ratio, _manaRemplissage.Size.Y);
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

	public void Afficher() => Visible = true;

	public void Masquer() => Visible = false;
}
