using Godot;

// Bulle de dialogue « banquise » : rendu procédural (aucun asset généré) d'une bulle
// cartoon aux tons glace, dessinée au-dessus d'un PNJ/panneau. Le fond épouse la taille
// du texte, avec retour à la ligne au-delà d'une largeur max, et une petite queue pointe
// vers le model 2D. Sert aussi de rappel de touche (étiquette « appuie sur ... »).
// Réutilisable : instanciée par DeclencheurDialogue et posée sur le PointBulle du Talkative.
//
// Le texte est dessiné directement en _Draw (DrawMultilineString) plutôt que via un Label :
// placement au pixel à partir des métriques de police (calcul immédiat), donc pas de bug de
// centrage dû au layout paresseux d'un Control à la première frame.
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
	private const float MargeCadre = 4f;     // garde entre la bulle et le bord de l'écran

	private Font _police;
	private string _texte = "";
	private Color _couleurTexte;
	private Vector2 _tailleTexte;   // taille mesurée du bloc de texte (sans marges)
	private Vector2 _tailleFond;    // taille de la bulle (texte + marges)
	private Color _fond;
	private Color _contour;
	private bool _avecQueue;
	// Décalages de la boîte pour la maintenir dans le cadre visible de la caméra (la
	// queue, elle, reste ancrée sur le PNJ). Recalculés chaque frame tant que la bulle
	// est visible, car la caméra bouge avec le joueur.
	private float _decalageX;
	private float _decalageY;

	public override void _Ready()
	{
		ZIndex = 100;   // au-dessus des décors et des entités
		// Police par défaut du thème (via un Label sonde une fois dans l'arbre).
		var sonde = new Label();
		AddChild(sonde);
		_police = sonde.GetThemeFont("font");
		sonde.QueueFree();
		Visible = false;
		SetProcess(false);   // ne recadre que quand la bulle est affichée
	}

	// Affiche une ligne de dialogue : bulle claire + queue pointant vers le PNJ.
	public void AfficherDialogue(string texte)
		=> Composer(texte, FondBulle, ContourBulle, TexteBulle, avecQueue: true);

	// Mise à jour incrémentale pendant un flux LLM (streaming) : même rendu qu'AfficherDialogue,
	// appelée à chaque token cumulé — la bulle grandit à mesure que le texte arrive. Nommée à
	// part pour clarifier l'intention côté DeclencheurDialogue (flux vs réplique figée).
	public void MettreAJourFlux(string texte)
		=> Composer(texte, FondBulle, ContourBulle, TexteBulle, avecQueue: true);

	// Affiche le rappel de touche : petite étiquette foncée, sans queue.
	public void AfficherRappel(string texte)
		=> Composer(texte, FondRappel, ContourBulle, TexteRappel, avecQueue: false);

	public void Cacher()
	{
		Visible = false;
		SetProcess(false);
	}

	private void Composer(string texte, Color fond, Color contour, Color couleurTexte, bool avecQueue)
	{
		_fond = fond;
		_contour = contour;
		_couleurTexte = couleurTexte;
		_avecQueue = avecQueue;
		_texte = texte;
		_decalageX = 0f;
		_decalageY = 0f;

		_tailleTexte = _police.GetMultilineStringSize(
			texte, HorizontalAlignment.Center, LargeurMax, TaillePolice);
		_tailleFond = _tailleTexte + new Vector2(Marge * 2f, Marge * 2f);

		Visible = true;
		SetProcess(true);
		QueueRedraw();
	}

	// Ordonnée du haut du corps de la bulle (négatif = au-dessus de l'ancre).
	private float HautCorps() => BasCorps() - _tailleFond.Y;

	// Ordonnée du bas du corps : au-dessus de la queue (ou d'un léger décalage sans queue).
	private float BasCorps() => -(_avecQueue ? QueueHauteur : QueueHauteur * 0.4f);

	// Maintient la boîte dans le rectangle visible de la caméra active. On passe par la
	// transform du viewport (qui intègre déjà limites et zoom de la caméra) plutôt que
	// d'accéder à un Camera2D : aucun couplage à CameraZone. Seule la boîte est décalée,
	// la queue reste ancrée sur le PNJ.
	public override void _Process(double delta)
	{
		if (!Visible)
			return;

		var inverse = GetViewport().GetCanvasTransform().AffineInverse(); // écran -> monde
		Vector2 htl = inverse * Vector2.Zero;
		Vector2 br = inverse * GetViewport().GetVisibleRect().Size;
		float visGauche = Mathf.Min(htl.X, br.X) + MargeCadre;
		float visDroite = Mathf.Max(htl.X, br.X) - MargeCadre;
		float visHaut = Mathf.Min(htl.Y, br.Y) + MargeCadre;

		float gx = GlobalPosition.X;
		float demiLargeur = _tailleFond.X / 2f;
		float dxMin = visGauche - (gx - demiLargeur);   // pour garder le flanc gauche visible
		float dxMax = visDroite - (gx + demiLargeur);   // pour garder le flanc droit visible
		float dx = dxMin > dxMax                          // bulle plus large que la vue : centrer
			? (visGauche + visDroite) / 2f - gx
			: Mathf.Clamp(0f, dxMin, dxMax);

		// Vertical : uniquement pousser vers le bas si le haut dépasse (rare au sol), sans
		// que la base de la queue ne passe sous l'ancre (on garde un petit reste de queue).
		float hautBoiteMonde = GlobalPosition.Y + HautCorps();
		float dy = hautBoiteMonde < visHaut
			? Mathf.Min(visHaut - hautBoiteMonde, -BasCorps() - 2f)
			: 0f;

		if (Mathf.IsEqualApprox(dx, _decalageX) && Mathf.IsEqualApprox(dy, _decalageY))
			return;

		_decalageX = dx;
		_decalageY = dy;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!Visible)
			return;

		var coinHautGauche = new Vector2(-_tailleFond.X / 2f + _decalageX, HautCorps() + _decalageY);
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
		DrawStyleBox(boite, new Rect2(coinHautGauche, _tailleFond));

		if (_avecQueue)
		{
			// Tip sur le PNJ (0,0) ; base sous la boîte décalée : la queue s'incline mais
			// continue de pointer le PNJ même quand la boîte est recadrée.
			float bas = BasCorps() + _decalageY;
			Vector2 gauche = new(_decalageX - QueueLargeur / 2f, bas);
			Vector2 droite = new(_decalageX + QueueLargeur / 2f, bas);
			Vector2 pointe = new(0f, 0f);
			DrawColoredPolygon(new[] { gauche, droite, pointe }, _fond);
			// On borde les deux flancs (le haut reste ouvert pour se fondre au corps).
			DrawLine(gauche, pointe, _contour, EpaisseurContour);
			DrawLine(droite, pointe, _contour, EpaisseurContour);
		}

		// Texte centré dans la marge : baseline de la 1re ligne = haut intérieur + ascent.
		// Placement au pixel (pas de layout de Control) => centré dès le premier affichage.
		var posTexte = new Vector2(coinHautGauche.X + Marge, coinHautGauche.Y + Marge + _police.GetAscent(TaillePolice));
		DrawMultilineString(_police, posTexte, _texte, HorizontalAlignment.Center, _tailleTexte.X, TaillePolice, -1, _couleurTexte);
	}
}
