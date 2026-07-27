# Usine du Père Noël — Boss Lutin Mecha, mini-jouet explosif et 2 ennemis normaux

Boss alternatif au Boss Cerf : un gros automate de bois piloté par un lutin, avec son
ennemi satellite (le mini-jouet kamikaze qu'il largue). Assets PixelLab + intégration
Godot complète.

## Choix validés par l'utilisateur

- **Silhouette du boss** : bipède tonneau (corps en tonneau cerclé, deux jambes
  courtaudes, bras à pinces, lutin en cockpit ouvert au sommet) — retenu parmi
  4 vignettes, parce que les jambes rendent le saut écrasant lisible.
- **Mini-jouet** : soldat de bois en habit rouge, mèche allumée sur le chapeau —
  retenu parmi 16, parce que ses jambes rendent la course kamikaze naturelle.

## Assets créés

| Dossier | Contenu |
|---|---|
| `assets/pnj/boss_lutin_mecha/` | `idle` (5), `marche` (7), `saut_accroupi` (4), `saut_vol` (3), `saut_impact` (3), `tir_armement` (3), `tir` (2), `trappe` (5), `transition` (5), `vaincu` (9) |
| `assets/ennemis/usine/mini_jouet_explosif/` | `chute` (1), `fonce` (7), `explosion` (7), `parachute.png` |
| `assets/projectiles/eclat_glace/` | `eclat_glace.png` + `impact/` (2 frames) |

**Économie de génération** (dans l'esprit du Boss Cerf) : le saut et le tir ont été
générés en **une seule animation chacun** puis découpés en sous-animations
(saut → accroupi / vol / impact ; tir → armement / tir) ; la **fermeture de trappe
rejoue l'ouverture à l'envers** via `AnimationsSprite.EnregistrerAnimation(..., inverse: true)`.
Le **touché**, l'**onde de choc** et le **balancement du parachute** sont procéduraux.
78 générations réelles, journalisées dans `BUDGET.md` (ligne 22).

Deux animations ont dû être **régénérées** : `vaincu` (le mecha restait debout au lieu
de s'effondrer) et les deux du jouet (l'explosion n'explosait pas, la course ne courait pas).

## Code

| Fichier | Rôle |
|---|---|
| `scripts/Entities/Pnj/BossLutinMecha.cs` | `: Boss`. Machine à états (Intro / Idle / Deplacement / SautAccroupi / SautVol / SautImpact / TirArmement / TirFeu / Trappe / TransitionPhase / Vaincu), 2 phases, 3 patterns télégraphiés, onde de choc, largage des jouets. Tout est `[Export]` (cadences, dégâts, seuil de phase, bornes d'arène). |
| `scripts/Core/ZoneBossLutinMecha.cs` | `: ZoneBoss`. Bornes de déplacement depuis le rectangle de l'arène + persistance de la défaite. **N'enchaîne pas sur l'écran de fin** (c'est une alternative au Cerf, la partie continue). |
| `scripts/Entities/Ennemis/MiniJouetExplosif.cs` | `: LivingEntity`. 3 états publics `Chute / Fonce / Explosion`. |
| `scripts/Entities/Damage/EclatGlace.cs` | `: Projectile`. Éclat + éclatement à l'impact (pendant de `BouleDeNeigeProjectile`). |
| `scripts/Common/DamageSource.cs` | +3 sources : `EclatGlace` (1), `EcrasementMecha` (2), `JouetExplosif` (2). |
| `scripts/Common/Effets.cs` | +`Balancement(...)` — oscillation de rotation en boucle, pendant angulaire de `Flottaison`, réutilisable par tout ce qui pend. |

Scènes : `scenes/boss/BossLutinMecha.tscn`, `scenes/ennemis/usine/MiniJouetExplosif.tscn`,
`scenes/projectiles/EclatGlace.tscn`, et les deux scènes de test
`scenes/test/TestBossLutinMecha.tscn` / `TestMiniJouetExplosif.tscn`.
La barre de vie réutilise `scenes/ui/boss_hud_barre.tscn` via `ZoneBoss`.

## Lisibilité des attaques

Chaque attaque a une **pose d'armement distincte**, tenue assez longtemps pour être lue :

- **Saut écrasant** — accroupi 0,9 s, puis bond, puis impact + onde de choc au sol
  (un joueur en l'air au bon moment n'est pas touché).
- **Tir de glace** — le canon se charge (givre visible) 0,9 s avant le départ de l'éclat.
- **Drop de jouets** — la trappe s'ouvre 0,5 s avant le largage.

**Récompense de l'esquive** : pendant `saut_impact`, le mecha est planté dans le sol et
les coups comptent **double** (`MultiplicateurVulnerable`) — même idiome que
l'étourdissement du Boss Cerf.

## Bug corrigé au passage (code partagé)

`ZoneBoss.FaireApparaitreBoss()` faisait `AddChild(boss)` depuis le signal `BodyEntered`,
donc **en plein flush des requêtes physiques** — Godot refuse d'ajouter un corps avec ses
formes de collision à cet instant (`Can't change this state while flushing queries`).
Bug latent jamais déclenché parce qu'aucune `ZoneBoss` n'était encore posée dans un
niveau ; ma scène de test est la première à l'exercer. L'ajout est désormais **différé**
(`CallDeferred`). **Le Boss Cerf en bénéficie aussi.**

## Vérification (headless, sondes temporaires puis supprimées)

- Compilation propre (0 erreur / 0 avertissement CS).
- Les 11 animations du boss chargent bien leurs frames.
- **Ancrage stable** : `y = 428` (surface du plancher) identique dans tous les états,
  sauf pendant le vol (`y = 419`) — le boss ne saute pas de position en changeant d'état.
- Enchaînement observé : `idle → tir_armement → tir → idle → saut_accroupi → saut_vol →
  saut_impact → transition (phase 2 pile à 20/40 PV) → idle → saut_accroupi → vaincu`.
- Fenêtre de vulnérabilité confirmée : 4 dégâts par boule pendant `saut_impact` contre 2 ailleurs.
- Trappe : ouverture → +2 jouets à 0,5 s → fermeture (animation inversée) → les jouets
  descendent, foncent et explosent.
- Jouet isolé : `Chute → (atterrissage, parachute détaché du jouet) → Fonce → Explosion
  au contact → libération`.
- Aucune erreur runtime sur les deux scènes de test.

## Points à trancher

- ~~**Emplacement des scènes**~~ — corrigé : `boss_cerf.tscn` a été déplacé en
  `scenes/boss/BossCerf.tscn`, les deux boss sont regroupés (voir partie 2).
- **Équilibrage non validé** : `PvMax = 40` et les dégâts sont repris tels quels du Boss
  Cerf (placeholders, cf. `DECISIONS.md`). À régler au ressenti en F5.
- Le boss n'est **pas encore posé dans `monde2.tscn`** — le groupe `Sol` de
  `UsinePereNoel` est toujours vide. Il n'existe pour l'instant que dans sa scène de test.

---

# Partie 2 — Deux ennemis « normaux » pour l'usine

Déclinaisons usine des deux archétypes de la banquise, choisies par l'utilisateur parmi
16 vignettes : **locomotive `[6]`** (chaudière rouge, cabine verte, panache net) et
**mécha `[13]`** (automate rouge/vert à engrenages, bras armé, main vide).

## Assets

| Dossier | Contenu |
|---|---|
| `assets/ennemis/usine/locomotive_jouet/` | `idle` (5), `detection` (5), `charge` (7), `etourdi` (5), `mort` (7) |
| `assets/ennemis/usine/mecha_lanceur/` | `idle` (5), `armer` (5), `lancer` (5), `mort` (5) |

31 générations réelles (`BUDGET.md` ligne 23). Le projectile est **réutilisé tel quel** :
`scenes/ennemis/BouleDeNeige.tscn`, zéro génération.

## Code — où se situe vraiment la réutilisation

`LocomotiveJouet` et `MechaJouetLanceur` sont deux sous-classes **minces** de `PnjMechant`,
qui porte déjà tout le commun : patrouille, `ZoneContact`, `ZoneDetection`, orientation,
`JouerSiPresente` et surtout la **séquence de mort animée** (`Mourir()` joue `mort/` puis
efface en fondu — donc rien à écrire pour la mort). Chaque ennemi n'ajoute que sa machine
à états et ses dossiers d'animations.

**Pourquoi pas une sous-classe directe de `OursDeNeige` / `BonhommeDeNeige` ?** Parce que
leur modèle de dégâts est spécifique à la neige et serait faux ici :
- `OursDeNeige` **ne meurt jamais** (la boule de neige l'étourdit, c'est un obstacle) et
  charge **sans télégraphe** ; la locomotive meurt en PV et doit télégraphier.
- `BonhommeDeNeige` **fond** au pouvoir de chaleur, ne perd jamais de PV, et n'hérite même
  pas de `PnjMechant` (il dérive de `LivingEntity`).

Les deux nouveaux ennemis reprennent donc leurs **structures d'états** à l'identique, sur
la vraie base partagée. Si le bonhomme est un jour migré sur `PnjMechant`, un socle
« lanceur télégraphié » pourra factoriser son cycle armer/lancer/recharge avec le mécha.

| Fichier | Rôle |
|---|---|
| `scripts/Entities/Ennemis/Usine/LocomotiveJouet.cs` | États `Roulement / Detection / Charge / Deraille`. Direction verrouillée dès le télégraphe (esquive d'un pas de côté). L'impact est détecté par `IsOnWall()`. Coups **doublés** pendant le déraillement. |
| `scripts/Entities/Ennemis/Usine/MechaJouetLanceur.cs` | États `Idle / Armer / Lancer`. Statique, vise, arme, lance une `BouleDeNeige` en cloche, recharge. |

Scènes : `scenes/ennemis/usine/LocomotiveJouet.tscn` (3 PV),
`MechaJouetLanceur.tscn` (2 PV), et `scenes/test/TestEnnemisUsine.tscn` (joueur, deux murs
encadrant la locomotive, un exemplaire de chaque). Les murs sont des `StaticBody2D` inline
— fixture de test, calibrés pour être franchissables par le joueur (64 px < 73 px de saut)
mais bloquants pour la locomotive.

## Vérification (headless, sonde temporaire puis supprimée)

- Compilation propre, 0 erreur CS ; les 3 scènes de test tournent sans erreur.
- Ancrage stable : base à `y = 59` sur toutes les animations des deux ennemis.
- **Boucle de la locomotive validée dans les deux sens** : `idle → detection` (42 frames
  ≈ 0,7 s) `→ charge → mur gauche` (`IsOnWall` vrai, x=483) `→ etourdi` (121 frames ≈ 2 s)
  `→ idle → detection → charge → mur droit` (x=937) `→ etourdi`.
- **Cycle du mécha validé sur 3 salves** : `idle → armer` (36 frames ≈ 0,6 s) `→ lancer`
  avec la boule instanciée **la même frame**, `→ idle`, recharge ≈ 2 s.
- Mort des deux ennemis : animation jouée puis nœud libéré.

## Limite d'outil rencontrée, et les correctifs appliqués

`animate_object` (v3) anime **à partir de la frame de base** : il refuse de détruire la
silhouette du sujet ou de changer sa pose de repos.

**Morts — corrigées en procédural (0 génération).** Les frames PixelLab ne s'effondraient
pas (la locomotive perdait sa cheminée, le reste restait debout). Elles ont été
**remplacées** par des frames dessinées : le sprite intact est découpé en blocs de 4-5 px,
projetés vers l'extérieur avec une gravité, qui retombent et s'entassent sur la ligne de
sol. Uniquement des translations sur la grille — ni rotation ni redimensionnement — donc
la palette et la netteté d'origine sont préservées. Vérifié frame par frame : le haut de
la boîte englobante descend de `y=3` à `y=54`, et la dernière frame est un tas plat de 6 px
au sol. C'est la même approche que la destruction des tas de neige (`BUDGET.md` ligne 21).

**Idle du mécha — compensé.** Le sprite retenu a le bras levé au repos, et la régénération
« bras le long du corps » n'a rien changé (v3 ne peut pas modifier la pose de base). Le
télégraphe d'armement est donc doublé de deux effets procéduraux : un **flash chaud** et une
**anticipation en écrase-étire** (le mécha se tasse sur ses appuis, puis se détend au tir).

**Emplacement des scènes de boss — uniformisé.** `scenes/entites/boss_cerf.tscn` a été
déplacé en `scenes/boss/BossCerf.tscn` : les deux boss vivent désormais dans `scenes/boss/`,
avec le nommage PascalCase des dossiers récents (`scenes/sol/usine/`, `scenes/ennemis/`).
Le fichier n'était référencé nulle part, le déplacement est donc sans effet de bord — et
`scenes/boss/` contenait déjà `FondBossCerf.tscn`.

> **Règle à garder** : ne pas relancer PixelLab plus d'une fois sur une destruction ou un
> changement de pose de repos — passer directement au procédural.
