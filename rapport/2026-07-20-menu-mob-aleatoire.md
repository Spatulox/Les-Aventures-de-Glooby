# Menu principal : mob aléatoire

## Objectif

Le personnage affiché à droite de l'écran-titre était toujours le joueur (`assets/player/idle`, codé en dur).
Il est désormais **tiré au hasard à chaque ouverture du menu** parmi tous les mobs disposant d'une animation
`idle` — le joueur et les PNJ — comme l'était déjà l'image de fond.

## Changements — un seul fichier : `scripts/UI/MenuPrincipal.cs`

- `AjouterPingouinIdle()` → **`AjouterMobAleatoire()`** : tire un dossier de mob, charge son `idle` et le monte
  sur l'`AnimatedSprite2D` décoratif (position, `FlipH` et cadence 6 fps bouclée inchangés).
- **`MobsDisponibles()`** : liste déduite du disque (`assets/player` + chaque sous-dossier de `assets/pnj`),
  filtrée sur « possède un dossier `idle` d'au moins 2 frames ». Rien n'est codé en dur : ajouter un PNJ animé
  suffit à le faire entrer dans le tirage. Le seuil de 2 frames écarte les `idle` placeholder figés
  (`lutin_noel`, `lutin_usine`) ; `fonceur`, `lanceur_boule_neige` (placeholder à plat) et `lutin_cgt`
  (pas d'anim `idle`) sont écartés faute de dossier. Le test `DirAccess.DirExistsAbsolute` en amont évite les
  `ERROR` que `ChargerFrames` journalise sur un chemin absent.
- **`MobAleatoire()`** : même motif de tirage que `FondAleatoire()` (`GD.Randomize` + `GD.Randi() % n`).
- **`ChargerFramesIdle(dossierMob)`** : la méthode existante est simplement paramétrée par le dossier.
- **Hauteur normalisée** : les frames vont de 64×64 à 96×96 ; le `Scale = 2.5` en dur est remplacé par
  `HauteurMob / hauteur_de_la_frame` (`HauteurMob = 240f`, soit 96 × 2.5). Tous les mobs occupent la même
  hauteur et, le sprite étant centré, partagent la ligne de sol du joueur d'avant.

Réutilise `AnimationsSprite.ChargerFrames` / `EnregistrerAnimation` — aucun nouveau code de chargement.

## Vérification

- `godot --headless --build-solutions --quit` : compile propre.
- 10 lancements headless instrumentés : les 4 mobs éligibles sortent bien
  (`player` échelle 2.5 — identique à avant —, `pingouin` 3.75, `boss_cerf` 2.61, `pere_noel` 2.5).
- Run headless final : aucune `ERROR` (hors `KeyboardGetLabelFromPhysical`, préexistante et propre au headless).
- **Reste à faire par un humain** : un `godot` interactif pour juger le rendu — que le boss cerf, le plus large,
  ne chevauche pas la colonne de boutons.
