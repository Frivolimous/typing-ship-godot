# CLAUDE.md

Guidance for Claude Code in this repo.

## What this is

Millennium Typer is a typing shoot-'em-up being ported from a 2011 Flash/AS3 game ("Typing Ship") to Godot 4.7 / C#
for a premium Steam release. Claude is the main developer. Jeremy owns design, playtesting, review and art
direction. The art and fonts will be replaced, so don't polish placeholder visuals beyond what's needed.

- Port progress, class-by-class mapping and the deliberate deviations from the original: [docs/porting.md](docs/porting.md).
  **Keep it current in the same commit as the code.**
- Things that must happen before release (fonts, rights, export settings, Steam): [docs/release-checklist.md](docs/release-checklist.md).
- Commands: [docs/commands.md](docs/commands.md), plus the `ts.bat` menu. When a command is added or changes, update both.

## Porting rules

- **The port is the spec now.** The game started as a faithful port of the original AS3 (its numbers, timings and
  quirks of feel); the AS3 source is no longer in the repo (Jeremy keeps a copy). Where the port still follows the
  original, docs/porting.md ("Notes from the original") records what it does.
- **Fix real bugs instead of preserving them** (Jeremy's call, e.g. the player missile heading). When a fix changes
  how the game plays, list it under "Deviations" in docs/porting.md.
- **Levels and tuning are data**, for Jeremy to edit: `data/levels/*.txt` (the original LevelData helpers, one per
  line, timed in seconds as played on Hard: `@0:02.77 missile sn  5,0 5,6 4,12`, `+0.55 mirror`) and `data/tuning.txt`. The format is documented for him in
  [docs/level-format.md](docs/level-format.md); keep that current when the format changes. Every mistake in a data
  file must stop the game with a message naming the file and line.
- **Hard is the reference** for level pacing: level timing is written for Hard and is the same at every
  difficulty. Jeremy hand-tunes the Hard levels; never change them to satisfy the pacing rule check. The other
  difficulties are thinned or filled from Hard by hand, with difficulty tags and five-value numbers in the level files.

## Layout

| Folder | What |
|---|---|
| `src/Core/` | **The rules**, plain C# with no Godot reference (a test checks): `Game` (state, the 24 Hz tick, weapons, typing), entities, bosses, the bot (`AutoTyper`), `Simulator`, the RNG, save data and scoring. `Levels/` parses and builds level files; `Data/` loads and checks the data files |
| `tests/Core.Tests/` | xUnit tests of the rules and data files |
| `game/` | The Godot project: presentation and input only |
| `data/` | words.txt, tuning.txt, typing-test.txt (the typing test's passages), mounts.txt (where engine flames and weapon charge-ups sit on each sprite), levels/ (read from disk in a checkout, embedded in the game assembly for exports) |
| `docs/` | Docs |
| `steam/` | Steam: Valve's steam_api libraries (`redist/`), the release build and SteamPipe upload script (`build.ps1`), and what's set up on the partner site ([steam/README.md](steam/README.md)). The game's Steam code is `game/scripts/Platform/` behind `IPlatform` (`G.Platform`); Core never sees it |

## Architecture

- **Core** (`TypingShip.Core`): `Game` holds a session's rules and state (the original GameModel, GameControl,
  ActionControl and KeyboardControl). `Game.Step()` is one 24 Hz tick. It tells the presentation what happens
  through `IGameEvents` (sprites spawned or removed, explosions, beams, score pop-ups, sounds, pauses, tutorials,
  achievements, level over), synchronously, mid-tick. Everything else the presentation reads from its state.
- Everything random in the rules comes from `Game.Rng` (PCG32, bit-identical to Godot's RandomNumberGenerator).
  The presentation has its own (`G.Rng`), so drawing never changes how the game plays. Same data, seed and typing
  give the same game: `Simulator.Run` (the bot, no Godot) matches a headless `--autotype` run tick for tick
  (`--trace` writes the state each tick, to compare runs).
- **Game** (`game/scripts`): `G` (Core/G.cs) is the global access point: `G.Model` (the Core `Game`), `G.Ctrl`,
  `G.World`, `G.UI`, `G.Sound`, `G.Starfield`, `G.Shell`, and `G.Rand()` for presentation-only randomness.
  `GameControl._PhysicsProcess` calls `G.Model.Step()`, then `Presenter.Sync()` copies each entity's state into its
  node (View/Views.cs), then ticks the visual effects (`IViewTicker`). The `Presenter` (View/Presenter.cs) implements
  `IGameEvents`: it makes and frees the nodes, and plays effects and sounds.
- **Shell** (UI/Shell.cs): the menus and other screens are `Screen`s; the shell shows one at a time with the
  original's colour fades, or none while a level is on (GameUI). Keys go to `KeyboardControl` in a level (which
  passes letters to `G.Model.TypeLetter`) and to the shell otherwise. Each screen's `TypeNav` maps typed words to its
  buttons. Save data is `G.Model.Save` (Core `SaveData`, stored as JSON in user:// by Core/SaveFile.cs).
- **Timing:** gameplay runs at a fixed **24 Hz** (the original frame rate; `physics_ticks_per_second=24`). Per-tick
  values are in the original's units (px/tick and so on). Godot's physics interpolation smooths the display. Call
  `ResetPhysicsInterpolation()` after placing a new node. Hand-drawn layers (stars, particles) interpolate
  themselves with `Engine.GetPhysicsInterpolationFraction()`.
- **Coordinates:** the viewport is 550x550 with stretch aspect `expand`, and a Camera2D keeps the square playfield
  centred. Wider windows show side panels. Ships live in `G.UI.GameView`, which is in game-view space (600x550,
  offset -25,-25, the original `GameUI.VIEW_*`). HUD elements use stage space (0-550).
- **Removed objects:** `Sprite.Remove()` takes a Core sprite out of play and sets `Removed`. Other objects may
  still hold a reference (a missile homing on a ship that escaped), so read positions through `P`, which keeps the
  last position, and check `Removed`.
- The tick relies on `Sprite.Update()` returning false only when the object removed itself (the next object slides
  into its slot). Keep that contract.

## Pinned versions

| Tool | Version | Where |
|---|---|---|
| Godot (.NET edition) | 4.7.2 stable mono | `C:\Tools\Godot_v4.7.2-stable_mono_win64\` (use `_console.exe` from scripts); `$env:GODOT` overrides |
| .NET SDK | 10.0.x (pinned in `global.json`) | |
| Target framework | net8.0 for every project | |
| Tests | xUnit v3 on Microsoft.Testing.Platform | |

## Verifying changes

Build with `dotnet build` and run `dotnet test` (both from the repo root). Rules changes get unit tests in
tests/Core.Tests. The rest of the testing happens through the dev flags in docs/commands.md:

- **Headless simulation** (fast, no window): `--headless --fixed-fps 24 --path game -- --autotype --quit-on-end
  [--wpm=N] [--botwpm=N] [--level=N] [--seed=N]` (or `--cps`, `--botcps`) prints a one-line result per level. A perfect bot should clear
  every level on Hard and up (Easy and Normal are thinned from Hard in the level files). A slow bot (`--botwpm` well below `--wpm`) should take damage.
- **Screenshots** (needs a window): `--screenshot=path.png --at=600,1200` saves frames, then quits. Look at them.
  Save into the scratchpad or `screenshots/` (ignored).
- Feel and difficulty can only be judged by Jeremy playing. Say so instead of claiming it's verified.

## Assets

`game/assets/` holds what was extracted from the original `Typing Ship.fla` (a CS6 zip), named by their AS3 linkage
class names (`Nebula0.png`, `LaserS.wav`, `Music2.wav`); only the backdrops, nebulae and the superman sprite are still the
original art. `game/assets/sprites/ai/` holds AI-generated placeholder
art (ships point up; the bosses are animated in code), cut out from sources in `C:\JS Projects\Typing Ship PUB\art\ai-sprites\`.
**Art sets:** the ships, missiles, turret and medals come from a set, a folder under `game/assets/sprites/` with a
`player.png` (`ai`, `custom`, ...), picked in the options, with F2 or `--art=NAME` (Core/Sprites.cs); art a set lacks is
borrowed from `ai`. Bosses always use `ai` (their art shapes the rules). Each set has its own flames, charge points and shields in
data/mounts.txt (`custom/player ...`); when ship art changes, move them there. **Music is git-ignored** (Jeremy keeps it backed up
elsewhere). Code must keep working when `game/assets/music/` is empty, and nothing should start tracking those files. The `.fla` and the raw art sources live outside the repo:
`C:\JS Projects\Typing Ship PUB\` and `C:\JS Projects\Typing Ship\` (Audio/, Bitmaps/, Vector/).
