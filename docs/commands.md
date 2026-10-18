# Commands

`ts.bat` (repo root) is a numbered menu of the everyday commands. Double-click it, or run `ts 3` to go straight
to option 3. Everything it does is listed below.

`$GODOT` stands for `C:\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`, or
whatever the `GODOT` environment variable is set to. Commands run from the repo root; the Godot project is in
`game/`.

## Everyday

| What | ts | Command |
|---|---|---|
| Play | 1 | `$GODOT --path game` |
| Open the Godot editor | 2 | `$GODOT --path game --editor` |
| Build the C# (rules, tests and game) | 3 | `dotnet build` |
| Re-import assets (after adding or changing files in game/assets/) | 4 | `$GODOT --headless --path game --import` |
| Edit a level, with the editing keys and grid | 8 | `$GODOT --path game -- --edit --overlay --level=N [--from=50 or --from=1:37] [--line=40] [--autotype]` |
| Check the level and tuning files | 9 | `$GODOT --headless --path game -- --check` |
| Run the tests | 10 | `dotnet test` |
| Level pacing report: typing demand over time for every level and difficulty, the game's levels and the Classic ones side by side, as a share of the difficulty's speed (average and 10-second peak), with each level's last spawn and the words on screen each second. Charts (`pacing/pacing.html`) and CSVs for spreadsheets (`pacing.csv`, `summary.csv`, `lines.csv` with the time each line of a level file happens, like `L42 -> 1:37.1`, plus `violations.csv` for the pacing rules and `realistic.csv` for a bot at 80% speed with typing mistakes) | 11 | `dotnet run --project src/Sim -- pacing [--level=N] [--seed=N] [--out=folder] [--bot-speed=80%] [--errors=3.5%]` |
| Check the pacing rules ([pacing-rules.md](pacing-rules.md), numbers in `data/pacing-rules.txt`): lists every rule each regular game level breaks at each difficulty, with the time, the measured value and the limit; then a count per level and how the realistic bot did. Exits with 1 if anything fails (warnings don't count), so it works as a check after every level edit | 13 | `dotnet run --project src/Sim -- pacing --check [--level=N]` |
| Play the Classic levels (the original 12 as they were), with a save file of their own | 12 | `$GODOT --path game -- --classic --profile=classic` |

How to write levels, the editing keys, and what the checks report: [level-format.md](level-format.md).

## Game options

Pass these after `--` on the Godot command line, e.g. `$GODOT --path game -- --level=0 --wpm=65`.

| Option | Effect |
|---|---|
| `--level=N` | Skip the menus and start at level N (0–11; bosses on 3, 7 and 11) |
| `--classic` | Play the Classic levels (data/levels/classic/, the original 12 as they were) instead of the game's own. A development baseline, removed before release. Scores save to the same slots, so use `--profile` to keep them apart |
| `--wpm=N` | Difficulty as target words per minute: 25 Easy, 40 Normal (default), 65 Hard, 75 Extreme, 90 Insane. Any number works (the rules use it as characters per second, N x 5 / 60). On the menus the difficulty box sets it instead |
| `--cps=N` | The same in characters per second, as the rules and tuning.txt count it (Normal is 3.33) |
| `--seed=N` | Fix the random seed, for repeatable runs |
| `--autotype` | A bot types for you, at the difficulty's WPM. Skips the menus (straight into level 0) unless `--menu` is given, turns tutorial pop-ups off, and doesn't save unless `--profile` is given |
| `--botwpm=N` | With `--autotype`: the bot types at N WPM instead (lower = takes damage). `--botcps=N` is the same in characters per second |
| `--quit-on-end` | Quit when the level is cleared or lost |
| `--noglow` | Turn the glow (bloom) off, to compare |
| `--menu` | With `--autotype`: start on the title menu anyway (the bot plays once you start a level) |
| `--map` | Start on the level map instead of the title |
| `--profile=NAME` | Use the save file `user://NAME.json` instead of `user://save.json` (keeps test runs out of your save) |
| `--keys=t:text,...` | Scripted input at given ticks: letters, digits and punctuation (except commas) are typed, `{space}`, `{comma}`, `{enter}`, `{esc}`, `{backspace}`, `{f1}`–`{f12}` press keys, `{click:XxY}` clicks at a stage position. E.g. `--keys=40:start,75:1,85:normal` |
| `--screenshot=path.png` | Save a screenshot after 10 ticks and quit (`--frames=N` for a different tick) |
| `--at=a,b,c` | With `--screenshot`: save `path_a.png`, `path_b.png`, ... at those ticks, then quit |
| `--speed=N` | Run N times faster (windowed runs; capped by frame rate) |
| `--edit` | The level-editing keys (F5 reload, F6 restart, F7/F8 back/ahead 10 s, F9 grid with each ship's line and time, F10 invincible, F11 bot on/off) and a banner with the file, the level's time and the next line. Starts level 0 unless `--level` is given |
| `--overlay` | With `--edit`: start with the grid and flight paths showing (F9 toggles them) |
| `--from=N` | Start the level N% of the way in (the in-game progress meter's number), or `--from=1:37` at that time. Works with or without `--edit` |
| `--line=N` | Start the level at line N of its file (the first ship that line or a later one makes) |
| `--check` | Load and check every data file, print "Data OK" or the first mistake, and quit |
| `--trace=file.txt` | Write one line of game state per tick (positions, words, score), to compare two runs exactly |

## Simulating a level

Runs as fast as the CPU allows, with no window, and prints one line per level:

```bash
$GODOT --headless --fixed-fps 24 --path game -- --autotype --quit-on-end --wpm=40 --seed=1
```

```
Level 0 cleared: score 292, ships 56, missed 0, health 5, ticks 3034
```

A tick is 1/24 s of game time, so 3034 ticks is about 2 minutes of play.

Add `--level=N` for another level. `ts 6` asks for a level and simulates it at all five difficulties; `ts 7` does
every level (60 runs, a few minutes). `ts 5` asks for a level and opens a window where the bot plays it.

The rules don't need Godot, so the tests run the same bot without it: `dotnet test` plays every level at every
difficulty in a few seconds (tests/Core.Tests/SimulatorTests.cs). For the same seed it plays the same game as the
headless run above, tick for tick.

The bot (`--autotype`) types at exactly the difficulty's WPM and never mistypes. It plays earliest-deadline-first:
whatever will hurt soonest (an incoming missile, a suicide ship, a laser about to fire, boss 3's beam) as long as it
can still finish the word in time, then the nearest ship (turret first), then the boss scanner, and the hull's
repair word last, only when there's nothing else to type. It's a yardstick for "can a typist at this speed survive this", not a model of a human.

## Tests

`dotnet test` runs everything in tests/Core.Tests: the data files load, the level format and its error messages,
the rules (weapons, typing, scoring, bosses), determinism (same seed and typing, same game), and the bot clearing
every level. `dotnet test --filter-method "*Scanner*"` runs the tests whose names match.

## Walking through the menus

`--keys` and `--screenshot` both count ticks (1/24 s of game time) from start-up, so a windowed run can play the
whole shell and capture each screen. This one goes title → map → level 1 on Insane, lets the bot clear it, then
continues from the score screen back to the map:

```bash
$GODOT --path game -- --profile=test --menu --autotype --speed=10 --keys=40:start,75:1,85:insane,1500:{space} --screenshot=shot.png --at=60,400,1490,1530
```

The save files live in `%APPDATA%\Godot\app_userdata\Millennium Typer\`. Delete `test.json` there to start the
profile fresh. On the title or map, Ctrl+Page Up unlocks every level and Ctrl+Page Down wipes the save (the
original's dev keys).
