# Zone de chargement de scène (fin de banquise) + diagnostic pingouin

## 1. Diagnostic : le pingouin des guirlandes ne bougeait pas
- Cause **géométrique**, pas liée au dialogue IA : le nœud `Grotte/Pnj/Pingouin`
  (désormais `Pingouin3` côté village dans `monde1.tscn`) partait vers la droite
  et **butait aussitôt sur le bord en biseau du segment de sol** (`Sol2`, normale
  ~63° > `floor_max_angle` 45° → classé mur). Plaqué, il n'atteignait jamais les
  60 px pour faire demi-tour.
- Même symptôme sur `Pingouin2` (coincé contre `Sol3`).
- Aucun correctif appliqué ici (diagnostic seulement) ; pistes proposées :
  demi-tour sur `IsOnWall()` dans `PnjAmical`, ou repositionner les PNJ sur du plat.

## 2. Ajout : `ZoneChargementScene` (transition vers la scène suivante)
Nouvelle zone réutilisable pour passer d'une scène de niveau à la suivante quand
le joueur atteint la fin d'un lieu.

- **`scripts/Core/ZoneChargementScene.cs`** (`: DeclencheurZone`) — à l'entrée du
  joueur : fondu au noir optionnel (`DureeFondu`) puis
  `GetTree().ChangeSceneToPacked(SceneSuivante)` (différé). `UneSeuleFois` forcé.
  - `[Export] PackedScene SceneSuivante` : **laissé vide** (le « xxx.tscn » à
    assigner par instance dans l'éditeur).
  - `[Export] float DureeFondu = 0.5f` (0 = bascule immédiate).
- **`scenes/core/zone_chargement_scene.tscn`** — GameObject `Area2D`
  (`collision_layer=0`, `collision_mask=2` → détecte le joueur) + `CollisionShape2D`
  rectangulaire, facile à déposer/redimensionner dans le monde.
- **`scenes/niveaux/monde1.tscn`** — instance `ZoneChargementSuivante` sous
  `Banquise/Interactifs`, position `(5150, 180)`, `scale (1, 1.5)` (fin de la
  banquise, dernier sol `Sol8` ≈ x 5236). `SceneSuivante` à renseigner (cible
  prévue : `monde2.tscn`, actuellement stub vide).

## Vérification
- `godot --headless --build-solutions` : compile sans erreur.
- Chargement headless de `monde1.tscn` : aucune erreur de parse/instanciation de
  la zone (les erreurs « Not supported by this display server » sont propres au
  headless et pré-existantes).

## Reste à faire (côté éditeur / utilisateur)
- Assigner `SceneSuivante` = `monde2.tscn` (ou la scène cible) sur l'instance.
- Remplir `monde2.tscn` (aujourd'hui vide).
- Note hors périmètre : `EcranFin.cs` référence encore `res://scenes/niveaux/monde.tscn` (renommé en monde1/monde2) → lien mort à corriger.
