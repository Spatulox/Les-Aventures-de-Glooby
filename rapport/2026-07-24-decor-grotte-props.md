# Décoration de la grotte — props inline

## Objectif
Peupler la grotte de `scenes/niveaux/monde.tscn` avec des éléments de décor pris
depuis `assets/`, sans créer ni instancier de `.tscn` (convention *props visuels
sans .tscn* → simples `Sprite2D` inline). Densité **dense**, profondeur **mixte**.

## Modifications (`scenes/niveaux/monde.tscn` uniquement)

**5 `ext_resource` ajoutés** (les autres textures de grotte étaient déjà déclarées) :
`g_fissure`, `g_veine`, `g_stalagmite` (`assets/decors/grotte/elements/stalagmite.png`),
`g_stalactite` (`assets/props/stalactite.png`), `g_eboulis`.

**26 `Sprite2D` ajoutés** sous `Grotte/Decor`, ancrés au sol via `surface = centre_tuile − 84` :
- **Sol bas** (12) : congère, tas de pierres, éboulis, flaque gelée, mini-lac (z=-1) ;
  grappe de cristaux, colonne de glace, colonne brisée, 2 stalagmites, gros cristal,
  rocher de glace (z=1).
- **Chambre du feu** (4) : champignon lumineux, fissure (z=-1) ; géode, champignon géant (z=1).
- **Entrée haute** (3) : fleur de givre, congère (z=-1) ; petit cristal (z=1).
- **Murs** (2) : veines de cristal sur MurDroit/MurGauche (z=-1).
- **Plafond** (3) : stalactites pendantes (z=1).
- **Plateformes laby** (2) : grappe de cristaux (z=1), éboulis (z=-1).

Layering mixte : clutter plat en `z_index=-1` (derrière le gameplay), formations
volumineuses + stalactites en `z_index=1`.

## Vérification
- `godot --headless --build-solutions --quit` : compilation propre.
- `godot --headless --quit-after` : scène chargée sans erreur de parse ni de
  ressource manquante. (Les `Not supported by this display server` sont des
  artefacts headless préexistants, sans lien avec le décor.)
- Réglage fin visuel (positions/z) à ajuster au besoin en F5 dans l'éditeur.
