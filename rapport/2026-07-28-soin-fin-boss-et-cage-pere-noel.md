# Soin de fin de combat + le Père Noël sort vraiment de sa cage

## Objectif

1. **Toute fin de combat de boss rend au joueur la totalité de sa vie.**
2. **Après la délivrance du Père Noël, on voyait toujours le prop du prisonnier** —
   le sprite « cage ouverte » montre lui aussi le Père Noël derrière les barreaux.

## Changements

### `scripts/Core/ZoneBoss.cs` — soin complet à la chute du boss

- `DeclencherEpilogue()` (branché sur le signal `Vaincu` du boss, protégé par
  `_epilogueLance`) appelle `GameState.Soigner(PvMax)`.
- Posé dans le **tronc commun** et non dans les trois `ZoneBossXxx.SurVictoire` : la
  règle vaut pour Rodolphe, le Lutin Mecha, le Père Noël **et toute arène future**,
  sans une ligne à recopier. Aucun poisson consommé (ce n'est pas un soin acheté).

### `scripts/Entities/Interactable/CagePereNoel.cs` — le libéré est un vrai PNJ

- Nouvel export **`PnjLibere`** (`PackedScene`) : le prisonnier **debout et libre**,
  instancié à l'ouverture de la cage. Vide = personne n'en sort (comportement d'avant).
- Nouvel export **`DecalagePnjLibere`** (défaut `(0, 50)`) : l'origine de la cage est
  au centre de son sprite, celle d'un PNJ à ses pieds. Réglé pour apparaître **juste
  au-dessus** du plancher de l'arène — un PNJ est un corps physique, il finit de
  descendre seul, alors qu'apparaître sous le sol le coincerait dedans.
- **`TextureOuverte` vide = la cage s'efface** (`_sprite.Visible = false`) au lieu de
  changer d'image. C'est le correctif : garder l'art « cage ouverte » afficherait le
  Père Noël à deux endroits à la fois. Le *nœud*, lui, reste — il porte le libéré et le
  contenu.
- Apparition du libéré et du contenu factorisées dans `PoserEnfant(scene, decalage)`
  (instanciation + `CallDeferred(AddChild)`, obligatoire depuis une sortie de zone).

### `scenes/interactifs/CagePereNoel.tscn`

- `TextureOuverte` retirée, `PnjLibere = scenes/entites/PereNoel.tscn`.
- `perenoel_cage_ouverte.png` n'est plus référencée (fichier conservé sur disque).

## Budget PixelLab

**0 génération** — le PNJ Père Noël libre réutilise `scenes/entites/PereNoel.tscn` et
ses frames `assets/pnj/pere_noel/idle`.

## Commits

| Commit | Contenu |
|---|---|
| `a2f58d9` | soin complet du joueur à la fin d'un combat de boss |
| `22c77bc` | le Père Noël libéré sort vraiment de sa cage |

## Vérification — non faite, et pourquoi

`dotnet build` **échoue**, pour une raison **étrangère à ces commits** : le travail en
cours non commité sur `BossPereNoel.cs` (lignes 297 et 303) référence
`DistanceEngagement` / `DistanceConfort`, qui n'existent nulle part — probablement en
cours de déplacement vers `scripts/Common/PorteeJoueur.cs` /
`scripts/Entities/Pnj/MechantFonceur.cs` (non suivis par git). Ni compilation propre ni
passage headless n'ont donc pu valider ces deux changements ; à refaire une fois la
solution de nouveau compilable.

## Reste à faire (connu, non traité)

- **L'ouverture de la cage n'est pas persistée** (`_ouverte` est en mémoire) : après un
  rechargement de sauvegarde, la cage réapparaîtrait fermée. Sans effet en pratique — le
  pantalon enchaîne sur l'écran de fin — mais `GameState.MarquerConsomme`/`EstConsomme`
  le règlerait en deux lignes.
