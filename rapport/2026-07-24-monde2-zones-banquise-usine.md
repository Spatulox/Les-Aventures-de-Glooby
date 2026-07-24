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

## Nodes de racine ajoutés (comme monde1)
- **`Joueur`** = instance de `player.tscn`, `position=(180,200)` → **c'est le point de spawn** (début de la Banquise). ⚠️ Pas encore de sol sous lui : il tombera tant que `Banquise/Sol` reste vide.
- **`Meteo`** = instance de `blizzard.tscn` (météo globale).
- **`MenuPause`** = `CanvasLayer` + script `MenuPause.cs`.
- **`Hud` NON ajouté** : c'est un **autoload** (`project.godot`), déjà présent dans toutes les scènes.

## Sol de la Banquise (monde2)
5 segments individuels sous `Banquise/Sol` (embout gauche + 3 centres `SolBanquise` + embout droit,
pas de 344 px, surface de marche y locale −46), instances `.tscn` visibles dans l'éditeur — même
approche que monde1. Positions ajustées ensuite à la main dans l'éditeur.

## SolBanquiseLigne / SolGrotteLigne → supprimés
Les composeurs runtime `SolBanquiseLigne` et `SolGrotteLigne` (scènes + scripts + `.uid`) ont été
**supprimés** : on pose désormais les segments `SolBanquise` / `SolGrotte` individuellement et visibles
dans l'éditeur (cf. `Banquise/Sol` de monde2). Références nettoyées dans `CLAUDE.md` et le commentaire
de `scripts/Entities/GuirlandeNoel.cs`.

## Vérif
`godot --headless --quit-after 10 scenes/niveaux/monde2.tscn` : scène chargée, pas d'erreur de
ressource/instanciation. Les erreurs `Not supported by this display server` (libellé clavier via
`EvenementEntree.Libelle` → `EcranParametres`) sont un artefact **headless uniquement** (pas de
DisplayServer), absent en lancement graphique normal.
