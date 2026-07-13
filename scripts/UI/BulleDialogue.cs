using Godot;

// Bulle de dialogue « banquise » : rendu procédural (aucun asset généré) d'une bulle
// cartoon aux tons glace, dessinée au-dessus d'un PNJ/panneau. Le fond épouse la taille
// du texte, avec retour à la ligne au-delà d'une largeur max, et une petite queue pointe
// vers le model 2D. Sert aussi de rappel de touche (étiquette « appuie sur ... »).
// Réutilisable : instanciée par DeclencheurDialogue et posée sur le PointBulle du Talkative.
public partial class BulleDialogue : Node2D
{
	// Palette glace/cartoon (bulle claire bordée de bleu froid, texte bleu profond).
	private static readonly Color FondBulle = new(0.93f, 0.97f, 1.0f);
	private static readonly Color ContourBulle = new(0.20f, 0.42f, 0.62f);
	private static readonly Color TexteBulle = new(0.10f, 0.21f, 0.34f);
	// Rappel de touche : étiquette pleine (contour bleu, texte clair).
	private static readonly Color FondRappel = new(0.20f, 0.42f, 0.62f);
	private static readonly Color TexteRappel = new(0.95f, 0.99f, 1.0f);

	private const int TaillePolice = 14;
	private const float LargeurMax = 240f;   // au-delà : retour à la ligne
	private const float Marge = 8f;          // padding intérieur autour du texte
	private const float QueueLargeur = 14f;
	private const float QueueHauteur = 10f;
	private const int RayonCoin = 7;
	private const int EpaisseurContour = 2;

	private Label _label;
	private Vector2 _tailleFond;
	private Color _fond;
	private Color _contour;
	private bool _avecQueue;

	public override void _Ready()
	{
		ZIndex = 100;   // au-dessus des décors et des entités
		_label = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_label.AddThemeFontSizeOverride("font_size", TaillePolice);
		AddChild(_label);
		Visible = false;
	}

	// Affiche une ligne de dialogue : bulle claire + queue pointant vers le PNJ.
	public void AfficherDialogue(string texte)
		=> Composer(texte, FondBulle, ContourBulle, TexteBulle, avecQueue: true);

	// Affiche le rappel de touche : petite étiquette foncée, sans queue.
	public void AfficherRappel(string texte)
		=> Composer(texte, FondRappel, ContourBulle, TexteRappel, avecQueue: false);

	public void Cacher() => Visible = false;

	private void Composer(string texte, Color fond, Color contour, Color couleurTexte, bool avecQueue)
	{
		_fond = fond;
		_contour = contour;
		_avecQueue = avecQueue;

		Font police = _label.GetThemeFont("font");
		Vector2 tailleTexte = police.GetMultilineStringSize(
			texte, HorizontalAlignment.Center, LargeurMax, TaillePolice);

		_tailleFond = tailleTexte + new Vector2(Marge * 2f, Marge * 2f);

		_label.Text = texte;
		_label.AddThemeColorOverride("font_color", couleurTexte);
		_label.Size = tailleTexte;
		// Origine (0,0) = pointe de la queue, juste au-dessus du PNJ. Le corps se dessine
		// au-dessus, centré horizontalement ; le texte est calé dans la marge.
		_label.Position = new Vector2(-tailleTexte.X / 2f, HautCorps() + Marge);

		Visible = true;
		QueueRedraw();
	}

	// Ordonnée du haut du corps de la bulle (négatif = au-dessus de l'ancre).
	private float HautCorps() => BasCorps() - _tailleFond.Y;

	// Ordonnée du bas du corps : au-dessus de la queue (ou d'un léger décalage sans queue).
	private float BasCorps() => -(_avecQueue ? QueueHauteur : QueueHauteur * 0.4f);

	public override void _Draw()
	{
		if (!Visible)
			return;

		var rect = new Rect2(new Vector2(-_tailleFond.X / 2f, HautCorps()), _tailleFond);
		var boite = new StyleBoxFlat
		{
			BgColor = _fond,
			BorderColor = _contour,
			CornerRadiusTopLeft = RayonCoin,
			CornerRadiusTopRight = RayonCoin,
			CornerRadiusBottomLeft = RayonCoin,
			CornerRadiusBottomRight = RayonCoin,
		};
		boite.SetBorderWidthAll(EpaisseurContour);
		DrawStyleBox(boite, rect);

		if (_avecQueue)
		{
			float bas = BasCorps();
			Vector2 gauche = new(-QueueLargeur / 2f, bas);
			Vector2 droite = new(QueueLargeur / 2f, bas);
			Vector2 pointe = new(0f, 0f);
			DrawColoredPolygon(new[] { gauche, droite, pointe }, _fond);
			// On borde les deux flancs (le haut reste ouvert pour se fondre au corps).
			DrawLine(gauche, pointe, _contour, EpaisseurContour);
			DrawLine(droite, pointe, _contour, EpaisseurContour);
		}
	}
}
