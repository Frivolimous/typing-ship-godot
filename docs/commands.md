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
| Level pacing report: typing demand over time for every level and difficulty, as a share of the difficulty's speed (average and 10-second peak), with each level's last spawn, the words on screen each second, and the pressure (the typing speed needed to stop every threat before it hits; the Demand / Pressure switch on the chart page). Charts (`pacing/pacing.html`) and CSVs for spreadsheets (`pacing.csv`, `summary.csv`, `lines.csv` with the time each line of a level file happens, like `L42 -> 1:37.1`, plus `violations.csv` for the pacing rules and `realistic.csv` for a bot at 80% speed with typing mistakes) | 11 | `dotnet run --project src/Sim -- pacing [--level=N] [--seed=N] [--out=folder] [--bot-speed=80%] [--errors=3.5%]` |
| Check the pacing rules ([pacing-rules.md](pacing-rules.md), numbers in `data/pacing-rules.txt`, pressure included): lists every rule each regular game level breaks at each difficulty, with the time, the measured value and the limit; then a count per level and how the realistic bot did. Exits with 1 if anything fails (warnings don't count), so it works as a check after every level edit | 13 | `dotnet run --project src/Sim -- pacing --check [--level=N]` |
| Endless report: plays endless runs at every difficulty with the perfect bot and the realistic one (90% and 80% of the speed, with mistakes), plus a world-class typist (140 WPM on Insane), one per seed, and prints the clears (waves with every ship shot down), the wave and time survived and the score, then the waves themselves (budget, patterns, delay, the typing speed they're balanced for; tuning.txt `[endless]`). `--cross`: each difficulty's 80% typist one difficulty down and up, and Insane typists on Easy | 14 | `dotnet run --project src/Sim -- endless [--runs=10] [--minutes=30] [--difficulty=Hard] [--cross]` |
| Endless detail: each run's waves and the damage they did, short runs against long, damage by number of patterns, and the patterns in the waves that hurt most | | `dotnet run --project src/Sim -- endless --detail [--runs=12] [--bot=0.9] [--difficulty=Hard]` |
| Play endless patterns on their own, many times, and print the hits each does (as in wave N of a run, or at --speed) | | `dotnet run --project src/Sim -- endless --solo=NAME [--wave=N] [--bot=0.9 | --botcps=N] [--runs=20]` |
| Play endless mode straight away | 15 | `$GODOT --path game -- --endless --wpm=65 [--seed=N]` |
| Play today's daily challenge straight away (or another day's; it's off the map until it's redesigned) | 19 | `$GODOT --path game -- --daily --wpm=65 [--date=2026-10-25]` |
| Check the endless patterns against the guidelines for writing them (letters, words, spawn time, the level they work out to, divers coming from the side or bottom) | 16 | `dotnet run --project src/Sim -- endless --patterns` |

| Release builds: exports Windows (and Linux with `-Linux`) with Godot's release template into `builds/`, then checks the exported game reads its embedded data. Warns if `game/assets/music` is empty | 17 | `powershell -ExecutionPolicy Bypass -File steam\build.ps1 [-Linux]` |
| Upload to Steam: the release build, uploaded with steamcmd to the main app's `beta` branch (or `-Branch NAME`; `-Upload playtest` for the Playtest app). Refuses without music. Asks for the Steam login. See [steam/README.md](../steam/README.md) | 18 | `powershell -ExecutionPolicy Bypass -File steam\build.ps1 -Upload main [-Branch beta] [-Description "..."] [-Linux]` |

How to write levels, the editing keys, and what the checks report: [level-format.md](level-format.md).

## Game options

Pass these after `--` on the Godot command line, e.g. `$GODOT --path game -- --level=0 --wpm=65`.

| Option | Effect |
|---|---|
| `--level=N` | Skip the menus and start at level N (0–11; bosses on 3, 7 and 11) |
| `--endless` | Skip the menus and start an endless run (numbered waves of patterns from data/levels/endless.txt, until the ship is destroyed). `--seed=N` picks the patterns; otherwise each run is different |
| `--daily` | Skip the menus and start the daily challenge: an endless run on the day's waves (UTC), the same for everyone on that difficulty. `--date=yyyy-mm-dd` plays another day's. Scores count for that day's best (and its leaderboard, with Steam) |
| `--wpm=N` | Difficulty as target words per minute: 25 Easy, 40 Normal (default), 65 Hard, 90 Extreme, 120 Insane. Any number works (the rules use it as characters per second, N x 5 / 60). On the menus the difficulty box sets it instead |
| `--cps=N` | The same in characters per second, as the rules and tuning.txt count it (Normal is 3.33) |
| `--seed=N` | Fix the random seed, for repeatable runs |
| `--autotype` | A bot types for you, at the difficulty's WPM. Skips the menus (straight into level 0) unless `--menu` is given, turns tutorial pop-ups off, and doesn't save unless `--profile` is given |
| `--botwpm=N` | With `--autotype`: the bot types at N WPM instead (lower = takes damage). `--botcps=N` is the same in characters per second |
| `--quit-on-end` | Quit when the level is cleared or lost |
| `--noglow` | Turn the glow (bloom) off, to compare |
| `--art=NAME` | Use an art set (a folder under game/assets/sprites with a player.png: `ai`, `custom`, ...) for this run, without saving it. In the game, F2 cycles the sets and the options have an ART SET row |
| `--menu` | With `--autotype`: start on the title menu anyway (the bot plays once you start a level) |
| `--map` | Start on the level map instead of the title |
| `--profile=NAME` | Use the save file `user://NAME.json` instead of `user://save.json` (keeps test runs out of your save) |
| `--keys=t:text,...` | Scripted input at given ticks: letters, digits and punctuation (except commas) are typed, `{space}`, `{comma}`, `{enter}`, `{esc}`, `{backspace}`, `{f1}`–`{f12}` press keys, `{click:XxY}` clicks at a stage position. E.g. `--keys=40:start,75:1,85:normal` |
| `--screenshot=path.png` | Save a screenshot after 10 ticks and quit (`--frames=N` for a different tick) |
| `--at=a,b,c` | With `--screenshot`: save `path_a.png`, `path_b.png`, ... at those ticks, then quit |
| `--speed=N` | Run N times faster (windowed runs; capped by frame rate) |
| `--edit` | The level-editing keys (F5 reload, F6 restart, F7/F8 back/ahead 10 s, F9 grid with each ship's line and time, F10 invincible, F11 bot on/off, F12 perfect or realistic bot) and a banner with the file, the level's time and the next line. Starts level 0 unless `--level` is given |
| `--overlay` | With `--edit`: start with the grid and flight paths showing (F9 toggles them) |
| `--from=N` | Start the level N% of the way in (the in-game progress meter's number), or `--from=1:37` at that time. Works with or without `--edit` |
| `--line=N` | Start the level at line N of its file (the first ship that line or a later one makes) |
| `--check` | Load and check every data file, print "Data OK" or the first mistake, and quit |
| `--trace=file.txt` | Write one line of game state per tick (positions, words, score), to compare two runs exactly |
| `--steam` | Use Steam in a checkout (Steam must be running): achievements unlock and endless scores go to the leaderboards, on Jeremy's account. Exported builds use Steam without it. Never with `--autotype` |
| `--nosteam` | Don't use Steam, even in an exported build |

A **release export** (ts 17, ts 18) ignores every option above except `--check` and `--nosteam`, and the
original's dev keys (Ctrl+Home invincible, Ctrl+Page Up unlock all, Ctrl+Page Down wipe the save) do nothing.
The editor and debug exports keep them all.

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

## Playtest logs

Every finished level or run adds a line to `runs.csv` next to the save (on Windows `%APPDATA%\Millennium Typer\`):
when, the build (version and commit, as on the title screen), the mode (level, endless or daily), the level or day,
the difficulty, cleared or lost, the score (before the score screen's bonuses), seconds, wave, health left, ships,
ships missed and accuracy. Ask playtesters to send it in with their feedback. Bot runs without `--profile` don't
write it.
