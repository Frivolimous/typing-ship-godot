# Roadmap and porting status

The original AS3 source is no longer in the repo (Jeremy keeps a copy). This file tracks what has been ported, what's left, where the port deliberately differs from the original, and the phases after the port.

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
5. ~~**CPS model, typing test and level balance.**~~ Done (2026-10):
   - **CPS in code, WPM for players.** The rules and tuning work in characters per second (CPS = WPM × 5 / 60);
     every player-facing number stays WPM (`Game.Cps`, `Difficulty.Cps`, the `Speed` helpers in
     src/Core/Playfield.cs). Where CPS arithmetic rounds differently from the original's whole-number WPM arithmetic,
     `Speed` keeps the original's result (checked tick for tick against the original levels while they were kept).
   - **Typing test:** five lore passages of about 250 characters in data/typing-test.txt (one at random, 90 s cap,
     net WPM and accuracy); the suggestion is the hardest difficulty at or below the result.
   - **Time-based level format** ([level-format.md](level-format.md)): waves at `@m:ss`, `+gap` seconds within,
     every time in seconds as played on Hard and the same at every difficulty. Converted from the old format by the
     engine (every event within a tick of before). A first distance-based rework was scrapped (branch
     level-pacing-rework, not merged).
   - **Pacing rules** ([pacing-rules.md](pacing-rules.md), Jeremy, from playtesting Hard) and their checker (`ts 13`),
     with a realistic bot (80% speed, 3.5% mistakes) and the pressure curve in the report.
   - **Hard pass:** Jeremy hand-tuned every Hard level; they aren't changed to satisfy the rule check.
   - **Difficulty pass:** the other difficulties thinned or filled by hand in the level files (Jeremy's call, for
     control and visibility): difficulty tags (`normal+:`) remove ships, five-value numbers (`1.5/0.9/0.5/0.5/0.5`)
     change counts and gaps, `repeat_over` spreads copies over a time. **Extreme and Insane are 90 and 120 WPM**
     (were 75 and 90), with movement flattened above Hard: the top difficulties are more ships, not faster ones.
     Every level and boss escort balanced at all five difficulties; boss 3's beam scales with typing speed.
   - **Classic:** the original 12 levels were kept as a baseline while the game's own were tuned, then removed
     (2026-10-22), with the AS3 source.
6. **New features and deeper game logic:** new mechanics, enemies, modes and systems beyond the original (the
   old TODO list in Facade.as is a starting point: dodging, tractor beam, mega bomb, boss AI, space mines).
   This comes before the visual overhaul so the final art covers the new content too. Decided so far:
   - **Endless mode:** a wave generator built from authored wave patterns, with density ramping over time.
     Steam leaderboards bracketed by CPS. No anti-cheat at release; patch later if cheating becomes a problem.
     **Done (Jeremy's design, 2026-10-22):** numbered waves of patterns from data/levels/endless.txt, each pattern
     `[level N name]` (1-15, the typing speed it takes to clear it; 45 to start, three a level), the same on every
     difficulty (no tags). Guidelines for writing them (level from letters, words and spawn time; letter range per
     level; a diving suicide ship counts 3 letters more, and at least half of a pattern's come in from the side or
     bottom and move 5 squares before diving) are checked by the Sim, not the game. Each wave has a budget of pattern
     levels for clearing (a level below the difficulty's own, +1 a wave), split at random across 1 to wave/3 patterns
     started a shrinking delay apart, with at most one missile-only pattern (or one per three patterns) so survival
     stays level; the next wave waits until the last is cleared, is announced ("WAVE n") and starts as that fades.
     Each wave is balanced for a typist 0.125 letters a second faster, up to Insane's: ship pace and enemy fire rate
     follow it, so Insane doesn't ramp (tuning.txt `[endless]`). The score is ship points x the difficulty's WPM / 25,
     so playing down doesn't pay; best score and longest run saved per difficulty (a global board can rank the
     scaled scores). Benchmarks (Jeremy, from Z-Type): a standard game about 5 minutes or under, a world-record game
     about 12; Jeremy's session target is 2-5 minutes. The bots: 80% typists survive a median 2:54-4:35 on every
     difficulty and clear every wave for about the first minute; a 140 WPM typist on Insane about 9½ minutes. No
     bosses; the wave and time are shown with the score. A big ENDLESS circle on the map, apart from the route and
     locked until the campaign is beaten (boss 3 cleared), the wave in the cockpit, the game-over screen for the
     result. The patterns come from the run's own seed, so the daily challenge can reuse them. Leaderboards come with
     Steam.
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
   [release-checklist.md](release-checklist.md) and [steam/README.md](../steam/README.md). **Done so far
   (2026-10-22):** Steamworks.NET behind `IPlatform` (scripts/Platform/; `G.Platform`, `NoPlatform` without Steam,
   on in exports, `--steam` in a checkout); the 18 achievements (six x three tiers, `SOLDIER_BRONZE` ...) unlocked as
   they're awarded and synced from the save at start-up; endless leaderboards per difficulty plus `endless_all`, sent
   on game over with the ranks shown on the game-over screen; Steam Cloud through Auto-Cloud (user:// is now
   `%APPDATA%\Millennium Typer`); Windows and Linux export presets, `steam/build.ps1` (ts 17, 18) for release
   builds and SteamPipe uploads; release exports drop the dev options and dev keys; the save's JSON is
   source-generated (safe if trimmed). Left: the daily board (with the daily challenge), Steam Deck keyboard, Workshop.

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

- **The player's ship sits 12 px higher** (438 in the game view, not 450), so its engine flames clear the cockpit
  drawn along the bottom. Enemy missiles and lasers have that much less distance to cover.
- **Copied ships don't drift.** Every point a ship flies to gets up to 10 px of random jitter. In the original, a
  copy (clone, mirror, offset, and so every repeat) re-rolled the jitter on top of the already-jittered point it
  copied, and rounded its halts down again, so a long chain of copies wandered in a random walk (about ±40 px by the
  40th) and its halts shrank a tick at a time. Copies now jitter around the point as written and keep the halt as
  written.
- **Extreme is 90 WPM and Insane 120** (the original had 75 and 90; Jeremy's call, 2026-10-18), so each
  difficulty is a clear step up and fast typists have something to chase. Above Hard, everything that moves or counts
  down with the typing speed (ship and missile speeds, halts, laser charges, stealth fades) uses a
  flattened pace, Hard x (cps / Hard)^0.5 (tuning.txt, "pace above hard"): Extreme moves 1.18x as fast as Hard and
  Insane 1.36x, close to what 75 and 90 WPM did, and the extra difficulty comes from more ships in the levels. The
  enemy fire rate and the score bonus still use the full speed.
- **Boss 3's beam asks the same of every difficulty.** Its four counter-words (8-10 letters each) must be typed
  before the beam strikes. The original timed the attack in steps of "boss 3 beam" / cps ticks, rounded down, so
  the share of the typing speed it took swung from 80% on Easy to 92% on Hard (and 67% on Insane once Insane got
  faster). The step is now Hard's (4 ticks) scaled by Hard's speed / the typing speed, not rounded, so it takes
  Hard's share everywhere; Hard plays exactly as before.
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
  instead of the original's 2 x 2 around the ship, whose lower row the cockpit now covers. Its cannon
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
- **Typing feedback** (new): on every word that the typing bar ends with the start of, the typed
  letters light up, and a typed word pops as it fires. A wrong letter (one that leaves no word matching) flashes the
  lit letters red with a shake. The original showed nothing until a whole word matched.
- **The bosses' new art moves where things happen** (Jeremy's call: the old positions aren't kept,
  not even for reference). The scanner and death explosions land on the new art, the turrets drop from boss 1's six
  gun muzzles, boss 0's ships launch from inside its two hangars (data/levels/bosses.txt), and the bosses' sizes
  changed a little. A boss stops where its whole art is on screen
  (the original's spot, or lower if the art would poke out of the top: boss 0 now stops lower), and the scanner never
  lands within 20 px of the top of the screen.
- **Counter-word while the shield is fading:** typing one of boss 2's counter-words while the player's shield is
  fading out brings the shield back. The original added to the fading shield, which then vanished anyway.
- **"chameleon"** recolours the player's ship with a shader: a random hue rotation (Flash's luminance-weighted
  matrix) and saturation x2. Flash's `AdjustColor` saturation scale isn't documented, so x2 is a guess.
- Crash guards with no gameplay effect: an EMP landing on a ship that already left, and a missile chasing a turret
  ship that already left.

## Notes from the original worth knowing

- The original worked in WPM; the port's rules work in CPS (`cps = wpm / 12`), so its numbers are the original's
  with the 12 folded in. The original's level clock was distance, `cps / 7200` a tick (`wpm / 24 / 3600`), and
  level script steps of 0.1 or more were scaled by `max(1 - cps/25, 0.5)` (`1 - wpm/300`); the levels are now in
  seconds, converted at Hard (phase 5 above).
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
