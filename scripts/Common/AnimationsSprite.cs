using Godot;
using System.Collections.Generic;

// Helpers réutilisables pour construire un SpriteFrames à partir de dossiers de
// PNG (une image = une frame, triés par nom). Partagés par les entités animées
// (Player, PNJ) et l'UI décorative (pingouin idle du menu) pour ne dupliquer ni
// le chargement des textures ni l'enregistrement d'une animation.
public static class AnimationsSprite
{
	// Charge les PNG d'un dossier, triés par nom, en tableau de textures.
	public static Texture2D[] ChargerFrames(string dossier)
	{
		var fichiers = new List<string>();
		foreach (var fichier in DirAccess.GetFilesAt(dossier))
		{
			if (fichier.EndsWith(".png"))
				fichiers.Add(fichier);
		}
		fichiers.Sort();

		var textures = new Texture2D[fichiers.Count];
		for (int i = 0; i < fichiers.Count; i++)
			textures[i] = GD.Load<Texture2D>($"{dossier}/{fichiers[i]}");
		return textures;
	}

	// Enregistre une animation nommée dans un SpriteFrames (cadence + boucle), en
	// n'en prenant qu'une tranche de frames [debut..fin] (fin < 0 => jusqu'au bout).
	public static void EnregistrerAnimation(SpriteFrames frames, string nom, Texture2D[] toutesLesFrames, float fps, bool boucle, int debut = 0, int fin = -1)
	{
		if (fin < 0)
			fin = toutesLesFrames.Length - 1;

		frames.AddAnimation(nom);
		frames.SetAnimationSpeed(nom, fps);
		frames.SetAnimationLoop(nom, boucle);

		for (int i = debut; i <= fin; i++)
			frames.AddFrame(nom, toutesLesFrames[i]);
	}
}
