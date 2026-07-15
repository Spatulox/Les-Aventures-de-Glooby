# Refonte de la map en metroidvania labyrinthique

Parcours gaté de bout en bout : **Village → Banquise (avec trou) → Labyrinthe (pouvoir de feu) → mur de glace fondu → Grotte (Boss Cerf) → écran de fin**. Un seul fichier modifié : `scenes/niveaux/monde.tscn`. **Zéro génération d'asset** (tout réutilise l'existant).

## Changements

**Trou dans la banquise** — Retrait de `Banquise/Sol/Sol4` (@x2224) → brèche ~x2080–2358 dans laquelle le joueur tombe. `CristalPetit` déplacé de 2080→1750 (dégageait l'entrée).

**Nouvelle place `Labyrinthe`** (racine, sœur de Banquise/Grotte, patron Sol/Interactifs/Camera) :
- Puits vertical x1950–2470, y~355→820, borné par 2 parois solides inline (`StaticBody2D`+`RectMurLab` 32×510) + un plancher (`RectPlancherLab` 556×40) + 2 cloisons-impasses (`RectCloisonLab` 140×24) jutant des parois.
- 9 paliers `PlateformeUnidirectionnelle` en quinconce, **espacés de 50px vertical** (sous la hauteur de saut réelle ≈73px → remontée fiable en traversant les one-way par le dessous).
- `pouvoir_chaleur_pickup` au fond (2210, 778) — récupère le **pouvoir de feu** (`GameState.PouvoirChaleurActif`).
- `CameraZone` dédiée : limites gauche 1880 / droite 2520 / **haut 317** / bas 950, `NomRegion="banquise"`. Le haut (317) = le bas de la zone banquise → handoff par hystérésis sans trou vertical ; le bas profond met le **filet anti-chute à 1250**, très sous le plancher (800) → tomber dans le trou ne renvoie plus au checkpoint.

**Mur de glace (gate)** — `mur_fondable` à x2700 (couture banquise/grotte), `IdMur="mur_glace_grotte"`. Infranchissable sans pouvoir ; touche A (`Player.UtiliserPouvoirChaleur`) → fond → accès grotte.

**Combat Boss Cerf réinstallé** (il n'existait plus dans la scène — `Grotte/Interactifs` était vide) sous `Grotte/Interactifs` :
- `checkpoint_peche` à l'entrée (2860, 296), `IdCheckpoint="grotte_entree"` → morts contre le boss respawnent ici, pas au village.
- 6× `stalactite_piege` au plafond (y75, x 3000→5250), groupe `stalactites_boss` (piétinement du boss).
- `BossHudBarre` + `ZoneBossCerf` (Area2D+script, trigger `RectZoneBoss` 2500×400) : `SceneBoss=boss_cerf`, `NomBoss="Boss Cerf (Rodolphe)"`, `PositionApparition=(5100,280)`, `CheminBarre=../BossHudBarre`, `PvBoss=40`, `UneSeuleFois=true`, **`LimiteGauche=2850`/`LimiteDroite=5350`** (override obligatoire — les défauts 5984/8480 venaient de l'ancien monde et auraient fait charger le boss hors grotte). Victoire → `ecran_fin.tscn`.

## Vérification

- `godot --headless --build-solutions --quit` → **0 erreur**.
- `godot --headless scenes/niveaux/monde.tscn --quit-after 200` → **aucune erreur** liée aux nouveaux nœuds (mur, zone boss, stalactites, zone caméra, paliers, pickup). Seules erreurs : `LutinNoel` cherchant `assets/pnj/lutin_noel/idle|marche` (dossiers d'anims absents) — **préexistant**, décor non modifié par cette tâche.

## À valider en playtest manuel (F5) — non couvrable en headless

- Ressenti des sauts du labyrinthe (descente/remontée des 9 paliers) et lisibilité du trou.
- Position fine du mur de glace et déclenchement de l'arène boss à l'entrée.
- Complexité « labyrinthique » : v1 = puits vertical en quinconce + 2 cloisons. Densifiable si trop simple.

---

# Révision 2 — grotte moche + trou infranchissable

Retours user : (1) le labyrinthe affichait le fond **banquise (ciel) sous terre**, (2) c'était « juste des escaliers », (3) la grotte du boss était vide/plate, (4) le trou (tuile entière retirée = 278px) était **infranchissable au saut**.

**20 décors de grotte inutilisés valorisés** (cf. TODO) : création de 13 props réutilisables dans `scenes/decors/props/grotte/` (ColonneGlace, ColonneBrisee, GrappeCristaux, Geode, ChampignonLumineux/Geant, GlaceEmpilee, MiniLacGele, Congere, TasPierres, VeineCristalMur, FissureLumineuse, FlaqueGelee) — pattern `Sprite2D` z_index=-1.

**Trou franchissable** — La tuile retirée est remplacée par `SolBordTrou` (one-way @x2340) décalée → **trou ramené à ~116px**, sautable (portée horizontale ≈150px) tout en restant « tombable ».

**Labyrinthe entièrement redessiné** (plus « des escaliers ») :
- Fond passé de `banquise` → **`grotte`** (cave sous terre, cross-fade en tombant).
- Vrai maze en **chicane** : 3 corridors **solides décalés** (L1 droite 1760–2636, L2 gauche 1465–2341, L3 droite, plancher bas) — la descente se fait en marchant au bord (le corridor du dessous dépasse sous le bord : gauche→droite→gauche), pas en tombant tout droit.
- Remontée par 4 plateformes one-way intermédiaires (`LedgeEntree/A/B/C`), **~65px par palier** (sous la hauteur de saut réelle 73px).
- Élargi (x1450–2650, y jusqu'à 845), parois solides, **~24 décors** de grotte répartis (colonnes, champignons lumineux, géodes, lacs gelés, veines murales) + recoins/impasses. Pickup au fond.
- `CameraZone` élargie (haut 317 = handoff banquise, bas 920 → filet 1220), `NomRegion="grotte"`.

**Grotte du boss agrandie + décorée** :
- Sol étendu à l'est (Sol11–13 → ~x6255), **~33 décors** de grotte ajoutés, 3 ledges de relief (dodge), 2 stalactites-pièges de plus (8 au total).
- Boss réajusté : `PositionApparition (5900,280)`, `LimiteDroite 6050`, trigger `RectZoneBoss` élargi (3300×400), `ZoneGrotte` élargie (couvre ~2650–6255).

## Vérification (v2)
- Build C# → **0 erreur**. Boot `monde.tscn` headless → **aucune erreur** sur les nouveaux props/corridors/zones/boss. Seules erreurs : `pingouin`/`lutin_noel` (dossiers d'anims PNJ absents) — **préexistant**.
- Toujours **F5 recommandé** pour caler le ressenti des sauts de la chicane (espacements posés à ~65px, sous le max, mais le feel se valide manette en main).

---

# Révision 3 — sols du labyrinthe invisibles → GameObject réutilisable

Retour user : les corridors/parois du labyrinthe étaient des `StaticBody2D`+`CollisionShape2D` **inline sans sprite** (sols invisibles), ce qui viole aussi la règle « tout élément de niveau doit être un `.tscn` réutilisable ».

**Nouveau GameObject réutilisable `scenes/plateformes/BlocGrotte.tscn`** (+ `scripts/Plateformes/BlocGrotte.cs`) : bloc de roche **solide (4 côtés) et VISIBLE**, redimensionnable via l'export `Taille` — la texture `assets/tiles/grotte_base.png` (128×128) est **tuilée** (region_rect + texture_repeat) pour couvrir n'importe quelle taille ; collision pleine de mêmes dimensions ; `z_index=-2` (devant les couches parallax, derrière les props et le joueur).

**Labyrinthe reconstruit sans aucune collision inline** : les 2 parois + 3 corridors + plancher sont désormais **6 instances de `BlocGrotte`** (parois 48×540, corridors 876×40, plancher 1170×40). Ledges de remontée repositionnés dans les gouffres (climb diagonal ~40px latéral + ~65px vertical sur corridors solides). Sub_resources inline (`RectMurLabV/RectCorridor/RectPlancherBas`) supprimées.

Reste une seule `CollisionShape2D` inline : le **trigger `ZoneBossCerf`** (Area2D) — zone de détection invisible par nature (pas un sol), pattern documenté dans l'architecture ; laissée telle quelle.

## Vérification (v3)
- Build C# → **0 erreur** (nouveau `BlocGrotte.cs` compile). Boot `monde.tscn` headless → **aucune erreur** sur `BlocGrotte`/labyrinthe ; aucune référence pendante aux sub_resources supprimées.

---

# Révision 4 — fond de l'arène du Boss Cerf + nettoyage du menu

**Fond de l'arène** (via le système de région caméra existant, pas de bricolage) :
- `scenes/boss/FondBossCerf.tscn` restructuré en **`Parallax2D` épinglé à la caméra** (`scroll_scale (0,0)`, `z=-100`, Sprite2D 320,180 ×2) — même pattern que `FondBanquise`/`FondGrotte` (il était un `Sprite2D` fixe qui n'aurait pas suivi la caméra).
- Nouveau conteneur de région **`Fonds/boss_cerf`** (Node2D, `modulate:a=0`) contenant `FondBossCerf` — enregistré automatiquement par `BackgroundManager`.
- `ZoneGrotte.NomRegion` passé de `grotte` → **`boss_cerf`** : en entrant dans la grotte (arène), la caméra déclenche `AfficherRegion("boss_cerf")` → cross-fade vers le fond du boss. Le labyrinthe garde la région `grotte` (cave).

**Menu principal** : `MenuPrincipal.FondAleatoire` exclut désormais tout fond contenant « boss » du tirage aléatoire de l'écran-titre → `fond_boss_cerf.png` réservé au combat, plus défloré au menu.

## Vérification (v4)
- Build C# → **0 erreur**. Boot `monde.tscn` headless → **aucune erreur** (région `boss_cerf` chargée). Le cross-fade en entrant dans l'arène est à confirmer en **F5**.

---

# Révision 5 — arène boss trop grande (fond activé trop tôt)

Retour user : le fond `boss_cerf` s'activait sur **toute la grotte** (dès l'entrée à x2650) alors que le boss n'apparaît qu'à x5900 → énorme vide entre le changement de fond et le boss.

**Scission de la caméra de grotte en deux `CameraZone`** (raccordées à x5560, même y72-328, tuilage propre pour l'hystérésis) :
- **`ZoneGrotte`** (approche, x2650→5560) : `NomRegion="banquise"` — fond banquise avant l'arène.
- **`ZoneArenaBoss`** (nouvelle, x5560→6300) : `NomRegion="boss_cerf"` — le fond du boss ne s'active qu'au niveau du boss (trigger x5580, spawn x5900). La région `grotte` (cave) reste sur le labyrinthe.

## Vérification (v5)
- Build C# → **0 erreur**. Boot headless → **aucune erreur sur les zones/fonds**.
- ⚠️ Préexistant (hors sujet, non introduit ici) : le PNJ **pingouin** spamme une `NullReferenceException` (`PnjAmical.cs:57`, `Sprite.Play("idle")`) car `assets/pnj/pingouin/idle` est vide/absent — même trou d'asset placeholder que `lutin_noel`. Un garde-fou (`if (Sprite.SpriteFrames.HasAnimation("idle"))`) ou les frames manquantes le corrigerait.
