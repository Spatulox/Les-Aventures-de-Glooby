# Franchissement automatique des marches — 2026-07-28

**État : corrigé partiellement** (tests arrêtés à la demande, calibrage non terminé).

## Demande

Le joueur doit passer les petits obstacles (~10px) comme une marche/un escalier, sans se
bloquer — sans casser la mécanique de pente douce/forte.

## Point de départ : le mécanisme existait déjà, et échouait d'un pixel

`Player.GererMarcheAutomatique` (ajouté le 2026-07-21) franchit les ressauts par sondes
`TestMove`. Deux défauts le rendaient inopérant précisément là où on l'attendait :

1. **Boucle bornée à `HauteurMarcheMax` pile.** Le plus haut décollement testé valait
   exactement la hauteur annoncée : pour un ressaut de cette hauteur la capsule finissait
   *à ras* de l'arête, et `TestMove` (marge 0,08px) y voyait encore une collision. La
   marche annoncée était donc la seule à ne jamais passer.
2. **Le test de plafond confondait plafond et face du ressaut.** `TestMove(depart, montee)`
   avec `montee` purement verticale rapportait le contact latéral avec la face à franchir
   — c'est-à-dire toujours, dès que le joueur y était collé. La recherche s'arrêtait donc
   dès `hauteur = 1`, et le franchissement ne fonctionnait que par chance, selon que la
   capsule était encore à un cheveu de la face. **C'est ce qui rendait le seuil non
   monotone** (mesuré : à seuil 12, 16px passait mais 18/20/22 bloquaient).

## Changements — `scripts/Entities/Player/Player.cs` (seul fichier de code)

- Nouvel export **`MargeFranchissement = 1f`** ; la boucle monte jusqu'à
  `HauteurMarcheMax + MargeFranchissement` (corrige le défaut 1).
- Le test de plafond exige désormais une **vraie normale de plafond** (`normal.Y > 0.5`)
  via un `KinematicCollision2D` réutilisé (`_contactMarche`, alloué une fois) — corrige le
  défaut 2, et rend le comportement monotone.
- **`HauteurMarcheMax` 10 → 20**.
- **`FloorSnapLength` 8 → 12** (`_Ready`) : recolle le joueur en *descente* de marche, et
  au passage fiabilise la glissade sur pente forte. Consigné dans `DECISIONS.md`.
- Commentaires de classe et de méthode mis à jour.

## Mesures (harnais headless jetable, supprimé depuis)

Marches de hauteur connue franchies dans les deux sens, `HauteurMarcheMax` forcé par essai.
**L'export ne vaut pas la hauteur franchie** : la capsule (scale non uniforme 1.04×1.2) et
le snap encaissent une dizaine de pixels « gratuits ».

| `HauteurMarcheMax` | marche réellement franchie |
|---|---|
| 0 (moteur seul) | 10px |
| **10 (ancienne valeur)** | **10px** — bloque à 15 |
| 16 | 20px |
| **20 (valeur retenue)** | **30px** — bloque à 34 |

Non-régressions vérifiées : descentes de 10/20/22px OK ; escalier 3×15px monté d'une
traite ; **pente douce montante** gravie jusqu'au plateau (-177) **sans glissade
parasite** ; **pente forte descendante** déclenche bien la glissade obligatoire et dévale
jusqu'en bas. Build C# propre, `--headless` sans nouvelle erreur.

## Ce qui reste ouvert

- **Calibrage non terminé.** `HauteurMarcheMax = 20` donne un franchissement réel d'environ
  **30px**, soit plus que les ~20px visés. Pour un seuil réel de 20px il faudrait
  descendre l'export à **16**. À trancher au play-test.
- **Anomalie non expliquée : une marche de 24px passe à tous les seuils testés**, y compris
  à seuil 12 où 21/22/23/25/26 bloquent. Bande isolée, cause non identifiée (piste : le
  scale non uniforme du `CapsuleShape2D`, que le serveur physique ne peut pas représenter
  exactement). Non élucidé.
- **Play-test manuel non fait** — le ressenti d'une montée instantanée et du snap de 12px
  ne se juge pas en headless.

## Constaté au passage, non corrigé (hors périmètre choisi)

- `01-monde1.tscn:312` — `Banquise/Sol/Sol4` à y=351 quand ses voisins sont à 329 :
  **marche de 22px** à x≈3635. Elle passe désormais grâce au correctif, mais reste un
  défaut de géométrie.
- `scenes/sol/usine/PenteUsine*.tscn` — polygones bâtis sur une surface `y = 8` alors que
  la collision des dalles `SolUsineBois*` est à `y = 39` : **31px** de marche dès qu'une
  pente usine sera posée à plat (aujourd'hui masqué, toutes les instances de monde2 sont
  pivotées en murs).
- `EstSurPenteForte()` ne teste que `PenteBanquise` : une `PenteUsineBois` forte (44,8°)
  n'impose jamais la glissade.
