# monde2.tscn — deux zones Banquise (35%) / UsinePereNoel (65%)

`scenes/niveaux/monde2.tscn` était vide. Reprise du patron d'organisation de `monde1.tscn`
(un `Node2D` par lieu, sous-groupes `Camera/Sol/Decor/Pnj/Interactifs/Frontiere` ; fonds dans
`Fonds` + `BackgroundManager`, un conteneur par région nommé comme le `NomRegion`).

## Ajouté
- **`Fonds`** (`BackgroundManager`) avec 2 conteneurs de région :
  - `banquise` (visible) = `FondBanquise` + `DecorBanquise`.
  - `usine` (`modulate.a=0`) = `FondUsine.tscn` (existant).
- **`Banquise`** — lieu 35% : `ZoneBanquise` (`camera_zone`) `position=(840,112)` `scale=(6.5625,1.6637502)`,
  `NomRegion="banquise"`. Bornes x ∈ [0, 1680].
- **`UsinePereNoel`** — lieu 65% : `ZoneUsine` `position=(3240,112)` `scale=(12.1875,1.6637502)`,
  `NomRegion="usine"`, `Type=1` (Souterrain → pas de blizzard). Bornes x ∈ [1680, 4800].
- Les deux lieux portent les sous-groupes `Sol/Decor/Pnj/Interactifs/Frontiere` **vides**
  (squelette à remplir dans l'éditeur).

## Découpage
Longueur totale 4800 px, départ x=0. Zones adjacentes non chevauchantes (bord commun x=1680) :
Banquise 1680 px (35%), UsinePereNoel 3120 px (65%). Vertical repris de la banquise de monde1.

## Vérif
`godot --headless --quit-after 5 scenes/niveaux/monde2.tscn` : chargement sans erreur.
La scène n'a pas encore de `Joueur`/`Hud`/`MenuPause` (à ajouter pour la rendre jouable).
