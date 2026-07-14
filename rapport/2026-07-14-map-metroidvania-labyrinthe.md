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
