# Writing levels

Each level is a plain text file in `data/levels/`: `level00.txt` is level 1 on the map, up to `level11.txt` (B3).
The boss fights' extra ships are in `data/levels/bosses.txt`, and the numbers behind every ship (speeds, points,
word lengths, health) are in `data/tuning.txt`. Edit them in any text editor; no code needed.

| File | Map | | File | Map | | File | Map |
|---|---|---|---|---|---|---|---|
| level00 | 1 | | level04 | 4 | | level08 | 7 |
| level01 | 2 | | level05 | 5 | | level09 | 8 |
| level02 | 3 | | level06 | 6 | | level10 | 9 |
| level03 | B1 | | level07 | B2 | | level11 | B3 |

The lines are the original game's LevelData helpers, one per line, so the old scripts read almost the same:

```
missile sn 0.05  5,0 5,6 4,12
mirror
```

is the original's `missile("sn", 0.05, 5,0, 5,6, 4,12); mirror();`. Anything after `#` (or `//`) is a note and is
ignored. Blank lines are fine, and so is any amount of space between things.

## Trying a change

`ts 8` asks for a level, where to start and whether the bot should play, then opens the game with the editing keys
on. Edit the file, save, and press **F5** in the game: it reloads the files and restarts the level where you were.

| Key | |
|---|---|
| F5 | Reload the files and restart the level from the same point. If a file has a mistake, the message says which line, and the old version keeps running |
| F6 | Restart from your start point |
| F7 / F8 | Jump back / ahead 10% |
| F9 | Show or hide the grid and the ships' flight paths. Each ship is labelled with its line in the file (`L23`) |
| F10 | Invincible on or off, to watch a whole level |

The start point can be a percentage of the level (`50`, as on the in-game progress meter) or a line of the file
(`L40` starts with the first ship that line makes). `ts 9` checks every file without starting the game, and the
game itself refuses to start with a broken file, saying where the mistake is.

## The grid and distance

Positions are `x,y` on a **12 x 12 grid** over the play area: `0,0` is the top-left corner, `12,12` the bottom-right,
`6,0` the middle of the top edge. Fractions are fine (`1.5,0`), and so are points off the edge (`-1,5`, `13,8`) for
ships that start or leave out of sight. Ships aim for a random spot up to 10 pixels around each point inside the
screen, so no two flights are quite the same.

Every ship line has a **distance**: how far the player travels after the previous line before this ship appears.
`0` means at the same moment as the one before. The level's length is the sum. Distance passes at the difficulty's
speed, so a 0.1 gap is about 9 seconds on Normal. Gaps of 0.1 or more are multiplied by `1 - wpm / 300` (never by
less than a half; see `[level scripts]` in tuning.txt), so waves bunch up more the faster the difficulty.

## Ship types

Ships are written with letter codes. `missile`, `laser` and `suicide` take two letters, **size** then **defence**
(the command gives the weapon), and `ship` takes all three: size, weapon, defence.

| Size | | Defence | |
|---|---|---|---|
| `s` | small: 4-letter word | `n` | none |
| `m` | medium: 6 letters, 2 missiles to kill | `s` | shield: a 3-letter word knocks it out first |
| `l` | large: 8 letters, 4 missiles to kill | `t` | turret: shoots down player missiles near the ship |
| `x` | stealth: fades in, 7 letters | `b` | both |
| `n` | invisible laser (what boss 3 drops) | | |

Laser ships' words are a letter longer, and suicide ships always have 3 letters. The exact numbers are in
`data/tuning.txt`, which explains each one.

## Commands

**Ships**

| Line | What it does |
|---|---|
| `missile sn 0.05  5,0 5,6 4,12` | A missile ship: starts at 5,0, flies to 5,6 and on to 4,12. Up to three points after the start. It fires as it sets off for its last point. (With only one point after the start it then leaves through the top-left corner, as in the original.) |
| `laser sn 0.1  6,0` | A laser ship. It must start on an edge (x 0 or 12, or y 0 or 12): it moves 2 squares in, stops, charges, fires at the player and backs out. An optional number after the point makes it stop that much less |
| `suicide mn 0.14  3,0 3,3` | A suicide ship: flies to 3,3, then dives at the player. Small ones (`sn`) are slow mines |
| `stealth 0.05  6,7 6,0` | A stealth ship appears at 6,7, fires five missiles, then leaves for 6,0. Add `shield` at the end for a shielded one |
| `ship lmt 0.1  3,0  move 3,3  halt 15  halt 10 fire  move 0,3` | Any ship with its own flight plan: start at 3,0, then the steps |
| `boss 0.2 1` | Boss 1, 2 or 3 flies in. The level ends when it's dead |

A `ship` line's steps: `move X,Y` flies to a point; `halt N` stays put for N x 400 / wpm ticks (on
Normal N x 10 ticks, about 0.4 seconds each); either can end with `fire`, which attacks as that step starts (a
missile, a laser charge, or for suicide ships the dive); `attack` on its own attacks right away. The last step should take the
ship off screen, or it's counted as escaping where it ends.

**Copies.** These copy ship lines above them, which is how most waves are made.

| Line | What it does |
|---|---|
| `mirror` | The ship above, flipped left to right (5,0 becomes 7,0), at the same moment |
| `mirror 0.01` | ...after a gap of 0.01 |
| `clone 0.01` | The ship above again, 0.01 later, same path |
| `clone 0.01 -2` | The ship two lines up (`-1` is the one just above, the default; this also works for `mirror` and `offset`) |
| `offset 0.015  0,-2` | The ship above again, moved by 0,-2 (path and all), 0.015 later |
| `repeat 0.01 3 2` | The last 2 ships, again 3 times. Each copy comes 0.01 after the one before, keeping the gap between the 2 |
| `repeat 0.01 7 1 mirrored` | The last ship 7 more times, mirrored each time, so they alternate sides |
| `insert -4` | Moves the ship above earlier: it now arrives together with, and just before, the ship at -4 (counting the moved one as -1, as the copy commands count) |

`repeat` is `repeat DISTANCE [TIMES [SHIPS [mirrored]]]`: times and ships default to 1. Copies count as ships for
the copy commands after them, so `mirror` after a `repeat` mirrors the last copy.

**Timing.** `wait 0.1` adds a pause with no ship. (`jump` and `loop` exist from the original but no level uses them.)

**Random numbers.** Anywhere a number goes, `3~6` means any number from 3 to 6, and `0|12` means 0 or 12, picked
afresh each time the level starts. The boss escorts use these: `stealth 0.3  2~10,5~7 0|12,5`.

## bosses.txt

Four sections, each a list of ship lines like a level:

- `[boss 1 escorts]`, `[boss 2 escorts]`, `[boss 3 escorts]`: ships that come in during the boss fight. When the
  list runs out it starts again from where the player is, until the boss dies.
- `[boss 1 hangar]`: the six suicide ships boss 1 launches each attack. Their points are measured from the boss
  art's top-left corner rather than the screen's.

## Mistakes

The game checks every file when it starts (and on F5) and stops with a message naming the file and line, for
example:

```
levels/level04.txt, line 4: "misile" isn't a command. Did you mean "missile"?
levels/level02.txt, line 30: A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at 5,5.
levels/level07.txt, line 12: mirror copies the ship 1 back, but there are only 0 events above this line.
```

The same goes for tuning.txt: a misspelt or missing setting stops the game with the line to fix.
