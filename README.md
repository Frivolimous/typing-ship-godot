# Millennium Typer

A typing shoot-'em-up: enemy ships fly in carrying words, and typing a word fires at its ship. Originally a
2011 Flash game (AS3, project name "Typing Ship"), now being rebuilt in Godot 4 / C# for a Steam release.

**Status:** early port. Level 0 plays start to finish: typing, missiles, laser charges, damage, hull repair,
scoring, death and level completion. Levels 1–11, bosses, menus and save data are still to come. See
[docs/porting.md](docs/porting.md).

## Requirements

| Tool | Version |
|---|---|
| Godot, .NET edition | 4.7.2 stable mono |
| .NET SDK | 10.0.x (pinned in `global.json`); the game targets net8.0 |

Godot is expected at `C:\Tools\Godot_v4.7.2-stable_mono_win64\`. Set the `GODOT` environment variable to point
the scripts somewhere else.

Music isn't in git. Copy the tracks into `assets/music/` as described in
[assets/music/README.md](assets/music/README.md). Without them the game runs with sound effects only.

## Running

Double-click `ts.bat` for a menu of the everyday commands, or:

```bash
dotnet build
"C:/Tools/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe" --path .
```

In game: type the words to shoot, Backspace to correct, Escape to pause, Space to replay after a level ends.
Typing the long word on the hull bar repairs one point of damage.

Every command, including the dev options (auto-typing bot, headless simulation, screenshots), is in
[docs/commands.md](docs/commands.md).

## Layout

```
project.godot, TypingShip.csproj   Godot project (C#)
scenes/main.tscn                   entry scene; everything else is built in code
scripts/
  Core/        globals (G), sprites, fonts, word list
  Game/        game loop, weapons and scoring, typing input, run state
  Level/       level scripts and their events
  Entities/    ships, missiles, turret, shields, charges, words
  Fx/          starfield, particles, beams, score pops
  UI/          in-game HUD
  Audio/       sound effects and music
  Dev/         auto-typing bot
assets/        sprites, sfx, music (extracted from the original .fla, named by their old AS3 class names)
data/          words.txt (word lists by length)
docs/          porting status, commands, release checklist
reference/     the original AS3 source, read-only, for porting
```

## Credits (original game)

- Programming and design: Yermiyah Hornick
- Art: Avi Kentridge
- Music: Binyamin Hornick
- Design consultant: Xelu
