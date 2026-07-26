# ReindeerBoss — assembler la grotte florale en niveau de boss

## Le bug : « je n'arrive pas à contrôler le joueur »

Les salles de `scenes/decors/grotte_florale/` étaient des **démos autonomes** : chacune
embarquait son propre `Joueur` (`player.tscn`). Mises bout à bout, N salles = **N joueurs**
qui reçoivent tous les entrées, et N `Camera2D` qui se disputent `current` — la vue suit le
dernier joueur ajouté pendant qu'on croit piloter le premier. Godot interdisant de supprimer
un nœud d'une instance, la correction ne pouvait pas venir du niveau : il fallait toucher aux
salles.

## Ce qui a été fait

**Salles (5 modules) — une seule suppression, le `Joueur`.**
`Sanctuaire`, `GalerieGelee`, `PuitsGivre`, `CaverneCristalline`, `GrandeSalle` : nœud
`Joueur` + son `ext_resource` retirés. **Fonds, `Parallax2D`, `Grade`, art : inchangés.**
Ajout de `<Salle>Demo.tscn` (salle + joueur) pour garder chaque salle jouable seule en F6,
comme `DecorGrotteDemo` / `DemoUsine`.

**`scenes/niveaux/ReindeerBoss.tscn` — le niveau.**
5 lieux alignés d'ouest en est (256 px de marge), un nœud par lieu (`Camera`, `Salle`,
`Interactifs`) façon `monde1`/`monde2` :

| Lieu | x | boîte | zone caméra |
|---|---|---|---|
| Sanctuaire | 0 | 2048×704 | `CameraZone` |
| Galerie | 2304 | 2176×576 | `CameraZone` |
| Puits | 4736 | 960×1088 | `CameraZone` |
| Caverne | 5952 | 1920×704 | `CameraZone` |
| ArenBoss (GrandeSalle) | 8128 | 2240×640 | `ZoneBossCerf` (fait aussi office de zone caméra) |

Un seul `Joueur`, un `BossHudBarre`, un `MenuPause`. Zones en `Type = Souterrain` (pas de
blizzard) et `NomAmbiance = "grotte"` / `"boss_cerf"` pour l'arène.

**Recalage des parallaxes, côté niveau uniquement.** Un `Parallax2D` se place depuis la
caméra (`−camX × scroll_scale`) : une salle posée à `dx` verrait son fond dériver d'autant.
Corrigé par **override d'instance** (`[editable path=...]` + `scroll_offset.x = dx ×
scroll_scale.x`) sur les 4 couches de chaque salle — les `.tscn` de salle ne sont pas touchés.
Ex. arène : `Fond` 2438.4, `DecorFar` 4064, `DecorMid` 5039.36, `DecorAvant` 9347.2.

**`PorteInterne` (nouveau, réutilisable).** `scripts/Core/PorteInterne.cs` +
`scenes/core/porte_interne.tscn` : pendant intra-scène de `ZoneChargementScene`. Téléporte le
joueur sur le `PointEntree` d'`IdDestination` (marqueur existant réutilisé, aucun nouveau
type) **au noir**, avec temps mort anti-rebond. Rien à câbler côté caméra : le sondage continu
de `Player.MettreAJourZoneCamera` applique seul la zone d'arrivée.
Chaîne posée : `sanctuaire → galerie → puits → caverne → grande`.

**Mutualisation du fondu.** Le voile noir plein écran de `ZoneChargementScene` est extrait en
`Effets.FondreAuNoirPuis(source, duree, action)` et partagé avec `PorteInterne` — la version
locale a disparu, pas de duplication.

**`scenes/boss/zone_boss_cerf.tscn` (nouveau).** `Area2D` + `ZoneBossCerf.cs`, sur le modèle
de `camera_zone.tscn` : `SceneBoss = boss_cerf.tscn`, `NomBoss = "Rodolphe"`, `PvBoss = 40`,
apparition à (1600, 440) local, barre liée à `../../BossHudBarre`. Premier `ZoneBoss` posé en
scène du projet ; la victoire enchaîne sur `ecran_fin.tscn` (déjà câblé).

## Vérification

Compilation propre, puis traversée scriptée en headless (sonde temporaire, supprimée depuis) :

```
joueurs dans l'arbre = 1 (attendu 1)
depart = (160, 517), SeuilChuteVide = 1004
porte 0 -> (2464, 393) OK | 876      porte 1 -> (4960, 961) OK | 1388
porte 2 -> (6112, 521) OK | 1004     porte 3 -> (8320, 457) OK | 940
SeuilChuteVide arène = 940 ; boss présent = BossCerf PV=40/40 pos=(9728, 440)
```

Les 4 portes mènent au bon `PointEntree`, chaque salle applique ses limites caméra et son
filet anti-chute, l'arène fait apparaître Rodolphe.

## Paliers de givre du Sanctuaire — visibles dans l'éditeur, absents en jeu

Les deux plateformes ajoutées sous `Sanctuaire/Sol` instancient `PlateformeGlace.tscn`, la
plateforme **conjurée par le pouvoir du joueur** : son `_Ready` arme un minuteur de `DureeVie`
(4 s) puis `Effets.Disparaitre` → `QueueFree`. Dans l'éditeur aucun script ne tourne, donc
elles s'affichent ; en jeu elles sont détruites bien avant qu'on les atteigne (elles sont à
x ≈ 1342-1556, le joueur démarre à x = 160). Sonde runtime : présentes à 0,1 s et 2 s,
**absentes à 5 s**.

`scripts/Plateformes/PlateformeGlace.cs` : `DureeVie ≤ 0` rend désormais la plateforme
**permanente** (ni pop d'apparition, ni fonte) — le pouvoir du joueur garde son défaut de 4 s.
`DureeVie = 0.0` posé sur `PlateformeGivre1/2` ; vérifiées vivantes à 10 s.

## JardinGrotte après le boss, débloqué par la victoire

Sixième lieu `Jardin` à x = 10624 (boîte 1792×704, `CameraZone` (896, 352) ×(7, 2.75),
`PointEntree` Id `jardin` à (150, 548), parallaxes recalés : `Fond` 3187.2, `DecorFond`
5843.2, `DecorAvant` 12217.6). `JardinGrotte.tscn` a perdu son `Joueur` embarqué comme les
cinq autres salles.

**Verrou de progression, via `GameState`** — `PorteInterne` gagne un export `BossRequis` :
tant que `GameState.EstBossVaincu(BossRequis)` est faux, la porte ne téléporte pas (et ne
consomme pas son temps mort). La porte de l'arène (`ArenBoss/Interactifs/PorteSortie`, posée
sur la `Sortie` de GrandeSalle) a `BossRequis = "Rodolphe"` — le même nom que
`ZoneBossCerf.NomBoss`, celui que `SurVictoire` passe à `MarquerBossVaincu`.

**Fin de partie rendue optionnelle** — `ZoneBossCerf` exportait en dur la bascule vers
`ecran_fin.tscn` 2,5 s après la victoire, ce qui aurait coupé le jeu avant le jardin.
Nouveaux exports `CheminSceneVictoire` (défaut `ecran_fin.tscn`) et `DelaiVictoire` ; **vide
dans ce niveau** = on reste dans le monde. À prévoir : de quoi terminer la partie depuis le
jardin (une `ZoneChargementScene` vers `ecran_fin.tscn`).

Sonde runtime : porte verrouillée → le joueur reste à x = 10208 ; après
`MarquerBossVaincu("Rodolphe")` → téléporté en (10774, 548), `SeuilChuteVide` = 1004 (zone du
jardin appliquée).

À savoir : `SurVictoire` appelle `GameState.Sauvegarder()`, donc une fois Rodolphe battu la
porte reste ouverte au chargement suivant (et le boss ne réapparaît plus) — c'est le
comportement déjà en place pour les boss.

## Boss dupliqué dans l'arène (bug de `ZoneBoss`)

`ZoneBoss` héritait de `DeclencheurZone` sans toucher à `UneSeuleFois` (défaut **false**) :
chaque nouvelle entrée du joueur dans l'arène rejouait `SurEntreeJoueur` → **un Rodolphe de
plus** à chaque retour (respawn, recul qui fait sortir puis rentrer, téléportation). Mesuré :
4 entrées = 4 boss vivants.

Correctif dans `ZoneBoss` (donc valable pour tout boss à venir) : `UneSeuleFois = true` dans
`PreparerDeclencheur`, plus un garde `Boss != null && IsInstanceValid(Boss)` dans
`SurEntreeJoueur` au cas où la zone serait réarmée. Re-mesuré : 4 entrées = **1 boss**.

Note : le boss n'est **pas** placé dans la scène, il est instancié par la zone à l'entrée du
joueur (design documenté) — d'où son absence dans l'éditeur, qui est normale.

`ZoneBossCerf.CheminSceneVictoire` a par ailleurs vu son **défaut passer à vide** (rester dans
le monde) : l'override posé sur l'instance était perdu à chaque sauvegarde de l'éditeur, et
le jeu repartait sur `ecran_fin.tscn` sitôt Rodolphe tombé. Un boss qui doit terminer la
partie renseigne désormais explicitement le chemin.

## Reste à faire / à l'œil

- **Play-test manuel** (`godot res://scenes/niveaux/ReindeerBoss.tscn`) : vérifier le calage
  des fonds salle par salle. Si un fond dérive dans l'autre sens, la formule devient
  `dx × (1 + scroll_scale.x)` (selon que Godot compose ou non la transform du parent).
- Vérifier que chaque `Sortie` est bien atteignable en sautant (les portes sont posées sur
  les marqueurs d'origine des salles).
- Aucun `Fonds`/`BackgroundManager` dans ce niveau : chaque salle porte son propre décor, une
  région par-dessus entrerait en conflit. `FondBossCerf.tscn` reste donc inutilisé ici.
- Les 5 `Grade` sont identiques (1.08, 1.08, 1.12) : leur superposition est sans effet.
