# Portée par zone + aperçu éditeur — bonhomme & ours de neige

## Objectif
Rendre la portée de détection des ennemis « bonhomme de neige » et « ours de neige » réglable **par instance** via une `CollisionShape2D`, et leur redonner un **aperçu dans l'éditeur** (1re frame d'idle), comme les autres PNJ.

## Changements

### `scripts/Entities/LivingEntity.cs` (logique mutualisée)
- Ajout de `CablerZoneDetection(nom = "ZoneDetection")` : câble une `Area2D` enfant facultative, suit le `Player` via `BodyEntered`/`BodyExited`, expose `ZoneDetectionPresente`.
- Ajout de `JoueurAPortee(out distance)` : renvoie le joueur présent dans la zone (`distance = 0`) ou `null`/`MaxValue` ; **repli** sur `JoueurLePlusProche` sans zone. Contrat : `distance == 0` = « dans la zone », donc les IA gardent leur test `joueur == null || distance > Portee` inchangé.
- `JoueurLePlusProche` **remonté ici** (dédupliqué de `PnjMechant` et `BonhommeDeNeige`).

### `scripts/Entities/Pnj/PnjMechant.cs`
- `_Ready` : appelle `CablerZoneDetection()`.
- `_PhysicsProcess` : `JoueurLePlusProche` → `JoueurAPortee`.
- Copie locale de `JoueurLePlusProche` supprimée. **`OursDeNeige` et `LanceurBouleNeige` inchangés** (repli distance pour le lanceur sans zone).

### `scripts/Entities/Ennemis/BonhommeDeNeige.cs`
- `_Ready` : appelle `CablerZoneDetection()`.
- `_PhysicsProcess` : `JoueurLePlusProche` → `JoueurAPortee` ; copie locale supprimée. Test Idle `distance <= Portee` inchangé.

### Scènes
- `scenes/ennemis/BonhommeDeNeige.tscn` et `scenes/ennemis/OursDeNeige.tscn` : ajout d'un `Sprite2D` **`Apercu`** (texture = `.../idle/00.png`, masqué au runtime par `MasquerApercuEditeur` déjà appelé) et d'une `Area2D` **`ZoneDetection`** (`CircleShape2D`, `collision_mask = 2`) à redimensionner par instance dans `monde.tscn`. Rayons placeholders (200 / 140).

### `CLAUDE.md`
- Documentation des deux conventions réutilisables (`Apercu` éditeur, `ZoneDetection` portée par instance) dans la section `LivingEntity`.

## Notes
- Les fichiers de l'ours ont été renommés/déplacés par l'utilisateur en cours de tâche : `Pnj/OursDeNeige.cs` → `Ennemis/OursDeNeige.cs`, `entites/ours_de_neige.tscn` → `ennemis/OursDeNeige.tscn`.

## Vérification
- `godot --headless --build-solutions --quit` : compilation propre.
- `godot --headless --quit-after 200` : boot OK (seules erreurs « Not supported by this display server » du menu en headless, préexistantes et sans lien).
- F5 manuel recommandé : redimensionner une `ZoneDetection` sur une instance dans `monde.tscn` et confirmer que la détection suit le cercle ; vérifier l'aperçu des ennemis dans l'éditeur.
