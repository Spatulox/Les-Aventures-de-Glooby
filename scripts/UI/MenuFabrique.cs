using Godot;
using System;

// Fabrique d'interface de menu réutilisable : compose un fond plein écran, un
// titre centré et une colonne de boutons verticaux au style commun. Partagée
// par le menu principal et le menu pause pour ne dupliquer ni la mise en page
// ni l'apparence des boutons.
public static class MenuFabrique
{
	// Recouvre tout le parent d'un rectangle de couleur : fond opaque du menu
	// principal, ou voile semi-transparent du menu pause.
	public static ColorRect AjouterFond(Control parent, Color couleur)
	{
		var fond = new ColorRect { Color = couleur };
		fond.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		fond.MouseFilter = Control.MouseFilterEnum.Ignore;
		parent.AddChild(fond);
		return fond;
	}

	// Crée une colonne centrée (titre optionnel puis boutons) et renvoie la
	// boîte verticale prête à recevoir des boutons via AjouterBouton.
	public static VBoxContainer AjouterColonne(Control parent, string titre = null)
	{
		var centre = new CenterContainer();
		centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		centre.MouseFilter = Control.MouseFilterEnum.Ignore;
		parent.AddChild(centre);

		var colonne = new VBoxContainer();
		colonne.AddThemeConstantOverride("separation", 12);
		centre.AddChild(colonne);

		if (titre != null)
		{
			var label = new Label
			{
				Text = titre,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			label.AddThemeFontSizeOverride("font_size", 28);
			colonne.AddChild(label);
		}

		return colonne;
	}

	// Ajoute un bouton large et lisible relié à surClic. Grisé si actif = false
	// (ex. "Continuer" tant qu'aucune sauvegarde n'existe).
	public static Button AjouterBouton(VBoxContainer colonne, string texte, Action surClic, bool actif = true)
	{
		var bouton = new Button
		{
			Text = texte,
			CustomMinimumSize = new Vector2(240, 36),
			Disabled = !actif
		};
		if (surClic != null)
			bouton.Pressed += surClic;
		colonne.AddChild(bouton);
		return bouton;
	}

	// Ajoute une ligne de texte centrée à une colonne (ex. rappel des touches).
	public static Label AjouterLigne(VBoxContainer colonne, string texte)
	{
		var label = new Label
		{
			Text = texte,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		colonne.AddChild(label);
		return label;
	}
}
