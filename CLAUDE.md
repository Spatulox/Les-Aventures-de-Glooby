# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Les Aventures de Glooby** — a 2D pixel-art platformer built in **Godot 4.6.3 (.NET/C# / Mono edition, net8.0)**. A penguin-ish hero crosses ice fields and a cave to defeat the Boss Cerf (Rodolphe). Renderer is Forward Plus, physics is Jolt. All code, comments, node names, and identifiers are in **French** — match that convention when adding code.

Pixel-art assets are generated with **PixelLab** under a per-mission generation budget (see `BUDGET.md`); reuse existing assets/animations rather than generating new ones. Project narrative and design rationale live in `RAPPORT.md`, `DECISIONS.md`, and `TODO.md` (read these before changing gameplay — many "odd" choices are deliberate and documented).

## Coding conventions (required)

- Code must be **as reusable as possible** — favor shared helpers over duplication: `Constantes` (`TailleTuile`), `Effets` (`Disparaitre`, `FlashCouleur`, `Flottaison`), and the `ElementRamassable` base for contact pickups. `GameState.EstConsomme/MarquerConsomme` is the generic persistent-element store.
- Code must be **visually readable by a human** — clear French naming, sensible structure.
- **Every class must have a class-level comment** (describing its purpose), as is already the case throughout `scripts/`.

## Reports

- When the user asks for a report, write it as a file in the project's **`rapport/`** folder (create the folder if it doesn't exist). Do not put requested reports elsewhere or only in chat.
- A report **concisely summarizes the changes actually made** in the conversation — a quick, scannable recap, not a verbose narrative.
- **One report per conversation**: reuse/update the same report file across the conversation instead of creating a new one each time.

## Git

- **Never commit during a task.** Only `git commit` when the user explicitly asks for it, each time. Finishing a change, verifying a build, or completing a plan step is **not** a signal to commit — leave the work uncommitted and let the user request it.

## Commands

Godot binary is at `/usr/local/bin/godot` (Godot 4.6.3 mono).

```bash
# Build the C# solution (compile check — do this after every code change)
godot --headless --build-solutions --quit

# Run the game headless (boots monde.tscn, catches runtime/scene errors)
godot --headless --quit-after 200      # runs ~200 frames then exits

# Run the game normally (for a human to actually play-test)
godot
```

There is **no test framework and no lint step**. Verification is done by compiling clean + running headless and watching for errors. The historical workflow (per `RAPPORT.md`) is to catch design bugs by *simulating traversal / scripted inputs headless*, not by re-reading code — a real manual F5 play-test is still recommended for game feel (falls, telegraph readability), which the headless environment cannot provide.

## Architecture

### Single continuous world (Hollow Knight style)

There is **one gameplay scene: `scenes/niveaux/monde.tscn`** (the game boots to `scenes/ui/menu_principal.tscn`, which is `run/main_scene`). The entire level lives in it — there is no scene reload during play (only the final victory → `scenes/ui/ecran_fin.tscn`). This was a deliberate refactor away from 7 separate reloaded scenes.

**The whole map is authored directly in `scenes/niveaux/monde.tscn`** (edited by hand in the Godot editor): all props/entities, the `CameraZone` regions, checkpoints, backgrounds. There is **no runtime world-assembly script** — the scene root has no build script; to add/move content, edit `monde.tscn` in the editor.

The scene is organized **one node per place** (`Village`, `Grotte`, and future boss arenas) so it stays hand-editable. **Each place node contains the same sub-groups**: `Sol` (the walkable ground), `Decor` (igloos + `decors/props` — **non-interactive scenery only**), `Pnj` (every living actor of the place: friendly PNJ *and* enemies — pingouins, lutins, bonhomme de neige…), `Interactifs` (checkpoints, hazards, pickups), `Camera` (the place's `CameraZone`, which now also carries the background region via its `NomRegion`), `Frontiere` (legacy empty group — the old `RegionTrigger` background gates have been removed; the region is driven by `CameraZone.NomRegion`). Add content by instancing the matching reusable `.tscn` under the right sub-group; add a place by duplicating the pattern. Cross-cutting/global nodes stay at the root: `Fonds` (per-region backgrounds + `BackgroundManager`), `Joueur` (the player instance), `MenuPause`.

**Layered backgrounds (`Fonds`).** `BackgroundManager` holds **one Node2D container per region** (e.g. `village`), and `AfficherRegion(nom)` cross-fades between them (`modulate:a`, which propagates to each container's children). Each region container stacks two reusable layers, back to front:
- a **fixed far background** — `scenes/decors/FondBanquise.tscn` / `FondGrotte.tscn`: a `Parallax2D` with `scroll_scale = (0,0)` (pinned to the camera) and no repeat, at `z_index = -100` (a single non-tiling skybox image sized for the 640×360 viewport).
- a **mid parallax décor** — `scenes/decors/DecorBanquise.tscn` / `DecorGrotte.tscn`: several `Parallax2D` layers (z −12…−3) that scroll at increasing speeds between the far background and the foreground.

Adding a region later = drop a new container (e.g. `grotte` = `FondGrotte` + `DecorGrotte`) under `Fonds` and set the matching place's `CameraZone.NomRegion` to that container name — the zone calls `AfficherRegion` itself when the player is inside it.

The current level runs west→east as three places: a **penguin village** (igloos, props, a fishing-hole checkpoint), then the open **banquise** ice field, then a **cave (grotte)**; the rest is built out from there. Village and banquise share the `banquise` background region (same biome); the cave uses the `grotte` region. Region backgrounds swap from `CameraZone.NomRegion` (no more `RegionTrigger` gates), and each place has its own non-overlapping `CameraZone` (village & banquise 1280 wide, grotte 2560).

Key pieces of the scene:
- **`Sol` — the ground is built from reusable `PlateformeUnidirectionnelle` (one-way) platform instances**, tiled side by side (collision width 278 → step 278 for a seamless floor), *not* a `TileMapLayer`. The player can drop through with down+jump. (An earlier version used a baked `Terrain` `TileMapLayer` painted from `TileSetFabrique` Wang tiles — see the historical notes below; the `is_ice`/`is_fragile` tile mechanics in `Player.cs` only apply when such a tilemap in the `sol` group is present, which the village does not have.)
- **`CameraZone` regions** adjust the player `Camera2D` limits for the room the player is in (the `Camera2D` is a child of the player, auto-following). Each zone is a **reusable GameObject** — an instance of `scenes/core/camera_zone.tscn` (drag it in, resize its rectangle). Its camera limits are **derived from its `CollisionShape2D` rectangle's world AABB** (`CameraZone.CalculerLimitesDepuisForme`), not hand-entered ints — the drawn rectangle *is* the room bounds. **Detection is continuous, not edge-triggered**: the zones register in the `CameraZone.Groupe` group and `Player.MettreAJourZoneCamera()` polls every physics frame for the zone whose rectangle `Contient(GlobalPosition)`, with **hysteresis** (keeps the current zone until another one contains the player — so gaps between zones, high jumps, and teleports/respawn are handled without a `BodyEntered` edge). The matched zone calls `Player.DefinirZoneCamera(...)` (sets the four limits + the fall-death threshold `SeuilChuteVide = bas + MargeChuteVide`, `[Export]`, default 300) and, if its `NomRegion` is set, `BackgroundManager.AfficherRegion(NomRegion)` to cross-fade the background. `SeuilChuteVide` is **relative to the active zone**, not an absolute Y — a single global threshold would misfire in deep rooms.
- **`ZoneBoss` (Area2D)** covers a boss arena and reveals the boss HP bar / arms the boss on player entry (see Player & Boss).

> Historical note: the map used to be generated procedurally by `scripts/Core/Monde.cs` (`_Ready`) painting one static `SalleXxx.Construire(...)` builder per room at a tile `Decalage`. That generator was run once, its result **baked into `monde.tscn`, then unplugged and removed** (`Monde.cs` + `scripts/Rooms/` no longer exist). If you see references to `SalleXxx`/`Decalage` in older reports, that's why.

### TileSets built in code

`scripts/Terrain/TileSetFabrique.cs` once built the `Terrain` `TileSet` programmatically from PixelLab 4×4 Wang tile sheets (32×32 tiles) instead of hand-authored `.tres` files; that `TileSet` is now baked into `monde.tscn` and the builder was unplugged, so the file is reduced to the two custom-data-layer key names (`is_ice`, `is_fragile`) still read by `Player.cs` to drive gameplay (sliding, breakable ice).

### Global state & autoloads

Two autoloads (`project.godot [autoload]`):
- **`GameState`** (`scripts/Core/GameState.cs`) — singleton via `GameState.Instance`. Holds PV, poissons (a **fixed start reserve of `PoissonsDepart = 50`, consumed only** to heal — fish are not picked up in the world), progression flags (`PouvoirChaleurActif`), melted-wall id set, and checkpoint position. Communicates outward through `[Signal]` events (`PvChanges`, `JoueurMort`, `CheckpointActif`, …). Since the world never reloads, respawn = teleport the player to `CheckpointPosition`, not a scene change.
- **`Hud`** (`scenes/ui/hud.tscn`) — persistent HUD (hearts, fish counter), subscribes to `GameState` signals.

**Input actions are registered in code** — *not* in `project.godot`. The single source of truth is `scripts/Core/CatalogueActions.cs` (`CatalogueActions.Toutes`: label + keyboard + gamepad defaults per action), posted into the `InputMap` by the `Parametres` autoload (`AppliquerDefautsAuMap()`, plus the player's persisted remapping). Adding/removing an action = editing that one list; the Paramètres screen iterates it automatically.

Note on Godot C# signals: a `[Signal] delegate FooEventHandler` generates a member named `Foo`, which **collides with a property named `Foo`** — a bug hit before (see `RAPPORT.md` Jalon B). Name properties and signals distinctly.

### Audio — musique & ambiance par zone

Le son est piloté par l'autoload **`GestionnaireAudio`** (`scripts/Core/GestionnaireAudio.cs`, `GestionnaireAudio.Instance`), qui **survit aux changements de scène** (menu → monde → écran de fin) et joue sur deux bus (`Musique`, `Ambiance`, définis dans `default_bus_layout.tres`). Comme `BackgroundManager`/`GestionnaireMeteo`, **il ne décide rien** : les zones lui demandent une ambiance par son nom, la météo lui annonce l'état courant.

**Point crucial : les musiques ne sont PAS dans les nœuds de la scène.** Un nœud `CameraZone` de `monde1.tscn` ne porte qu'une **clé texte** (`NomAmbiance`, ex. `"village"`) ; les pistes elles-mêmes vivent dans des **ressources `.tres`** de `assets/audio/ambiances/` (une par lieu). Pour changer ce qui joue, on édite le `.tres` (dock FileSystem), pas la scène.

Chaîne de résolution :
- **`AmbianceSonore`** (`.tres`, `[GlobalClass]`) = le son d'un lieu : un `Nom` (la clé, ex. `"village"`) + une liste de `Variantes` par état météo. `GestionnaireAudio` **découvre tous les `.tres` du dossier au boot** et les indexe par `Nom` — déposer un nouveau `.tres` suffit à enregistrer un lieu (aucun code à toucher).
- **`VarianteAmbiance`** (sous-ressource) = le son d'un lieu **dans un état** (`Etat` = `"normal"` ou `"blizzard"`, repli sur `"normal"`). Deux canaux : `Musiques` (playlist musicale) et `Ambiances` (lit de fond bouclé, vent/gouttes) + leurs volumes.
- **`PisteMusicale`** (sous-ressource) = **une musique + sa `Probabilite`** (slider 0-100). C'est l'unité pondérée de `Musiques`. Dans l'inspecteur : `Variantes → [i] → Musiques → [j] → Probabilite`.

Sélection & enchaînement : une piste est tirée **au sort pondéré** (normalisé sur la somme réelle des probas ; `PushWarning` si ≠ 100), en évitant la répétition immédiate ; les morceaux **s'enchaînent en fondu** (`DureeFondu`, 1.5s) et la piste suivante démarre en fin de morceau — d'où l'**import des musiques en `loop = false`** (un flux bouclé n'émettrait jamais `Finished` et resterait bloqué).

Câblage des zones : `DeclencheurZone.AppliquerCommeSalle` (appelé par `CameraZone.Appliquer`) fait `GestionnaireAudio.JouerAmbiance(NomAmbiance ?? NomRegion)`. **`NomAmbiance` découple la musique du décor** : il n'est à renseigner que quand la musique diffère du fond visuel — c'est le cas du **village** (`NomRegion = "banquise"` pour partager le décor, mais `NomAmbiance = "village"` pour sa propre musique) ; les zones où musique = région le laissent vide (repli sur `NomRegion`).

Blizzard : `GestionnaireMeteo` appelle `GestionnaireAudio.DefinirEtat("blizzard" | "normal")`. Si la variante blizzard a **ses propres `Musiques`**, la musique normale est **mise en pause** (position conservée, `_musiqueSuspendue`) le temps du blizzard puis **reprend là où elle en était** ; si la variante blizzard ne renseigne **que** `Ambiances`, la musique normale continue et seul le lit de fond change.

Musique de boss : soit l'export `ZoneBoss.Musique` (un `AudioStream` unique, via `JouerMusiquePonctuelle`), soit — pour un tirage pondéré de plusieurs thèmes — une ambiance dédiée (`boss_cerf.tres`) pointée par `NomAmbiance` sur la zone d'arène.

### LivingEntity, Player & Boss

**`LivingEntity.cs`** (`scripts/Entities/`, abstract `CharacterBody2D`, implements `Damageable`) — the shared base for everything that "lives": both `Player` and `Boss` (and any future PNJ) extend it. It owns `PvMax`/`Pv`/`EstVaincu`, the `PvChanges`/`Vaincu` signals, `DefinirPvMax`, the single damage entry `TakeDamage(DamageSource)` (with `AjusterDegats`/`ApresDegats`/`Mourir` virtual hooks), and reusable movement helpers `AppliquerGravite`/`AppliquerFriction`/`Sauter` (tunables `Gravity`/`MaxFallSpeed`/`Friction`/`JumpVelocity`). The generic `Mourir()` marks the entity beaten, zeroes velocity and emits `Vaincu`. It also carries two reusable authoring conventions:
  - **Editor idle preview (`Apercu`).** Because entity sprites are `AnimatedSprite2D`s loaded *at runtime* (empty in the editor), every entity `.tscn` should carry a `Sprite2D` child **named `Apercu`** whose `texture` is the entity's first idle frame (`res://assets/pnj/<nom>/idle/00.png`), purely so the entity is visible/positionnable in the Godot editor. `LivingEntity.MasquerApercuEditeur()` (called from each entity's `_Ready`) hides it at runtime; a scene without an `Apercu` node is simply ignored. Add this node when creating any new entity scene.
  - **Per-instance detection reach (`ZoneDetection`).** An enemy's "reach" can be driven by an **optional `Area2D` child named `ZoneDetection`** (with a `CollisionShape2D`, `collision_layer = 0` / `collision_mask = 2` so it only detects the player) instead of a hard-coded distance. `LivingEntity.CablerZoneDetection()` (called in `_Ready`) wires `BodyEntered`/`BodyExited`, and `JoueurAPortee(out distance)` returns the tracked player with `distance = 0` when inside the zone (else `float.MaxValue`) — so AIs keep their `joueur == null || distance > Portee` test unchanged. When **no** `ZoneDetection` node exists, `JoueurAPortee` falls back to `JoueurLePlusProche` + the code distance (`Portee`/`PorteeDetection`). Drop a `ZoneDetection` into an enemy scene and resize its shape **per instance in `monde.tscn`** to tune each enemy's reach independently. Used by `BonhommeDeNeige` and `OursDeNeige`.

- **`Player.cs`** (`scripts/Entities/Player/`, `: LivingEntity`) — timer-based controller: coyote time + jump buffer, accelerated slide (faster on `is_ice`), snowball throw, short post-hit invincibility, fragile-tile break delay, relative fall-death safety net. Uses the base movement helpers. Its HP lives in **`GameState`, not the base `Pv`** (persistent, HUD-bound, respawn), so it overrides `TakeDamage(DamageSource)` to route damage there and `IsInvincibleToDamage` to its invincibility timer; `Blesser(int direction, DamageSource source)` is the knockback-carrying entry point called by the boss/hazards (and `TakeDamage` just delegates to it with a neutral direction). Tunables are `[Export]` fields at the top.
**Boss OO hierarchy.** Bosses split generic scaffolding from per-boss content, and each boss has its own arena zone:
- **`Boss.cs`** (`scripts/Entities/Pnj/`, abstract `: LivingEntity`) — the reusable *animated* base: loads its animations by folder (`AjouterAnimation(...)`), and overrides `Mourir()` to play `AnimationMort` and disable physics/collision before delegating to `LivingEntity.Mourir`. PV/damage come from `LivingEntity`. Subclasses supply `ConstruireAnimations()` + `Initialiser()` and their own AI; **nothing boss-specific lives here**.
- **`BossCerf.cs`** (`: Boss`) — only Cerf specifics: `enum Etat`/`Pattern` state machine (`_PhysicsProcess`), two phases (transition at 50% HP), dodgeable charge that stuns the boss (`AjusterDegats` ×3 window) into a wall, piétinement (stalactites) + souffle de givre (which **reuse the idle/charge animations** for budget), and the tuning exports. HP/damage numbers (`PvMax=40`, etc.) are unvalidated placeholders (`DECISIONS.md`).
- **`ZoneBoss.cs`** (`scripts/Core/`, extends `DeclencheurZone`) — reusable, **inheritable** arena trigger. On player entry it **spawns the boss** (`SceneBoss` at `PositionApparition`, a sibling of the zone), links & reveals its HP bar (`BossHudBarre.Lier` then `Afficher`), arms its PV (`PvBoss`), and plays music (`Musique`, currently no assets). Hooks: `ConfigurerBoss(boss)` (before `AddChild`) and `DemarrerCombat(joueur)`. `[Export]`: `SceneBoss`, `NomBoss`, `PositionApparition`, `CheminBarre`, `PvBoss`, `Musique`.
- **`ZoneBossCerf.cs`** (`: ZoneBoss`) — Cerf arena: sets the boss's charge bounds (`LimiteGauche/Droite`) and, on `Vaincu`, transitions to `ecran_fin.tscn`. In `monde.tscn` the boss is **not placed statically** — the `ZoneBossCerf` node spawns it; `BossHudBarre` starts hidden and is bound at spawn.

## Assets layout

`assets/` holds generated PNGs (`tiles/`, `backgrounds/`, `props/`, `player/`, `pnj/boss_cerf/`, `ui/`) — PNJ art (bosses) lives under `pnj/`. **Ground art lives in `assets/sol/`, one subfolder per biome** — `assets/sol/` root = banquise (3 tileable centre segments + 2 embouts + 4 pentes) with `assets/sol/elements/` for the small ice blocks, `assets/sol/grotte/` (3 centres + 2 fins), `assets/sol/usine/` (3 centres + 2 embouts + 4 pentes, wooden factory floor) — mirroring `scenes/sol/<biome>/` and `scripts/sol/` — it is *not* décor. `assets/decors/banquise/` keeps only the `parallax/` layers. `.godot/` is generated (git-ignored); `.claude/` and `.idea/` are local-only.

**`scenes/` mirrors `scripts/`** — every reusable element of the world is a `.tscn` "GameObject" you can drop into a level from the Godot editor, filed by role (put new `.tscn` in the matching folder):
- **`niveaux/`** — playable levels: `monde1.tscn`, `monde2.tscn`, and the boss arenas `ReindeerBoss.tscn` / `BossEnd.tscn`. `EcranScenesDebug` scans this folder, so a new level shows up in the debug menu with no code change.
- **`entites/`** — living actors (PNJ, player): `player.tscn`, `lutin_cgt.tscn`, `pingouin.tscn`… **Boss entity scenes do NOT live here** — see `boss/`.
- **`boss/`** — everything boss-related: the boss entities, in PascalCase (`BossCerf.tscn`, `BossLutinMecha.tscn`, `BossPereNoel.tscn`), plus their arena triggers (`zone_boss_cerf.tscn`, `zone_boss_pere_noel.tscn`).
- **`ennemis/`** — hostile actors (`PnjMechant` subclasses), flat for the banquise (`BonhommeDeNeige.tscn`, `OursDeNeige.tscn`, `BouleDeNeige.tscn`) and **one subfolder per biome beyond it, mirroring `assets/ennemis/<biome>/` and `scripts/Entities/Ennemis/<Biome>/`**: `ennemis/grotte_florale/` (`GardienRonces.tscn` patrouilleur/poursuivant, `FleurCarnivore.tscn` plante embusquée, `BulbeExplosif.tscn` piège à retardement, `NueePollen.tscn` volant). Chaque scène porte son `Apercu`, sa `ZoneDetection` (portée réglable par instance) et ses zones d'attaque (`ZoneContact`, `ZoneMorsure`, `ZoneExplosion`).
- **`interactifs/`** — interactables/hazards: `checkpoint_peche.tscn`, `mur_fondable.tscn`, `stalactite_piege.tscn`, `pouvoir_chaleur_pickup.tscn`.
- **`projectiles/`** — `boule_de_neige.tscn`.
- **`decors/`** — non-interactive décor: `igloo.tscn` and the `DecorBanquise`/`DecorGrotte` parallax sets. **Convention (props purement visuels) : un prop qui n'a NI collision NI comportement ne doit PAS avoir de `.tscn` dédié** — c'est un simple `Sprite2D` inline directement dans `monde.tscn` (`texture = ExtResource(<png>)`, `z_index = -1`, + un `offset` si l'art doit être ancré au sol). Un `.tscn` réutilisable n'est justifié que si l'élément porte une collision ou un script (sinon il n'apporte rien). Le PNG est référencé directement (`res://assets/props/...`). Le seul survivant de `decors/props/` est **`grotte/GlaceEmpilee.tscn`**, gardé parce qu'il porte un `StaticBody2D` + `CollisionShape2D` (ce n'est donc pas de la déco pure). Voir aussi la règle « éléments avec collision = `.tscn` visible » (murs/sols).
- **`ui/`** — `hud.tscn`, `boss_hud_barre.tscn`, `menu_principal.tscn` (the boot scene, `run/main_scene`), `ecran_fin.tscn` (le **générique de fin défilant** : la scène ne porte que le fond et la colonne vide, tout le texte vient de `assets/credits/generique.tres`).
- **`core/`** — scene-driven zone helpers: `camera_zone.tscn` (reusable camera-limit region; limits derive from its resized collision rectangle, and its `NomRegion` drives the background).
- **`plateformes/`** — the platform GameObjects (`PlateformeFixe`, `PlateformeFragile`, …). Their default sprite + collision is now baked into the `.tscn` so they render in the editor; `PlateformeFixe.cs._Ready` still re-applies texture/collision from the `Taille` export at runtime (editor preview = the "Petite" default). **Note:** these are a parallel system — `monde.tscn` does **not** instance them; its walkable ground/ledges are collision tiles painted into the `Terrain` `TileMapLayer`. The `Plateforme*` scenes are currently only used in `test/TestPlateformes.tscn`.
- **`sol/`** — the **solid ground** GameObjects, distinct from `decors/` (they carry collision) and from `plateformes/` (they are never traversable). **Filed in one subfolder per biome, mirroring `assets/sol/`**: `sol/banquise/` (`SolBanquise.tscn` + `SolBanquiseEmbout{Gauche,Droit}.tscn` ground segments, walking surface at local y = −46 ; `PenteBanquise*.tscn` slope segments with a `CollisionPolygon2D` ; `PlateformeBanquise*.tscn` small raised ice blocks — plaque/bloc/congère), `sol/grotte/` (`SolGrotte.tscn` = centre A, `SolGrotteCentre{B,C}.tscn`, `SolGrotteEmbout{Gauche,Droit}.tscn`, surface at y = −84), `sol/usine/` (`SolUsineBoisCentre{A,B,C}.tscn` + `SolUsineBoisEmbout{Gauche,Droit}.tscn` wooden floor segments — origin at the **left edge**, surface at y = +8 ; `PenteUsine{Douce,Forte}{Montante,Descendante}.tscn` ; plus `SolUsineBois.tscn`, a `[Tool]` **row** that instances N of those segment scenes from its `NombreSegments`/`EmboutGauche`/`EmboutDroit` exports). **All of them stay on the default collision layer 1**, so `bas`+`saut` cannot drop through them — only `plateformes/PlateformeUnidirectionnelle` (layer 5, `Constantes.LayerPlateformesTraversables`) is traversable. Scripts mirror this in `scripts/sol/`.
- **`test/`** — throwaway test scenes (`TestPlateformes.tscn`).

`scripts/` is organized by role — put new C# in the matching folder (namespaces are not used; classes are global, so a file's folder is purely for humans):
- **`Common/`** — shared, reusable helpers with no gameplay identity of their own: `Constantes` (`TailleTuile`), `Effets` (`Disparaitre`, `FlashCouleur`, `Flottaison`), `DeclencheurZone`, the `DamageSource` enum (+ `MontantDegats` per source), the `Damageable` interface (`TakeDamage`/`IsInvincibleToDamage`) with its `Degats.Infliger(cible, source)` helper (single entry point that applies damage while respecting immunity), the `FriendlyLivingEntity` marker interface (entities that implement it never take any damage, whatever the source), and the `Talkative` family — `Talkative` (contract of anything that speaks) plus its three **optional** extensions detected by cast in `DeclencheurDialogue`: `TalkativeAutomatique` (bubble scrolls on a timer), `OllamaTalkative` (line generated by the local LLM), `TalkativeAChoix` (the player answers by picking a pre-written reply).
- **`Core/`** — global systems & scene-driven zones: `GameState`, `BackgroundManager`, `CameraZone` (its `NomRegion` drives the background region — the old `RegionTrigger` was removed), `ZoneBoss` + `ZoneBossCerf`, the weather system (`GestionnaireMeteo` + `MeteoZone`), the audio system (`GestionnaireAudio` autoload + the data resources `AmbianceSonore` / `VarianteAmbiance` / `PisteMusicale`, whose `.tres` live in `assets/audio/ambiances/`), and the dialogue engine `DeclencheurDialogue` + its data resources `NoeudDialogue` / `ChoixDialogue` (dialogue trees with player replies, whose `.tres` live in `assets/dialogues/`, named `<lieu>_<pnj>.tres`).
- **`Entities/`** — in-world actors and interactables. The shared base `LivingEntity` lives at the folder root; the rest is split by role into subfolders: `Pnj/` (the `Boss` base + `BossCerf`), `Player/` (`Player`), `Damage/` (damage-dealing entities: the `Projectile` base + `Snowball`), `Interactable/` (`MurFondable`, `StalactitePiege`, the `PouvoirChaleurPickup` ramassable), and `Misc/` (`Checkpoint`, the `ElementRamassable` base). `Boss` and `Player` both extend `LivingEntity` (which implements `Damageable`).
- **`Plateformes/`** — platform behaviours: `PlateformeFixe`, `PlateformeMobile`, `PlateformeGlissante`, `PlateformeFragile`, `PlateformeUnidirectionnelle`.
- **`sol/`** — solid ground behaviours (mirrors `scenes/sol/`, but **flat**: one file per element, no biome subfolder): `SolBanquise`, `PenteBanquise`, `PlateformeBanquise`, `SolGrotte`, `SegmentSolUsineBois` (one usine floor segment) + `PenteUsineBois`, and `SolUsineBois` (the `[Tool]` row that instances the segment scenes — the only script here that builds children). The segment/pente scripts are **markers**: the `.tscn` is the single source of truth for sprite + collision, they never re-apply anything at runtime, and none of them touches `CollisionLayer` (layer 1 = non-traversable).
- **`Terrain/`** — `TileSetFabrique` (now just the baked TileSet's custom-data-layer key names).
- **`UI/`** — `Hud`, `BossHudBarre`, `EcranFin`, and the menus `MenuPrincipal` + `MenuPause` built via the shared `MenuFabrique`. C'est aussi le seul dossier hors `Core/` à porter des ressources `[GlobalClass]` : `CreditsGenerique` / `EntreeCredits` (le contenu du générique de fin — titre, rôles, noms, vitesse de défilement, tailles de police), dont l'unique `.tres` vit dans **`assets/credits/`**. Ajouter quelqu'un aux crédits = éditer ce `.tres` dans l'inspecteur, jamais le C# ni la scène.
