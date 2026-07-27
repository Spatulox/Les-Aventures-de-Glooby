# Deux boss de fin : `monde2` → `BossEnd`, qui choisit lequel spawner

**Besoin** : deux boss de fin, le second **caché**, accessible seulement si le joueur a donné ses 50 poissons au lutin CGT. La fin de `monde2.tscn` charge `BossEnd.tscn`, et c'est **`BossEnd` qui décide quel boss faire apparaître**.

L'aiguillage est donc au **spawn du boss**, pas au changement de scène : une seule arène sert de fin normale ou de fin cachée, sans dupliquer la scène.

**Rien à créer côté condition** : le don des 50 poissons est déjà persisté par le dialogue (`ChoixDialogue.IdMemoire = "lutin_cgt_don_poissons"` dans `assets/dialogues/banquise_fin_lutin_cgt.tres` → `DeclencheurDialogue.ValiderChoix` → `GameState.MarquerConsomme`, sérialisé dans `DonneesSauvegarde.ElementsConsommes`). Constante : `LutinCgt.IdDonPoissons`.

## Changements

### `scripts/Core/ZoneBoss.cs` — le boss caché

4 exports ajoutés sur la base réutilisable (donc réglables dans l'inspecteur, sur n'importe quelle arène) :

| Export | Rôle |
|---|---|
| `MemoireRequise` | id `GameState.EstConsomme` qui bascule vers la variante |
| `SceneBossAlternative` | le boss caché |
| `NomBossAlternatif` | vide = garder `NomBoss` |
| `PvBossAlternatif` | 0 = garder `PvBoss` |

Trois propriétés résolvent l'embranchement une bonne fois — `SceneChoisie`, `NomChoisi` (public), `PvChoisis` — et **tout le reste de la classe passe par elles**, jamais par les exports bruts : test `EstBossVaincu`, `DefinirPvMax`, spawn. `VariantePrise` exige mémoire **et** scène alternative ensemble : un câblage à moitié fait reste sans effet plutôt que de spawner un boss nul.

Deux effets de bord corrigés au passage :
- `Barre.DefinirNom(NomChoisi)` est maintenant appelé avant de révéler la barre — sinon une arène à deux boss affichait le nom authoré sur `BossHudBarre` (« Boss ») au lieu de celui réellement apparu.
- **`GetParent().CallDeferred(AddChild, boss)`** au lieu de `AddChild` direct. On est appelé depuis `BodyEntered`, donc en plein flush des requêtes physiques : les `_Ready` du boss qui touchent une `CollisionShape2D` ou sa `ZoneDetection` échouaient (`Can't change this state while flushing queries`), laissant des formes dans le mauvais état. Bug préexistant, invisible jusqu'ici parce que dans `ReindeerBoss` le joueur démarre loin de l'arène ; il crache 4 erreurs dès que le joueur apparaît dans la zone.

### `scripts/Core/ZoneBossCerf.cs`

`MarquerBossVaincu(NomChoisi)` au lieu de `NomBoss` : dans une arène à deux boss, seul celui réellement combattu est marqué vaincu (et c'est ce nom qu'une `PorteInterne` doit citer en `BossRequis`).

### `scenes/niveaux/BossEnd.tscn`

Le fichier était un `Node2D` vide. Rempli sur le patron de `ReindeerBoss.tscn` — arène **fonctionnelle mais nue**, à habiller :

- `Fonds` (`BackgroundManager`) → `usine` → `FondUsine`
- `Arene/ZoneBossCerf` — centre (1376, 240), scale (10.75, 2.5) → arène x ∈ [0, 2752]
  - normal : `boss_cerf`, `NomBoss = "PereNoel"`, `PvBoss = 40`
  - caché : `MemoireRequise = "lutin_cgt_don_poissons"`, `NomBossAlternatif = "BossCache"`, `PvBossAlternatif = 60`
  - `CheminSceneVictoire = res://scenes/ui/ecran_fin.tscn` (c'est la fin du jeu ; l'écran n'était plus atteint par aucun chemin)
- `Arene/Sol/SolUsineBois` — 6 segments + 2 embouts = 2752 px, surface de marche y = 408
- `Arene/Interactifs/Entree` — `PointEntree` d'Id `bossEnd`
- `Joueur`, `BossHudBarre`, `MenuPause`

`SceneBossAlternative` pointe **provisoirement sur `boss_cerf` aussi** : les deux branches sont donc jouables et vérifiables tout de suite, seuls le nom et les PV diffèrent. À remplacer par la vraie scène du boss caché.

### `scenes/niveaux/monde2.tscn`

Une insertion sous `UsinePereNoel/Interactifs` — transition **simple, sans condition** :

```
ZoneSortieUsine  (instance de scenes/core/zone_chargement_scene.tscn)
  position            = (4700, 200)
  CheminSceneSuivante = uid://d3ixspn6np4ud   (BossEnd)
  PointEntreeCible    = bossEnd
```

`x = 4700` est juste avant le bord est de `ZoneUsine` (centre 3240, `scale.x` 12.1875 → x ∈ [1680, 4800]). Le `y` est un **placeholder** : `UsinePereNoel/Sol` est encore vide.

## Vérification

- `godot --headless --build-solutions --quit` → 0 erreur C#.
- **Aiguillage testé dans les deux sens** (scène de test jetable, supprimée depuis, qui posait la mémoire puis chargeait `BossEnd`) :
  - sans don → `variante=False nom=PereNoel`
  - avec don → `variante=True nom=BossCache`
- `BossEnd.tscn` et `monde2.tscn` bootent headless sans erreur. Les 4 `Can't change this state while flushing queries` présentes avant le `CallDeferred` ont disparu. (Le `ObjectDB instances leaked at exit` est préexistant, il sort aussi sur `monde1`/`monde2`.)
- **Non testé en jeu** : `UsinePereNoel/Sol` n'a aucun sol, le joueur ne peut pas atteindre `x = 4700`. La transition `monde2 → BossEnd` ne sera jouable qu'une fois le plancher de l'usine posé.

## Reste à faire (éditeur)

1. Poser le sol de `UsinePereNoel` dans `monde2`, puis recaler le `y` de `ZoneSortieUsine`.
2. Habiller `BossEnd` (décor d'usine, ambiance) et ajuster taille d'arène / `PositionApparition`.
3. Créer la vraie scène du boss caché et la mettre dans `SceneBossAlternative` ; renommer `PereNoel` / `BossCache` si besoin — ces noms sont la clé de `GameState.EstBossVaincu`, ils doivent rester **distincts entre eux et de `"Rodolphe"`**.

## Point ouvert

`DeclencheurDialogue.ValiderChoix` fait `MarquerConsomme` **sans** `Sauvegarder()`. Le don survit au changement de scène (`GameState` est un autoload) mais pas à un quit avant le prochain checkpoint — les poissons dépensés non plus, donc c'est cohérent, mais à trancher si l'accès au boss caché doit être acquis définitivement dès le don.
