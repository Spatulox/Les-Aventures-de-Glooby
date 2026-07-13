# Système de sauvegarde de la progression

Persistance disque de la progression + « Continuer » fonctionnel, avec les données
regroupées dans une **structure dédiée** dont `GameState` est le gestionnaire.

## Changements

### Nouveaux fichiers
- **`scripts/Core/DonneesSauvegarde.cs`** — classe de données regroupant tout l'état
  sauvegardable (version, PV, poissons, pouvoir chaleur, checkpoint id + position,
  `ElementsConsommes`, `BossVaincus`) + sa propre (dé)sérialisation Godot
  (`VersDictionnaire` / `DepuisDictionnaire`, tolérante aux clés absentes).
- **`scripts/Core/Sauvegarde.cs`** — helper statique d'E/S disque JSON
  (`user://sauvegarde.json`) : `Existe` / `Ecrire` / `Lire`.

### `GameState` devient gestionnaire de `DonneesSauvegarde`
- Détient `private DonneesSauvegarde _donnees` ; les propriétés `Pv`, `Poissons`,
  `PouvoirChaleurActif`, `CheckpointId/Position` et `EstConsomme/MarquerConsomme`
  deviennent une **façade** déléguant à `_donnees` (aucun appelant externe cassé).
- `NouvellePartie()` = simple remplacement d'instance (`new DonneesSauvegarde { Pv = PvMax }`).
- Ajout : `Sauvegarder()`, `Charger()` (remplace l'instance puis ré-émet PvChanges /
  PoissonsChanges / CheckpointActif), API boss `EstBossVaincu`/`MarquerBossVaincu`.
- `SauvegardeExiste` → `Sauvegarde.Existe()` (au lieu de `false` codé en dur).

### Déclencheurs de sauvegarde
- **`Checkpoint.SurEntreeJoueur`** — `Sauvegarder()` à **chaque contact** d'un campement
  (même déjà actif), pas seulement au changement de checkpoint.
- **`ZoneBossCerf.SurVictoire`** — `MarquerBossVaincu` + `Sauvegarder()` avant l'écran de fin.

### Chargement
- **`MenuPrincipal.ContinuerPartie`** — appelle `Charger()` avant de charger `monde.tscn`.
- **`Player._Ready`** — se téléporte au checkpoint chargé (`TeleporterAuCheckpoint`) si une
  partie a été restaurée ; nouvelle partie = position zéro, spawn du `.tscn` conservé.
- **`ZoneBoss.SurEntreeJoueur`** — ne respawn pas un boss déjà vaincu (via `EstBossVaincu`).

## Vérification
- `godot --headless --build-solutions --quit` : build propre, zéro erreur.
- `godot --headless --quit-after 200` : menu chargé sans erreur (seule l'erreur pré-existante
  `KeyboardGetLabelFromPhysical` du menu Paramètres, propre au display server headless).
- Round-trip complet (créer une partie → checkpoint → quitter → Continuer → reprise état/position,
  boss non-respawné) : à valider en play-test manuel (F5), non reproductible en headless.
