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

Add a rule-check report to the pacing sim:

1. For every regular level × every difficulty, check each rule above and list violations with the rule, the measured value, the limit, and the timestamp (`m:ss`) where it occurs.
2. Print a summary of violations per level, and exit non-zero if any exist, so it can be used as a check after every level edit.
3. Recommended: add a second bot profile that types at 80% of P with a 2–5% error rate, to show where real players are likely to fail. Report it separately; the rules above are checked against the perfect bot.

## Required level changes

Timestamps refer to the current Hard run (P = 65, seed 1), before the duration and scaling changes. Apply these first, then convert the levels to fixed timing and fatter waves. After editing, re-run the sim and confirm each level passes every rule.

Where a note says load reaches 0, that means no new spawn for 10+ seconds; whether the screen was actually empty should be confirmed from words on screen.

### Level 1
- Opening is too slow: load sits at 0.17–0.29P from 0:03–0:09. Bring the first wave in around 0:04–0:05.
- Fill the dip at 0:20–0:23 (0.38P).
- Raise the average from 0.60P to about 0.67P.

### Level 2
- Slightly too easy. Increase density about 5–7% overall: average from 0.63P to about 0.67P.

### Level 5
- **1:17 lull:** not earned (previous bump peaks at 0.77P). Remove the dip; keep load ≥ 0.5P and let the bump flow into the 1:24 climb.
- **1:37 lull:** earned but slightly long (12 seconds below 0.5P). Shorten to about 9 seconds by bringing the 1:49 wave in about 3 seconds earlier.
- Also check the lulls at **1:03** (dips to 0.26P after a 0.95P bump) and **2:06** (12 seconds after a 0.91P bump); both currently fail the earned-lull rule.
- Expected result: average rises from 0.63P to about 0.66P.

### Level 6
- **1:31 lull:** load reaches 0 at 1:35–1:37 and the playtest showed an empty screen. Add a few short words in this stretch; shorten the lull to ≤ 10 seconds.
- **1:08 lull:** 8 seconds below 0.5P after about 3 seconds above 1.0P; trim by about 1 second.
- Do not change the ending.

### Level 7
- Overall pacing and ending are good; only shrink the big lulls.
- **2:15 lull:** 11 seconds below 0.5P, load at 0 from 2:16–2:20, after a 0.72P bump. Shrink to about 4 seconds with no dip below 0.3P and no empty screen.
- **1:39 lull:** 9 seconds, dips to 0.11P after a 0.94P bump. Shrink to about 4 seconds with no dip below 0.3P.
- **0:41 lull:** earned, but trim from 9 to about 7 seconds.
- Do not change the ending.

### Level 9
- **1:30 lull:** about 10 seconds below 0.3P, load at 0 from 1:36–1:38. Bring the next wave in about 5 seconds earlier (start around 1:35), or add 1–2 short words around 1:35.
- **Last two peaks:** raise the 1:22 peak from 1.11P to about 1.30P and the 1:50 peak from 1.28P to about 1.45P. Each may stay above 1.0P for up to about 8 seconds.
- **Keep the early peaks** (1.42P at 0:17, 1.35P at 0:39) as they are.
- Expected result: average about 0.73P.

### Level 10
- Lower the 1:10 spike from 1.69P to about 1.6P, in line with the 1:05–1:08 plateau (1.55–1.57P).
- Do not change the 1:05 climb or anything else.

### Level 11 — trim to fit all rules
- **Opening, 0:07–0:41:** about 35 seconds almost entirely above 1.0P, with peaks of 1.58P and 1.55P. Split into two waves with a short, earned lull around 0:20; keep peaks ≤ about 1.3P.
- **1:32–1:50:** about 18 seconds above 1.0P. Trim to ≤ 13 seconds or split.
- **2:07 spike:** lower from 1.63P to 1.6P. Make the release clean: load below 0.65P from about 2:14 to 2:19 (currently 0.71P at 2:16 breaks it).
- **Final wave, 2:42–2:51:** raise to about 1.4P so the final peak beats every first-half peak.
- **Overall:** cut about 15% of words (≈ 176 → 150 at Hard), mainly from the two long sustained stretches. Target average about 0.80P, with the last spawn at about 2:55.

### Levels 3 and boss levels
- **Level 3:** no rule changes required beyond duration and scaling. Its last peak is at 1:11, followed by about 80 seconds at 0.6–0.9P; playtest whether the final minute holds up.
- **Levels 4, 8, 12:** exempt from all rules, including duration; no changes.

## How the checker reads these rules

Details the spec leaves open, as `src/Core/PacingRules.cs` implements them:

- **Times** are m:ss from the start of the level, counting each second from its start (the sim's second 1 is 0:00).
  The checker's timestamps match the ones in "Required level changes".
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
