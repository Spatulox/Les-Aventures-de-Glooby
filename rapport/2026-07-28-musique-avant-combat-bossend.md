# Musique de l'arène finale : avant / pendant / après le combat

## Objectif

Dans `04-BossEnd.tscn`, la musique de combat (`boss_cerf`) démarrait dès l'entrée
dans l'arène et ne s'arrêtait plus. Désormais le thème de combat ne joue **que**
pendant le combat :

| Moment | Musique |
|---|---|
| Entrée dans l'arène + prologue | `toys` (ambiance `usine`, continuité du niveau) |
| Combat | `boss_cerf` (musique de la zone, inchangée) |
| Chute du boss + épilogue | `toys` de nouveau |
| Générique de fin | `boss_cerf` |

## Changements

### `scripts/Core/ZoneBoss.cs`
- Nouvel export **`NomAmbiancePrologue`** : ambiance HORS combat de l'arène (entrée,
  prologue, puis épilogue). Vide = comportement d'origine (l'arène sonne comme le
  combat dès l'entrée).
- `DeclencherEpilogue` (signal `Vaincu` du boss) coupe le thème de combat **dès la
  chute du boss**, sans attendre le battement `DelaiEpilogue` : l'épilogue se joue sur
  la musique du lieu.
- `Appliquer` (salle caméra) utilise `AmbianceCourante`, qui renvoie
  `NomAmbiancePrologue` avant le combat et `NomAmbiance` ensuite.
- `LancerCombat` bascule sur la musique de combat **explicitement** (drapeau
  `_combatEngage` + `JouerAmbianceSalle`) : la détection de salle du `Player` est à
  hystérésis, elle ne rappelle pas `Appliquer` puisque le joueur ne change pas d'arène
  entre le prologue et le combat.
- `ReinitialiserCombat` (mort du joueur) repasse en avant-combat : revenir à l'arène
  réentend la musique du lieu, le thème de combat repart quand le boss réapparaît.

### `scripts/Common/DeclencheurZone.cs`
- Extraction de **`JouerAmbianceSalle(nomRegion, nomAmbiance)`** (repli sur la région si
  pas de clé propre) hors de `AppliquerCommeSalle` : une salle peut désormais changer de
  musique sans changer de salle, sans dupliquer la règle de repli.

### `scenes/niveaux/04-BossEnd.tscn`
- `ZoneBossFinale` : ajout de `NomAmbiancePrologue = "usine"` (→ `toys`).
  `NomAmbiance = "boss_cerf"` reste la musique du combat.

### `scripts/UI/EcranFin.cs`
- L'export `NomAmbiance` (qui existait déjà, vide) passe par défaut à **`"boss_cerf"`** :
  le thème du boss revient pour le générique. Valeur mise côté `.cs` et non dans
  `ecran_fin.tscn` pour ne pas risquer d'être écrasée par une sauvegarde de scène
  depuis l'éditeur ; elle reste surchargeable par instance dans l'inspecteur.

## Vérification

- `godot --headless --build-solutions --quit` : compilation propre.
- Lancement headless de `04-BossEnd.tscn` : aucun avertissement d'ambiance introuvable
  (seules les erreurs habituelles « Not supported by this display server »).
- Reste à valider à l'oreille en jeu (F5) : bascule au moment de l'apparition du boss.
