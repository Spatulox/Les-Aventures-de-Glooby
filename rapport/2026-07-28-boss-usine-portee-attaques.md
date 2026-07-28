# Boss de l'usine — attaques pondérées par la distance

Le Père Noël et le Lutin Mecha tiraient leurs patterns au hasard, sans regarder où était
le joueur : punch au sol dans le vide, tir de glace à bout portant. Ils lisent désormais
la distance et jouent ce qui porte ; hors de portée, ils viennent chercher le joueur.

## Zones d'engagement (nouveau, réglable dans l'éditeur)

Deux `Area2D` ajoutées dans **`scenes/boss/BossPereNoel.tscn`** et
**`scenes/boss/BossLutinMecha.tscn`** (`collision_layer = 0`, `collision_mask = 2`) :

| Zone | Père Noël | Lutin Mecha | Rôle |
|---|---|---|---|
| `ZoneCorpsACorps` | 320 × 130 | 300 × 130 | le joueur est collé → attaques de contact |
| `ZoneDistance` | 760 × 320 | 760 × 320 | anneau utile des attaques à distance |
| *(hors des deux)* | | | plus rien ne porte → rapprochement |

Le ±160 du Père Noël colle exactement à sa `PorteeOnde` : tout punch choisi peut toucher.
**Ces rectangles sont le bouton de réglage** — redimensionnables par instance dans l'éditeur,
sans toucher au C#.

## Code

- **`scripts/Common/PorteeJoueur.cs`** (nouveau) — enum `CorpsACorps` / `Distance` / `HorsPortee`.
- **`LivingEntity`** — la détection par zone, qui ne gérait qu'une `ZoneDetection` unique, devient
  générique : `CablerZonePresence(nom)` / `JoueurDansZone(nom)` câblent autant d'`Area2D` nommées
  qu'on veut. `CablerZoneDetection`/`JoueurAPortee` sont réécrits par-dessus, comportement inchangé.
- **`Boss`** — `CablerZonesEngagement()` + `EvaluerPortee()`, mutualisés puisque les deux boss en
  font le même usage. Repli sur `RayonCorpsACorps` / `RayonDistance` si la scène n'a pas les zones.

## Père Noël

- **Tirage pondéré** : collé → punch au sol 60 % (c'est SA seule attaque de contact) ;
  à distance → cadeau lancé 45 % / salve 35 % / cheminée 20 % — **le punch disparaît
  complètement**, il ne balaierait que du vide.
- **Rapprochement** : hors de portée il ne tente plus rien — cheminée (téléportation, sa marque)
  ou nouvel état `Approche`, une marche franche interrompue dès que le joueur revient à portée.
- `DistanceConfort`/`DistanceEngagement` supprimés : la bande de va-et-vient est maintenant celle
  que dessinent les deux zones.

## Lutin Mecha

- **Tirage pondéré** : collé → saut écrasant 55 % / trappe 30 % / tir 15 % ;
  à distance → tir 55 % / saut 25 % / trappe 20 %.
- **Rapprochement** : hors de portée, marche ou bond (il ne se téléporte pas). Le `Deplacement`
  s'arrête dès qu'il est au contact, au lieu d'entrer dans le joueur.
- **Trajectoire du bond corrigée** — le vrai changement de feel : le point de chute est **figé à
  l'instant où il s'accroupit**, et la vitesse horizontale calculée pour y retomber
  (`ecart / dureeVol`, `dureeVol = 2·|v0|/g`), plafonnée par `VitesseSautMax` (ex-
  `VitesseSautHorizontale`, 130 → 280). Le joueur a donc tout `DelaiAccroupi` pour s'écarter :
  bouger suffit à esquiver, et le mecha tombe là où il *était*.

## Vérification

Compilation propre. Un harnais headless jetable a piloté les deux boss hors arène (joueur
téléporté à 40 / 260 / 880 px), puis a été supprimé :

- zones câblées sur les deux boss (`zoneCaC=True zoneDist=True`) ;
- collé → `saut_accroupi` / `punch_sol` ; à distance → `tir` / `lancer_bas`, jamais de punch ;
- hors de portée → cheminée (Père Noël, ressort à 150 px du joueur) et bond/marche (Mecha) ;
- **atterrissage du bond : écart de 1,4 / 6,5 / 0,1 / 0,0 px** avec le point figé, alors que le
  joueur était déplacé de +200 px pendant le télégraphe. À 880 px le bond est plafonné et il
  faut trois sauts pour recoller — comportement voulu.

Reste à valider en jeu (F5) : la lisibilité du télégraphe accroupi à `VitesseSautMax` = 280.

## Hors périmètre

`scenes/niveaux/03-monde2.tscn` (décors retournés, échelle du Père Noël) est du travail en cours
non commité. `scenes/boss/ProloguePereNoel.tscn`, réécrit par Godot au passage des runs headless
(uid/unique_id), a été restauré.
