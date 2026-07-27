# Boss Lutin Mecha + mini-jouet explosif (usine du Père Noël)

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

- **Emplacement des scènes** : `scenes/boss/BossLutinMecha.tscn` suit ta consigne, mais
  `boss_cerf.tscn` vit dans `scenes/entites/` — les deux boss sont donc dans des dossiers
  différents. À uniformiser si ça te gêne.
- **Équilibrage non validé** : `PvMax = 40` et les dégâts sont repris tels quels du Boss
  Cerf (placeholders, cf. `DECISIONS.md`). À régler au ressenti en F5.
- Le boss n'est **pas encore posé dans `monde2.tscn`** — le groupe `Sol` de
  `UsinePereNoel` est toujours vide. Il n'existe pour l'instant que dans sa scène de test.
