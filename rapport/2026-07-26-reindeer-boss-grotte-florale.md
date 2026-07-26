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

**Recalage des parallaxes — `RecalageParallaxe` (nouveau, réutilisable).**
Première approche (des `scroll_offset` écrits en dur dans le niveau) : **fausse deux fois**.
`Parallax2D` réécrit `scroll_offset` à chaque frame, donc le réglage était effacé en jeu ; et
l'éditeur, qui n'a pas de caméra, l'appliquait tel quel — d'où les props envoyés très loin à
droite dans `ReindeerBoss.tscn` alors que les salles seules étaient correctes.

Comportement réel mesuré : `position = screen_offset × (1 − ScrollScale)`, en coordonnées
monde, **et** la transform du parent s'applique par-dessus. Le décalage correct vaut donc
`−ancrage × (1 − ScrollScale)`, appliqué aux **enfants** de la couche (les seuls que
`Parallax2D` ne réécrit pas).

`scripts/Core/RecalageParallaxe.cs` + `scenes/core/recalage_parallaxe.tscn` : un nœud à
déposer sous la racine du niveau, qui traite tous les `Parallax2D` de ses frères au `_Ready`.
Les 20 `scroll_offset` du niveau ont disparu, ainsi que les `[editable path]` qui n'existaient
que pour eux — **l'éditeur remontre chaque salle telle qu'elle est authorée**, et déplacer une
salle ne demande plus aucun recalcul.

Vérification différentielle (Galerie, caméra au même point de la salle) — position monde
relative à la salle, salle seule puis dans le niveau :

```
Fond 1628,5 | DecorFar 1085,1 | DecorMid 805,4 | DecorAvant 47,2   (salle seule)
Fond 1628,5 | DecorFar 1085,1 | DecorMid 805,4 | DecorAvant 47,2   (dans ReindeerBoss)
```

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

## Barre de vie du boss trop grande

Les trois PNG de `assets/ui/boss/` font **642×159** pour un viewport de **640×360** : le cadre
prenait toute la largeur et 44 % de la hauteur. Jamais vu jusqu'ici, faute de `ZoneBoss` posée
en scène.

`scenes/ui/boss_hud_barre.tscn` : `Barre` passe en `scale = (0.25, 0.25)` calée à (240, 6) —
soit **160×40 à l'écran, 25 % de la largeur et 11 % de la hauteur**, centrée en haut (jauge
interne x 259..382, y 18..34). Le label `NomBoss` suit (font 8, contour 2). Aucun code touché :
`BossHudBarre` ne gère pas la mise en page. Un seul curseur pour ajuster : le `scale` du nœud
`Barre`.

## Props qui volent — décor de parallaxe vs espace monde

Trois éléments posés à la main (`PlateformeGlace`, `PlateformeGlace2`, `PanneauBois`) vivaient
dans `CaverneCristalline/DecorAvant`, c'est-à-dire **dans un `Parallax2D`** (`scroll_scale`
1.15) : ils défilaient 15 % plus vite que le sol. Invisible dans l'éditeur, où rien ne défile.
Remontés à la racine de la salle **à coordonnées identiques** (la couche est à (0, 0), donc le
rendu éditeur ne bouge pas), ils sont désormais solidaires du terrain.

`RecalageParallaxe` avertit maintenant au lancement dès qu'un nœud à collision se trouve dans
une couche de parallaxe, en donnant son chemin — c'est ce qui a permis de les trouver. Règle :
un prop se met sous le **nœud de lieu** (`Sanctuaire`, `Galerie`…) ou sous `Salle/DecorBord`
(un `Node2D`, espace monde), jamais dans `Fond` / `DecorFar` / `DecorMid` / `DecorAvant`.

## Don des 50 poissons au lutin CGT

Déjà en place et vérifié : `ChoixDialogue.CoutPoissons = 50` **masque** le don quand la
réserve est insuffisante (le choix ne peut donc jamais mentir), et `ValiderChoix` dépense les
poissons **puis** marque `IdMemoire = "lutin_cgt_don_poissons"` via
`GameState.MarquerConsomme` — la grève financée est donc notée dans la partie (persistée au
prochain checkpoint, `Checkpoint.cs` appelant `Sauvegarder`). Le don est `UneSeuleFois`.

Ce qui manquait : le joueur trop pauvre voyait juste la ligne disparaître. Deux exports
ajoutés à `ChoixDialogue`, génériques et data-driven :

- **`SiReserveInsuffisante`** — inverse le test de `CoutPoissons` : le choix n'apparaît QUE si
  Glooby ne peut pas payer. Il ne prélève rien (`CoutEffectif`, utilisé par `ValiderChoix`).
- **`MasqueSiMemoire`** — masque le choix si un AUTRE `IdMemoire` est déjà consommé : une fois
  le don fait, le regret ne se réaffiche pas alors que c'est le don qui a vidé la réserve.

`assets/dialogues/banquise_fin_lutin_cgt.tres` gagne `ChoixPasAssez` (« Je n'ai pas assez de
poissons... »), branché sur les deux nœuds qui proposent le don. Sonde headless :

```
50 poissons        -> « Tiens, prends mes 50 poissons. » / revendications / bon courage
5 poissons         -> « Je n'ai pas assez de poissons... » / revendications / bon courage
après le don (0)   -> revendications / bon courage      (EstConsomme = True, réserve 0)
```

## Portes calées sur le marqueur `Sortie` de leur salle

Les portes portaient les coordonnées du marqueur **recopiées à la main** dans le niveau.
Déplacer `Sortie` dans la salle (l'arène est passée de (2080, 64) à (2097, 460), au sol) ne
faisait donc plus bouger la porte : la sortie du boss restait en hauteur, inatteignable.

`PorteInterne` gagne un export **`Marqueur`** (NodePath) : au `_Ready` elle se pose sur le
marqueur visé. Les cinq portes du niveau pointent sur `../../Salle/Sortie` — déplacer le
marqueur dans la scène de salle suffit désormais, sans toucher au niveau. Les positions
authorées ont été remises en accord avec les marqueurs pour que l'éditeur ne mente pas.

Vérifié en jeu, Rodolphe vaincu, Glooby posé sur le sol de l'arène et touche « droite »
maintenue (vraies entrées, pas de téléportation) :

```
portes : Sanctuaire / Galerie / Puits / Caverne / ArenBoss -> toutes calées sur leur marqueur
t=1,0s : (10161, 480)  ← marche sur le sol de l'arène
t=2,0s : (10792, 575)  ← dans le Jardin
```

## Sortie du Jardin décalée en jeu

`ZoneChargementScene` (et son `Marker2D`/`PointEntree` « JardinGrotteEnd ») avaient été posés
dans `JardinGrotte/DecorAvant`, donc **dans un `Parallax2D`** (`scroll_scale` 1.15) : la zone
défilait plus vite que le sol, et le recalage de salle la déportait en plus de +1594 px. Même
famille que les props volants. Remontés à la racine de la salle, coordonnées inchangées :

```
Jardin posé à (10624, 0)
ZoneChargementScene : local salle (1673, 493)   (authoré (1673, 493))
Marker2D            : local salle (1609, 508)   (authoré (1609, 508))
```

L'aller-retour avec `monde2` est cohérent : le Jardin pointe sur `uid://qopyq3e1vk2k`
(monde2), qui renvoie sur `uid://dfcn030tpflmp` (ReindeerBoss) avec
`PointEntreeCible = "JardinGrotteEnd"`.

## Charge de Rodolphe : murs et franchissement

La charge ne s'arrêtait qu'aux bornes `LimiteGauche/Droite` de l'arène : contre un mur, le
boss poussait dans le vide jusqu'à atteindre la coordonnée limite. Et `Velocity.Y` était forcé
à 0 — il chargeait en lévitation à sa hauteur d'apparition.

- **Gravité** appliquée dans `_PhysicsProcess` (`LivingEntity.AppliquerGravite`) ; chaque état
  ne pilote plus que l'horizontale.
- **Mur = fin de charge** : `IsOnWall()` déclenche `PasserEnEtourdi()`, donc la fenêtre de
  vulnérabilité ×3 s'ouvre là où le décor l'arrête, pas seulement aux bornes de l'arène.
- **Obstacle bas = saut** : deux rayons devant lui (au ras des sabots, puis à
  `HauteurFranchissable`) distinguent une marche d'un mur plein. Marche → `Sauter`, il garde
  son élan et poursuit vers le joueur ; mur → étourdissement.
- Les obstacles ne sont jugés **que sabots au sol** : sans ce garde, les rayons perdaient la
  marche dès le décollage et la charge s'interrompait en plein saut.

**Mesure de l'obstacle.** Un rayon horizontal parti du museau ne marche pas : les ressauts du
décor sont des dalles dont le bas s'arrête au-dessus des sabots (7 px de vide sous la face),
le rayon passait dessous sans rien voir. On repart donc du **point de contact** rendu par
`MoveAndSlide` et on cherche le sommet de l'obstacle avec un **rayon vertical descendant**,
lancé depuis la hauteur franchissable : il part toujours du ciel libre, et s'il ne touche
rien c'est que l'obstacle monte plus haut que ce que Rodolphe sait franchir — un mur.

Exports : `HauteurFranchissable` (96 — couvre les ressauts de l'arène, 64 à 67 px, sans
approcher ses murs) et `MargeFranchissement` (16). L'impulsion de saut est **calculée**
(`v = √(2·g·h)`), donc un seul nombre à régler.

Testé dans l'arène réelle, boss lâché à droite et joueur posé sur le plateau haut :

```
charge vers la gauche : sauts en x 1422, 1265 (monte à y=323, le plateau haut), 983, 827,
                        556, 283  ->  arrêt net à x=78, mur gauche (face à 46)
charge vers la droite : sauts en x 289, 562, 831, 988, 1270, 1426
                        ->  arrêt net à x=2144, mur droit (face à 2163)
```

Les murs de l'arène étaient au départ faits de dalles décalées, avec un vide de 11 px au ras
du sol : le boss y lisait une « marche de 65 px » et sautillait sur place. Ils ont été refaits
d'un seul tenant (gauche x[0, 46] y[45, 503], droit x[2163, 2240] y[0, 503]) — **c'est la
géométrie qui a été corrigée, aucun garde-fou logiciel n'a été laissé dans l'IA**.

## Reste à faire / à l'œil

- **Play-test manuel** (`godot res://scenes/niveaux/ReindeerBoss.tscn`) : vérifier le calage
  des fonds salle par salle. Si un fond dérive dans l'autre sens, la formule devient
  `dx × (1 + scroll_scale.x)` (selon que Godot compose ou non la transform du parent).
- Vérifier que chaque `Sortie` est bien atteignable en sautant (les portes sont posées sur
  les marqueurs d'origine des salles).
- Aucun `Fonds`/`BackgroundManager` dans ce niveau : chaque salle porte son propre décor, une
  région par-dessus entrerait en conflit. `FondBossCerf.tscn` reste donc inutilisé ici.
- Les 5 `Grade` sont identiques (1.08, 1.08, 1.12) : leur superposition est sans effet.
