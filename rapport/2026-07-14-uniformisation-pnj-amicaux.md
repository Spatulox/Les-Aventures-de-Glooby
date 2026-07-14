# Uniformisation des PNJ amicaux legacy sur `PnjAmical`

Fusion des trois PNJ « legacy » (`Node2D` avec chargement d'anim dupliqué + bulle câblée à la
main) dans le pattern unifié `PnjAmical` / `AnimationsSprite` / `DeclencheurDialogue`, comme
`Pingouin.cs` et `Boss.cs`.

## Changements

### `PnjAmical` (base) — 2 ajouts réutilisables
- **Garde statique** dans `_PhysicsProcess` : si `DistancePatrouille <= 0`, le PNJ reste
  immobile (idle) au lieu de patrouiller.
- **Lecture d'anim robuste** (`ChoisirAnimation`) : joue `parler` pendant une conversation si
  l'anim existe, `marche` seulement si le dossier a des frames, sinon `idle` — évite de jouer
  une animation vide pour les PNJ statiques.

### Pingouin fusionné
- `Pingouin.ConstruireAnimations()` enregistre désormais `parler` (7 fps), jouée
  automatiquement pendant les dialogues.
- Frames déplacées : `assets/pnj/pingouin_ancien/parler` → `assets/pnj/pingouin/parler`.
- **Supprimés** : `assets/pnj/pingouin_ancien/`, `scripts/Entities/PNJPingouin.cs` (+uid),
  `scenes/pnj/PNJPingouin.tscn`. Plus aucune référence `pingouin_ancien`/`PNJPingouin`.

### `LutinCGT` → `LutinCgt : PnjAmical`
- Réécrit dans `scripts/Entities/Pnj/LutinCgt.cs` (statique, `DistancePatrouille = 0`).
  Conserve l'`enum PoseLutin`, le dictionnaire de poses (dossier + rectangle du slogan) et le
  `Label "Slogan"` configuré dans `Initialiser()`. Dialogue via `Talkative`.
- Nouvelle scène `scenes/entites/lutin_cgt.tscn` (CharacterBody2D + Sprite2D + DeclencheurDialogue) ;
  ancienne `scenes/pnj/LutinCGT.tscn` supprimée (dossier `scenes/pnj/` vidé/retiré).

### `PNJSimple` supprimé → sous-classes concrètes
- `scripts/Entities/PNJSimple.cs` (+uid) supprimé.
- Créés `scripts/Entities/Pnj/LutinUsine.cs` et `PereNoel.cs` (`PnjAmical` statiques, dossier
  d'idle codé en dur).
- Scènes `scenes/props/noel/{LutinUsine,PereNoel}.tscn` réécrites sur la structure `PnjAmical`
  (chemins conservés → `DemoUsine.tscn` inchangé). `DialogueTexte` (chaîne) → `Lignes` (string[]),
  `AuPassage = true`.

### Placeholders
- Ajout de `placeholder.png` (carré de couleur uni) dans `assets/pnj/{lutin_cgt,lutin_usine,pere_noel}/`.

## Vérification
- Import assets (`--import`) et build .NET (`--build-solutions`) : OK, 0 erreur.
- Chargement headless de `DemoUsine.tscn`, `lutin_cgt.tscn`, `PereNoel.tscn` : aucun script
  error / nœud manquant / animation vide.
- Grep de non-régression : plus aucune occurrence live de `pingouin_ancien`, `PNJPingouin`,
  `PNJSimple`, `LutinCGT`.
- `monde.tscn` non touché.
