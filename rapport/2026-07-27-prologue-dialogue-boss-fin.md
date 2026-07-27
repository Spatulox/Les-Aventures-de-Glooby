# `BossEnd` : on parle d'abord, on se bat ensuite, et on repart avec le pantalon

**Besoin** : donner une vraie fin à l'arène finale. Le joueur **discute** avec un PNJ
amical, un **fondu au noir** l'échange contre le boss, et la victoire ne se solde plus
par un minuteur mais par **l'objet de la quête**. Deux fins, selon le don des 50
poissons au lutin CGT :

| | Fin normale (pas de don) | Fin cachée (don) |
|---|---|---|
| Prologue | **Père Noël** : refuse d'échanger le pantalon, il n'a pas le temps | **Lutin d'usine**, pose *paquet* : pas de pantalon de rechange, ils sont en grève |
| Combat | Boss Père Noël (45 PV) | Boss Lutin Mecha (40 PV) |
| Après | le boss **lâche le pantalon** | on **délivre le Père Noël de sa cage**, il donne le pantalon |
| Fin | ramasser le pantalon → `ecran_fin` | idem |

Les deux prologues sont en **dialogue écrit** (pas d'IA) : mêmes répliques à chaque
partie, aucune attente de génération.

## 1. Le signal manquant — `scripts/Core/DeclencheurDialogue.cs`

Le moteur n'avait **aucun signal de fin** : le seul point de notification était
`Talkative.SurFinDialogue()`, qui ne distingue pas « conversation menée à son terme »
de « le joueur s'est éloigné ». Ajout de :

```csharp
[Signal] public delegate void DialogueTermineEventHandler(bool complet);
```

émis dans `TerminerDialogue`, dans le bloc `if (etaitEnDialogue)` déjà présent, avec
`complet = !sortie`, **après** `SurFinDialogue()` (un PNJ à usage unique s'y est déjà
marqué consommé, les abonnés voient donc l'état d'après).

Générique, sans une ligne de gameplay dans le moteur — et déjà utilisé deux fois :
`ZoneBoss` pour lancer le combat, `CagePereNoel` pour ouvrir la cage. Vérifié qu'aucune
propriété `DialogueTermine` n'existe (collision signal/propriété, piège Jalon B).

## 2. La phase prologue — `scripts/Core/ZoneBoss.cs`

Trois exports calqués sur le trio d'aiguillage existant :

| Export | Rôle |
|---|---|
| `ScenePnjPrologue` | interlocuteur du boss normal (vide = arène sans prologue) |
| `ScenePnjPrologueAlternatif` | celui de la fin cachée |
| `DureeFonduPrologue` | demi-fondu de l'échange (0,5 s) |

résolus par `ScenePrologueChoisie`, jumelle de `SceneChoisie` — l'alternative n'est prise
que si elle est assignée, donc une arène peut n'avoir qu'un seul PNJ pour ses deux boss.

Le corps de `SurEntreeJoueur` est extrait tel quel dans `LancerCombat(joueur)` ;
`SurEntreeJoueur` devient un aiguillage (boss vaincu → boss vivant → prologue en cours →
prologue à jouer → combat).

**`LancerPrologue`** instancie le PNJ **au point d'apparition du boss** (`Marker2D
ApparitionBoss`) — c'est ce qui rend l'échange invisible sous le noir — branche
`DialogueTermine`, et ajoute le nœud en **différé** (`CallDeferred(AddChild)`), même
raison que le boss : on arrive de `BodyEntered`, en plein flush des requêtes physiques.

**Aucun identifiant de mémoire sur la zone.** C'est le PNJ qui dit s'il a encore quelque
chose à raconter, via `Talkative.PeutParler()` — donc via `UneSeuleFois` + `IdDialogue`
que `PnjAmical` porte déjà et que `SurFinDialogue()` mémorise dans `GameState`. La clé
vit d'un seul côté et ne peut pas se désynchroniser. Si le PNJ n'a plus rien à dire,
l'instance est libérée (`Free()`, jamais entrée dans l'arbre) et le combat démarre
directement. `TrouverDeclencheur` cherche le `DeclencheurDialogue` **par type**, pas par
nom : aucune convention de nommage à connaître.

**`SurPrologueTermine(complet)`** ignore `complet == false`. Sinon : `DialogueModal = true`
(le joueur reste figé pendant le noir, sans quoi il reprend la main pendant le fondu),
`Sauvegarder()`, puis `Effets.FondreAuNoirPuis` — le PNJ est libéré au noir complet et
`LancerCombat` prend le relais. `DialogueDisponible = false` au passage : filet contre un
PNJ qui aurait laissé son rappel de touche armé, Espace resterait sinon détourné du saut.

`ReinitialiserCombat` (mort du joueur) libère aussi le PNJ de prologue.

## 3. Le butin de boss — `scripts/Entities/Pnj/Boss.cs`

```csharp
[Export] public PackedScene Butin;
```

Lâché par `Mourir()` **à l'endroit exact où le boss est tombé**, en frère de lui-même
(il reste donc en place quand le boss est libéré), en `CallDeferred(AddChild)` — le coup
fatal vient en général d'un contact, donc d'un flush de requêtes physiques.

Le butin appartient au **boss** et non à l'arène : deux boss qui partagent la même salle
ne lâchent pas la même chose, et le régler ici évite de le dupliquer sur chaque zone.
`BossPereNoel.tscn` porte le pantalon ; `BossLutinMecha.tscn` ne lâche rien (c'est la
cage qui donne).

## 4. Le pantalon — `PantalonPickup`

`scripts/Entities/Interactable/PantalonPickup.cs` + `scenes/interactifs/PantalonPickup.tscn`,
un `ElementRamassable` de plus (contact, flottaison, auto-retrait s'il est déjà pris).

Il mémorise `pantalon_obtenu`, sauvegarde, puis enchaîne sur `CheminSceneSuite`
(`ecran_fin.tscn`) après un fondu. **C'est lui qui clôt la partie** : `CheminSceneVictoire`
a été retiré de l'arène, parce que 2,5 s après la mort du boss ne laissent pas le temps
d'aller chercher un objet à l'autre bout de la salle.

Piège traité : `ElementRamassable` libère le nœud juste après `Ramasser()`, donc l'arbre
est capturé **avant** le fondu (le rappel ne peut plus demander `GetTree()` à un nœud
mort). Le voile, lui, vit sous la racine et survit au changement de scène.

**Visuel : placeholder.** `assets/props/pantalon.png`, un 32×32 dessiné à la main en
procédural — **0 génération PixelLab**. À remplacer par de l'art quand le budget rouvre :
c'est un simple échange de texture dans la scène.

## 5. La cage du Père Noël — `CagePereNoel`

`scripts/Entities/Interactable/CagePereNoel.cs` + `scenes/interactifs/CagePereNoel.tscn`,
posée **en bout d'arène et visible dès le début** : on voit le Père Noël prisonnier
pendant tout le combat, ce qui explique visuellement qu'un lutin défende l'atelier.
Les deux sprites existaient déjà (`assets/props/noel/perenoel_cage_{fermee,ouverte}.png`).

Trois emprunts, aucune mécanique neuve :
- **`MemoireRequise`** — la cage se retire d'elle-même au `_Ready` dans la fin normale,
  où le Père Noël est le boss et n'a rien à faire en cage ;
- **`BossRequis`** — le verrou de progression de `PorteInterne`
  (`GameState.EstBossVaincu`) ;
- **`Talkative`** sur le modèle de `PanneauBois` — tout le rappel de touche et la bulle
  viennent du `DeclencheurDialogue` enfant. La cage ne fait qu'échanger son sprite et
  lâcher son `Contenu`.

`Dialogue` renvoie `LignesAvant` (il supplie) ou `LignesApres` (il remercie et donne le
pantalon) selon que le boss est tombé, et c'est `DialogueTermine(complet: true)` qui
ouvre — s'éloigner en plein milieu n'ouvre pas la cage à distance.

## 6. Pose « paquet » du lutin — `scripts/Entities/Pnj/LutinUsine.cs`

Export `Pose { Etabli, Paquet }` qui choisit le dossier de frames
(`assets/pnj/lutin_usine/{idle,paquet}`), sur le modèle de `LutinCgt.Pose`. Le lutin
étant immobile dans les deux cas, il n'y a toujours qu'une animation `idle`.

## 7. Les scènes et les dialogues

- **`scenes/boss/ProloguePereNoel.tscn`** / **`PrologueLutinUsine.tscn`** — deux
  nouvelles scènes plutôt que d'éditer `scenes/props/noel/` (`LutinUsine.tscn` sert dans
  `DemoUsine.tscn` et n'a rien à faire avec un dialogue de boss). Gabarit
  `pingouin.tscn`, avec le nœud **`Apercu`** que les scènes `props/noel/` oubliaient.
  `UneSeuleFois` + `IdDialogue`, `ConversationRessource`, **pas de `DialogueDynamique`**.
- **`assets/dialogues/bossend_pere_noel.tres`** / **`bossend_lutin_usine.tres`** —
  convention `<lieu>_<pnj>.tres`, modèle `banquise_fin_lutin_cgt.tres`. Racine (2
  répliques + 3 réponses) → nœud intermédiaire → **nœud terminal sans `Choix`**, sur
  lequel toutes les branches convergent : un nœud sans choix disponible referme la
  conversation, ce qui déclenche le combat.
- **`scenes/niveaux/BossEnd.tscn`** — édition chirurgicale : les deux prologues sur
  `ZoneBossFinale`, la cage sous `Arene/Interactifs` en (1600, 152) avec ses répliques,
  et retrait de `CheminSceneVictoire`. Aucun décor déplacé.

## Vérification

- `godot --headless --build-solutions --quit` : **0 erreur, 0 avertissement CS**.
- Les quatre `.tres`/scènes chargent : Godot a **ré-sérialisé** les deux arbres de
  dialogue (uid ajouté, apostrophes échappées), ce qui prouve qu'ils parsent.
- **Deux fins jouées de bout en bout** en headless (harnais jetables, supprimés depuis :
  ils téléportaient le joueur, martelaient `action` et abattaient le boss à coups de
  `Degats.Infliger`) :

  | Étape | Fin normale | Fin cachée |
  |---|---|---|
  | Entrée | `pnj=ProloguePereNoel cage=aucun` | `pnj=PrologueLutinUsine cage=CagePereNoel` |
  | Après dialogue | `BossPereNoel 45/45` | `BossLutinMecha 40/40` |
  | Boss vaincu | pantalon lâché en (1450, 279) | rien lâché |
  | Délivrance | — | pantalon sorti de la cage en (1600, 192) |
  | Ramassage | `pantalon_obtenu` → fin | `pantalon_obtenu` → fin |

- **Cage verrouillée** : 20 appuis sur `action` devant la cage **avant** le combat →
  `pantalon=aucun`, `bossVaincu=False`. Elle ne s'ouvre qu'après la chute du Mecha.
- Traversées précédentes toujours valides : prologue rejoué une seule fois par partie
  (2ᵉ entrée → combat direct, aucun PNJ), sortie de zone en pleine conversation
  (`complet=false`) → aucun combat déclenché.
- Runs complets de 900 frames (donc bien après la bascule sur `ecran_fin`) et boots de
  600 frames de `monde1`, `monde2`, `BossEnd` : **aucune erreur nouvelle** (seuls restent
  les 12 « Not supported by this display server » et le « resources still in use at
  exit » pré-existants).

## 8. Correctif : le Père Noël du prologue apparaissait à moitié sous l'écran

**Symptôme** : l'interlocuteur amical s'affichait enfoncé, la moitié basse hors caméra.

**Cause** : un conflit de conventions d'ancrage. Les scènes de boss ont leur **origine
aux pieds** (`BossPereNoel.tscn` : sprite à y = −44), mais les scènes de `PnjAmical` ont
leur **origine au centre du corps** (sprite à y = 0). Le PNJ de prologue partage le
`Marker2D ApparitionBoss` du boss : posé là, un art de 96×96 débordait de 48 px sous
son point d'ancrage. Sol de l'arène à y = 279, borne basse de la caméra à 304 : les
pieds tombaient vers 312, **8 px sous le champ**.

**Correctif** : les deux scènes de prologue passent à la convention des boss — origine
aux pieds. Décalages calculés sur la **boîte opaque réelle** de chaque PNG (mesurée, pas
devinée : le Père Noël occupe les lignes 3→94 d'un cadre 96×96, le lutin 12→54 d'un
64×64) :

| | `AnimatedSprite2D` / `Apercu` | `CollisionShape2D` | zone de dialogue | `AncrageBulle` |
|---|---|---|---|---|
| Père Noël | (0, −46) | (0, −15) | (0, −45) | (0, −105) |
| Lutin usine | (0, −22) | (0, −11) | (0, −21) | (0, −56) |

Deux erreurs voisines corrigées au passage, trouvées par la même sonde :

- **`ProloguePereNoel.tscn` affichait l'`Apercu` du lutin.** J'avais relevé les deux uid
  de texture d'un `grep` multi-fichiers sans étiquette et je les avais **intervertis** :
  `uid://tyfrorwb8q85` est `lutin_usine/idle`, pas `pere_noel/idle`. Godot privilégiant
  l'uid sur le chemin, c'est bien le lutin qui s'affichait — seulement dans l'éditeur
  (`MasquerApercuEditeur` cache le nœud en jeu), mais trompeur à l'ouverture de la scène.
- **Le sol de l'arène est à y = 279, pas 248.** J'avais déduit 248 de `CLAUDE.md`
  (« surface at y = +8 » pour `sol/usine/`) au lieu de le mesurer. Conséquence : la cage
  flottait 31 px au-dessus du plancher (posée à y = 152, recalée à **188**), et le
  pantalon sortait au milieu des barreaux — `DecalageContenu` passe à (−50, 78) pour
  qu'il tombe **au pied de la cage, côté joueur**.

**Vérifié à la sonde** (jetable, supprimée) qui compare les pieds dessinés au sommet réel
du sol (raycast) et à la borne basse de la caméra :

```
[ProloguePereNoel]  tete=190 pieds=279  sol=279  ecart=0  sousCamera=False
[PrologueLutinUsine] tete=237 pieds=279  sol=279  ecart=0  sousCamera=False
[CagePereNoel]      tete=96  pieds=279  sol=279  ecart=0  sousCamera=False
```

Les deux fins rejouées entièrement après le recalage (les zones de dialogue ayant bougé) :
prologue → combat → butin/cage → ramassage, `PANTALON en (1450, 279)` côté fin normale et
`(1550, 266)` côté fin cachée.

## 9. Art final du pantalon (fin du placeholder)

`assets/items/pantalon_final_pickup.png` + `pantalon_final_aura.png` remplacent le
32×32 procédural, supprimé (`assets/props/pantalon.png`).

**Mise à l'échelle nécessaire** : les deux PNG font 160×160, le pantalon y occupe
54×107 px — soit **2,4× la hauteur du joueur** (44 px dessinés). Réglages posés dans
`PantalonPickup.tscn` : pantalon à `scale 0.3` (**16×32 px dessinés**), halo à
`scale 0.45` (**60 px de diamètre**), le halo en `z_index = -1` derrière l'objet.

**Ancrage** : sprites et collision décalés de (0, −24), donc **l'origine du ramassable
est son point de contact au sol** — même convention que les boss. `Boss.LacherButin` le
dépose à la position (aux pieds) du boss tombé, ça tombe donc juste sans calcul ; côté
cage, `DecalageContenu` passe de (−50, 78) à **(−50, 91)** pour viser le sol (279)
depuis l'origine de la cage (188).

**Deux primitives réutilisables ajoutées à `Effets`**, dans le style de
`Balancement`/`Flottaison` :

- `RotationContinue(cible, dureeTour)` — boucle sur un tour complet (≠ `Balancement`,
  qui fait un aller-retour) ;
- `Pulsation(cible, ampleur, duree)` — respiration d'échelle, `ampleur` exprimée en
  **fraction** de l'échelle courante, pour que l'effet soit identique quelle que soit la
  taille réglée dans la scène.

`PantalonPickup.PreparerVisuel` garde sa `Flottaison` et anime le halo s'il existe
(nœud `Aura` absent = objet simplement statique, pas une erreur) : le vêtement n'a
qu'une frame, c'est le halo qui attire l'œil au bout de l'arène.

**Vérifié à la sonde** (jetable, supprimée), fin cachée jouée jusqu'au ramassage :

```
[pantalon] origine y=278,1  dessine 16x32 px  tete=238  pieds=270  sol=279
[aura]     rotation=12,4deg  echelle=0,447  diametre=60 px
```

Les deux tweens tournent, l'objet flotte ~9 px au-dessus du sol (halo compris) et se
ramasse dans les deux fins. Aucune référence restante au placeholder ; boots de
`BossEnd` et `monde1` sans erreur nouvelle.

## 10. Épilogue : le boss tombé laisse la place à un PNJ

Symétrique du prologue, dans la même classe : après la chute du boss, **un second fondu
au noir** échange le vaincu contre un PNJ amical.

`scripts/Core/ZoneBoss.cs` :

| Export | Rôle |
|---|---|
| `ScenePnjEpilogue` / `ScenePnjEpilogueAlternatif` | le PNJ d'après-combat, par branche |
| `DelaiEpilogue` (2 s) | battement avant le fondu, le temps de voir le boss s'affaisser |
| `DureeFonduEchange` (0,5 s) | **renommé** depuis `DureeFonduPrologue` — c'est le même effet dans les deux sens (aucune scène ne le surchargeait) |

`SceneEpilogueChoisie` reprend exactement la forme de `ScenePrologueChoisie`. Laisser
`ScenePnjEpilogue` vide et ne renseigner que l'alternative donne **un épilogue à la seule
fin cachée** : la branche normale retombe sur un champ vide, donc sur aucun épilogue —
c'est ce qui est câblé, le Père Noël lâchant déjà son pantalon.

`ZoneBoss` s'abonne lui-même à `Boss.Vaincu` dans `LancerCombat` (`DeclencherEpilogue`),
en plus de l'abonnement de `ZoneBossPereNoel`. La bascule libère le boss, masque la barre
de vie et instancie le PNJ **à la position exacte du vaincu**. `DialogueModal` est tenu
pendant le noir, comme à l'aller. Ajout **direct** et non différé ici, contrairement au
prologue : on est appelé depuis un tween (étape de process), pas depuis `BodyEntered`.

**Le lutin d'épilogue** — `scenes/boss/EpilogueLutinNoel.tscn` (un `LutinNoel`, ancrage
aux pieds comme les prologues : sprites/collision à −28, art 64×64 au contenu 3→60) et
`assets/dialogues/bossend_epilogue_lutin_noel.tres`, un nœud sans choix :

> « Moi, je n'ai jamais suivi leur piquet ! Un lutin, ça termine sa tournée. »
> « Vite, allons libérer le père noel ! »

Deux détails imposés par le code existant :
- **`LutinNoel.Initialiser()` force `Aleatoire = true`** (une réplique au hasard). Un
  simple tableau `Lignes` n'aurait donc jamais délivré les deux répliques dans l'ordre :
  d'où l'arbre `.tres`, que le chemin `TalkativeAChoix` parcourt séquentiellement en
  ignorant `Aleatoire`. `Lignes` reste renseigné en repli.
- **`DistancePatrouille = 0`** sur l'instance, sinon il déambule en pleine scène de fin
  (`Initialiser` ne touche pas à ce champ, la valeur de scène survit).

**Vérifié à la sonde** (jetable, supprimée), fin cachée jouée en entier :

```
[f192] MECHA VAINCU
[f548] EPILOGUE : EpilogueLutinNoel en (1450, 279)  boss=libere  modal=False
[f631] PANTALON RAMASSÉ
```

L'arbre se charge bien (`repliques=2 choix=0`, `conversation=True`, `patrouille=0`) — un
`.tres` cassé serait passé inaperçu, le lutin retombant en silence sur ses `Lignes`.
Le rappel de touche s'affiche devant lui, puis la cage et le pantalon s'enchaînent.

### Incident : l'éditeur a écrasé le câblage

L'éditeur Godot, ouvert sur `BossEnd.tscn`, a ré-enregistré la scène par-dessus mon
édition et **supprimé la ligne `ScenePnjEpilogueAlternatif`** (piège connu). Remise
depuis. **Recharger la scène dans l'éditeur** avant d'y retoucher.

## 11. Arène recalée sur sa zone caméra, et fermée par des murs

Suite du point ci-dessous : l'arène a été **rétrécie pour tenir dans `ZoneBossFinale`**,
mesurée à **x ∈ [17, 782]** (765 px).

| Nœud | Avant | Après |
|---|---|---|
| `SolUsineBois` | x 0, `NombreSegments` 3 (1720 px) | x −116, **`NombreSegments = 1`** (1032 px) |
| `ApparitionBoss` | x 1450 | **x 520** |
| `CagePereNoel` | x 1600 | **x 700** (bords 620→780) |
| `Entree` / joueur | x 200 | inchangé |

**Le sol ne peut pas faire exactement 765 px** : il se construit par segments de 344
(`SegmentSolUsineBois.LargeurSegment`), donc les largeurs disponibles sont 688 ou 1032.
688 laisserait un trou de 77 px au bord droit de l'arène — un piège mortel pile là où la
caméra s'arrête. J'ai donc pris **1032, centré sur la zone** (−116 → 916) : les deux
extrémités du plancher, embouts compris, tombent hors caméra, donc on ne voit jamais le
bord du sol depuis l'arène.

### Murs d'arène — `scenes/mur/MurArene.tscn`

Les limites de caméra **ne bloquent personne** : elles cadrent l'image, mais joueur, PNJ
et boss continuent de marcher au-delà, hors champ (c'est ce qui rendait le débordement
possible). D'où un `MurArene` (`StaticBody2D`, couche 1 comme le sol), posé sous
`Arene/Sol` en `MurGauche` (x 1) et `MurDroit` (x 798) : rectangle 32×640, **face
intérieure pile sur la borne**, hauteur égale à celle de la zone.

Deux choix à signaler :
- **`MurNonAgrippable`**, et ce n'est pas un détail : le jeu a le wall jump, et une paroi
  pleine sur toute la hauteur de la salle se remonterait jusqu'à sortir par le haut —
  exactement ce qu'on cherche à empêcher.
- **Pas de sprite**, contrairement à `MurGrotte` : il n'existe aucun art de mur d'usine
  (`assets/decors/usine/usine_mur.png` est un fond 360×180, qu'il faudrait déformer), et
  ces parois se posent au ras du bord de l'écran. C'est le rectangle de collision qui les
  rend visibles et déplaçables dans l'éditeur ; on les étire par le `scale` de l'instance,
  comme une `CameraZone`.

### Vérification

Sonde jetable (supprimée), joueur collé à chaque paroi et poussé dedans **en sautant** :

```
[arene]  x de 17 a 782, haut=-336
[droite] x max atteint=771 (borne 782) -> BLOQUE | y min=251 -> pas d'escalade
[gauche] x min atteint=29  (borne 17)  -> BLOQUE
[pnj]    PrologueLutinUsine lance vers le mur droit -> x=776 -> BLOQUE
```

Les 11 px de marge correspondent au rayon de la capsule du joueur. Le `y min` prouve que
le wall jump ne mord pas sur la paroi.

Puis les **deux fins rejouées en entier**, murs et nouvelles positions en place :

```
FIN NORMALE  prologue ProloguePereNoel x=520 -> BossPereNoel VAINCU x=520 -> PANTALON RAMASSÉ
FIN CACHÉE   prologue PrologueLutinUsine x=520 -> BossLutinMecha VAINCU x=520
             -> EPILOGUE EpilogueLutinNoel x=520 -> cage -> PANTALON RAMASSÉ
```

Sol présent sous les deux bornes de l'arène (`279,1` des deux côtés), marqueur, entrée et
cage tous mesurés **dans** les bornes. Boots de `BossEnd` et `monde1` au niveau de bruit
pré-existant, à l'identique.

### Point de séquence à surveiller

La cage se déverrouille dès la mort du boss, alors que le lutin d'épilogue n'apparaît que
`DelaiEpilogue + DureeFonduEchange` plus tard (2,5 s). Un joueur très rapide pourrait donc
délivrer le Père Noël **avant** d'entendre « vite, allons libérer le père noel ! ». Traverser
l'arène demande plus que ça, et le joueur est figé pendant le fondu — mais si le beat doit
être garanti, il faudrait conditionner la cage à l'épilogue plutôt qu'à la seule victoire.

### ⚠️ Géométrie de l'arène (résolu par le point 11)

La même sauvegarde a **réduit `ZoneBossFinale`** (position x 860 → 416,9 ; `scale.x`
6,589 → 3,149). Mesuré : l'arène couvre maintenant **x ∈ [17, 782]**, alors que

- `ApparitionBoss` (donc le prologue, le boss et l'épilogue) est à **x = 1450** ;
- `CagePereNoel` est à **x = 1600** ;
- le sol `SolUsineBois` va toujours de 0 à 1720.

Le rectangle de la zone sert à la fois de **limites caméra** et de **bornes de
déplacement du boss** : tout ce qui est au-delà de 782 est hors champ et hors bornes.
Non corrigé ici — c'est un choix d'auteur en cours, pas un bug de code. À trancher : soit
ré-agrandir la zone, soit ramener marqueur et cage dans les nouvelles bornes.

## 12. L'éclat de glace du Père Noël vise le joueur

**Symptôme** : le pic de glace partait à l'horizontale et traversait toute la salle sans
jamais menacer un joueur qui n'était pas exactement à sa hauteur.

**Cause** : `TirerEclat` construisait sa vélocité à plat —
`new Vector2(_direction * VitesseEclat, 0f)` — et `EclatGlace.tscn` règle **`Gravite = 0`**,
donc rien ne redressait la trajectoire en vol : d'où le « horizontal à l'infini ».

**Correctif** (`scripts/Entities/Pnj/BossPereNoel.cs`) : nouvelle méthode `ViserJoueur`
qui renvoie la ligne unitaire **bouche du canon → joueur**, `TirerEclat` la multipliant par
`VitesseEclat`. Replis sur l'horizontale dans le sens du regard si aucun joueur n'est en
scène, ou s'il est pile sur la bouche — un vecteur nul normalisé enverrait l'éclat
n'importe où.

L'angle d'éventail de phase 2 **n'est plus multiplié par `_direction`** : la visée porte
désormais le sens du tir, une rotation symétrique de ±`AngleEventail` autour d'elle suffit.

Aucun changement dans `Projectile` : sa surcharge vectorielle `Initialiser` gérait déjà un
tir hors horizontale, et son `Rotation = VelociteCourante.Angle()` oriente le sprite dans
l'axe du vol. Avec `MasqueProjectile` (couche 1 comprise), l'éclat visé finit maintenant sa
course dans le sol ou dans un mur d'arène au lieu de filer jusqu'à expiration.

### Vérification

Sonde jetable (supprimée) mesurant la **trajectoire réelle** de chaque éclat (déplacement
entre deux relevés) contre la ligne bouche→joueur au moment du tir :

```
phase 1   poste(120,-110)  cap=-24,0   vise=-24,0    ecart=0,0
          poste(120, 60)   cap= 55,3   vise= 55,3    ecart=0,0
          poste(-120,-90)  cap=-167,5  vise=-167,5   ecart=0,0   (joueur derriere et au-dessus)
phase 2   poste(120,-110)  cap=-6,0 / -24,0 / -42,0  ecart=18 / 0 / 18
          poste(120, 60)   cap= 73,3 / 55,3 / 37,3   ecart=18 / 0 / 18
```

19 éclats mesurés : **écart 0° en phase 1**, et un éventail **symétrique** à ±18° autour
de la visée en phase 2, quel que soit le côté où se trouve le joueur.

Deux pièges de mesure traversés, qui valent d'être notés : lire `eclat.Rotation` au spawn
ne vaut rien (elle n'est posée qu'à la 1re frame **physique**, la sonde tournant en
`_Process`) — d'où la mesure par déplacement ; et le boss ne tire pas si le joueur est
au-delà de `DistanceConfort = 200`, il s'approche indéfiniment, ce qui rendait les
premières sondes muettes.

### Non traité : le Mecha

`BossLutinMecha` tire le même `EclatGlace` par la surcharge horizontale
(`eclat.Initialiser(this, _direction)`), donc son tir reste plat. Hors de la demande, à
dire si tu veux le même traitement.

### Reste à faire : un vrai F5

Le headless ne juge ni la lisibilité de la liste de réponses, ni le rythme du fondu, ni
le fait que l'échange PNJ→boss soit invisible, ni si la cage est bien lisible en bout
d'arène (elle fait 160×192 et le boss se déplace jusque-là).

## Incident : sauvegarde du joueur écrasée

Les premiers runs de vérification ont écrit dans la **vraie sauvegarde**
(`user://pantalon.json`, via `GameState.Sauvegarder()`). J'avais copié le mauvais fichier
(`sauvegarde.json`, vestige du 13 juillet) : la sauvegarde du 27/07 14:57 **est perdue**.

Le fichier a été remis dans un état cohérent de début de partie (`village_depart`,
`monde1.tscn`, 50 poissons, 5 PV) — il pointait sinon sur une scène de harnais supprimée,
ce qui aurait fait planter « Continuer ». Le supprimer donne un départ propre. Les runs
suivants ont été encadrés d'une copie/restauration.

## Limites connues

- **Abandonner le prologue le consomme quand même** : `PnjAmical.SurFinDialogue()` marque
  `IdDialogue` consommé sur **tous** les chemins de sortie. En jeu ce n'est pas
  atteignable (une conversation à arbre est modale) — il a fallu une téléportation
  scriptée pour le provoquer. Comportement pré-existant de `UneSeuleFois`.
- **Musique** : `NomAmbiance = "boss_cerf"` s'applique dès l'entrée dans l'arène, donc le
  thème de combat tourne pendant la conversation. Il faudrait un `NomAmbiancePrologue`.
- **Halo tournant** : `RotationContinue` boucle sur un tour complet, donc la reprise
  n'est invisible que parce que le halo est à peu près symétrique. À garder en tête
  avant de l'appliquer à un art qui ne l'est pas.
- `scenes/ui/ecran_fin.tscn` parle encore de Rodolphe alors que les deux fins de
  `BossEnd` y mènent. Pré-existant, non traité.
