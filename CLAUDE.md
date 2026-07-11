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

**The whole map is authored directly in `scenes/niveaux/monde.tscn`** (edited by hand in the Godot editor): the `Terrain` `TileMapLayer` and its baked tiles, all props/entities, the `CameraZone` regions, the boss, checkpoints, backgrounds. There is **no runtime world-assembly script** — the scene root has no build script; to add/move content, edit `monde.tscn` in the editor.

Key pieces of the scene:
- One shared `TileMapLayer` named `Terrain`, whose `TileSet` is baked into `monde.tscn` (it was once built in code by `TileSetFabrique`, since unplugged — see below); tiles were painted into it and saved.
- **`CameraZone` (Area2D) regions** adjust the player `Camera2D` limits on room entry. Camera limits are per-zone, and the player's fall-death threshold (`SeuilChuteVide`) is **relative to the active zone**, not an absolute Y — a single global threshold would misfire in deep rooms.
- **`ZoneBoss` (Area2D)** covers a boss arena and reveals the boss HP bar / arms the boss on player entry (see Player & Boss).

> Historical note: the map used to be generated procedurally by `scripts/Core/Monde.cs` (`_Ready`) painting one static `SalleXxx.Construire(...)` builder per room at a tile `Decalage`. That generator was run once, its result **baked into `monde.tscn`, then unplugged and removed** (`Monde.cs` + `scripts/Rooms/` no longer exist). If you see references to `SalleXxx`/`Decalage` in older reports, that's why.

### TileSets built in code

`scripts/Terrain/TileSetFabrique.cs` once built the `Terrain` `TileSet` programmatically from PixelLab 4×4 Wang tile sheets (32×32 tiles) instead of hand-authored `.tres` files; that `TileSet` is now baked into `monde.tscn` and the builder was unplugged, so the file is reduced to the two custom-data-layer key names (`is_ice`, `is_fragile`) still read by `Player.cs` to drive gameplay (sliding, breakable ice).

### Global state & autoloads

Two autoloads (`project.godot [autoload]`):
- **`GameState`** (`scripts/Core/GameState.cs`) — singleton via `GameState.Instance`. Holds PV, poissons (a **fixed start reserve of `PoissonsDepart = 50`, consumed only** to heal — fish are not picked up in the world), progression flags (`PouvoirChaleurActif`), melted-wall id set, and checkpoint position. Communicates outward through `[Signal]` events (`PvChanges`, `JoueurMort`, `CheckpointActif`, …). Since the world never reloads, respawn = teleport the player to `CheckpointPosition`, not a scene change.
- **`Hud`** (`scenes/ui/hud.tscn`) — persistent HUD (hearts, fish counter), subscribes to `GameState` signals.

**Input actions are registered in code** in `GameState.ConfigurerActionsParDefaut()` (move_left/right, jump, slide, lancer, manger, pouvoir_chaleur) — *not* in `project.godot`. Change key bindings there.

Note on Godot C# signals: a `[Signal] delegate FooEventHandler` generates a member named `Foo`, which **collides with a property named `Foo`** — a bug hit before (see `RAPPORT.md` Jalon B). Name properties and signals distinctly.

### LivingEntity, Player & Boss

**`LivingEntity.cs`** (`scripts/Entities/`, abstract `CharacterBody2D`, implements `Damageable`) — the shared base for everything that "lives": both `Player` and `Boss` (and any future PNJ) extend it. It owns `PvMax`/`Pv`/`EstVaincu`, the `PvChanges`/`Vaincu` signals, `DefinirPvMax`, the single damage entry `TakeDamage(DamageSource)` (with `AjusterDegats`/`ApresDegats`/`Mourir` virtual hooks), and reusable movement helpers `AppliquerGravite`/`AppliquerFriction`/`Sauter` (tunables `Gravity`/`MaxFallSpeed`/`Friction`/`JumpVelocity`). The generic `Mourir()` marks the entity beaten, zeroes velocity and emits `Vaincu`.

- **`Player.cs`** (`scripts/Entities/Player/`, `: LivingEntity`) — timer-based controller: coyote time + jump buffer, accelerated slide (faster on `is_ice`), snowball throw, short post-hit invincibility, fragile-tile break delay, relative fall-death safety net. Uses the base movement helpers. Its HP lives in **`GameState`, not the base `Pv`** (persistent, HUD-bound, respawn), so it overrides `TakeDamage(DamageSource)` to route damage there and `IsInvincibleToDamage` to its invincibility timer; `Blesser(int direction, DamageSource source)` is the knockback-carrying entry point called by the boss/hazards (and `TakeDamage` just delegates to it with a neutral direction). Tunables are `[Export]` fields at the top.
**Boss OO hierarchy.** Bosses split generic scaffolding from per-boss content, and each boss has its own arena zone:
- **`Boss.cs`** (`scripts/Entities/Pnj/`, abstract `: LivingEntity`) — the reusable *animated* base: loads its animations by folder (`AjouterAnimation(...)`), and overrides `Mourir()` to play `AnimationMort` and disable physics/collision before delegating to `LivingEntity.Mourir`. PV/damage come from `LivingEntity`. Subclasses supply `ConstruireAnimations()` + `Initialiser()` and their own AI; **nothing boss-specific lives here**.
- **`BossCerf.cs`** (`: Boss`) — only Cerf specifics: `enum Etat`/`Pattern` state machine (`_PhysicsProcess`), two phases (transition at 50% HP), dodgeable charge that stuns the boss (`AjusterDegats` ×3 window) into a wall, piétinement (stalactites) + souffle de givre (which **reuse the idle/charge animations** for budget), and the tuning exports. HP/damage numbers (`PvMax=40`, etc.) are unvalidated placeholders (`DECISIONS.md`).
- **`ZoneBoss.cs`** (`scripts/Core/`, extends `DeclencheurZone`) — reusable, **inheritable** arena trigger. On player entry it **spawns the boss** (`SceneBoss` at `PositionApparition`, a sibling of the zone), links & reveals its HP bar (`BossHudBarre.Lier` then `Afficher`), arms its PV (`PvBoss`), and plays music (`Musique`, currently no assets). Hooks: `ConfigurerBoss(boss)` (before `AddChild`) and `DemarrerCombat(joueur)`. `[Export]`: `SceneBoss`, `NomBoss`, `PositionApparition`, `CheminBarre`, `PvBoss`, `Musique`.
- **`ZoneBossCerf.cs`** (`: ZoneBoss`) — Cerf arena: sets the boss's charge bounds (`LimiteGauche/Droite`) and, on `Vaincu`, transitions to `ecran_fin.tscn`. In `monde.tscn` the boss is **not placed statically** — the `ZoneBossCerf` node spawns it; `BossHudBarre` starts hidden and is bound at spawn.

## Assets layout

`assets/` holds generated PNGs (`tiles/`, `backgrounds/`, `props/`, `player/`, `pnj/boss_cerf/`, `ui/`) — PNJ art (bosses) lives under `pnj/`. `.godot/` is generated (git-ignored); `.claude/` and `.idea/` are local-only.

**`scenes/` mirrors `scripts/`** — every reusable element of the world is a `.tscn` "GameObject" you can drop into a level from the Godot editor, filed by role (put new `.tscn` in the matching folder):
- **`niveaux/`** — playable levels: `monde.tscn` (the single continuous world, `uid://bfmhiv7v30so8`).
- **`entites/`** — living actors: `player.tscn`, `boss_cerf.tscn`.
- **`interactifs/`** — interactables/hazards: `checkpoint_peche.tscn`, `mur_fondable.tscn`, `stalactite_piege.tscn`, `pouvoir_chaleur_pickup.tscn`.
- **`projectiles/`** — `boule_de_neige.tscn`.
- **`decors/`** — non-interactive décor: `igloo.tscn`, the `DecorBanquise`/`DecorGrotte` sets, and **`decors/props/`** — the small reusable décor props (`Rocher`, `CristalPetit`, `CristalGros`, `StalactiteDecor` [purely cosmetic, distinct from the `interactifs/stalactite_piege` trap], `FleurGivre`); each is a `Sprite2D` root at `z_index=-1`. `monde.tscn` instances these rather than embedding raw `Sprite2D` nodes, so editing a prop once updates every placement.
- **`ui/`** — `hud.tscn`, `boss_hud_barre.tscn`, `menu_principal.tscn` (the boot scene, `run/main_scene`), `ecran_fin.tscn`.
- **`core/`** — scene-driven zone helpers: `region_trigger.tscn`.
- **`plateformes/`** — the platform GameObjects (`PlateformeFixe`, `PlateformeFragile`, …). Their default sprite + collision is now baked into the `.tscn` so they render in the editor; `PlateformeFixe.cs._Ready` still re-applies texture/collision from the `Taille` export at runtime (editor preview = the "Petite" default). **Note:** these are a parallel system — `monde.tscn` does **not** instance them; its walkable ground/ledges are collision tiles painted into the `Terrain` `TileMapLayer`. The `Plateforme*` scenes are currently only used in `test/TestPlateformes.tscn`.
- **`test/`** — throwaway test scenes (`TestPlateformes.tscn`).

`scripts/` is organized by role — put new C# in the matching folder (namespaces are not used; classes are global, so a file's folder is purely for humans):
- **`Common/`** — shared, reusable helpers with no gameplay identity of their own: `Constantes` (`TailleTuile`), `Effets` (`Disparaitre`, `FlashCouleur`, `Flottaison`), `DeclencheurZone`, the `DamageSource` enum (+ `MontantDegats` per source), the `Damageable` interface (`TakeDamage`/`IsInvincibleToDamage`) with its `Degats.Infliger(cible, source)` helper (single entry point that applies damage while respecting immunity), and the `FriendlyLivingEntity` marker interface (entities that implement it never take any damage, whatever the source).
- **`Core/`** — global systems & scene-driven zones: `GameState`, `BackgroundManager`, `CameraZone`, `ZoneBoss` + `ZoneBossCerf`, `RegionTrigger`.
- **`Entities/`** — in-world actors and interactables. The shared base `LivingEntity` lives at the folder root; the rest is split by role into subfolders: `Pnj/` (the `Boss` base + `BossCerf`), `Player/` (`Player`), `Damage/` (damage-dealing entities: the `Projectile` base + `Snowball`), `Interactable/` (`MurFondable`, `StalactitePiege`, the `PouvoirChaleurPickup` ramassable), and `Misc/` (`Checkpoint`, the `ElementRamassable` base). `Boss` and `Player` both extend `LivingEntity` (which implements `Damageable`).
- **`Plateformes/`** — platform behaviours: `PlateformeFixe`, `PlateformeMobile`, `PlateformeGlissante`, `PlateformeFragile`, `PlateformeUnidirectionnelle`.
- **`Terrain/`** — `TileSetFabrique` (now just the baked TileSet's custom-data-layer key names).
- **`UI/`** — `Hud`, `BossHudBarre`, `EcranFin`, and the menus `MenuPrincipal` + `MenuPause` built via the shared `MenuFabrique`.
