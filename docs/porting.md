# Roadmap and porting status

The original AS3 is in [../reference/original-as3/](../reference/original-as3/). This file tracks what has been
ported, what's left, where the port deliberately differs from the original, and the phases after the port.

## Milestones

1. ~~**Vertical slice:** level 0 playable end to end.~~ Done.
2. ~~**Full gameplay:** levels 1–11, bosses (3, 7, 11) with `bossLoop`, scanner, scramble objects, superman and
   chameleon cheats; shields, turrets, stealth ships, suicide ships and mines verified.~~ Done: every level clears
   with the bot at all five difficulties (seeds 1–5). Difficulty and feel still need Jeremy to play them.
3. ~~**Game shell:** title menu, level map, difficulty select, typing test, pause/mute, score screen, game over,
   victory, credits, transitions, tutorial pop-ups, achievements, save data (best score per level, unlocks).~~ Done:
   every screen checked in windowed screenshot runs (`--keys` walks the menus), and the bot sims are unchanged.
   The menus can be driven by typing as well as the mouse (see Deviations).
4. ~~**Refactor and level editor:** rules move into a plain C# core with unit tests. Level scripts and tuning
   numbers move into data files that Jeremy can edit without touching code. Add tools to make editing quick:
   reload a level without restarting, start from any point in a level, and show the 12x12 spawn grid and ship
   paths in game.~~ Done. The rules are in src/Core with xUnit tests (tests/Core.Tests); levels are data/levels/*.txt
   (format: [level-format.md](level-format.md)) and the numbers data/tuning.txt. The level files were proven to
   build exactly the old C# scripts' events (every level, WPM 1–150, several seeds, same random draws; the test and
   the old scripts are in commit 8bdb8a9, deleted after), and 240
   headless bot runs (levels 0–11 x 5 difficulties x 3 seeds, plus a slow bot) give per-tick traces byte-identical
   to the pre-refactor game. After that proof the presentation got random numbers of its own, so a given `--seed`
   now plays out differently from before (same odds, different draws), and the same as in the Core-only simulator,
   tick for tick. Editing: `--edit` (F5 reload, F6–F8 restart and jump, F9 grid and paths, F10
   invincible), `--from`, `--line`, `--check`; `ts 8`–`10`. Checked with windowed walk-throughs of every screen and of
   the editing keys (including F5 after an edit, and F5 on a broken file). Level design itself is for Jeremy to try.
5. **CPS model, typing test and pacing rework** (replaces "difficulty review"):
   - **CPS in code, WPM for players.** The rules and tuning work in characters per second
     (CPS = WPM × 5 / 60); every player-facing number stays WPM. **Done:** `Game.Cps`, `Difficulty.Cps`, the
     `Speed` helpers (src/Core/Playfield.cs) and tuning.txt in CPS units. The Classic levels play tick for tick as
     before: `ClassicTraceTests` checks 180 bot runs (every level and difficulty, two seeds and a slow bot)
     against hashes of their traces recorded before the change (tests/Core.Tests/classic-traces.txt), and headless
     `--trace` runs give the same hashes. Where CPS arithmetic rounds differently from the original's whole-number
     WPM arithmetic, `Speed` keeps the original's result (whole ticks, missile speeds); see its comments.
   - **Keep the original calibration.** The original pacing was balanced on what real people at a stated WPM
     could manage, in a sawtooth difficulty curve. The pacing report (`ts 11`) shows the Classic levels'
     average demand at about 70–76% of the stated WPM (across all 12 levels), with 10-second peaks from about
     2.2× on Easy down to 1.3× on Insane. Whatever the rework does,
     those stated-to-true ratios hold at each difficulty.
   - **Typing test:** prose of about 250 characters with a 90 s cap, written as lore that introduces the
     game. "Fun and close enough" matters more than matching in-game typing exactly. Timer starts on the first
     keystroke; it reports net WPM and accuracy. **Done:** five passages in data/typing-test.txt (all kept by
     Jeremy; the test picks one at random), rules in src/Core/TypingTestRun.cs, the screen in
     UI/Screens.cs. The suggestion keeps the old mapping (the hardest difficulty at or below the result).
   - **Difficulty selection:** open to experiments (Jeremy iterated on it a lot in the original).
   - **Pacing rules** ([pacing-rules.md](pacing-rules.md), Jeremy, from playtesting Hard): durations, average load
     0.65–0.80, lulls, earned lulls, sustained intensity, peaks and the level arc; boss levels exempt, level 3 exempt
     from the arc rule for now. **Done:** the checker (`ts 13`, `pacing --check`, numbers in data/pacing-rules.txt)
     and a realistic bot (80% speed, 3.5% mistakes) in the report. The rules themselves get revised after the Hard
     pass, from what the hand-tuned levels actually do.
   - **A first rework was scrapped** (branch level-pacing-rework, not merged): Jeremy wasn't happy with the
     levels, and the distance-based format with pace/duration/density lines made them hard to reason about.
   - **Hard pass (now).** Back to basics, with Hard as the reference:
     1. ~~**Time-based level format.**~~ Done. Waves start at `@m:ss`, lines within use `+gap` seconds, no prefix is
        the same moment; `halt`, `repeat` gaps and every other time are seconds as played on Hard. Timing is the
        same at every difficulty (no pace line); a level's length is its content. A header can name numbers
        (`HOVER = 3.8`), and per-difficulty values (`0.9/0.9/0.7/0.6/0.5`) are allowed only there. Removed: `pace`,
        `duration`, `density`, difficulty tags, `only` blocks and inline `/` values. Halts convert to other
        difficulties with the cps scaling they always had. See [level-format.md](level-format.md).
     2. ~~**Conversion.**~~ Done, by the engine at Hard, for the game's levels and Classic alike (both are the
        original levels for now). Each gap became its Hard seconds, rounded to hundredths: all 928 events arrive
        within one tick (1/24 s) of before, 82% on the same tick; Classic Hard runs last the same and spawn the same
        ships as before. Halts, paths and random draws are identical at all five difficulties (checked against
        a capture of the old engine). `repeat` gaps convert cleanly; the old quirk where a repeated group's inner
        gap of 0.1 or more shrank again never occurs in the levels.
     3. ~~**Tools.**~~ Done: the report's `lines.csv` and the F9 overlay give each line's Hard time (`L42 -> 1:37`);
        the report has words on screen per second and each level's last spawn; the report and the rule check share
        one definition of average load (letters from the start to the last spawn plus the tail, over that time).
     4. **Jeremy hand-tunes the Hard levels** in data/levels/. Once he's done, they aren't changed to satisfy the
        rule check.
   - **Difficulty pass (after Hard).** Derive the other difficulties from Hard, then adjust by hand. Until then
     every difficulty plays Hard's wave timing (with its own ship speeds), so Easy and Normal are much too dense.
     **TBD:** thin ships within each wave rather than dropping whole waves, so lower difficulties keep Hard's peaks
     and lulls in the same places. If Insane still plays too hard after that, flatten its ship speed curve slightly
     (e.g. cps^0.9) rather than cut density. Tuning times could also be written as seconds on Hard (laser charge
     ≈ 3.8 s) to match the level files.
   - **Classic:** the original 12 levels stay available as a baseline and check during development, and are
     removed before release. **Done:** data/levels/classic/ (`--classic`, `ts 12`), converted to seconds on Hard
     like the rest; data/levels/ is the game's
     own set, for now unchanged copies.
6. **New features and deeper game logic:** new mechanics, enemies, modes and systems beyond the original (the
   old TODO list in Facade.as is a starting point: dodging, tractor beam, mega bomb, boss AI, space mines).
   This comes before the visual overhaul so the final art covers the new content too. Decided so far:
   - **Endless mode:** a wave generator built from authored wave patterns, with density ramping over time.
     Steam leaderboards bracketed by CPS. No anti-cheat at release; patch later if cheating becomes a problem.
   - **Daily challenge:** one seed per UTC day, so everyone gets the same wave pattern (words can differ,
     because typed words return to the pool). Unlimited attempts, best score counts, one leaderboard per day.
     Daily modifiers ("lasers only", "double shields") come later.
   - **Custom word lists and language packs:**
     - Validate lists: every word length the ships use (3–10) needs enough words, at least about 6 and
       ideally 20 or more.
     - Accented Latin, Cyrillic and Greek are fine. Languages typed through an input method (Chinese, Japanese,
       Korean) don't work with matching one keystroke at a time.
     - Case-folding needs care per language (e.g. Turkish i).
     - The final font must cover the supported alphabets.
     - Cheat words (superman, pause, mute, ...) can clash with list words; move them or localise them.
     - Only official packs count on leaderboards.
     - Check the licence of every word source.
     - Ship a set of pre-selected lists, and use Steam Workshop for sharing.
     - Word packs only: no menu translation.
   - Larger word lists are needed anyway: higher densities run the current 9- and 10-letter lists dry.
7. **Visual overhaul** (done for now: the backdrops, nebulae and superman sprite stay the original art until an art pass;
   the old Flash ship, missile and menu art is deleted): new art and fonts, side-panel HUD and decoration, effects (shaders, particles, screen
   shake), and UI polish. Make it look as good as possible. **Started:** AI-generated placeholder art for the
   player, the enemies, the missiles and the three bosses (game/assets/sprites/ai/; the bosses are animated in code:
   hangar doors, gun glow, cannon charge, beam); engine flames and laser charge points placed by data/mounts.txt; new explosions (flash,
   shockwave, fireballs, sparks, smoke) and a three-layer starfield. The cockpit frame is now drawn in code in the
   original's shape, with screens for the score (counting up), typed text, hull pips and progress, in bundled OFL fonts.
   Also restyled to match: the pause screen, tutorial briefings, the achievement pop-up (a toast above the cockpit)
   and the score pop-ups; banners mark a level's start, a boss's arrival and a cleared level; the player's death is a
   short sequence of explosions ending in one big blast; and a bloom (HDR 2D, Fx/ScreenFx.cs) makes overbright light
   glow. The "Low Health!" text is gone: the cockpit rim and the hull pips show it.
8. **Placeholder cleanup:** ~~replace the Flash-portal leftovers ("submit high score", "display high score", the
   sponsor link and "More games") with real features or remove them.~~ Removed (Jeremy, with the screens restyle);
   the title shows the best total score instead. Leaderboards come with Steam (phase 9).
9. **Steam integration:** Steamworks, achievements, cloud saves, Steam Deck support and store builds, plus
   the endless and daily leaderboards and Workshop word lists from phase 6. See
   [release-checklist.md](release-checklist.md).

## Class map

Rules (`src/Core/`, namespace `TypingShip.Core`, no Godot) are split from presentation (`game/scripts/`).

| AS3 | Rules (src/Core) | Presentation (game/scripts) |
|---|---|---|
| Facade | Rng.cs (Math.random, as Godot's PCG32), Playfield.cs (sizes, difficulties) | Core/G.cs (globals) |
| GameModel | Game.cs (run state), SaveData.cs (the SharedObject: JSON, version 10, wiped if different, as before) | Core/SaveFile.cs (user://save.json) |
| GameControl | Game.cs: `Step`, `Tick`, boss flow (`bossMode` escorts, `killBoss`, fade-out), death, pause | Game/GameControl.cs (runs the tick, then draws) |
| ActionControl | Game.Actions.cs (incl. `shootInstant`, achievements, tutorial pop-ups 7 and 8) | |
| KeyboardControl, KeyCon | Game.Typing.cs (typed words, cheats) | Game/KeyboardControl.cs (keys in game), UI/Shell.cs (menus). Mouse handling is in each widget |
| LevelData | data/levels/*.txt; Levels/LevelScript.cs (the file format), LevelBuilder.cs (the helpers: `missile`, `mirror`, `repeat`...), Data/GameData.cs (loading, checks) | |
| LevelEvent, SpawnEvent, ObjectCommand | Levels/LevelEvents.cs; the ship-code numbers in data/tuning.txt (Data/Tuning.cs) | |
| BossEvent, BossShip, Scanner | Levels/LevelEvents.cs, Boss.cs (incl. boss tutorial pop-ups, tutFlags 9 and 10) | View/Views.cs (BossView, ScannerView) |
| GameObject, GameSprite, TextObject | Entities.cs (Entity, Sprite) | View/Views.cs (SpriteView, WordLabel) |
| EnemyShip, PlayerShip, Missile, Turret | Entities.cs | View/Views.cs, View/Presenter.cs |
| Charge, ShieldView, StealthEffect | Entities.cs (the charge timer, the shield flag, the fade-in count) | View/Views.cs (ChargeGlow), View/ShieldView.cs, Fx/Effects.cs |
| ScrambleObject | Scramble.cs | Unused, as in the original |
| Starfield, Star, Particle, Nebula | | Fx/Starfield.cs (stars, nebulae), Fx/Explosions.cs (the particles); engine flames in Fx/EngineFlames.cs, placed by Fx/Mounts.cs + data/mounts.txt |
| LaserEffect, EMPEffect | | Fx/Effects.cs (LaserEffect, EmpEffect) |
| PopScore, Overflow | Entities.cs (Overflow); the score itself in Game | Fx/Effects.cs (PopScore) |
| GameUI, ShipSystem, PopWarning | Entities.cs (Hull: the repair word) | UI/GameUI.cs (HUD, HullBar), UI/Cockpit.cs (the cockpit frame, typed text) |
| SoundControl | | Audio/SoundControl.cs |
| SpriteSheets | ArtMask.cs (which pixels of the boss art are visible: scanner placement is a rule) | Core/Sprites.cs; `recolour` (chameleon) is a shader |
| WordList | WordList.cs + data/words.txt | |
| StringData | | Core/Strings.cs. GameStrings (old pre-menu texts) dropped: nothing used it |
| MenuUI, MapUI, LevelButton, DiffSelect (+BoxButton) | | UI/MenuUI.cs |
| UIButton, BitButton, MenuBox | | UI/Widgets.cs (TextButton, BitButton, MenuBox), plus TypeNav for typed navigation |
| TypingTest, ScoreUI, GameOverUI, VictoryUI, AchieveUI (+Award), PauseUI | SaveData.cs (Scoring: the score screen's bonuses and medals) | UI/Screens.cs |
| PopAchieve, PopBox | | UI/Popups.cs |
| Transition, PauseB, MuteB | | UI/Shell.cs (screen manager, fades, input outside the game). Pause and mute buttons are drawn placeholders: the originals were vector clips that weren't extracted |
| (new) the bot | AutoTyper.cs, Simulator.cs | Dev/DevAutoTyper.cs |
| (new) level editing | | Dev/LevelEditor.cs (`--edit`, `--from`, `--line`) |
| MenuBt, ConfirmMenu | | Dropped: never put on stage in the original (ConfirmMenu was an older difficulty/WPM picker) |
| FlashPreloadProfiler | | Dropped |

## Deviations from the original

- **Player missiles launch straight ahead.** The original passed the ship's rotation in degrees as radians, so
  missiles swung out at about -117° before homing. Fixed.
- **Escape pauses and resumes.** The original paused with an on-screen button or by typing "pause". Typing
  "pause" still works.
- **Letter keys don't auto-repeat** when held. Backspace still does.
- **Letters come from the typed character**, not the physical key code, so non-QWERTY layouts work.
- **Widescreen:** the 550x550 playfield is centred, with side panels filling the rest of the window.
- **Smooth motion:** logic still runs at 24 Hz, but the display is interpolated at the monitor's refresh rate.
- **Type to navigate the menus** (the original's TODO). Typing a button's first word works like clicking it:
  `start`, `high`, `achievements`, `more`, `credits`, `test`, `menu`, `submit`, `yes`, `no`, `done`, `skip`, a
  difficulty name in the difficulty box, an achievement's name to show its goals. On the map a level's label
  selects it: `1`–`9`, and `b1`–`b3` for the bosses, which are labelled B1–B3 instead of all "B". The letters typed
  so far show at the top of the screen. The mouse works as before.
- **More keys and clicks:** Escape closes the difficulty box and leaves the achievements screen; any key leaves the
  credits; Space or Enter continues from the game over screen (as MENU); Enter or a click also continues from the
  score screen; Space, Enter or Escape skip the victory credits. Any key also closes a stub message box.
- **The typing test is reachable**, from a TYPING TEST button on the title (top right on the map), with a MENU button
  for the mouse. The original had the screen but no way to open it. Its result is saved and the difficulty box marks
  the hardest difficulty at or below it. In the original the result only set a WPM that the difficulty box then
  overwrote.
- **The typing test is a new design** (phase 5): one of the passages of story in data/typing-test.txt, picked at
  random, for up to 90 seconds from the first key, with a clock in the corner. Wrong keys still flash the cross and
  don't move on, but now count against accuracy. The result is net WPM (correct characters, 5 to a word; the
  original counted words) and accuracy, and the box names the suggested difficulty, chosen as before.
- **The typing test no longer gives an achievement.** The original set the bronze Perfection medal but announced
  it as "Explorer", a bug.
- **Escape counts as pausing "without clicking"** for the silver Explorer medal, as typing "pause" does.
- **The difficulty box stays on the stage.** Next to level 5 it hung off the right edge.
- **No Flash-portal stubs:** submit and display high score, the sponsor link and More games are gone. The bronze
  Explorer medal, which the sponsor link gave, is now for viewing the credits.
- **The screens outside a level are new** (phase 7), in the cockpit's style over the moving starfield: the title
  (the player's ship, which launches off the top when you START), a level map drawn in code (a star chart with the
  route lit as far as you've reached, the bosses' own ships as their nodes, health and accuracy medal icons), the
  mission report, game over, achievements (medals), credits (as text, from the original's picture), the typing
  test (a miss flashes the passage's frame instead of a big red cross) and the message box. The fades between
  screens are dark rather than white or black, except the white flash into a level.
- **RETRY on the game-over screen** (new): the same level again, at the same difficulty.
- **Boss 3's counter-words** appear in one row above the player (left to right, a gap in the middle for the beam),
  in Classic too, instead of the original's 2 x 2 around the ship, whose lower row the cockpit now covers. Its cannon
  and the four lights around it are dark between attacks and light up as it charges.
- **Lasers and EMPs leave from the ship's nose gun** (its charge point) rather than its centre.
- **Quitting:** Escape on the title or map, or QUIT GAME on the options, asks "EXIT THE GAME?".
- **Options screen** (new; OPTIONS on the title and map, where the mute buttons were; they're gone, and a level has
  just a big PAUSE button, while typing "mute" or "unmute" still works): fullscreen, music and sound volumes, glow,
  screen shake, screen flashes (off skips full-screen flashes and dims a hit's), tutorials. Saved in
  user://settings.json (UI/Options.cs, Core/Settings.cs). Escape on a tutorial now turns the tutorials off for good,
  as the TUTORIALS switch.
- The score screen's count-up runs at 60 steps a second (the original's 10 ms timer, which Flash capped at about that).
- **The "New Achievement" pop-up** is a toast that slides in from the right: above the cockpit in a level, low down on
  the menus, and under the final score on the score screen (the original's was a box at the top centre).
- **"superman" ends.** The cheat's hero is removed once it has flown off the top. The original kept it in the object
  list forever, so a level could never end after typing "superman" (unless you then typed "kamikaze").
- **Turret explosions** happen at the turret. The original used the turret's position inside its ship, so the
  explosion went off near the top-left corner of the game view.
- **Boss 2's beam** starts from its plain frame each time. The original could keep showing a shield-hit frame from
  the previous beam if that one ended with counter-words left over.
- **Typing "pause" on the pause screen resumes** (as Escape does). The original only offered MENU there.
- **Typing feedback** (new, in Classic too): on every word that the typing bar ends with the start of, the typed
  letters light up, and a typed word pops as it fires. The original showed nothing until a whole word matched.
- **The bosses' new art moves where things happen**, in Classic too (Jeremy's call: the old positions aren't kept,
  not even for reference). The scanner and death explosions land on the new art, the turrets drop from boss 1's six
  gun muzzles, boss 0's ships launch from inside its two hangars (data/levels/*/bosses.txt), and the bosses' sizes
  changed a little. The Classic boss traces were re-recorded for it. A boss stops where its whole art is on screen
  (the original's spot, or lower if the art would poke out of the top: boss 0 now stops lower), and the scanner never
  lands within 20 px of the top of the screen.
- **Counter-word while the shield is fading:** typing one of boss 2's counter-words while the player's shield is
  fading out brings the shield back. The original added to the fading shield, which then vanished anyway.
- **"chameleon"** recolours the player's ship with a shader: a random hue rotation (Flash's luminance-weighted
  matrix) and saturation x2. Flash's `AdjustColor` saturation scale isn't documented, so x2 is a guess.
- Crash guards with no gameplay effect: an EMP landing on a ship that already left, a missile chasing a turret ship
  that already left, and `insert(-1)` (a no-op in AS3; levels 6 and 8 use it).

## Notes from the original worth knowing

- The original worked in WPM; the port's rules work in CPS (`cps = wpm / 12`), so its numbers are the original's
  with the 12 folded in. The original's level clock was distance, `cps / 7200` a tick (`wpm / 24 / 3600`), and
  level script steps of 0.1 or more were scaled by `max(1 - cps/25, 0.5)` (`1 - wpm/300`); the levels are now in
  seconds, converted at Hard (see the Hard pass above).
- Enemies only actually fire a missile when an accumulator passes 1. The rate is `clamp(cps/12.5, 0.3, 1)`
  (`wpm/150`) per fire command, so at low difficulty most fire commands do nothing.
- Laser charge time is `500 / cps` ticks (`6000 / wpm`). At half charge the word turns red and the charge sound plays.
- The typed buffer holds 20 characters. Overflowing or pressing Backspace costs 1 point each.
- A word whose ship leaves the screen stays typeable for 30 ticks (`Overflow`), so a half-typed word isn't wasted.
- `WordList.Shift` never picks one of the last five words in a list, so returned words aren't reused immediately.
- **Boss fights** (levels 3, 7, 11): the boss flies in and stops; a Scanner ring appears on a random visible pixel.
  Each 3-letter scanner word shrinks the ring 30 px; after nine, a 10-letter word appears, and typing it hits the
  boss. Three hits kill it (+100); it fades out over 200 ticks in explosions and the level ends. Until then
  `bossMode` is on: `bossLoop` keeps sending escorts, and kills score nothing. Attacks: boss 0 launches six suicide ships from its
  hangar every 48 x 400 / wpm ticks (48 x 33.3 / cps); boss 1 drops an invisible laser turret at one of six gun ports (shielded once the boss is
  on its last hit); boss 2 charges a beam that hits four times, and each of the four counter-words (8–10 letters)
  typed in time shields one hit.
- **Some waves can't be cleared at exactly the set WPM.** Speeds and typing both scale with WPM, so this holds at
  every difficulty: level 10's finale (suicide waves of 2, 4, 6 and 8 ships, 5.4 s apart at 40 WPM) and boss 2's beam
  (four long counter-words in 9 s at 40 WPM) need more typing than a typist at exactly the set speed can do. The bot
  survives them by healing beforehand and still ends level 10 at 3–4 health. This is how the original played: its
  difficulty box sets one of the five fixed WPMs (the typing test result was never used).
- The player's EMP is a single charge: typing a second shield word within 5 ticks of the first re-aims it, and the
  first ship stays shielded with no word. Only reachable above ~140 WPM; kept as in the original.
- **Tutorial pop-ups** show once per run of the game below 50 WPM (Easy and Normal), and switch off for the rest of the
  session after dying or pressing Escape on one. Only four have triggers: enemy missiles (7), first damage (8), a boss arriving
  (9) and the first hit on a boss (10). The triggers for 0–6 were commented out in the original; their texts are in
  Strings.Tutorial.
- **Achievements** advance one tier at a time and in order: Conqueror needs boss 1, then 2, then 3; Riddler needs
  superman, then kamikaze, then chameleon; Explorer needs the sponsor link, then pausing by keyboard, then killing
  superman. Defender and Perfection count gold medals across levels (1, then 5, then all 12).
- **Losing a level still records its score** as the level's best if it's higher, but not as cleared.
- The level map is reached by START on the title and stays up for the rest of the session: there's no way back to
  the title screen.
