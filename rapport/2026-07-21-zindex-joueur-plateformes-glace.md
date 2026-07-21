# Z-index : le joueur devant les plateformes de glace

## Problème

Les plateformes posées par le pouvoir de glace masquaient le joueur — on ne voyait plus ses pieds en se tenant dessus.

**Cause** : joueur et plateforme étaient tous deux à `z_index = 0` (aucun ne le déclarait). À z égal, Godot dessine dans l'ordre de l'arbre, et `Player.UtiliserPouvoirGlace` fait `GetParent().AddChild(plateforme)` — la plateforme atterrit donc **après** le nœud `Joueur` dans les enfants de `Monde`, et passe devant lui. Chaque nouvelle plateforme repassait aussi devant les précédentes.

## Changements

### `scripts/Common/Constantes.cs`
Ajout d'un bloc de strates de rendu, dans l'esprit du bloc `Layer*` existant (source de vérité unique) :

| Constante | Valeur | Rôle |
|---|---|---|
| `ZFond` | -100 | ciels fixes (FondBanquise / FondGrotte / FondBossCerf) |
| `ZDecor` | -1 | props de décor |
| `ZPlanDeJeu` | 0 | sol, plateformes, PNJ, projectiles |
| `ZJoueur` | **1** | le joueur — nouvelle strate |
| `ZDialogue` | 100 | bulles de dialogue |

Les valeurs négatives sont descriptives (elles constatent l'existant déjà posé dans la vingtaine de `.tscn` concernés, rien n'a été retouché de ce côté). Seule `ZJoueur` est nouvelle.

### `scenes/entites/player.tscn`
`z_index = 1` sur le nœud racine `Player`. Vérifié au préalable : seuls `monde.tscn` et `test/TestPlateformes.tscn` instancient le player, et aucun n'override `z_index` — la modification prend donc bien effet partout.

### Non modifié
`PlateformeGlace.tscn` / `PlateformeGlace.cs` restent au plan de jeu (0), là où ils doivent être : devant les props de décor (-1), derrière le joueur (1). Pas de `MoveChild` ni de `ZIndex` posé au spawn — le correctif est déclaratif et couvre aussi tout futur élément instancié en cours de partie.

## Vérification

- `godot --headless --build-solutions --quit` → compilation propre.
- `godot --headless --quit-after 200` → aucune erreur nouvelle. Les `ERROR: Not supported by this display server` remontées par `MenuPrincipal.ToucheDe` (libellés de touches) sont préexistantes et propres au headless.
- **Reste à faire : play-test manuel** (`godot`) — le rendu n'est pas observable en headless. Poser une plateforme de glace, monter dessus, vérifier que les pieds sont visibles ; en enchaîner plusieurs ; contrôler qu'il n'y a pas de régression devant le sol/les props ni derrière les bulles de dialogue.
