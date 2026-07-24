# Points d'entrée nommés + correction dépendance circulaire des transitions

Objectif : faire apparaître le joueur **où on veut** dans une scène selon la porte
d'arrivée (revenir de monde2 côté est de monde1, pas au village), et réparer une
transition qui « ne faisait rien ».

## 1. Points d'entrée nommés (pattern « portes »)
Une `ZoneChargementScene` indique désormais *par quelle porte* on arrive dans la
scène cible ; celle-ci pose des marqueurs identifiés, le joueur spawn sur le bon.

- **`scripts/Core/PointEntree.cs`** (nouveau, `: Marker2D`) — marqueur réutilisable
  dans le groupe `points_entree`, avec `[Export] string Id`. Helper statique
  `Trouver(arbre, id)`.
- **`scenes/core/point_entree.tscn`** (nouveau) — GameObject déposable dans l'éditeur.
- **`GameState.cs`** — `PointEntreeDemande` (état de session, non sauvegardé) :
  la porte visée, posée avant la bascule, consommée au spawn.
- **`ZoneChargementScene.cs`** — `[Export] PointEntreeCible` : rempli dans
  `PointEntreeDemande` juste avant le changement de scène.
- **`Player._Ready`** — si une porte est demandée, téléporte sur le `PointEntree`
  d'Id correspondant ; sinon position authorée du nœud `Joueur` (comportement
  d'origine). La position retenue devient le point de respawn du niveau.

## 2. Bug « monde2 → monde1 ne fait rien » : dépendance circulaire
- **Cause** : `monde1.tscn` embarquait `monde2.tscn` comme `PackedScene`, et
  `monde2.tscn` embarquait `monde1.tscn`. Godot ne résout pas la boucle
  (« Parse Error: Busy ») : la seconde référence tombe à `null`, donc la zone de
  retour avait `SceneSuivante == null` → avertissement + sortie, aucune transition.
  (monde1→monde2 « marchait » car monde1 chargé en premier ; c'est justement la
  ref retour qui cassait.)
- **Correction** : la cible est référencée par **chemin** (string) et chargée à la
  volée, ce qui supprime le cycle.
  - `ZoneChargementScene.cs` : `SceneSuivante` (`PackedScene`) →
    `[Export(File)] string CheminSceneSuivante`, et `ChangeSceneToPacked` →
    `ChangeSceneToFile`.
  - `monde1.tscn` : `CheminSceneSuivante = "res://scenes/niveaux/monde2.tscn"`
    (ext_resource `PackedScene` monde2 supprimé).
  - `monde2.tscn` : `CheminSceneSuivante = "res://scenes/niveaux/monde1.tscn"`
    + `PointEntreeCible = "monde1End"` (ext_resource `PackedScene` monde1 supprimé).
- **Câblage scène** : `monde1.tscn` porte un `PointEntree` Id `monde1End`
  (`5066, 266`, côté est).

## Vérification
- `dotnet build` : **0 erreur / 0 avertissement**.
- Rechargement complet des ressources en headless : plus aucune erreur
  « Busy »/« Failed loading » sur monde1/monde2.
- Reste un vrai F5 pour le ressenti (fondu, position de spawn).

## Notes / reste à faire
- Non committé (en attente de demande explicite).
- Convention : dans `monde2.tscn`, la `ZoneChargementScene` est rattachée sous
  `Fonds/banquise` (conteneur de fond) au lieu d'un groupe gameplay type
  `Banquise/Interactifs`. Fonctionne (transform identité) mais déroge à
  « `Fonds` = décors uniquement » — à déplacer si on veut rester cohérent.
