# Caméra & fond de région par détection continue — 2026-07-13

## Problème
Le changement de salle reposait sur des déclencheurs par bord (`BodyEntered`) : `CameraZone`
pour les limites caméra, `RegionTrigger` séparés pour le fond. Une téléportation (respawn)
ne traverse aucune `Area2D` → la caméra restait bloquée sur la salle où l'on était mort, et
il fallait entretenir des nœuds de frontière à la main.

## Solution : un seul mécanisme continu
Chaque frame, le `Player` détecte par position la `CameraZone` qui le contient et l'applique
(limites caméra **+** fond de région). Robuste aux téléportations, plus de `RegionTrigger`.

- **`scripts/Core/CameraZone.cs`**
  - Nouvel export `NomRegion` : la zone appelle elle-même `BackgroundManager.AfficherRegion`
    dans `Appliquer(Player)` (limites + fond en un seul point).
  - N'est plus déclenchée par `BodyEntered` (`PreparerDeclencheur` s'inscrit au groupe
    `"zones_camera"` et retourne `false` pour ne pas câbler le signal). Helper `Contient`.
- **`scripts/Entities/Player/Player.cs`**
  - `MettreAJourZoneCamera()` appelée chaque frame (après `MoveAndSlide`) et au respawn.
    Hystérésis : garde la zone courante tant qu'aucune autre ne contient le joueur — gère
    les petits trous entre zones, les sauts et les téléportations sans à-coup. Champ
    `_zoneCameraActive`. Remplace l'ancien `ReappliquerZoneCamera()`.
- **`scenes/niveaux/monde.tscn`**
  - `NomRegion` posé sur `ZoneVillage`/`ZoneBanquise` (`banquise`) et `ZoneGrotte` (`grotte`).
  - Retrait des 2 instances `RegionTrigger` (`GrotteVersBanquise`, `BanquiseVersGrotte`) et
    de l'`ext_resource` `region_trigger` inutilisé (nœuds `Frontiere` vides conservés).
- **`scripts/Core/BackgroundManager.cs`** + **`CLAUDE.md`** : commentaires/notes à jour.
- `RegionTrigger.cs` (+`.uid`) et `region_trigger.tscn` **supprimés** (plus aucune
  référence : `CameraZone.NomRegion` remplace entièrement leur rôle).

Emprises mesurées (rect 256) : Village [0,634] · Banquise [636,2574] · Grotte [2621,5027]
— non chevauchantes, d'où le besoin d'hystérésis pour traverser les petits trous.

## Vérification
- `godot --headless --build-solutions --quit` : build C# OK, 0 erreur/warning.
- `godot --headless --quit-after 200` : aucune erreur runtime/scène, aucun warning caméra ;
  plus aucune référence pendante à `region_trigger` dans `monde.tscn`.
- Reste le play-test manuel (`godot`) : Village→Banquise→Grotte (caméra + fond suivent),
  et mort/chute en Grotte après checkpoint Village → caméra **et** fond reviennent au
  Village au respawn.
