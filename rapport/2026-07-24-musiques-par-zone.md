# Musiques par zone : probabilités éditables + pause/reprise au blizzard (2026-07-24)

Câblage des 7 pistes `.mp3` déposées, ajout d'un **tirage pondéré éditable dans
l'inspecteur**, et changement du comportement musical du blizzard vers une
**vraie pause/reprise** (au lieu d'un crossfade qui détruisait la piste normale).

La tuyauterie audio existait déjà (autoload `GestionnaireAudio`, ressources
`AmbianceSonore`/`VarianteAmbiance` par lieu, 2 bus Musique/Ambiance, fondus
croisés, hook `DefinirEtat("blizzard"/"normal")` depuis la météo). Le travail a
donc porté sur la pondération, la pause/reprise, et le remplissage des `.tres`.

## Décisions (validées avec l'utilisateur)

- Proba ≠ 100 % → **normalisation sur la somme réelle + avertissement console**
  (pas de blocage).
- Blizzard → **vraie pause + reprise à la position** du morceau normal.

## Changements

| Fichier | Changement |
|---|---|
| `scripts/Core/PisteMusicale.cs` | **nouveau** — `[GlobalClass] Resource` : paire (`Musique`, `Probabilite`) éditable dans l'inspecteur. Unité d'une playlist pondérée. |
| `scripts/Core/VarianteAmbiance.cs` | `Musiques` passe de `Array<AudioStream>` à `Array<PisteMusicale>` ; + `TirerMusique(dernierIndex, out index)` : tirage pondéré, évite la répétition immédiate, `PushWarning` si somme ≠ 100 (normalise quand même). |
| `scripts/Core/GestionnaireAudio.cs` | Canal Musique = tirage pondéré (Ambiance reste uniforme). **Pause/reprise blizzard** : `_musiqueSuspendue` + `SuspendreMusique` (fondu ↓ puis `StreamPaused`) / `ReprendreMusiqueSuspendue` (reprend à la position) / `LibererMusiqueSuspendue` (au changement de lieu). `BasculerCanal`/`Enchainer` factorisés via `NombrePistes`/`ProchainePiste`. |
| `assets/audio/ambiances/village.tres` | variante `normal` = Winter Wonderland Quest (50) + Snowy Village Morning (50). |
| `assets/audio/ambiances/banquise.tres` | `normal` = Frozen Horizon (100) ; `blizzard` = The Frost Before the Fall (100). |
| `assets/audio/ambiances/grotte.tres` | `normal` = Echoes in the Deep (100). |
| `assets/audio/ambiances/boss_cerf.tres` | `normal` = Frostborn Charge (50) + Frostborn Charge (1) (50). |
| `assets/audio/ambiances/menu.tres` | migré au format `PisteMusicale` ; piste = **Le_Pingouin-Judoka-Ponga** (100), remplace l'ancien `ice_cave_lofi` (désormais orphelin). |
| `assets/audio/ambiances/usine.tres` | **nouveau** — `Nom = "usine"` sans variante (silencieux) : évite le warning « ambiance introuvable » de `ZoneUsine` (monde2) et prêt pour la musique d'usine à venir. |

## Câblage des zones

- **monde1** : `ZoneVillage.NomAmbiance="village"` déjà posé ; `ZoneBanquise` et
  `ZoneGrotte` retombent sur leur `NomRegion` (`banquise`/`grotte`). Rien à
  éditer — les `.tres` suffisent.
- **monde2** : `ZoneBanquise` → `banquise` ; `ZoneUsine` → `usine` (placeholder
  silencieux).
- **Blizzard** : la banquise a maintenant une variante `blizzard` avec sa propre
  piste. `GestionnaireMeteo` appelait déjà `DefinirEtat` — le nouveau code met la
  musique normale en pause et la reprend à la fin.

## Reste à faire

- **Boss** : aucun `ZoneBossCerf` n'est encore instancié dans les scènes. Quand
  l'arène sera posée, mettre `NomAmbiance = "boss_cerf"` sur la zone (son
  `Appliquer` appelle déjà `AppliquerCommeSalle` → `JouerAmbiance`) et laisser
  l'export `Musique` vide → tirage aléatoire pondéré des 2 pistes Frostborn.
- **Usine** : déposer les pistes d'usine, remplir `usine.tres` (mêmes `PisteMusicale`).

## Vérification

- `godot --headless --build-solutions --quit` → compilation propre, classes
  globales enregistrées (`PisteMusicale` incluse).
- `godot --headless --import` → aucun échec de parse des `.tres`.
- Boot menu (`--quit-after 120`) → charge l'ambiance `menu` au nouveau format,
  aucun warning.
- `monde1.tscn` / `monde2.tscn` (`--quit-after 200`) → **aucun** warning
  « ambiance introuvable », aucune erreur de script liée à l'audio. (Les erreurs
  « Not supported by this display server » sont préexistantes et sans rapport
  avec l'audio.)
- **Test manuel `godot` recommandé** (le headless ne juge pas le son) : entrée
  village → 1 des 2 pistes, ~50/50 sur plusieurs entrées ; aller-retour = pas de
  redémarrage ; banquise → Frozen Horizon ; forcer un blizzard
  (`MeteoZone.ChanceBlizzard = 1f` temporairement) → The Frost Before the Fall en
  fondu, Frozen Horizon en pause, puis **reprise à la position** en fin de
  blizzard (remettre `ChanceBlizzard = 0.2f`) ; grotte pendant un blizzard = pas
  de piste suspendue orpheline ; éditer une proba ≠ 100 → warning console.
