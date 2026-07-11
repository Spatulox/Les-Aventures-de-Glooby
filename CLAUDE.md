# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Les Aventures de Glooby** — a 2D pixel-art platformer built in **Godot 4.6.3 (.NET/C# / Mono edition, net8.0)**. A penguin-ish hero crosses ice fields and a cave to defeat the Boss Cerf (Rodolphe). Renderer is Forward Plus, physics is Jolt. All code, comments, node names, and identifiers are in **French** — match that convention when adding code.

Pixel-art assets are generated with **PixelLab** under a per-mission generation budget (see `BUDGET.md`); reuse existing assets/animations rather than generating new ones. Project narrative and design rationale live in `RAPPORT.md`, `DECISIONS.md`, and `TODO.md` (read these before changing gameplay — many "odd" choices are deliberate and documented).

## Coding conventions (required)

- Code must be **as reusable as possible** — favor shared helpers over duplication: `Constantes` (`TailleTuile`), `Outils` (`Attacher`, `Instancier`, `AjouterDecor`, `PlacerFondRepete`), `TerrainPeintre` (`Segment` record + `PeindreSegments`/`PeindreBandeSol`), `Effets` (`Disparaitre`, `FlashCouleur`, `Flottaison`), `TileSetFabrique`, and the `ElementRamassable` base for contact pickups. `GameState.EstConsomme/MarquerConsomme` is the generic persistent-element store.
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

There is **one gameplay scene: `scenes/monde.tscn`** (set as `run/main_scene`). The entire level lives in it — there is no scene reload during play (only the final victory → `ecran_fin.tscn`). This was a deliberate refactor away from 7 separate reloaded scenes.

`scripts/Monde.cs` (`_Ready`) assembles everything:
- One shared `TileMapLayer` named `Terrain`, whose `TileSet` is built in code by `TileSetFabrique.CreerMonde()`.
- Each **room ("Salle") is painted at a tile offset (`Decalage`)** into that same layer — offsets are the `DecalageXxx` constants at the top of `Monde.cs`. Rooms do not overlap because of these offsets.
- **`CameraZone` (Area2D) regions** adjust the player `Camera2D` limits on room entry. Camera limits are per-zone, and the player's fall-death threshold (`SeuilChuteVide`) is **relative to the active zone**, not an absolute Y — a single global threshold would misfire in deep rooms.

### Rooms: `SalleXxx.cs`

Each room is a **static class with a `Construire(...)` method** (e.g. `SalleDepart`, `SalleCarrefour`, `SalleBoss`). `Construire` paints terrain bands and instantiates props/entities at its `Decalage`. They do **not** subclass Node — they are builders invoked once from `Monde.cs`. To add/modify a room: edit its `SalleXxx.cs` and register its `Decalage` + camera zone in `Monde.cs`.

Terrain within a room is described declaratively (see `SalleDepart.Segments`) and stamped by `TerrainPeintre.PeindreBandeSol(...)` (a surface row + N fill rows). Props/scenes are placed via `Outils.AjouterDecor(...)` and `Outils.Instancier(...)`. **Important (`Outils.cs`): set exported properties inside the `avantAjout` callback, BEFORE `AddChild`** — `_Ready()` runs immediately on add, so a `.Set(...)` afterward is too late.

### TileSets built in code

`TileSetFabrique.cs` builds `TileSet` resources programmatically from PixelLab 4×4 Wang tile sheets (32×32 tiles) instead of hand-authored `.tres` files. Sources are registered by a string key via `tileSet.SetMeta("banquise_plein", sourceId)` and looked up by rooms. Custom data layers `is_ice` and `is_fragile` drive gameplay (sliding, breakable ice); collision polygons are added per tile. **A source must be added to the TileSet before creating its tiles**, or custom-data layers won't exist yet on the `TileData`.

### Global state & autoloads

Two autoloads (`project.godot [autoload]`):
- **`GameState`** (`scripts/GameState.cs`) — singleton via `GameState.Instance`. Holds PV, poissons, progression flags (`PouvoirChaleurActif`), melted-wall / collected-fish id sets, and checkpoint position. Communicates outward through `[Signal]` events (`PvChanges`, `JoueurMort`, `CheckpointActif`, …). Since the world never reloads, respawn = teleport the player to `CheckpointPosition`, not a scene change.
- **`Hud`** (`scenes/hud.tscn`) — persistent HUD (hearts, fish counter), subscribes to `GameState` signals.

**Input actions are registered in code** in `GameState.ConfigurerActionsParDefaut()` (move_left/right, jump, slide, lancer, manger, pouvoir_chaleur) — *not* in `project.godot`. Change key bindings there.

Note on Godot C# signals: a `[Signal] delegate FooEventHandler` generates a member named `Foo`, which **collides with a property named `Foo`** — a bug hit before (see `RAPPORT.md` Jalon B). Name properties and signals distinctly.

### Player & Boss

- **`Player.cs`** (`CharacterBody2D`) — timer-based controller: coyote time + jump buffer, accelerated slide (faster on `is_ice`), snowball throw, short post-hit invincibility, fragile-tile break delay, relative fall-death safety net. Tunables are `[Export]` fields at the top.
- **`BossCerf.cs`** — explicit state machine (`enum Etat`: Intro/Idle/Telegraphe/Charge/Etourdi/Pietinement/SouffleGivre/Vaincu; `enum Pattern`). Two phases (transition at 50% HP), charge is dodgeable and stuns the boss (×3 damage window) into a wall. Piétinement and Souffle de Givre **reuse the idle/charge animations** (budget economy) — only the gameplay result is new. HP/damage numbers (`PvMax=40`, etc.) are unvalidated placeholders pending a real play-test (`DECISIONS.md`).

## Assets layout

`assets/` holds generated PNGs (`tiles/`, `backgrounds/`, `props/`, `player/`, `boss_cerf/`, `ui/`). `scenes/` holds reusable `.tscn` (player, boule_de_neige, poisson, checkpoint_peche, mur_fondable, stalactite_piege, pickups…). `scripts/` holds all C#. `.godot/` is generated (git-ignored); `.claude/` and `.idea/` are local-only.
