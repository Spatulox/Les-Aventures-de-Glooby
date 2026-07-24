# Aperçu éditeur du LutinCgt selon sa Pose

## Problème
Dans l'éditeur, le Sprite2D « Apercu » de `lutin_cgt.tscn` était figé sur
`pancarte_levee/00.png`. L'instance de `monde.tscn` (`Grotte/Pnj/LutinCgt`, réglée sur
`Pose = 0` = BrasCroises) s'affichait donc en pancarte levée — l'aperçu ne correspondait pas
à la pose choisie.

## Changement (un seul fichier : `scripts/Entities/Pnj/LutinCgt.cs`)
Même schéma que `PanneauBois` :
- Classe marquée `[Tool]`.
- `Pose` devient une propriété (champ `_pose` + setter) qui appelle `AppliquerApercu()`.
- `AppliquerApercu()` : garde `IsNodeReady()`, puis charge la 1re frame de la pose
  (`Configs[_pose].Dossier + "/00.png"`, mapping déjà existant) sur le nœud `Apercu`.
- `_Ready()` surchargé : en éditeur, applique l'aperçu et **n'appelle pas** `base._Ready()`
  (qui masquerait justement l'`Apercu`) ; runtime inchangé.

Aucune modification de `monde.tscn`, `lutin_cgt.tscn` ni `PnjAmical`.

## Vérification
- `godot --headless --build-solutions --quit` : build clean.
- Boot headless : pas d'erreur liée à LutinCgt (les `Not supported by this display server`
  sont préexistants et viennent du menu, pas de ce changement).
- Éditeur (à confirmer par un humain) : ouvrir `monde.tscn` → le LutinCgt s'affiche en
  bras croisés ; changer `Pose` dans l'inspecteur met l'image à jour immédiatement.
