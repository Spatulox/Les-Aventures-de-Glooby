# Système de caméra réutilisable (CameraZone)

## Objectif
Rendre le système de caméra (façon Hollow Knight) le plus réutilisable possible :
supprimer la double saisie redondante, découpler du joueur, et faire des zones des
GameObjects glissables/éditables dans l'éditeur Godot.

## Constat de départ
- `Camera2D` enfant du joueur (auto-suivi) ; seules ses `Limit*` sont modifiées.
- `CameraZone : DeclencheurZone` portait 4 `[Export] int` (LimGauche/Droite/Haut/Bas)
  saisis à la main, alors qu'ils **égalaient exactement l'AABB monde de la
  `CollisionShape2D`** de chaque zone (double source de vérité, dérive possible).
- Couplage en dur : `joueur.GetNode<Camera2D>("Camera2D")` + `SeuilChuteVide = LimBas + 300f`
  (offset magique).
- 8 zones posées à la main sous `ZonesCamera` (Area2D+script nus), pas de `.tscn`.

## Changements

### `scripts/Core/CameraZone.cs` (réécriture)
- Les limites sont **dérivées de la `CollisionShape2D`** de la zone (`CalculerLimitesDepuisForme` :
  AABB = `forme.GlobalPosition ± rect.Size/2 * GlobalScale`, arrondi). La forme est trouvée
  par type (marche pour le `.tscn` comme pour d'anciens nœuds). Aucun mode de limites
  manuel (choix utilisateur) — le rectangle dessiné *est* la salle.
- `[Export] float MargeChuteVide = 300f` remplace le `+300f` codé en dur.
- Garde-fou : si pas de `RectangleShape2D`, `GD.PushWarning` + no-op.

### `scripts/Entities/Player/Player.cs` (découplage)
- Camera2D mise en cache dans `_Ready` (`_camera`).
- Nouvelle méthode publique `DefinirZoneCamera(gauche, droite, haut, bas, margeChute)` :
  applique les 4 limites et recale `SeuilChuteVide = bas + margeChute`. `CameraZone` ne
  connaît plus le chemin `"Camera2D"` ni l'offset. `SeuilChuteVide` reste un champ dérivé
  (non `[Export]`).

### `scenes/core/camera_zone.tscn` (nouveau GameObject)
- `Area2D` (script `CameraZone`) + `CollisionShape2D` avec une `RectangleShape2D`
  `resource_local_to_scene = true` : chaque instance glissée obtient sa propre forme
  redimensionnable sans « Make Unique ». Workflow : glisser, redimensionner → bornes suivent.

### Migration des 8 zones de `scenes/niveaux/monde.tscn`
- Les 8 `@Area2D@NN` (sous `ZonesCamera`) converties en **instances** de `camera_zone.tscn`
  (`ZoneCam1..8`), forme de collision existante réutilisée en override, 4 int supprimés
  (dérivés de la forme — comportement identique, forme==AABB vérifié).
- Ext_resource du script `CameraZone` (`12_qf4rj`) retiré (plus référencé) ; ext_resource
  `PackedScene` de `camera_zone.tscn` ajouté. Override caméra de départ sur `Joueur`
  (`limit_right=2752 limit_bottom=400`) conservé.

### Documentation
- `CLAUDE.md` : description caméra mise à jour (limites dérivées de la forme,
  `camera_zone.tscn`, `Player.DefinirZoneCamera`) + entrée `scenes/core/`.

## Vérification
- `dotnet build` : réussi, 0 avertissement, 0 erreur.
- `godot --headless --quit-after 200 scenes/niveaux/monde.tscn` : aucune erreur de chargement.
- `grep` : plus de `GetNode<Camera2D>` hors Player ; plus de `LimGauche`/`12_qf4rj` ni
  d'anciens nœuds `@Area2D@54..68` dans monde.tscn.
- Aperçu éditeur recommandé : glisser `camera_zone.tscn`, redimensionner (deux instances
  indépendantes → prouve local-to-scene) ; traverser les 8 salles (transitions, salle de
  départ bornée, chute → respawn checkpoint).
