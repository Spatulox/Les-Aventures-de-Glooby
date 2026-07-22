using Godot;

// HUD minimal : cœurs de vie (trois états plein/vide dérivés du même sprite pour
// un ancrage stable) + compteur de poissons + barre de mana de glace.
public partial class Hud : CanvasLayer
{
	private HBoxContainer _coeurs;
	private Label _labelPoissons;
	private TextureProgressBar _manaBarre;

	private Texture2D _coeurPlein;
	private Texture2D _coeurVide;

	public override void _Ready()
	{
		// Autoload persistant : masqué par défaut, il ne s'affiche qu'en jeu
		// (MenuPause.Afficher à l'entrée du monde, MenuPrincipal.Masquer au menu).
		Visible = false;

		_coeurPlein = GD.Load<Texture2D>("res://assets/ui/hud/coeur_plein.png");
		_coeurVide = GD.Load<Texture2D>("res://assets/ui/hud/coeur_vide.png");

		_coeurs = GetNode<HBoxContainer>("Coeurs");
		_labelPoissons = GetNode<Label>("Poissons/LabelPoissons");

		// Barre de mana : contour arrondi (texture_over) + remplissage cyan
		// (texture_progress) + piste sombre (texture_under teintée). La valeur 0..100
		// pilote le remplissage gauche→droite ; pas de calcul de largeur à la main.
		// Barre PixelLab (outil UI) : textures + nine-patch réglés dans hud.tscn
		// (texture_under = piste vide, texture_progress = remplissage cyan). Le code
		// ne fait que piloter la valeur 0..100 ; la taille se règle via le nœud.
		_manaBarre = GetNode<TextureProgressBar>("ManaGlace");

		var etat = GameState.Instance;
		ConstruirePv(etat.Pv, etat.PvMax);
		MettreAJourPoissons(etat.Poissons);

		// La jauge de mana ne s'affiche qu'une fois le pouvoir de glace débloqué.
		_manaBarre.Visible = etat.PouvoirGlaceActif;
		MettreAJourManaGlace(etat.ManaGlace, etat.ManaGlaceMax);

		etat.PvChanges += OnPvChanges;
		etat.PoissonsChanges += OnPoissonsChanges;
		etat.PouvoirGlaceObtenu += OnPouvoirGlaceObtenu;
		etat.ManaGlaceChanges += MettreAJourManaGlace;
	}

	private void OnPouvoirGlaceObtenu() => _manaBarre.Visible = true;

	private void MettreAJourManaGlace(float mana, float max)
	{
		float ratio = max > 0f ? Mathf.Clamp(mana / max, 0f, 1f) : 0f;
		_manaBarre.Value = ratio * 100f;
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

		for (int i = 0; i < pvMax; i++)
		{
			var coeur = new TextureRect
			{
				Texture = _coeurPlein,
				CustomMinimumSize = new Vector2(24, 24),
				ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional
			};
			_coeurs.AddChild(coeur);
		}

		MettreAJourPv(pv);
	}

	// Chaque cœur bascule entre plein et vide selon les PV restants. Le remplacement
	// de texture est stable : les deux sprites ont la même silhouette et le même cadre.
	private void MettreAJourPv(int pv)
	{
		var enfants = _coeurs.GetChildren();
		for (int i = 0; i < enfants.Count; i++)
		{
			if (enfants[i] is TextureRect coeur)
				coeur.Texture = i < pv ? _coeurPlein : _coeurVide;
		}
	}

	private void MettreAJourPoissons(int total) => _labelPoissons.Text = $"x {total}";

	public void Afficher() => Visible = true;

	public void Masquer() => Visible = false;
}
