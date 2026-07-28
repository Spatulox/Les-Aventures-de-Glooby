using System;
using System.Collections.Generic;

// Fiche d'une facilité de la partie de test : sa clé (identifiant interne et clé de
// stockage dans GameState), son libellé dans l'écran de debug, son état par défaut, et
// l'effet éventuel à appliquer au lancement du niveau. Immuable.
//
// Deux natures d'options, d'où le Appliquer optionnel :
//   - PONCTUELLE (Appliquer non nul) : elle pose un état une fois pour toutes au
//     démarrage — débloquer un pouvoir, marquer une mémoire de progression. Le
//     gameplay n'a rien à savoir de l'option, il voit juste un état normal ;
//   - CONTINUE (Appliquer nul) : rien à poser au départ, c'est le gameplay qui
//     l'interroge à chaque coup via GameState.OptionDebugActive (invincibilité,
//     one-shot, mana infini).
public class OptionDebug
{
	public string Cle { get; }
	public string Libelle { get; }
	public bool ParDefaut { get; }

	// Effet de lancement d'une option ponctuelle, ou null pour une option continue.
	public Action<GameState> Appliquer { get; }

	public OptionDebug(string cle, string libelle, bool parDefaut, Action<GameState> appliquer = null)
	{
		Cle = cle;
		Libelle = libelle;
		ParDefaut = parDefaut;
		Appliquer = appliquer;
	}
}

// Catalogue des options de debug : la source de vérité unique, sur le modèle de
// CatalogueActions. L'écran de debug en déduit ses cases à cocher et GameState en
// déduit les effets à appliquer — ajouter une facilité de test = ajouter UNE ligne
// ici, il n'y a ni UI ni chargement à retoucher.
//
// Les défauts reproduisent l'ancien mode debug monolithique (pouvoirs, mana infini,
// invincibilité, one-shot) : décocher est un ajustement, pas la norme. La route du
// lutin CGT, elle, part décochée — c'est une branche d'histoire, pas une facilité.
public static class CatalogueOptionsDebug
{
	public const string Invincible = "invincible";
	public const string OneShot = "one_shot";
	public const string PouvoirChaleur = "pouvoir_chaleur";
	public const string PouvoirGlace = "pouvoir_glace";
	public const string ManaInfini = "mana_infini";
	public const string RouteLutinCgt = "route_lutin_cgt";

	// Libellés volontairement COURTS : l'écran de debug tient dans un viewport de
	// 640×360 et les cases ne doivent pas manger la place de la liste des niveaux.
	public static readonly IReadOnlyList<OptionDebug> Toutes = new List<OptionDebug>
	{
		new(Invincible, "Invincible", true),
		new(OneShot, "Ennemis en un coup", true),
		new(PouvoirChaleur, "Pouvoir chaleur", true,
			etat => etat.ObtenirPouvoirChaleur()),
		new(PouvoirGlace, "Pouvoir glace", true,
			etat => etat.ObtenirPouvoirGlace()),
		new(ManaInfini, "Mana infini", true),
		// Donner ses 50 poissons au lutin CGT ouvre la fin secrète (voir
		// ZoneBoss.MemoireRequise) : on pose directement la mémoire du don.
		new(RouteLutinCgt, "Route lutin CGT", false,
			etat =>
			{
				// On vide AUSSI la réserve, comme le ferait le CoutPoissons du choix :
				// sans la dépense, la partie de test serait incohérente — « j'ai tout
				// donné » avec de quoi se soigner en poche — et les choix conditionnés à
				// la réserve (voir ChoixDialogue.SiReserveInsuffisante) se tromperaient
				// de branche. On vide la réserve entière plutôt que de recopier un 50 qui
				// vit dans le .tres du dialogue : le don, c'est « tous ses poissons ».
				etat.MarquerConsomme(LutinCgt.IdDonPoissons);
				etat.DepenserPoissons(etat.Poissons);
			}),
	};

	// Clés cochées par défaut, utilisées quand aucun choix explicite n'est fourni
	// (lancement debug hors de l'écran : sondes headless, appels historiques).
	public static HashSet<string> ClesParDefaut()
	{
		var cles = new HashSet<string>();
		foreach (var option in Toutes)
			if (option.ParDefaut)
				cles.Add(option.Cle);
		return cles;
	}
}
