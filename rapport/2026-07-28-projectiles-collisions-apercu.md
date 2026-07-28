# Projectiles : arrêtés par le sol, au plan du joueur, aperçu du cadeau explosif

Deux demandes : (1) les projectiles traversaient les plateformes et devaient éclater
au contact du décor solide, en restant sur le même plan de rendu que le joueur ;
(2) le cadeau explosif devait être visible dans l'éditeur Godot.

## 1. Les projectiles traversaient le sol — `Constantes.MasqueProjectile`

**Cause racine** : le masque valait `LayerTerrain | LayerJoueur | LayerPnj` (= 7) et
**oubliait le layer 5** (`LayerPlateformesTraversables`). Or le sol de monde1 est
entièrement bâti en `PlateformeUnidirectionnelle` : les tirs traversaient donc le
plancher lui-même, pas seulement les plateformes suspendues.

| Avant | Après |
|---|---|
| `LayerTerrain \| LayerJoueur \| LayerPnj` (7) | `MasqueMarcheur \| LayerJoueur \| LayerPnj` (23) |

Exprimé via `MasqueMarcheur` : un projectile est arrêté par exactement ce qui porte un
corps qui marche, plus ses cibles. Les deux constantes ne peuvent plus diverger.

**Conséquence assumée et documentée** : le « one-way » n'est qu'un réglage du solveur
pour les corps physiques — une `Area2D` détecte la plateforme **quel que soit le sens
d'arrivée**. Un tir éclate donc dessus par-dessous comme par-dessus. C'est le
comportement demandé (« arrêtés dès qu'ils touchent »), noté en commentaire pour ne pas
être repris pour un bug plus tard.

## 2. Plan de rendu — `Projectile._Ready`

`ZIndex = Constantes.ZJoueur` posé dans la base : vaut pour **tous** les projectiles
(cadeau, boule de neige, éclat de glace) sans toucher aux sous-classes. Les projectiles
étant instanciés en cours de partie, ils arrivent en fin d'arbre et se dessinent après
le joueur à z égal — bon ordre.

## 3. Aperçu éditeur du cadeau explosif

`CadeauExplosif` construit ses `SpriteFrames` au runtime : dans l'éditeur son
`AnimatedSprite2D` est vide, la scène est invisible donc impossible à placer.

- `CadeauExplosif.tscn` porte un `Sprite2D` **`Apercu`** (1re frame de
  `cadeau_explosif_vol`, échelle 0,5 comme l'`AnimatedSprite2D`), masqué au démarrage.
- La convention « Apercu » n'existait que sur `LivingEntity`, or un projectile est une
  `Area2D` → règle extraite dans **`scripts/Common/ApercuEditeur.cs`**, appelée par
  `LivingEntity.MasquerApercuEditeur()` (qui délègue) **et** par `Projectile._Ready`.
  Toute scène de projectile qui ajoute un nœud `Apercu` en bénéficie désormais sans code.

## Fichiers touchés

| Fichier | Changement |
|---|---|
| `scripts/Common/Constantes.cs` | `MasqueProjectile` 7 → 23 + commentaire |
| `scripts/Common/ApercuEditeur.cs` | **nouveau** — helper partagé `Masquer(Node)` |
| `scripts/Entities/Damage/Projectile.cs` | `ZIndex`, appel `ApercuEditeur.Masquer` |
| `scripts/Entities/LivingEntity.cs` | `MasquerApercuEditeur` délègue au helper |
| `scenes/projectiles/CadeauExplosif.tscn` | nœud `Apercu` + masque/z alignés |
| `scenes/projectiles/boule_de_neige.tscn` | masque/z alignés |
| `scenes/projectiles/EclatGlace.tscn` | masque/z alignés |

Les `.tscn` sont alignés pour que l'éditeur montre la même chose que le runtime, mais le
code reste la source de vérité (`_Ready` repose les couches d'office).

## Vérifications

- `godot --headless --build-solutions` : compilation propre, aucune erreur ni warning.
- Chargement headless des trois scènes de projectile : `z=1`, `mask=23`, `apercu=false`
  (masqué) sur le cadeau. Scène de test supprimée après coup.
- Points de tir contrôlés : boule du joueur (origine −7, rayon 9) à ~20 px au-dessus de
  la surface de marche, `Boss.PointDeTir` au niveau du torse — aucun risque
  d'éclatement immédiat au spawn.

## Non fait / à noter

- `OndeDeChoc` garde son masque à 2 (joueur seul) : ce n'est pas un `Projectile` mais
  une zone d'effet qui s'étale au sol, elle ne doit pas « éclater » sur le terrain.
- Pas d'`Apercu` sur la boule de neige ni l'éclat de glace (non demandé) — le helper est
  en place, il suffit d'ajouter le nœud à leur `.tscn`.
- Portée du tir du joueur désormais bornée par la chute : ~110 px avant impact au sol
  (Vitesse 320, Gravité 480, spawn ~29 px au-dessus de la surface). À valider en jeu.

## Commits

Deux commits, contenant **strictement** ces changements :

- `cf0c7c3` — collisions + plan de rendu
- `4bf53f1` — aperçu éditeur + helper partagé

`LivingEntity.cs` contenait aussi un refactor en cours côté Marc (« zones de présence »,
`_joueursParZone`, `CablerZonePresence`) : la version commitée a été reconstruite dans
l'index à partir de `HEAD` + la seule ligne du helper, le refactor est resté intact et
non commité dans le working tree. Idem pour le reste du WIP (`03-monde2.tscn`,
`ZoneBoss.cs`, `CagePereNoel.*`, `GardienRonces.cs`, `LocomotiveJouet.cs`,
`PorteeJoueur.cs`, `MechantFonceur.cs`).
