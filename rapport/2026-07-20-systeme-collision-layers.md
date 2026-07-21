# Système de collision — simplification des layers

**Objectif** : chaque PNJ collisionne avec le sol et les murs, aucun PNJ ne
collisionne jamais avec un autre PNJ ni avec le joueur.

## Le problème : le layer 1 signifiait deux choses

Le terrain **et** le joueur étaient sur le layer 1 (`player.tscn` n'avait pas de
`collision_layer`, il héritait du défaut). Un PNJ qui masquait le layer 1 pour
tenir sur le sol se cognait donc au joueur.

Tout le système n'était qu'un contournement de cette ambiguïté : `LayerSolPnj`
(layer 2) existait uniquement pour que le terrain soit **redéclaré une seconde
fois** sur un autre layer, que les PNJ masquaient à la place du 1. D'où 5 scripts
faisant `CollisionLayer |= LayerSolPnj` à la main, et un `MasqueSolPnj = 18` à
recopier dans chaque scène de PNJ — recopie que 3 scènes avaient ratée.

## La correction : le joueur a son propre layer

| Layer | Contenu | Qui le masque |
|---|---|---|
| 1 | Terrain (sol, murs, plateformes pleines) | Joueur + PNJ |
| 2 | Joueur | *aucun corps* — seulement les zones de détection |
| 3 | PNJ (amicaux, méchants, boss) | *aucun corps* — seulement les projectiles |
| 5 | Plateformes traversables (one-way) | Joueur + PNJ |

Joueur et PNJ ont désormais **le même masque**, `17`. Aucun corps ne masque les
layers 2 ni 3 : l'absence de collision joueur↔PNJ et PNJ↔PNJ tient **par
construction**, plus par réglage recopié.

## Bugs corrigés

1. **Le joueur heurtait le Boss Cerf comme un mur** — `boss_cerf.tscn` avait
   oublié son `collision_layer` et était resté sur le layer 1.
2. **`LutinUsine` / `PereNoel` traversaient tout sol plein** — `collision_mask = 16`,
   l'ancien bug déjà corrigé partout ailleurs (latent : non placés dans `monde.tscn`).
3. **Une boule de neige ne pouvait pas toucher `Fonceur` / `LanceurBouleNeige`** —
   layer 4 contre un masque de projectile à 1 (latent, pour la même raison).
4. **Le `DeclencheurDialogue` de `PanneauBois` et toutes les zones d'interaction**
   se déclaraient sur le layer terrain et scannaient le terrain (défaut 1/1) ; elles
   ne fonctionnaient que parce que le joueur s'y trouvait aussi.

### Couplage critique boss ↔ projectile

La boule de neige ne touchait le boss que **par accident** : le boss était resté
sur le layer 1, que le projectile masquait par défaut. Même cause que le bug n°1.
Corriger le layer du boss sans élargir le masque du projectile aurait rendu le
boss invulnérable — les deux changements ont donc été livrés ensemble.

## Modifications

**Code**
- `scripts/Common/Constantes.cs` — `LayerSolPnj` et `MasqueSolPnj` **supprimés** ;
  ajout de `LayerTerrain`, `LayerJoueur`, `LayerPnj`, `MasqueMarcheur` (17),
  `MasqueProjectile` (7).
- `scripts/Entities/LivingEntity.cs` — nouveau `AppliquerCollisionsPnj()`, appelé
  par les `_Ready()` de `Boss`, `PnjAmical`, `PnjMechant` : un nouveau PNJ est
  correct même si son `.tscn` est mal réglé.
- `scripts/Entities/Player/Player.cs` — pose `LayerJoueur`/`MasqueMarcheur` ; ses
  deux requêtes physiques (`ObtenirFrictionSol`, `UtiliserPouvoirChaleur`), qui
  balayaient tous les layers, sont restreintes au terrain.
- `scripts/Entities/Damage/Projectile.cs` — pose `CollisionLayer = 0` et
  `MasqueProjectile`.
- `scripts/Entities/Pnj/BossCerf.cs` — cône de givre : `CollisionMask = 1` →
  `Constantes.LayerJoueur`.
- **Ligne `CollisionLayer |= LayerSolPnj` supprimée** de `SolBanquise`,
  `PenteBanquise`, `PlateformeBanquise`, `MurSolide`, `MurFondable` (les trois
  premiers n'avaient plus que ça dans `_Ready()` : l'override entier a sauté).

**Scènes**
- `player.tscn` → `layer = 2` ; `boss_cerf.tscn` → `layer = 4`, `mask = 17`.
- Les 7 autres scènes de PNJ → `mask = 17` (depuis 18 ou 16).
- Toutes les `Area2D` d'interaction → `layer = 0`, `mask = 2` : les
  `DeclencheurDialogue`, `ZoneContact`, `ZoneChargeDegats`, checkpoints, les deux
  pickups de pouvoir, stalactites piège, `PlateformeFragile/ZoneDetection`,
  `PanneauBois`, et `ZoneBossCerf` dans `monde.tscn` (édition chirurgicale, 2
  lignes ajoutées).
- `boule_de_neige.tscn` → `layer = 0`, `mask = 7`.
- `camera_zone.tscn` → `layer = 0`, `mask = 0` : elle détecte par sondage de
  position (`Contient`) et n'utilise jamais `BodyEntered`, ses layers ne
  servaient qu'à la déclarer à tort comme du terrain.

**`project.godot`** — section `[layer_names]` ajoutée (absente jusqu'ici : l'éditeur
affichait « Layer 1…32 » sans nom).

### Gain collatéral

`PlateformeFixe`, `PlateformeGlissante`, `PlateformeFragile`, `PlateformeMobile`
et le `MurGauche` de `monde.tscn` étaient traversés par les PNJ. Ils sont devenus
corrects **sans aucune modification** : rester sur le layer 1 par défaut suffit
désormais.

## Vérifié

- `dotnet build` : **0 erreur, 0 avertissement**. La suppression de
  `LayerSolPnj`/`MasqueSolPnj` sert de filet — toute référence oubliée aurait
  cassé la compilation.
- `monde.tscn` chargé en headless : aucune erreur.
- Relevé des layers/masks **réels au runtime** sur tous les `CollisionObject2D`
  de `monde.tscn` : terrain `1/1`, PNJ `4/17`, joueur `2/17`, zones `0/2`,
  traversables `16/0`. Conforme au tableau.
- **Contrôle exhaustif de toutes les paires d'entités** (`layer` de l'une ∩ `mask`
  de l'autre) : aucune entité n'en voit une autre.
- Dérive verticale sur 300 frames physiques : `Pingouin`, `Pingouin2`,
  `LutinNoel`, `LutinNoel2`, `Joueur` — dérive < 0,3 px, aucun ne traverse le sol.
- Boss et projectile instanciés à part (le boss est spawné par `ZoneBossCerf`,
  donc absent au chargement) : boss `4/17`, boule `0/7`, zone de charge `0/2` →
  la boule détecte bien le boss (4 ∩ 7 ≠ 0), le joueur ne heurte pas son corps
  (4 ∩ 17 = 0).

## Reste à faire — test manuel F5

Le headless ne juge pas le ressenti, et le passage du joueur du layer 1 au layer 2
touche **toutes** les zones de détection. À repasser en revue :

- dialogue avec chaque PNJ (pingouins, lutins), panneau en bois ;
- checkpoint, les deux pickups de pouvoir, stalactite piège, plateforme fragile ;
- entrée dans l'arène du boss ;
- **vider la barre de vie du boss à la boule de neige jusqu'à le vaincre** — le
  test critique du couplage ci-dessus ;
- traverser les PNJ et le corps du boss sans blocage, tout en prenant les dégâts
  de la charge ;
- en suspens du rapport précédent : glisser sur une plateforme traversable et
  laisser la glissade se terminer dessus.

## Hors périmètre (constaté, non traité)

`CLAUDE.md` est en retard : il décrit `scripts/Entities/Pnj/` comme ne contenant
que « the `Boss` base + `BossCerf` », alors qu'il existe **9 classes de PNJ**
(`PnjAmical` → Pingouin, LutinCgt, LutinNoel, LutinUsine, PereNoel ; `PnjMechant`
→ Fonceur, LanceurBouleNeige ; `Boss` → BossCerf). Ses sections sur les layers
sont à réécrire après ce changement. `LutinUsine.tscn` / `PereNoel.tscn` sont par
ailleurs rangés dans `scenes/props/noel/` au lieu de `scenes/entites/`.
