# Millennium Typer

A typing shoot-'em-up: enemy ships fly in carrying words, and typing a word fires at its ship. Originally a
2011 Flash game (AS3, project name "Typing Ship"), now being rebuilt in Godot 4 / C# for a Steam release.

**Status:** all the gameplay is ported: levels 0–11 with their three bosses, every enemy type, hull repair,
scoring, death and the cheat words, and so are the menus, score screens and save data. Levels and tuning are
text files anyone can edit ([docs/level-format.md](docs/level-format.md)). See [docs/porting.md](docs/porting.md).

## Requirements

| Tool | Version |
|---|---|
| Godot, .NET edition | 4.7.2 stable mono |
| .NET SDK | 10.0.x (pinned in `global.json`); the game targets net8.0 |

Godot is expected at `C:\Tools\Godot_v4.7.2-stable_mono_win64\`. Set the `GODOT` environment variable to point
the scripts somewhere else.

Music isn't in git. Copy the tracks into `game/assets/music/` as described in
[game/assets/music/README.md](game/assets/music/README.md). Without them the game runs with sound effects only.

## Running

Double-click `ts.bat` for a menu of the everyday commands, or:

```bash
dotnet build
"C:/Tools/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe" --path game
```

In game: type the words to shoot, Backspace to correct, Escape to pause, Space to replay after a level ends.
Typing the long word on the hull bar repairs one point of damage.

Every command, including the dev options (auto-typing bot, headless simulation, screenshots), is in
[docs/commands.md](docs/commands.md).

## Layout

```
TypingShip.sln     the solution: dotnet build / dotnet test from here
src/Core/          the rules, plain C# (no Godot): game state and tick, ships, weapons, typing, bosses,
                   level files, tuning, save data, the auto-typing bot
tests/Core.Tests/  xUnit tests of the rules and data files
game/              the Godot project (presentation and input)
  scenes/main.tscn   entry scene; everything else is built in code
  scripts/           G (globals), the views of the rules' objects, effects, HUD, menus, sound, dev tools
  assets/            sprites, sfx, music (extracted from the original .fla, named by their old AS3 class names)
data/              words.txt, tuning.txt, levels/ (Jeremy edits these: see docs/level-format.md)
docs/              porting status and roadmap, commands, level format, release checklist
reference/         the original AS3 source, read-only, for porting
```

## Credits (original game)

- Programming and design: Yermiyah Hornick
- Art: Avi Kentridge
- Music: Binyamin Hornick
- Design consultant: Xelu
