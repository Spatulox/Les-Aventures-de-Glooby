# Rangement des sols par biome (banquise / grotte / usine)

## 1. Assets : `assets/sol_usine/` → `assets/sol/usine/`

`git mv` du dossier (9 PNG + `.import`) ; les `.import` gardent leur **`uid`** (aucune
référence cassée), seul `source_file` est réécrit — Godot a réimporté de lui-même.
`assets/sol/` a donc maintenant la même forme que `scenes/sol/` : racine = banquise,
`grotte/`, `usine/`.

## 2. Scènes : `scenes/sol/` éclaté en sous-dossiers par biome

| Avant | Après |
| --- | --- |
| `scenes/sol/SolBanquise*.tscn`, `PenteBanquise*.tscn`, `PlateformeBanquise*.tscn` (12) | `scenes/sol/banquise/` |
| `scenes/sol/SolGrotte*.tscn` (5) | `scenes/sol/grotte/` |
| `scenes/decors/usine/SolUsineBois.tscn` + `PenteUsine*.tscn` (5) | `scenes/sol/usine/` |

Les sols d'usine étaient rangés dans `decors/` alors qu'ils portent une collision —
ils rejoignent `sol/` avec les autres. `FondUsine.tscn`, `DemoUsine.tscn` et
`TestPenteUsine.tscn` restent dans `scenes/decors/usine/` (décor / scènes de test).

## 3. Nouveaux `.tscn` de segments de sol usine

Les assets usine n'avaient aucune scène : le plancher n'existait qu'en code, généré à
la volée par `SolUsineBois` (`StaticBody2D` + `Sprite2D` + collision créés au runtime),
donc invisible et non posable dans l'éditeur.

Créé, sur le modèle banquise/grotte (sprite + collision bakés, visibles dans l'éditeur) :

- `scenes/sol/usine/SolUsineBoisCentreA.tscn` / `CentreB` / `CentreC`
- `scenes/sol/usine/SolUsineBoisEmboutGauche.tscn` / `EmboutDroit`
- `scripts/sol/SegmentSolUsineBois.cs` — script marqueur (enum `TypeSegment` +
  `LargeurSegment = 344f`), la scène reste seule source de vérité.

Géométrie reprise à l'identique du générateur : origine au **bord gauche** (sprite non
centré, ×2), surface de marche à **y = +8**, collision `RectangleShape2D` 344×172 en
(172, 94) — les pentes usine se calent déjà sur cette hauteur.

## 4. `SolUsineBois` (la rangée) réutilise ces scènes

`Reconstruire`/`PoserSegment` n'assemblent plus de nœuds à la main : la rangée
**instancie** les 5 scènes de segment. Une seule géométrie pour les deux usages
(rangée `NombreSegments` ↔ segment posé à la main), et plus de
`StaticBody2D + CollisionShape2D` nu créé en code.

## 5. Références mises à jour

`monde1.tscn`, `monde2.tscn`, `DemoUsine.tscn`, `TestPenteUsine.tscn`,
`TestPlateformesBois.tscn`, les commentaires de `SolBanquise.cs` / `SolGrotte.cs` /
`PenteUsineBois.cs`, et `CLAUDE.md` (sections *Assets layout*, `scenes/sol/`,
`scripts/sol/`).

## Vérification

- `godot --headless --build-solutions --quit` → build OK.
- Run headless (`--quit-after 90/200`) de `monde1`, `monde2`, `TestPenteUsine`,
  `DemoUsine`, `TestPlateformesBois` → **aucune erreur de ressource**. Ne subsistent
  que les `ERROR: Not supported by this display server` préexistants
  (`EvenementEntree.Libelle`, libellés clavier indisponibles sans display).
- Sonde `TestPenteUsine` : joueur `auSol=True` à y=186 sur toute la rangée → collision
  du plancher inchangée après le passage aux scènes de segment.
- Reste à faire : un F5 manuel pour le ressenti (rien de visuel n'a bougé).
