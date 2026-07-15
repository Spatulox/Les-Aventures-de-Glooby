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

## Correctifs suivants (même conversation)

### Orientation du pingouin (marchait « à l'envers »)
- L'art PNJ du projet regarde **à gauche** par défaut, alors que la logique de `FlipH`
  supposait un art tourné à droite → pingouin tourné à l'envers en marchant.
- Ajout dans `PnjAmical` d'un `[Export] bool ArtRegardeADroite` (défaut false = art à gauche)
  et calcul du miroir en conséquence. Corrige le pingouin sans toucher aux scènes.

### Passage à un `AnimatedSprite2D` de scène (comme `Boss`)
Suite au retrait des `placeholder.png` et des nœuds `Sprite2D` des scènes (remplacés par un
`AnimatedSprite2D` vide dans chaque `.tscn`) :
- `PnjAmical` et `PnjMechant` ne construisent plus d'`AnimatedSprite2D` à la volée avec repli
  carré : ils récupèrent l'`AnimatedSprite2D "AnimatedSprite2D"` de la scène et y chargent
  `SpriteFrames = ConstruireAnimations()` au `_Ready` (exactement le pipeline de `Boss.cs`).
  Dossier vide => animation sans frame (PNJ invisible), plus de carré placeholder.
- Scènes `PereNoel.tscn` / `LutinUsine.tscn` reconverties en `AnimatedSprite2D` (retrait du
  `Sprite2D` + texture placeholder supprimée). Commentaires « carré placeholder » nettoyés
  dans toutes les sous-classes (Pingouin, LutinNoel, LutinCgt, LutinUsine, PereNoel, Fonceur,
  LanceurBouleNeige).
- `placeholder.png` conservés uniquement pour `fonceur/` et `lanceur_boule_neige/`.

## Vérification
- Build .NET (`--build-solutions`) : OK, 0 erreur.
- Chargement headless de `pingouin`, `lutin_noel`, `fonceur`, `lanceur_boule_neige`,
  `lutin_cgt`, `PereNoel`, `DemoUsine` + boot `monde` : aucun script error / nœud manquant.
- Plus aucune référence `placeholder` / `Sprite2D` dans les scènes PNJ.
- Grep de non-régression : plus aucune occurrence live de `pingouin_ancien`, `PNJPingouin`,
  `PNJSimple`, `LutinCGT`.
- `monde.tscn` non modifié.
