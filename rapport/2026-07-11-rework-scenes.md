# Rework de `scenes/` + GameObjects réutilisables

## Objectif
Ranger le dossier `scenes/` (tout était à la racine) et faire de **tous** les éléments
du monde des `.tscn` réutilisables, éditables facilement depuis l'éditeur Godot.

## Changements

### Arborescence `scenes/` en miroir de `scripts/`
Déplacements via `git mv` :
- `niveaux/` : `monde.tscn`
- `entites/` : `player.tscn`, `boss_cerf.tscn`
- `interactifs/` : `checkpoint_peche.tscn`, `mur_fondable.tscn`, `stalactite_piege.tscn`, `pouvoir_chaleur_pickup.tscn`
- `projectiles/` : `boule_de_neige.tscn`
- `ui/` : `hud.tscn`, `boss_hud_barre.tscn`, `menu_principal.tscn`, `ecran_fin.tscn`
- `core/` : `region_trigger.tscn`
- `decors/` : `igloo.tscn` (rejoint les sets `Decor*` déjà présents)
- `plateformes/`, `test/` : inchangés (déjà rangés)

`outil_bake.tscn` (ancien outil d'éditeur ayant servi à « baker » le monde généré
procéduralement dans `monde.tscn`, dont le script `OutilBake.cs` n'existe plus) était
un résidu mort : **supprimé** (dossier `outils/` du coup inutile).

### Références mises à jour partout
Déplacer un `.tscn` casse ses `path=` ; tout a été remis à jour :
- `project.godot` : `run/main_scene` → `ui/menu_principal.tscn`, autoload `Hud` → `ui/hud.tscn`.
- Chemins codés en dur en `.cs` : `EcranFin.cs`, `MenuPrincipal.cs` (×2), `MenuPause.cs`,
  `ZoneBossCerf.cs`.
- `ext_resource path=` internes : `monde.tscn`, `player.tscn`, `TestPlateformes.tscn`.
- Doublons obsolètes des `.tscn` réapparus à la racine `scenes/` supprimés (seuls les
  fichiers déplacés dans les sous-dossiers subsistent).

### Plateformes visibles dans l'éditeur
`PlateformeFixe.tscn` et `PlateformeFragile.tscn` n'affichaient **aucune image** (texture
assignée au runtime dans `_Ready`). Le visuel + collision par défaut est maintenant baké
dans le `.tscn` :
- `PlateformeFixe.tscn` : `Sprite2D` (`fixe_petite.png`, `scale (2,2)`) + `CollisionShape2D`
  (`RectangleShape2D` de la config « Petite »).
- `PlateformeFragile.tscn` : `Sprite2D` (`fragile_etat1.png`, `scale (2,2)`).
- `PlateformeFixe.cs._Ready` réapplique toujours texture/collision selon l'export `Taille`
  (aperçu éditeur = « Petite »), donc le comportement runtime est inchangé.

### Props décor → scènes réutilisables
`monde.tscn` contenait ~19 nœuds `Sprite2D` décor **inline** (bruts, `z_index=-1`), non
réutilisables. Créés dans `decors/props/` (racine `Sprite2D`, texture + `z_index=-1`) :
- `Rocher.tscn`, `CristalPetit.tscn`, `CristalGros.tscn`,
  `StalactiteDecor.tscn` (purement cosmétique, distinct du piège `interactifs/stalactite_piege`),
  `FleurGivre.tscn`.
- Dans `monde.tscn`, chaque `Sprite2D` inline est remplacé par une **instance** de la scène
  de prop correspondante (position conservée). Les `ext_resource` de textures devenues
  inutiles sont convertis en `PackedScene`. Modifier un prop une fois met à jour toutes ses
  occurrences.

### Documentation
- `CLAUDE.md` (section « Assets layout ») : nouvelle arborescence `scenes/` en miroir de
  `scripts/` + scènes de props décor ; chemins `monde`/`hud`/`ecran_fin` mis à jour.

## Vérification
- `dotnet build` : génération réussie, 0 avertissement, 0 erreur.
- `godot --headless --quit-after 200` sur `niveaux/monde.tscn` : aucune erreur de chargement
  de ressource (`Failed loading resource` / `Cannot open file`).
- `grep` : plus aucun chemin `res://scenes/` pointant vers un ancien emplacement.
- Aperçu éditeur recommandé (ouvrir `niveaux/monde.tscn`) pour confirmer visuellement le
  placement des props.

## Note
- La seule référence résiduelle à `res://scenes/monde.tscn` est dans le cache généré
  `.godot/editor/` (dernière scène ouverte) — inoffensive, régénérée par l'éditeur.
- **Dualité plateformes** : `monde.tscn` n'instancie **aucune** scène `Plateforme*`. Son
  sol/rebords jouables sont des **tuiles de collision** peintes dans la couche `Terrain`
  (`TileMapLayer`, `physics_layer_0`). Les GameObjects `Plateforme*` restent un système
  parallèle, utilisé uniquement dans `test/TestPlateformes.tscn` — à instancier dans le
  monde si on veut un jour des plateformes mobiles/fragiles (le tilemap ne peut ni bouger
  ni se casser dynamiquement).
