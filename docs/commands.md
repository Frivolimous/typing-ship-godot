# Commands

`ts.bat` (repo root) is a numbered menu of the everyday commands. Double-click it, or run `ts 3` to go straight
to option 3. Everything it does is listed below.

`$GODOT` stands for `C:\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`, or
whatever the `GODOT` environment variable is set to.

## Everyday

| What | Command |
|---|---|
| Build the C# | `dotnet build` |
| Play | `$GODOT --path .` |
| Open the Godot editor | `$GODOT --path . --editor` |
| Re-import assets (after adding or changing files in assets/) | `$GODOT --headless --path . --import` |

## Game options

Pass these after `--` on the Godot command line, e.g. `$GODOT --path . -- --level=0 --wpm=65`.

| Option | Effect |
|---|---|
| `--level=N` | Start at level N (0–11; bosses on 3, 7 and 11) |
| `--wpm=N` | Difficulty as target words per minute: 25 Easy, 40 Normal (default), 65 Hard, 75 Extreme, 90 Insane |
| `--seed=N` | Fix the random seed, for repeatable runs |
| `--autotype` | A bot types for you, at the difficulty's WPM |
| `--botwpm=N` | With `--autotype`: the bot types at N WPM instead (lower = takes damage) |
| `--quit-on-end` | Quit when the level is cleared or lost |
| `--screenshot=path.png` | Save a screenshot after 10 frames and quit (`--frames=N` for a different frame) |
| `--at=a,b,c` | With `--screenshot`: save `path_a.png`, `path_b.png`, ... at those frames, then quit |
| `--speed=N` | Run N times faster (windowed runs; capped by frame rate) |

## Simulating a level

Runs as fast as the CPU allows, with no window, and prints one line per level:

```bash
$GODOT --headless --fixed-fps 24 --path . -- --autotype --quit-on-end --wpm=40 --seed=1
```

```
Level 0 cleared: score 292, ships 56, missed 0, health 5, ticks 3034
```

A tick is 1/24 s of game time, so 3034 ticks is about 2 minutes of play.

Add `--level=N` for another level. `ts 6` asks for a level and simulates it at all five difficulties; `ts 7` does
every level (60 runs, a few minutes). `ts 5` asks for a level and opens a window where the bot plays it.

The bot (`--autotype`) types at exactly the difficulty's WPM and never mistypes. It plays earliest-deadline-first:
whatever will hurt soonest (an incoming missile, a suicide ship, a laser about to fire, boss 2's beam) as long as it
can still finish the word in time, then hull repairs when there's slack, then the nearest ship (turret first), then
the boss scanner. It's a yardstick for "can a typist at this speed survive this", not a model of a human.
