using Godot;

// Ennemi « Gardien des ronces » (grotte florale) : la sentinelle de base du lieu, et le modèle
// du méchant FONCEUR — patrouille, sursaut de surprise, bond sur la cible, puis marche sur elle.
// Toute cette machine à états vit dans MechantFonceur (partagée avec la locomotive jouet) ; il ne
// reste ici que ce qui appartient au gardien : ses animations et son réglage par défaut.
//
// Son bond est BORNÉ à hauteur du joueur (RueeBorneeParJoueur par défaut) : un bond court, ~2× sa
// taille, qui ne le dépasse pas. Sa marche de poursuite est plus lente que le joueur, donc semable
// en courant, mais il ne lâche pas tant que sa cible reste à portée.
//
// Il blesse au contact (ZoneContact) et se tue normalement (PV), contrairement aux ennemis
// « obstacles » de la banquise (ours, bonhomme) que l'on ne fait qu'étourdir.
//
// Frames chargées depuis res://assets/ennemis/grotte_florale/gardien_ronces/{idle,marche,mort}.
// Pas d'animation de repérage dédiée (budget) : la base rejoue idle quand velocite.X == 0, et
// marche pendant la ruée.
public partial class GardienRonces : MechantFonceur
{
	protected override SpriteFrames ConstruireAnimations()
	{
		var frames = new SpriteFrames();
		frames.RemoveAnimation("default");
		const string b = "res://assets/ennemis/grotte_florale/gardien_ronces";
		AjouterAnimation(frames, "idle", $"{b}/idle", 6f, true);
		AjouterAnimation(frames, "marche", $"{b}/marche", 10f, true);
		AjouterAnimation(frames, "mort", $"{b}/mort", 8f, false);
		return frames;
	}
}
