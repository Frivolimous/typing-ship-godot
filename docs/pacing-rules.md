# Millennium Typer — Level Pacing Rules

Spec for level pacing. Rules were derived from playtesting the Hard difficulty (stated 65 WPM) against the pacing sim (`dotnet run --project src/Sim -- pacing`, seed 1). All rules are expressed relative to the player's WPM so they apply to every difficulty.

**Checking:** `dotnet run --project src/Sim -- pacing --check` (`ts 13`) checks every regular level at every difficulty and
exits non-zero on any violation. The numbers below live in [data/pacing-rules.txt](../data/pacing-rules.txt), which
is what the checker reads: change a number there, not here, and keep this file in step. Exemptions are listed there too.

## Definitions

- **P** — the difficulty's stated WPM (the player's speed). All thresholds are fractions of P (e.g. `0.3P` = 30% of P).
- **Demand** — the sim's existing metric: letters of every **new** word that appears (ships, missiles, turrets, boss words) over the **last 10 seconds** (rolling window), as WPM at 5 letters per word. Lull, intensity, peak and arc rules are measured on this series, at 1-second resolution.
- **Load** — demand ÷ P. Load 0 means no new word has spawned in the last 10 seconds; earlier words may still be on screen.
- **Words on screen** — the report's existing per-second count of words currently on screen and typeable.
- **Empty screen** — words on screen = 0.
- **Burst** — a contiguous run of seconds where load ≥ 0.5.
- **Lull** — a contiguous run of seconds where load < 0.5.
- **Peak** — the maximum load within a burst. Its time is the second at which that maximum occurs.
- **Last spawn** — the second at which the level's final word spawns.

## Scope and exemptions

- **Boss levels (4, 8, 12) are exempt from every rule in this document, including duration.** Boss fights keep their current difficulty-dependent length (e.g. level 12 runs about 4:09 on Easy and 1:31 on Insane). This is intended.
- **Intro exemption:** ignore the first 8 seconds of each level when checking the lull, sustained-intensity, peak and arc rules.
- **Outro exemption:** ignore everything after the last spawn when checking those same rules. (Demand only falls after the last spawn, and ship speed varies by difficulty, so this stretch says nothing about pacing.)
- **Level 3 is exempt from the level arc rule (7) for now** (Jeremy, 2026-10-15): its last peak comes early. Revisit after playtesting.
- Rules apply to regular levels (1–3, 5–7, 9–11) at **every difficulty**.

## Scaling: fixed timing, fatter waves

Difficulty scales three things. The level's timing structure does not change.

**Fixed at every difficulty:**
- **Wave start times, wave lengths and lull lengths**, in seconds. Waves do not come more often at higher difficulty.

**Scales with difficulty:**
- **Words per wave scale with P.** Faster players get more words in the same wave, so spawns within a wave are closer together.
- **Ship movement speed** (existing behavior, kept).
- **Attack speed** (existing behavior, kept): how often enemies fire and how fast their attacks travel.

```
words ≈ average_load × P × spawn_duration_minutes
```

How each one interacts with these rules:

- **Attack frequency counts toward demand.** Missiles and other attacks carry words, and demand already includes them. When sizing waves, budget for the attack words as well as the ship words: a wave that is on target with ships alone will run over once faster attacks add their words. Check the total in the sim, not the ship words alone.
- **Movement speed and projectile speed do not show up in demand.** They change how long a word stays typeable before it reaches the player (time-to-impact), not how many letters arrive. Higher difficulties are therefore harder than their load numbers suggest. The rules don't limit this; keep the current speed scaling, and if a difficulty feels harder than its load numbers, check time-to-impact before changing wave sizes.
- **Movement speed does not affect duration**, because duration ends at the last spawn plus a fixed tail (rule 1).

## Rules

### 1. Duration

Duration is measured from level start to the last spawn, plus a fixed tail:

```
level_duration = last_spawn_time + TAIL
TAIL = 5 seconds (constant, the same at every difficulty; adjust if needed)
```

The time ships take to leave the screen after the last spawn is not part of the duration, so ship speed differences between difficulties don't affect it.

Target:

```
level_duration = 90 + 9 × (level − 1) seconds
→ last_spawn_time = 85 + 9 × (level − 1) seconds (with TAIL = 5)
```

| Level | 1 | 2 | 3 | 5 | 6 | 7 | 9 | 10 | 11 |
|---|---|---|---|---|---|---|---|---|---|
| Duration | 1:30 | 1:39 | 1:48 | 2:06 | 2:15 | 2:24 | 2:42 | 2:51 | 3:00 |

### 2. Average load

- Average load over **level start → last spawn + TAIL** (including intro and lulls) must be **0.65–0.80**.
- Target rises with level: `target = 0.65 + 0.015 × (level − 1)` (level 1 ≈ 0.65, level 11 ≈ 0.80).

### 3. Lulls

- **Below 0.3P:** at most **7 seconds** at a time.
- **Empty screen:** at most **3 seconds** at a time (measured on words on screen, not demand).
- **Below 0.5P:** at most **25%** of the level's checked time.

### 4. Earned lulls

A lull must be earned by the burst immediately before it.

- A lull may only drop **below 0.3P** if the previous burst's peak was **above 1.0P**.
- A lull's length (seconds below 0.5P) must be at most:

```
max_lull_seconds = min(10, 4 + seconds_previous_burst_spent_above_1.0P)
```

### 5. Sustained intensity

- **Above 1.0P:** at most **13 seconds** at a time.
- **Above 1.2P:** at most **20%** of the level's checked time.

### 6. Peaks

- Normal peaks: at most **1.5P**.
- **One spike per level** may reach up to **1.6P**, if:
  - it stays above 1.5P for at most **5 seconds**, and
  - it is followed by a **release**: load falls below 0.65P within 5 seconds after the spike and stays below 0.65P for at least 5 seconds.
- There is no limit on how fast load can climb into a peak.

### 7. Level arc

- The level's **highest peak** must occur in the **second half** of the level.
- The level's **final peak** must be **higher than every peak in the first half**.
- Early peaks can be high (early challenge is fine); the ending just has to top them.

### 8. Pressure

Added after the Hard pass (2026-10-18), because load can't see threats bunching up: a wave of mines with 3-letter
words barely moves load, but if they all dive at once they all have to be typed in the same few seconds.

- **Threats** are what hurts the player: enemy missiles, suicide ships (mines included) and laser shots. They're
  measured in a run where nobody types, so every threat plays out: when its word first shows on screen, when it hits,
  and the letters it takes to stop (its word, plus the main word under a shield).
- **Pressure** is the slowest steady typing speed that stops every threat before it hits, typing the most urgent first:
  over each stretch of time, the letters of the threats that both appear and hit inside it, divided by its length. Each
  second shows the worst stretch of up to 20 s around it, as a share of P. A typist slower than that gets hit there.
- **Pressure above 1.0P** fails: even a perfect typist at P can't stop everything in time.
- **Pressure above 0.85P** warns: a typist a little under P, or one who mistypes, gets hit there.

It's an upper bound in one way (ships aren't shot down before they fire, so every missile they'd launch counts) and
generous in another (no reaction time, and no missile flight time after a laser ship's word is typed). Checked against
the realistic bot on the Hard pass levels: where pressure rose above the bot's share of P, the bot took its hits.

## Validation

`pacing --check` (`ts 13`) checks every regular level at every difficulty against the rules above, lists each
violation with the rule, the measured value, the limit and the time (`m:ss`), summarises them per level and exits
non-zero if there are any. The pacing report (`ts 11`) also plays a realistic bot (80% of P, 3.5% mistakes) and
reports it separately, to show where real players are likely to fail; the rules are checked on the perfect bot.

The level changes this spec first asked for (levels 1, 2, 5, 6, 7, 9, 10 and 11, from the 2026-10 playtest) were
made in Jeremy's hand-tuning of the Hard levels; see git history for the list.

## How the checker reads these rules

Details the spec leaves open, as `src/Core/PacingRules.cs` implements them:

- **Times** are m:ss from the start of the level, counting each second from its start (the sim's second 1 is 0:00).
  The checker's timestamps match the pacing report's.
- **Empty screen** counts only words the player can see: ships inside the view and fully faded in. Ships that have
  spawned but not yet flown into view don't count, even though their words can already be typed.
- **Peaks** are one per burst. On a plateau, the peak's time is the plateau's last second. A level whose load never
  drops below the burst level is one burst with one peak.
- **Spikes** are counted per stretch above the peak cap, so two spikes inside one burst are two spikes.
- **Duration** passes within ± "duration tolerance" (3 s) of the target. The tail is "duration tail" in data/pacing-rules.txt.
- **Average load** is one definition, shared with the pacing report: the letters of every word that appears from the
  start of the level to the last spawn plus the tail, divided by that time and by P.
- **Average target:** outside 0.65–0.80 is a violation; inside it but more than "average target tolerance" (0.03)
  from the level's target is a warning, which doesn't fail the check.
- **Perfect bot:** every rule is checked on the perfect bot's run (seed 1). The realistic bot (80% of P, 3.5%
  mistakes, `--bot-speed` and `--errors` change them) is reported separately in `realistic.csv` and the check's
  last lines. The perfect bot clears words the moment it can type them, so it sees more empty screen than a real
  player would; if the empty-screen rule proves too strict, measure it on the realistic bot instead.
