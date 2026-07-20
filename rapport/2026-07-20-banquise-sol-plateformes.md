# Banquise — sol en SolBanquise + éléments transformés en plateformes

## Objectif

Remplacer les `PlateformeUnidirectionnelle` de la banquise par `SolBanquise`/`PenteBanquise`,
caler le sol le plus bas possible dans le cadre, et rendre les éléments de
`assets/decors/banquise` utilisables comme plateformes (assets + `.tscn`).

## Changements

### Nouveau — `scripts/decors/PlateformeBanquise.cs` + `scenes/decors/PlateformeBanquise.tscn`

Plateforme traversable (one-way) au visuel banquise, paramétrée par un export
`Type` (`Plaque` / `Bloc` / `Congere`). Texture et boîte de collision sont
appliquées au runtime depuis un dictionnaire de config — même patron que
`SolBanquise`/`PenteBanquise`. Couche `Constantes.LayerPlateformesTraversables`,
`OneWayCollision = true` : le joueur saute au travers par en dessous et
redescend avec bas+saut.

C'est ce script qui rend « plateformes » les trois éléments de
`assets/decors/banquise/elements` (plaque_flottante, bloc_empilable, congere).

### Nouveau — `scenes/decors/SolBanquiseLigne.tscn`

`SolBanquiseLigne.cs` existait déjà mais n'avait pas de scène : impossible à
déposer depuis l'éditeur. La `.tscn` corrige ça.

### `scenes/niveaux/monde.tscn` (2 Edits chirurgicaux)

Les 5 `PlateformeUnidirectionnelle` de `Banquise/Sol` (`Sol1`, `Sol2`, `Sol3`,
`Sol5`, `SolBordTrou`, toutes en y=315) sont remplacées par :

| Nœud | Scène | Position | Réglage |
|---|---|---|---|
| `SolOuest` | SolBanquiseLigne | (1379, 350) | `NombreSegments = 0` |
| `SolEst` | SolBanquiseLigne | (2200, 350) | `NombreSegments = 0` |
| `MarcheCongere` | PlateformeBanquise | (1810, 288) | `Type = 2` (Congère) |
| `Pas1` | PlateformeBanquise | (1915, 326) | `Type = 0` (Plaque) |
| `Pas2` | PlateformeBanquise | (1990, 312) | `Type = 1` (Bloc) |
| `Pas3` | PlateformeBanquise | (2065, 326) | `Type = 0` (Plaque) |

L'ext_resource `sol` (PlateformeUnidirectionnelle) est **conservé** : le Village
et la Grotte s'en servent toujours.

## Calage vertical — pourquoi y = 350

La surface de marche d'un `SolBanquise` est à **y local = −50**. Village,
banquise et grotte marchent tous à **y monde = 300** → les étagères sont posées
à **y = 350**, ce qui conserve exactement la ligne de marche existante.

Effet recherché : les sprites de glace (180px+ de haut, plus les stalactites
suspendues des embouts) débordent vers le bas au-delà de la limite basse de
caméra (y ≈ 316). Le sol occupe donc le bas du cadre et se lit comme une
banquise, au lieu d'une dalle fine.

## Continuité horizontale (rien n'a été déplacé)

- `SolOuest` couvre **1251..1851** — jointif avec Village/Sol5 (bord droit 1251).
- Trou de saut **1851..2072** (221 px) — remplace l'ancien `SolBordTrou`.
- `SolEst` couvre **2072..2672** — recouvre Grotte/Sol1 (bord gauche 2641) de
  31 px, jonction sans couture.

Décors vérifiés, tous posés, aucun flottant ni enterré : FleurGivre@1500,
Rocher@1600, CristalPetit@1750 sur `SolOuest` ; CristalGros@2200,
FleurGivre2@2420 sur `SolEst`.

Traversée du trou (sauts courts) : sol 300 → congère 270 → Pas1 308 →
Pas2 282 → Pas3 308 → sol est 300.

## Second passage — dossier `sol/` et fin de la traversée bas+saut

### Les sols sortent de `decors/` → `scenes/sol/` + `scripts/sol/`

Un sol porte de la collision : ce n'est pas du décor. Déplacés (via `git mv`,
historique préservé) :

| Avant | Après |
|---|---|
| `scenes/decors/SolBanquise.tscn` | `scenes/sol/SolBanquise.tscn` |
| `scenes/decors/SolBanquiseLigne.tscn` | `scenes/sol/SolBanquiseLigne.tscn` |
| `scenes/decors/PenteBanquise.tscn` | `scenes/sol/PenteBanquise.tscn` |
| `scenes/decors/PlateformeBanquise.tscn` | `scenes/sol/PlateformeBanquise.tscn` |
| `scripts/decors/*.cs` (+ `.uid`) | `scripts/sol/*.cs` |

`scripts/` suit `scenes/` comme l'impose CLAUDE.md. Références mises à jour :
les 4 `ext_resource` de script, `monde.tscn` (2 lignes), `DemoUsine.tscn`
(1 ligne) et le `GD.Load` de `SolBanquiseLigne.cs`. Il ne reste plus aucun
chemin vers `decors/SolBanquise…`. `scripts/decors/` ne contient plus que
`CameraDemoParallax.cs`.

### Correction — plus de chute au travers avec bas+saut

**Cause** : c'est `PlateformeBanquise` qui était fautive. Je l'avais posée sur
`Constantes.LayerPlateformesTraversables` (layer 5) avec `OneWayCollision = true`,
or `Player.GererTraverseePlateforme()` démasque précisément le layer 5 sur
bas+saut. Les marchepieds du trou étaient donc traversables — au-dessus d'un
trou mortel, c'est une mort accidentelle.

`SolBanquise` et `PenteBanquise` n'ont jamais été en cause : ils ne touchent pas
à `CollisionLayer`, donc layer 1, jamais démasqué.

**Correctif** : `PlateformeBanquise._Ready()` ne force plus ni `CollisionLayer`,
ni `CollisionMask = 0`, ni `OneWayCollision` → sol plein sur le layer 1.
Commentaire de classe réécrit en conséquence (sol surélevé, pas plateforme
traversable).

## Troisième passage — topographie, cadrage, assets

### Bug de fond : une décision jamais appliquée

`DECISIONS.md` documentait « floor_snap_length passé de 1 à 8 ». Un grep sur tout
le dépôt ne trouvait **aucune occurrence** de `FloorSnapLength` : le réglage
n'avait jamais été posé. Sans lui, toute pente fait dévaler le joueur en petits
sauts — et pas seulement à 45° : à `Speed=220` il avance 3,67 px/frame, soit déjà
1,45 px de chute sur une pente *douce*, au-delà du snap par défaut de 1 px.
Posé dans `Player._Ready()`. C'était le prérequis de tout le reste.

### Bug de fond : caméra figée verticalement sur toute la carte

Les zones caméra faisaient 243-256 px de haut pour un viewport de 360. Sous cette
hauteur, le clamp de Godot reçoit `min > max` et retourne toujours `min` : **la
caméra ne suit plus le joueur en vertical**. Le cadrage du jeu n'était donc choisi
par personne. Toutes les zones sont passées à ≥ 360.

| Zone | Avant (haut→bas) | Après | Effet |
|---|---|---|---|
| `ZoneBanquise` | 73 → 317 (243) | −100 → 500 (600) | la caméra suit ; les 180 px de glace sont visibles |
| `ZoneVillage` | 74 → 330 (256) | 74 → 434 (360) | cadrage inchangé, mais légal |
| `ZoneGrotte` | 72 → 328 (256) | 72 → 432 (360) | idem |
| `ZoneArenaBoss` | 72 → 328 (256) | 72 → 432 (360) | idem |
| `ZoneBossCerf` | 72 → 328 (256) | 72 → 432 (360) | idem — **trouvée par la vérification**, je l'avais ratée |
| `ZoneLabyrinthe` | 317 → 919 (603) | inchangée | était déjà correcte |

Village/grotte/arènes gardent leur bord haut : la plage de clamp se réduit à un
point unique, donc leur cadrage reste **identique à l'ancien**. Leurs sols sont
fins, leur faire suivre la caméra révélerait du vide dessous.
`MargeChuteVide = 196` compense le bord bas descendu, pour garder le seuil de
mort d'origine.

### Topographie : PenteBanquise enfin utilisée

`Banquise/Sol` devient rampe → plateau → rampe → plat. Les pentes entrent à
y local −50 et sortent à −50 ∓ 136, donc le raccord est exact par construction.

| Nœud | Scène | position | Type | Surface |
|---|---|---|---|---|
| `RampeOuest` | PenteBanquise | (1418, 350) | DouceMontante | 300 → 164 |
| `Plateau` | SolBanquise | (1762, 214) | CentreA | 164 |
| `RampeEst` | PenteBanquise | (2106, 214) | DouceDescendante | 164 → 300 |
| `PlatEst1` / `PlatEst2` | SolBanquise | (2450/2544, 350) | CentreB / CentreC | 300 |
| `Glace1/2/3` | PlateformeBanquise | 104 / 44 / 104 | Plaque / Bloc / Congère | escalier au-dessus du plateau |

La **pente forte est écartée** : 44,8° contre un `FloorMaxAngle` par défaut de 45°,
0,2° de marge. Seule la douce (21,6°) est utilisée.

Contrainte assumée : 1390 px de banquise ÷ 344 = 4 segments. Une colline en coûte
2, donc **il n'y avait pas la place pour une colline *et* le trou** — le trou
disparaît. Les 5 props ont été reposés sur la nouvelle topographie (aucun sur une
rampe, ils ne pivotent pas).

### Village converti lui aussi

Point soulevé par l'utilisateur : `monde.tscn` utilisait encore
`PlateformeUnidirectionnelle`. La banquise n'en avait déjà plus, mais le Village
(5), la Grotte (16) et le Labyrinthe (21) si. Le **Village passe en SolBanquise**
(même biome `banquise`, et la jonction ouest devient homogène). Grotte et
Labyrinthe sont des cavernes : y mettre de la glace de mer serait incohérent —
laissés en l'état, à trancher.

### Sprites bakés dans les .tscn (remarque utilisateur)

Défaut réel : mes `.tscn` ne contenaient qu'un `Sprite2D` **vide**, les PNG
n'apparaissaient qu'au lancement du jeu — donc invisibles dans l'éditeur, où l'on
compose pourtant la map. `PlateformeUnidirectionnelle` fait l'inverse depuis
toujours. Les 3 scènes s'alignent sur ce patron : texture + collision du type par
défaut écrites dans la scène, `_Ready` les ré-appliquant depuis `Type` au runtime.

| Scène | Visuel baké | Collision bakée |
|---|---|---|
| `SolBanquise.tscn` | `sol_centre_a.png` (CentreA) | Rect 344×112 @ (0,6) |
| `PenteBanquise.tscn` | `pente_douce_montante.png` | Polygone (−172,−50)…(172,−186) |
| `PlateformeBanquise.tscn` | `plaque_flottante.png` (Plaque) | Rect 96×28 @ (0,−4) |

### Une .tscn par variante (l'éditeur affichait la mauvaise pente)

Le bakage précédent figeait **le type par défaut**, une seule fois pour toutes les
instances : `PenteBanquise.tscn` contenait `DouceMontante`. Or `RampeEst` porte
`Type = 1` (descendante), et l'éditeur n'exécute aucun script — il dessinait donc
une rampe montante là où le jeu, lui, était correct (`_Ready` réécrit tout au
lancement). Même écart silencieux sur `Sol1` du village (embout affiché en centre)
et sur `PlatEst1/2`.

Corrigé par **12 scènes figées**, une par variante, générées depuis les tables
`Configs` des scripts (aucune valeur recopiée à la main) :

- `SolBanquiseCentreA/B/C`, `SolBanquiseEmboutGauche/Droit`
- `PenteBanquiseDouceMontante/Descendante`, `...ForteMontante/Descendante`
- `PlateformeBanquisePlaque/Bloc/Congere`

Les 3 scènes génériques sont supprimées ; `monde.tscn` référence la variante
exacte par nœud (les surcharges `Type` disparaissent), `SolBanquiseLigne` mappe
type → scène, `DemoUsine` pointe vers `SolBanquiseCentreA`.

### PNJ qui traversaient le sol

Régression que j'ai introduite en convertissant le village. Les PNJ portaient
`collision_mask = 16`, soit **le layer 5 uniquement** (`LayerPlateformesTraversables`) :
ils avaient été réglés à l'époque où tout le sol était en `PlateformeUnidirectionnelle`.
`SolBanquise`/`PenteBanquise` étant sur le layer 1 (terrain plein), les PNJ ne les
voyaient pas du tout et tombaient au travers. Le joueur, lui, a `17` = layer 1 + 5,
d'où l'asymétrie qui rendait le bug invisible côté joueur.

**Premier correctif, mauvais** : masque porté à `17` comme le joueur. Ça remettait
les PNJ sur le sol, mais créait aussitôt des collisions joueur ↔ PNJ. Cause : **le
layer 1 est ambigu**, le terrain *et* le joueur s'y trouvent (le joueur n'a pas de
`collision_layer` explicite, il hérite du défaut 1). Masquer le layer 1 pour tenir
sur le sol, c'est forcément se cogner au joueur aussi. Aucun réglage de masque ne
peut séparer les deux tant que le layer 1 signifie deux choses.

**Correctif retenu** : lever l'ambiguïté avec un layer dédié,
`Constantes.LayerSolPnj` (layer 2). Tout terrain plein s'y déclare **en plus** du
layer 1 ; les PNJ masquent `MasqueSolPnj = 18` (layer 2 + traversables), donc plus
du tout le layer 1.

| Acteur | layer | mask | Effet |
|---|---|---|---|
| Joueur | 1 | 17 | voit le terrain (via 1) et les traversables |
| Sol / murs | **3** (= 1+2) | — | solides pour le joueur *et* pour les PNJ |
| PNJ, boss | 4 | **18** | voient le sol (via 2), **pas** le joueur |

Ajouté au layer 1 plutôt que substitué : un terrain qu'on oublierait de marquer
resterait solide pour le **joueur**. La panne possible est côté PNJ, jamais côté
joueur — c'est le sens le moins grave. `MurSolide` et `MurFondable` sont marqués
aussi, sinon les PNJ traverseraient les murs.

Le **boss** avait le problème symétrique : aucun masque explicite, donc `1`
(terrain seul), alors que le sol de son arène est en `PlateformeUnidirectionnelle`
(layer 5) — il traversait le sol de son propre combat. Bug latent préexistant,
corrigé avec le même `18`.

### CRITIQUE — le jeu était devenu impossible à terminer

Découvert en allant vérifier autre chose. Ma refonte de la banquise avait **coupé
la progression du jeu**, et mes tests précédents ne l'avaient pas vu.

La chaîne :

1. L'unique entrée du **Labyrinthe** est `LedgeEntree` en (2000, 380). On y accédait
   en tombant à travers le sol de la banquise — alors en `PlateformeUnidirectionnelle`
   (traversable) **et** percé d'un trou en 2080..2196.
2. J'ai bouché le trou *et* rendu le sol plein → plus aucun accès.
3. Or le Labyrinthe contient `PouvoirChaleur` (2500, 805).
4. Sans lui, `MurGlace` (2700, 255) ne peut pas fondre…
5. …et c'est exactement ce qui bloquait mon joueur de test à x=2678. **J'avais
   qualifié ce blocage de « porte de progression voulue » sans vérifier que la clé
   était encore atteignable.** Ni grotte, ni boss, ni fin.

**Premier correctif, insuffisant** : rouvrir un trou. Le joueur entrait, mais le
trou débouchait au niveau du plateau (164) → remontée de 201 px pour un saut de
73,5. Le Labyrinthe devenait un **piège sans issue**. Attrapé parce que le test de
sortie ne comptait que les positions *posées* : une première version regardait le
`y` minimum et voyait « sorti » sur un simple sommet de saut.

**Correctif retenu** : le trou doit être dans une section **plate à y=300**, comme
à l'origine (remontée de 65 px). Or une colline complète (rampe + plateau + rampe
= 688 px) ne rentre ni à l'ouest de l'entrée (610 px) ni à l'est (502 px). Le
plateau saute : deux rampes accolées font exactement 688 px et tiennent pile entre
le village et le trou.

| Nœud | position | Portée | Surface |
|---|---|---|---|
| `RampeOuest` (montante) | (1418, 350) | 1246→1590 | 300 → 164 |
| `RampeEst` (descendante) | (1762, 214) | 1590→1934 | 164 → 300 |
| **trou** | — | 1936→2050 (114 px) | entrée du Labyrinthe |
| `PlatEst1` / `PlatEst2` | (2226/2570, 350) | 2054→2742 | 300 |

La colline devient une crête à x=1590 au lieu d'un plateau, et les 3 blocs de glace
s'y reposent en escalier. Les 5 props repassent sur le plat est.

### Zones caméra renommées, et le sol de la « Grotte » converti

Le nom `Grotte` désignait en fait de la banquise : cette zone porte
`NomRegion = "banquise"`, alors que `ZoneLabyrinthe` porte `"grotte"`. C'est le
**nom** qui était faux, pas la région — ce que j'avais signalé à tort comme un
possible bug de fond.

- `ZoneGrotte` (couloir est) → **`ZoneBanquise2`**
- `ZoneLabyrinthe` (la vraie caverne) → **`ZoneGrotte`**

Aucune référence en code : les zones sont trouvées par groupe, pas par nom.

Du coup `SolBanquise` y est cohérent (ce n'est pas une caverne). Les 16
`PlateformeUnidirectionnelle` de `Grotte/Sol` deviennent **11 segments** de 344
(centres espacés de 327 → 17 px de recouvrement, zéro couture) couvrant
exactement l'ancienne étendue 2636→6250, plus les 3 corniches à y=293 (surface
243, soit 57 px au-dessus du sol — sautables avec 73,5 px).

Reste seul le **Labyrinthe** (21 nœuds) en `PlateformeUnidirectionnelle`, exclu à
votre demande — et c'est la vraie caverne, donc `SolBanquise` n'y conviendrait pas.

### La scène devient la seule source de vérité, centres unifiés

Symptôme signalé : baisser la hauteur de collision d'un `SolBanquise` dans
l'éditeur ne changeait pas la hauteur à laquelle joueur et PNJ se posent. Mesuré :

```
SolBanquiseCentreA.tscn   editeur: 344x83 @ (0,20.5)   runtime: 344x112 @ (0,6)
```

Les scripts réappliquaient sprite et collision depuis leurs tables `Configs` à
chaque `_Ready()`. Avec une scène par variante, cette réapplication n'avait plus
d'utilité — elle ne faisait qu'écraser le réglage éditeur. **Retirée** des trois
scripts ; il ne reste que la ligne `CollisionLayer` (comportement, pas géométrie).
Les tables `Configs` mortes ont été supprimées.

Centres unifiés : `SolBanquiseCentreA/B/C` disparaissent au profit d'une seule
`SolBanquise.tscn`. `SolBanquiseLigne` perd son alternance A/B/C et ses
dictionnaires, réduit à trois constantes de chemin.

Surface de marche harmonisée sur celle de `SolBanquise.tscn` : **y = −29**
(collision 91 de haut, bas à 62). Embouts et pentes réalignés dessus, sinon un
segment non retouché aurait fait une marche avec les autres. Les nœuds de
`monde.tscn` ont été décalés en compensation pour que la ligne de marche reste
**exactement** à 300 dans le monde — vérifié : x=700, 1100, 2100, 2300, 2600,
3000, 4000, 5900 renvoient tous `y=300`.

### Assets rangés

`assets/decors/sol/` → `assets/sol/` et `assets/decors/banquise/elements/` →
`assets/sol/elements/`. Le `parallax/` reste sous `decors/` (c'est du vrai décor).
Chemins mis à jour dans les 3 tables `Configs` et les `.import` ; 12 assets
réimportés sans erreur.

## Vérification

- `godot --headless --build-solutions --quit` → `dotnet_build_project [ DONE ]`, 0 erreur.
- `godot --headless --quit-after 200 res://scenes/niveaux/monde.tscn` → **0 erreur** sur 200 frames.
- `godot --headless --quit-after 60 res://scenes/decors/usine/DemoUsine.tscn` → 0 erreur
  (vérifie que le déplacement n'a rien cassé côté usine).
- Contrôle runtime des layers (script temporaire, supprimé depuis) sur
  `Banquise/Sol` — les 8 nœuds ressortent en `layer=1 | one_way=false` :
  `SolOuest` (2 segments), `SolEst` (2 segments), `MarcheCongere`, `Pas1`,
  `Pas2`, `Pas3`. Le layer 5 étant le seul démasqué par bas+saut, la traversée
  est bien impossible partout sur la banquise.
- (Le boot par défaut remonte des `ERROR: Not supported by this display server` dans
  `MenuPrincipal.ToucheDe` — préexistant, propre au headless, sans rapport avec ce travail.)

### Vérifications du troisième passage

- **Profil du sol par raycast** (balayage x = 0 → 2716, pas de 8 px) :
  **aucun trou**. Profil mesuré 300 → 164 → 300, pente à **21,7°** relevés contre
  21,6° théoriques. Les 3 marches de glace ressortent à y = 86 / 14 / 86.
- **Traversée simulée** (`move_right` maintenu, 900 frames physiques) :

  ```
  frame 300 : x=1280 y=270 sol=true     attaque la rampe
  frame 400 : x=1585 y=149 sol=true     sur le plateau (pieds a 164)
  frame 600 : x=2319 y=284 sol=true     redescendu a 300
  x atteint = 2678 (entree grotte a 2636)
  y le plus bas = 290  ->  jamais tombe
  ```

  `sol=true` à chaque relevé : la pente est réellement marchable et le joueur
  ne se bloque nulle part. C'est la preuve que `FloorSnapLength` fonctionne.
- **Zones caméra** : les 6 zones mesurées à ≥ 360 (600 / 360 / 360 / 360 / 360 / 603).
- **Sprites** : texture du `.tscn` comparée à celle appliquée au runtime pour les
  3 scènes + `PlateformeUnidirectionnelle` en témoin → identiques, aucune dérive
  possible entre l'aperçu éditeur et le jeu.
- **Éditeur vs runtime, les 12 variantes** : pour chacune, la texture *et* la
  géométrie de collision écrites dans le `.tscn` sont comparées à ce que le script
  applique au runtime → **12/12 identiques**. L'aperçu éditeur ne peut plus mentir.
- **Re-traversée après bascule sur les variantes** : profil et continuité
  inchangés, aucun trou. Le joueur atteint x = 2678.
- **PNJ posés sur le sol** — après 4 s de simulation, les 4 PNJ du monde sont
  `au_sol = true` avec du sol réel sous eux (raycast vers le bas) :

  ```
  Pingouin    y=314.9  au_sol=true   sol sous lui a y=331
  Pingouin2   y=283.9  au_sol=true   sol sous lui a y=300
  LutinNoel   y=288.9  au_sol=true   sol sous lui a y=300
  LutinNoel2  y=288.9  au_sol=true   sol sous lui a y=300
  ```

  (Première version du test : seuil de chute en pixels — il classait `Pingouin`
  en échec alors qu'il reposait simplement sur la marche basse à 331. Remplacé
  par `is_on_floor()` + raycast, qui mesure la bonne chose.)
- **Joueur ↔ PNJ** après le passage au layer 2 : layers/masques relevés au runtime
  (joueur `1/17`, PNJ `4/18`, `Plateau` `layer=3`), les 4 PNJ `au_sol=true`, et le
  joueur poussé vers l'est traverse bien le pingouin (x 278 → 382 en passant par 308).
- **Non-régression bas+saut** après le changement de layer du sol : le joueur posé
  sur le plateau presse bas+saut → **chute = 0,0 px**, il ne traverse pas. (Le
  masque tombe à `1` pendant la traversée, et le sol est en layer 3, donc toujours
  vu par le bit 1.)
- **Chaîne de progression, bout en bout** (le test qui manquait) :

  ```
  trou banquise 1936->2050, au-dessus de LedgeEntree (1856..2134) : OUI
  bords du trou : gauche=293  droite=300   -> remontee de ~65px, sautable
  chute a x=2000 -> repose sur LedgeEntree a y=365 : DANS LE LABYRINTHE
  pouvoir avant / apres passage sur le pickup : false -> true
  depuis le labyrinthe, point POSE le plus haut : y=156 a x=1612 : SORTI
  traversee complete : x=200 -> 2678, s'arrete au MurGlace (2700)
  ```

  Entrée, récupération du pouvoir, sortie et retour au mur : la boucle est fermée.
- **Sol de la « Grotte » après conversion** :

  ```
  TROUS sol 2636->6250 : aucun
  corniches x=3500/4550/5600 -> surface 243 (57px au-dessus du sol, saut 73.5)
  zones camera : ZoneVillage, ZoneBanquise, ZoneBanquise2, ZoneGrotte,
                 ZoneArenaBoss, ZoneBossCerf
  traversee : x=2900 -> 6228 (fin du sol 6250), y le plus bas 289 : jamais tombe
  ```
- Scripts temporaires supprimés, aucun processus Godot résiduel.

### Point d'attention sur le village

Le déplacement de `Sol2` en (306, 381) crée une surface à 331 face à `Sol3` resté
à 300, soit une **marche de 31 px**. `SolBanquise` étant un sol plein (pas une
pente), elle **bloque la marche** : la traversée en marchant seule s'arrête à
x = 382. Un saut la franchit (31 px contre 73,5 px de saut) — vérifié, saut #1 à
x = 382, puis le joueur file jusqu'à la grotte. À confirmer que c'est voulu.

Effet positif au passage : cette marche coupe l'ancien ressaut de 75 px entre
`Sol1` (375) et le sol principal — 75 px était au-dessus de la hauteur de saut
(73,5), donc infranchissable. Il devient 44 + 31.

Un F5 manuel reste nécessaire : le ressenti de la montée/descente et la lisibilité
du nouveau cadrage ne se valident pas en headless.

## Reste à trancher

Grotte (16 nœuds) et Labyrinthe (21) utilisent encore
`PlateformeUnidirectionnelle`. Ce sont des cavernes : `SolBanquise` (glace de mer)
y serait incohérent, et la mémoire projet indique `BlocGrotte` pour le sol de
grotte. À confirmer avant conversion.

Observation hors périmètre : `ZoneGrotte` porte `NomRegion = "banquise"` alors que
`ZoneLabyrinthe` porte `"grotte"` — la région `grotte` existe donc bien, et la
grotte affiche probablement le mauvais fond.

## Reste à trancher — `PenteBanquise` n'est pas placée

`PenteBanquise` est complète (asset, script, `.tscn`, pentes douces marchables)
mais **non instanciée** dans la banquise. Raison géométrique :

- Une pente fait 344 px de large et impose un dénivelé net fixe de **136 px**
  (douce) ; revenir au même niveau demande montante+descendante ≥ **688 px** de
  sol libre.
- La plus large fenêtre sans décor de la banquise est 1750→2200 = **450 px**.
  Toute fenêtre de 688 px contient ≥ 2 décors, qui seraient enterrés ou laissés
  en l'air — or les décors ne doivent pas bouger.
- Une pente purement décorative dans le trou serait invisible : la limite basse
  de caméra est à 16 px sous la ligne de marche, ~94 % de la pente serait hors champ.
- Une pente avec collision dans le trou transformerait la chute mortelle en
  « coincé dans une cuvette » (`SeuilChuteVide` ≈ 616).
- Les pentes fortes (≈44.8°) sont écartées : elles frôlent le `floor_max_angle`
  de 45° par défaut, et `Player.cs` appelle `MoveAndSlide()` sans réglage de pente.

**Question ouverte** : autoriser le déplacement de 2–3 décors de la banquise (ou
descendre la `CameraZone` pour révéler une cuvette) afin d'y loger une vraie
pente marchable ?

## Fix — traversée des plateformes en sortie de glissade

**Symptôme** : après un `shift` (glissade), quand le joueur se remet debout il
traverse les `PlateformeUnidirectionnelle` comme s'il n'avait plus de hitbox.

**Cause** : désalignement vertical des deux formes de collision du joueur dans
`scenes/entites/player.tscn`.

| Forme | Géométrie | Bas (local) |
|---|---|---|
| `CollisionDebout` | Capsule r=11 h=32, `scale=(1.04, 1.4)`, `y=0` | `+22.4` |
| `CollisionGlisse` | Rect 26×14, `y=9` | `+16.0` |

Pendant la glissade le corps se pose sur le bas de `CollisionGlisse` : son origine
est donc 6,4 px plus bas qu'en debout. `FinirGlissade()` réactive `CollisionDebout`
sans recaler la position → la capsule réapparaît **encastrée de 6,4 px** dans le
sol. Sur le sol plein (layer 1) la dépénétration corrige silencieusement ; sur une
plateforme `OneWayCollision` un corps déjà à l'intérieur n'est jamais repoussé →
traversée.

**Correction** : `CollisionGlisse.position` `Vector2(0, 9)` → `Vector2(0, 15.4)`,
ce qui aligne son bas sur `+22.4`. Aucun changement de code. La forme de glissade
reste nettement plus basse que la debout (haut `+8.4` vs `-22.4`), donc « passer
sous un obstacle » est préservé.

**Vérifié** : compilation propre, run headless sans nouvelle erreur (les erreurs
`MenuPrincipal.ToucheDe` sont préexistantes — API clavier non supportée en
headless). **Test manuel F5 encore à faire** : glisser sur une plateforme
traversable et laisser la glissade se terminer dessus.
