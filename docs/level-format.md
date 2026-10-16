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

**Classic levels.** `data/levels/classic/` holds the original 12 levels and their bosses.txt, converted to this
format at Hard's timing: a baseline to compare against while the game's own levels are tuned (play them with
`--classic`, see [commands.md](commands.md)). They're removed before release. A test checks they still play as
recorded, so leave them alone; edit the ones in `data/levels/`. For now those are still copies of the Classic ones.

## Time

**Every time in a level file is in seconds, as the level plays on Hard, and the timing is the same at every
difficulty.** Hard is the reference: levels are written and tuned for Hard first; the other difficulties are worked
out from Hard later.

A level is a list of **waves**. Each wave starts with an absolute time, `@m:ss`, and the lines in it use gaps:

```
@0:09.28 missile sn  0,3 7,3 12,3      # at 9.28 s into the level
+0.55 missile sn  12,6 5,6 0,6         # 0.55 s after the line above
mirror                                 # no time: the same moment as the line above
repeat 0.55 3 2                        # three more copies of the last two ships, 0.55 s apart

@0:17.49 laser sn  6,0                 # the next wave
```

- `@m:ss` is minutes and seconds from the start of the level, with decimals if you like: `@0:07`, `@1:37.5`.
  The `@` times must go in order; the game refuses a file where one is earlier than the one before.
- `+0.6` is a gap in seconds after the line above (any line with a time of its own).
- No prefix means the same moment as the line above.

The level lasts until its last wave is over: there is no length to set and nothing is rescaled. The progress meter
in the game counts up to the last ship.

Times are seconds on Hard, but they're the same at every difficulty, so on Hard a level's timing is exactly what
the file says. What does change with the difficulty is how fast ships fly, charge and fire, and how long a `halt`
lasts (see Commands).

## Trying a change

`ts 8` asks for a level, where to start and whether the bot should play, then opens the game with the editing keys
on. Edit the file, save, and press **F5** in the game: it reloads the files and restarts the level where you were.

| Key | |
|---|---|
| F5 | Reload the files and restart the level from the same time. If a file has a mistake, the message says which line, and the old version keeps running |
| F6 | Restart from your start point |
| F7 / F8 | Jump back / ahead 10 seconds |
| F9 | Show or hide the grid and the ships' flight paths. Each ship is labelled with its line in the file and the time it came in (`L23 1:37.4`) |
| F10 | Invincible on or off, to watch a whole level |
| F11 | The bot on or off: let it play a stretch, then switch it off and type yourself (or the other way round). It types at the difficulty's speed (or `--botwpm`) |

The start point can be a time (`1:37`), a percentage of the level (`50`, as on the in-game progress meter) or a
line of the file (`L40` starts with the first ship that line makes). The banner shows the level's time and the
next line to come in. `ts 9` checks every file without starting the game, and the game itself refuses to start
with a broken file, saying where the mistake is.

The pacing report (`ts 11`) writes `pacing/lines.csv`: the time each line of each level happens, like `L42 -> 1:37.1`.

## The grid

Positions are `x,y` on a **12 x 12 grid** over the play area: `0,0` is the top-left corner, `12,12` the bottom-right,
`6,0` the middle of the top edge. Fractions are fine (`1.5,0`), and so are points off the edge (`-1,5`, `13,8`) for
ships that start or leave out of sight. Ships aim for a random spot up to 10 pixels around each point inside the
screen, so no two flights are quite the same.

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

Every command below can start with a time (`@m:ss` or `+gap`) except `repeat` and `insert`.

**Ships**

| Line | What it does |
|---|---|
| `missile sn  5,0 5,6 4,12` | A missile ship: starts at 5,0, flies to 5,6 and on to 4,12. Up to three points after the start. It fires as it sets off for its last point. (With only one point after the start it then leaves through the top-left corner, as in the original.) |
| `laser sn  6,0` | A laser ship. It must start on an edge (x 0 or 12, or y 0 or 12): it moves 2 squares in, stops, charges, fires at the player and backs out. An optional number after the point makes it stop for less (in the original's halt units) |
| `suicide mn  3,0 3,3` | A suicide ship: flies to 3,3, then dives at the player. Small ones (`sn`) are slow mines |
| `stealth  6,7 6,0` | A stealth ship appears at 6,7, fires five missiles, then leaves for 6,0. Add `shield` at the end for a shielded one |
| `ship lmt  3,0  move 3,3  halt 3.8  halt 2.5 fire  move 0,3` | Any ship with its own flight plan: start at 3,0, then the steps |
| `boss 1` | Boss 1, 2 or 3 flies in. The level ends when it's dead |

A `ship` line's steps: `move X,Y` flies to a point; `halt S` stays put for S seconds on Hard (longer on easier
difficulties and shorter on harder ones, in step with the ships' speed); either can end with `fire`, which attacks
as that step starts (a missile, a laser charge, or for suicide ships the dive); `attack` on its own attacks right
away. The last step should take the ship off screen, or it's counted as escaping where it ends.

**Copies.** These copy ship lines above them, which is how most waves are made.

| Line | What it does |
|---|---|
| `mirror` | The ship above, flipped left to right (5,0 becomes 7,0), at the same moment |
| `+0.55 mirror` | ...0.55 s later |
| `+0.55 clone` | The ship above again, 0.55 s later, same path |
| `clone -2` | The ship two lines up (`-1` is the one just above, the default; this also works for `mirror` and `offset`) |
| `+0.8 offset  0,-2` | The ship above again, moved by 0,-2 (path and all), 0.8 s later |
| `repeat 0.55 3 2` | The last 2 ships, again 3 times. Each pair comes 0.55 s after the one before, keeping the gap between the 2 |
| `repeat 0.55 7 1 mirrored` | The last ship 7 more times, mirrored each time, so they alternate sides |
| `insert -4` | Moves the ship above earlier: it now arrives together with, and just before, the ship at -4 (counting the moved one as -1, as the copy commands count) |

`repeat` is `repeat GAP [TIMES [SHIPS [mirrored]]]`, with the gap in seconds; times and ships default to 1. Copies
count as ships for the copy commands after them, so `mirror` after a `repeat` mirrors the last copy.

**Other.** `@1:30 wait` marks a moment with no ship (rarely needed now that waves have times). `jump` and `loop`
exist from the original but no level uses them.

**Random numbers.** Anywhere a number goes, `3~6` means any number from 3 to 6, and `0|12` means 0 or 12, picked
afresh each time the level starts. The boss escorts use these: `stealth  2~10,5~7 0|12,5`.

## The header: named numbers

The top of a file, before the first command, can name numbers to use anywhere a number goes:

```
HOVER = 3.8
SWARM_GAP = 0.9/0.9/0.7/0.6/0.5

@0:05 ship lmn  4,0  move 4,3  halt HOVER  move 0,3
+SWARM_GAP clone
repeat SWARM_GAP 4
```

- A name is letters, digits and `_` (capitals are the convention). Set each once.
- One value (`HOVER = 3.8`) works the same at every difficulty. A range or choice (`2~4`, `0|12`) is rolled where
  it's used.
- **Five values separated by `/` (Easy to Insane) are allowed only in the header**, as a deliberate exception:
  that's the one way a number can differ by difficulty. A line can't have `1/2/3/3/4` written in it directly.

## bosses.txt

Four sections, each a list of ship lines like a level:

- `[boss 1 escorts]`, `[boss 2 escorts]`, `[boss 3 escorts]`: ships that come in during the boss fight. When the
  list runs out it starts again from the current moment, until the boss dies. Their `@` times count from when the
  batch starts, not from the start of the level.
- `[boss 1 hangar]`: the six suicide ships boss 1 launches each attack. Their points are measured from the boss
  art's top-left corner rather than the screen's.

## Removed

These were part of the format before times moved to seconds, and the game now refuses them with a message saying
what to do: `pace`, `duration`, `density`, difficulty tags (`hard+:`), `only` blocks, and `/` values in a line
(use a header constant). Thinning ships within each wave for the easier difficulties will come with the difficulty
pass, after the Hard levels are tuned.

## Mistakes

The game checks every file when it starts (and on F5) and stops with a message naming the file and line, for
example:

```
levels/level04.txt, line 4: "misile" isn't a command. Did you mean "missile"?
levels/level02.txt, line 30: A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at 5,5.
levels/level07.txt, line 12: @0:40 is earlier than the @ time on line 9. @ times must be in order.
```

The same goes for tuning.txt: a misspelt or missing setting stops the game with the line to fix.

## How the old files were converted

The levels were converted from the old distance-based format by the engine itself, at Hard: each gap became the
seconds it took on Hard (the old 0.1-and-over gap shrink included), rounded to hundredths. Every ship arrives within
one tick (1/24 s) of when it did before, and 82% on exactly the same tick. Halts were converted so they last
exactly as many ticks as before at every difficulty, and ships' paths and random numbers are unchanged.

## The typing test

The typing test's passages are in `data/typing-test.txt`, one paragraph each, separated by blank lines; the test
picks one at random. Keep them to about 250 characters (150 to 300 is allowed) of plain keyboard characters. The
file explains the rest.
