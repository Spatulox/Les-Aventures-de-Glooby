# Rework du niveau : village de pingouins (monde.tscn)

## Objectif
Retravailler `scenes/niveaux/monde.tscn` : sol en `PlateformeUnidirectionnelle`, usage des
décors et `decors/props`, un début de jeu = **village de pingouins** (igloos + checkpoint),
architecture de nœuds claire et éditable à la main, et zones de caméra **sans chevauchement**.
Le reste du monde sera construit ensuite.

## Changements (`scenes/niveaux/monde.tscn` réécrit)
Ancien contenu (grand `TileMapLayer` baké, salles profondes, arène du boss, noms de nœuds
cryptiques) remplacé par une scène propre et minimale.

**Organisation : un nœud par « endroit » de la map** (`Village`, `Grotte`, et futures arènes
de boss), chaque endroit contenant les mêmes sous-groupes :
- **`Sol`** — plateformes `PlateformeUnidirectionnelle` (one-way) juxtaposées à `y=315`,
  pas de 278 px (= largeur de collision) → sol continu sans trou. Plus de tilemap.
- **`Decor`** — igloos (maisons du village) + props (`Rocher`, `CristalGros`, `CristalPetit`,
  `FleurGivre`, `StalactiteDecor` pour la grotte), props en `z_index=-1` en arrière-plan.
- **`Interactifs`** — checkpoints / pièges / pickups (village : `checkpoint_peche`
  `village_depart`).
- **`Camera`** — la `camera_zone` de l'endroit (limites dérivées de la forme). Zones
  **adjacentes sans chevauchement** (bord commun à x=2560).
- **`Frontiere`** — le `region_trigger` qui révèle le fond de l'endroit.

Nœuds globaux / transversaux à la racine :
- **`Fonds`** — `BackgroundManager` + un conteneur par région (`village`, `grotte`) ; voir la
  section fonds multi-couches.
- **`Joueur`** — instance de `player.tscn` à `(120, 260)` ; override caméra
  `limit_right=2560 / limit_bottom=400` pour cadrer dès la 1ʳᵉ frame.
- **`MenuPause`** — conservé.

Ajouter un endroit = dupliquer le patron (`Sol`/`Decor`/`Interactifs`/`Camera`/`Frontiere`)
sous un nouveau nœud + un conteneur de fond sous `Fonds`.

Instance du joueur allégée : les surcharges bakées inutiles (SpriteFrames, formes de
collision) ont été retirées — `player.tscn` définit déjà tout, `Player.cs` charge ses
animations au runtime.

## Repères de placement
- Surface du sol ≈ `y=300` (haut de collision de la plateforme). Décors alignés dessus
  (igloo 96×64 → `y=268`, rocher 96×96 → `252`, cristal gros 64×64 → `268`, cristal petit /
  fleur 32×32 → `284`).
- Viewport 640×360 ; région caméra 2560×400 (≥ viewport) → `SeuilChuteVide = 400+300 = 700`.

## À noter (le reste sera fait après)
- Boss (`ZoneBossCerf`, `BossHudBarre`), murs fondables, stalactites-pièges, pickup pouvoir
  chaleur, régions grotte/crevasse et `RegionTrigger` : retirés de la scène village, à
  réintroduire dans les zones suivantes (les `.tscn`/scripts restent disponibles).
- Le sol one-way n'a pas de tuiles `is_ice`/`is_fragile` (mécanique liée au tilemap absent).

## Fonds multi-couches par région
Système de fond en couches, une région = un conteneur Node2D sous `Fonds`
(`BackgroundManager`), fondu croisé via `modulate:a` (hérité par les enfants) :
- **Fond lointain fixe** — nouvelles scènes réutilisables `scenes/decors/FondBanquise.tscn`
  et `FondGrotte.tscn` : un `Parallax2D` `scroll_scale = (0,0)` (figé sur la caméra),
  **sans répétition**, `z_index = -100` (image unique « skybox » cadrée pour le 640×360).
- **Décor parallax intermédiaire** — instances des scènes existantes
  `DecorBanquise.tscn` / `DecorGrotte.tscn` (couches `Parallax2D`, z −12…−3) placées entre
  le fond lointain et le premier plan (plateformes/props).
- `monde.tscn` : `Fonds` → `village` (Node2D) → `FondBanquise` + `DecorBanquise`.
  Ajouter une grotte plus tard = conteneur `grotte` (`FondGrotte` + `DecorGrotte`) +
  `RegionTrigger` appelant `AfficherRegion("grotte")`.

Remplace l'ancien fond unique qui se répétait (`Parallax2D` `repeat_times=20`).

## Grotte (à droite du village)
Le monde continue vers la droite par le nœud d'endroit `Grotte` (même structure que
`Village`), dans le même `monde.tscn` :
- **`Grotte/Sol`** : 10 plateformes one-way (x 2780→5282) prolongent le sol.
- **`Fonds/grotte`** (racine) : `FondGrotte` fixe + `DecorGrotte` parallax, `modulate` alpha 0
  au départ (révélé au passage).
- **`Grotte/Camera`** : `ZoneGrotte` (`camera_zone`, 2560×400 à x=3840) **adjacente sans
  chevauchement** à `ZoneVillage` (bord commun à x=2560).
- **Frontières bidirectionnelles** : `Village/Frontiere/VersVillage` (x=2500 →
  `AfficherRegion("village")`) et `Grotte/Frontiere/VersGrotte` (x=2620 →
  `AfficherRegion("grotte")`) pour un fondu de fond dans les deux sens.
- **`Grotte/Decor`** : stalactites (`StalactiteDecor` ×4, en hauteur), cristaux et rochers
  répartis sur x 2800→4900.

## Documentation
- `CLAUDE.md` : section architecture mise à jour (groupes de nœuds, sol en
  `PlateformeUnidirectionnelle`, village + grotte, fonds multi-couches par région).

## Vérification
- `dotnet build` : 0 avertissement, 0 erreur.
- `godot --headless --quit-after 200 scenes/niveaux/monde.tscn` : aucune erreur de chargement,
  aucun avertissement `CameraZone` (forme résolue).
- Boot par défaut (`menu_principal`) headless : sans erreur.
- Aperçu éditeur recommandé pour ajuster finement le placement (le headless ne rend pas le
  visuel).
