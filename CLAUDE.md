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

- **The original is the spec.** The AS3 source is in [reference/original-as3/](reference/original-as3/). Port its
  behaviour (numbers, timings, quirks of feel), not just its intent. Read the relevant AS3 before porting a feature.
- **Fix real bugs instead of preserving them** (Jeremy's call, e.g. the player missile heading). When a fix changes
  how the game plays, list it under "Deviations" in docs/porting.md.
- Level scripts keep the original lowercase helper names (`missile`, `laser`, `mirror`, `repeat`, ...) so AS3
  scripts paste in almost unchanged. `Math.random()` becomes `G.Rand()`.
- A structural refactor (rules in a plain C# core, data files, unit tests, like eternal-dungeon) is planned *after*
  the full port is verified against the original. Don't start it piecemeal.

## Architecture (current)

- `G` (scripts/Core/G.cs) is the global access point, in place of the old `Facade`: `G.Model`, `G.Ctrl`,
  `G.Action`, `G.UI`, `G.Sound`, `G.Words`, `G.Starfield`, `G.Shell`, and `G.Rand()`.
- **Shell** (scripts/UI/Shell.cs): the menus and other screens are `Screen`s; the shell shows one at a time with
  the original's colour fades, or none while a level is on (GameUI). Keys go to `KeyboardControl` in a level and to
  the shell otherwise. Each screen's `TypeNav` maps typed words to its buttons. Save data is `G.Model.Save`
  (scripts/Core/SaveData.cs, JSON in user://).
- **Timing:** all gameplay runs in `GameControl._PhysicsProcess` at a fixed **24 Hz** (the original frame rate;
  `physics_ticks_per_second=24`). Per-tick values are in the original's units (px/tick and so on). Godot's physics
  interpolation smooths the display. Call `ResetPhysicsInterpolation()` after placing a new node. Hand-drawn layers
  (stars, particles) interpolate themselves with `Engine.GetPhysicsInterpolationFraction()`.
- What used to be Flash ENTER_FRAME listeners are `ITicker`s registered with `G.Ctrl.AddTicker`, ticked every
  physics frame, paused or not. Tickers that should freeze while paused check `G.Model.Running`.
- **Coordinates:** the viewport is 550x550 with stretch aspect `expand`, and a Camera2D keeps the square playfield
  centred. Wider windows show side panels. Ships live in `G.UI.GameView`, which is in game-view space (600x550,
  offset -25,-25, the original `GameUI.VIEW_*`). HUD elements use stage space (0-550).
- **Removed objects:** `GameSprite.Remove()` detaches and frees the node and sets `Removed`. Other objects may
  still hold a reference (a missile homing on a ship that escaped), so read positions through `P`, which keeps the
  last position, and check `Removed`. Never call engine members on a removed sprite.
- The game update loop relies on `Update()` returning false only when the object removed itself (the next object
  slides into its slot). Keep that contract.

## Pinned versions

| Tool | Version | Where |
|---|---|---|
| Godot (.NET edition) | 4.7.2 stable mono | `C:\Tools\Godot_v4.7.2-stable_mono_win64\` (use `_console.exe` from scripts); `$env:GODOT` overrides |
| .NET SDK | 10.0.x (pinned in `global.json`) | |
| Target framework | net8.0 | |

## Verifying changes

Build with `dotnet build`. Most testing happens through the dev flags in docs/commands.md:

- **Headless simulation** (fast, no window): `--headless --fixed-fps 24 --path . -- --autotype --quit-on-end
  [--wpm=N] [--botwpm=N] [--level=N] [--seed=N]` prints a one-line result per level. A perfect bot should clear
  every level at every difficulty. A slow bot (`--botwpm` well below `--wpm`) should take damage.
- **Screenshots** (needs a window): `--screenshot=path.png --at=600,1200` saves frames, then quits. Look at them.
  Save into the scratchpad or `screenshots/` (ignored).
- Feel and difficulty can only be judged by Jeremy playing. Say so instead of claiming it's verified.

## Assets

`assets/` holds what was extracted from the original `Typing Ship.fla` (a CS6 zip), named by their AS3 linkage
class names (`Enemy0.png`, `LaserS.wav`, `Music2.wav`). **Music is git-ignored** (Jeremy keeps it backed up
elsewhere). Code must keep working when `assets/music/` is empty, and nothing should start tracking those files. The `.fla` and the raw art sources live outside the repo:
`C:\JS Projects\Typing Ship PUB\` and `C:\JS Projects\Typing Ship\` (Audio/, Bitmaps/, Vector/).
