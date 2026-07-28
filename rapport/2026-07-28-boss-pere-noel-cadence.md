# Boss Père Noël — cadence relevée et va-et-vient

Un seul fichier touché : `scripts/Entities/Pnj/BossPereNoel.cs`. Aucun `.tscn`, aucun asset (le budget PixelLab est clos : seules `idle` et `marche` existent, et `marche` devient enfin visible).

## Problèmes corrigés

1. **Cycle trop lent** — 2 à 3,8 s entre deux attaques en phase 1.
2. **Boss statufié** — l'état `Approche` ne se déclenchait que si le joueur dépassait 200 px. L'arène de `04-BossEnd` ne fait que ~765 px de large : la condition n'arrivait quasi jamais, l'anim `marche` n'était jamais jouée.
3. **TP puni le boss** — `DistanceReapparition` (220) > `DistanceConfort` (200) : chaque cheminée était suivie d'une approche de 1,8 s **sans attaque**.

## Changements

**L'état `Approche` est supprimé ; l'`Idle` devient mobile** (`PatinerAutourDuJoueur`). Le boss tient une *bande* de distance au lieu d'un seuil :
- `> DistanceEngagement` (220) → il avance à `VitesseMarche` (95) ;
- `< DistanceConfort` (120) → il recule à `VitesseRecul` (70) ;
- entre les deux → piétinement avant/arrière, sens inversé toutes les `DureeOscillation` (0,4 s).

Il reste **toujours tourné vers le joueur**, marche arrière comprise. Contre une borne d'arène le pas est annulé (anim → `idle`) au lieu de pousser le mur. La friction est désormais coupée sur `Idle` seulement : tous les états de combat restent strictement immobiles pour que les télégraphes se lisent.

Conséquence : `ChoisirPattern` n'a plus de branche « rejoindre le joueur » — **chaque fin d'idle débouche sur une attaque**.

**Cheminée devenue menaçante** : la sortie de TP enchaîne directement sur une attaque (`AttaquerImmediatement`, tirage cadeaux/givre) au lieu de repasser par l'idle ; `DistanceReapparition` passe à 150 (dans la bande de tir) ; si le clamp des bornes écrase la cible (arène étroite), il ressort du côté opposé plutôt que collé au mur ; deux cheminées d'affilée sont interdites (`_dernierPattern`).

**Cadence** (valeurs par défaut, aucune surchargée en scène) :

| | Avant | Après |
|---|---|---|
| Idle phase 1 / phase 2 | 1,0–1,6 / 0,5–1,0 | **0,45–0,8 / 0,2–0,45** |
| `DelaiArmementCadeaux` / `DureeLargage` / `DureeEssouffle` | 0,9 / 0,4 / 0,9 | **0,5 / 0,25 / 0,55** |
| `DelaiArmementGivre` / `DureeJet` | 0,8 / 0,4 | **0,45 / 0,25** |
| `DureeDisparition` / `DureeReapparition` | 0,35 / 0,35 | **0,25 / 0,25** |
| Tirage patterns | 45 / 40 / 15 % | **45 / 35 / 20 %** |

Exports **nouveaux** : `VitesseRecul`, `DistanceEngagement`, `DureeOscillation`. Export **supprimé** : `DureeApprocheMax`.

PV (45), `SeuilPhase2`, `MultiplicateurVulnerable` (×2) et le nombre de cadeaux (1 / 3) sont inchangés : la difficulté vient de la cadence et de la mobilité, pas d'un mur de projectiles.

## Vérification

- `godot --headless --build-solutions --quit` → compilation propre, zéro warning.
- `scenes/test/TestBossPereNoel.tscn` en headless (700 puis 1600 frames) → aucune erreur runtime.
- Observation instrumentée (~34 s de jeu) avec les bornes réelles de `04-BossEnd` reproduites : le boss oscille bien entre `dist` 152 et 382, alterne `marche` / `idle` (11 / 19 échantillons), et **reste dans le couloir [77, 722]** — jamais collé à un mur.
- `04-BossEnd.tscn` en headless ne peut pas être testé jusqu'au combat : le boss est derrière le dialogue de prologue, infranchissable sans input.

**Reste à faire : un play-test manuel** (`godot`, niveau BossEnd). Le headless ne juge pas le game feel. À surveiller : lisibilité des télégraphes (rouge = cadeaux, bleu = givre) à la nouvelle cadence — si c'est trop serré, remonter `DelaiArmementGivre` / `DelaiArmementCadeaux` de 0,1 s.
