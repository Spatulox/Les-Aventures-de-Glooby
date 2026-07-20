# No-clip du joueur à travers les plateformes en fin de glissade — 2026-07-20

## Problème
En fin de glissade, le joueur traversait la plateforme sur laquelle il glissait et tombait
au sol du dessous. Symptôme visible **à la fin** de la glissade, cause réelle **au démarrage**.

## Cause
Les deux hitboxes du joueur n'étaient pas alignées par le bas :

| Hitbox | Bas (local) |
|---|---|
| `CollisionDebout` (capsule) | **22.2** |
| `CollisionGlisse` (rect) | **16.0** — 6.2 px trop haut |

Chaîne complète :
1. Au **démarrage** de la glissade, les pieds de la hitbox de glissade sont 6.2 px plus haut
   → le corps descend de 6.2 px pour venir reposer sur la plateforme (`y` 537.8 → 544.0).
2. À la **fin**, `CollisionDebout` est réactivée : ses pieds sont désormais 6.2 px **sous** la
   surface de la plateforme.
3. Une `PlateformeUnidirectionnelle` ne repousse **jamais** un corps déjà passé sous sa
   surface → contact rejeté, `IsOnFloor()` passe à `false`, chute.

Invisible sur `SolBanquise` (layer 1, solide, qui dépénètre) : le bug ne touche que les
21 plateformes traversables qui composent une partie du sol de `monde.tscn`.

Le coupable direct était un **override d'instance** : `monde.tscn` fixait
`CollisionGlisse position = Vector2(0, 9)` sur le nœud `Joueur`, ce qui écrasait `player.tscn`
(une première correction faite dans `player.tscn` seul est restée sans effet).

## Correctifs
- **`scenes/niveaux/monde.tscn`** — suppression de l'override `CollisionGlisse` sur l'instance
  `Joueur` (`player.tscn` redevient la seule source de vérité).
- **`scenes/entites/player.tscn`** — `CollisionGlisse` à `y = 15.2`, soit un bas à **22.2**,
  aligné sur la capsule debout. La hitbox reste bien plus basse (14 px vs 38.4) : glisser sous
  un obstacle fonctionne toujours.
- **`scripts/Entities/Player/Player.cs`** — commentaire dans `_Ready` documentant l'invariant
  (hitboxes alignées par le bas) et la conséquence en cas de régression.

## Vérification
Harnais headless jetable (joueur posé sur un `PlateformeUnidirectionnelle` du corridor de la
grotte, glissade scriptée, trace par frame de `y` / `IsOnFloor` / bas des deux hitboxes),
supprimé après coup :

- **avant** : `f=32 y=543.97` (enfoncement), puis `f=53 sol=false` → chute jusqu'à `y=667.78`
- **après** : `basDebout=22.20 basGlisse=22.20`, `y` reste à 537.7 et `sol=true` sur toute la
  durée, relevé compris

`godot --headless --build-solutions --quit` : build C# OK, 0 erreur.
`godot --headless --quit-after 200` : boot propre (les `Not supported by this display server`
sont préexistants et liés au mode headless).

**Réserves** : une seule plateforme traversable testée (corridor grotte) ; un play-test manuel
`godot` reste utile pour le ressenti du relevé.
