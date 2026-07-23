using Godot;

// Barre de chargement discrète ancrée EN BAS de l'écran, affichée pendant le menu principal
// (et au-delà si le joueur lance la partie avant la fin) UNIQUEMENT quand Ollama télécharge
// quelque chose (binaire/installeur, puis modèle). NON bloquante : le menu reste pleinement
// utilisable dessous. Overlay CanvasLayer attaché à la racine (survit au changement de scène
// menu → monde). S'abonne aux signaux d'OllamaService et se retire dès la fin du provisionnement.
public partial class EcranChargementOllama : CanvasLayer
{
	private const float HauteurBandeau = 34f;
	private const double DureeAvantRetraitErreur = 8.0; // l'erreur reste lisible avant de disparaître

	private Label _phase;
	private ProgressBar _barre;
	private bool _fige; // vrai dès qu'une erreur est affichée : plus de mise à jour de progression

	public override void _Ready()
	{
		Layer = 100; // au-dessus du menu/HUD, mais on n'intercepte pas les clics

		// Bandeau plein largeur collé au bas du viewport (anchors bas ; OffsetTop = hauteur).
		var bandeau = new PanelContainer
		{
			AnchorLeft = 0f,
			AnchorRight = 1f,
			AnchorTop = 1f,
			AnchorBottom = 1f,
			OffsetTop = -HauteurBandeau,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		bandeau.AddThemeStyleboxOverride("panel", StyleBandeau());
		AddChild(bandeau);

		var marge = new MarginContainer();
		marge.AddThemeConstantOverride("margin_left", 12);
		marge.AddThemeConstantOverride("margin_right", 12);
		marge.AddThemeConstantOverride("margin_top", 6);
		marge.AddThemeConstantOverride("margin_bottom", 6);
		bandeau.AddChild(marge);

		var ligne = new HBoxContainer();
		ligne.AddThemeConstantOverride("separation", 10);
		marge.AddChild(ligne);

		_phase = new Label
		{
			Text = "Préparation des dialogues IA…",
			VerticalAlignment = VerticalAlignment.Center,
		};
		ligne.AddChild(_phase);

		_barre = new ProgressBar
		{
			MinValue = 0,
			MaxValue = 1,
			Value = 0,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			CustomMinimumSize = new Vector2(0, 16),
		};
		ligne.AddChild(_barre);

		var svc = OllamaService.Instance;
		if (svc != null)
		{
			svc.ProvisionnementProgresse += SurProgres;
			svc.ProvisionnementErreur += SurErreur;
			svc.ProvisionnementTermine += SurTermine;
		}
	}

	// Fond sombre semi-opaque avec un liseré supérieur froid : lisible par-dessus le menu.
	private static StyleBoxFlat StyleBandeau()
	{
		var style = new StyleBoxFlat { BgColor = new Color(0.04f, 0.07f, 0.12f, 0.85f) };
		style.BorderColor = new Color(0.20f, 0.42f, 0.62f);
		style.BorderWidthTop = 2;
		return style;
	}

	// Met à jour phase + barre à chaque avancée du téléchargement (ignoré une fois figé sur erreur).
	private void SurProgres(string phase, float ratio)
	{
		if (_fige)
			return;
		_phase.Text = phase;
		_barre.Value = ratio;
	}

	// Échec d'une étape : on affiche la raison en rouge, on masque la barre, et le bandeau se
	// retire seul après un délai (pour laisser le temps de lire). Le jeu reste jouable (repli statique).
	private void SurErreur(string message)
	{
		if (_fige)
			return;
		_fige = true;
		_barre.Visible = false;
		_phase.Text = $"⚠ Dialogues IA indisponibles — {message}";
		_phase.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.45f));
		GetTree().CreateTimer(DureeAvantRetraitErreur).Timeout += QueueFree;
	}

	// Provisionnement fini : succès ⇒ la barre disparaît aussitôt ; échec sans message précis
	// déjà affiché ⇒ message d'erreur générique (le cas messagé est géré par SurErreur).
	private void SurTermine(bool succes)
	{
		if (succes)
		{
			QueueFree();
			return;
		}
		if (!_fige)
			SurErreur("téléchargement impossible (vérifie ta connexion internet).");
	}
}
