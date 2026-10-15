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
5. **Difficulty review:** play every level at every difficulty (bot simulations plus Jeremy's playtests) and
   check each one demands roughly its target WPM (25/40/65/75/90). Retune where it doesn't.
6. **New features and deeper game logic:** new mechanics, enemies, modes and systems beyond the original (the
   old TODO list in Facade.as is a starting point: dodging, tractor beam, mega bomb, boss AI, space mines).
   This comes before the visual overhaul so the final art covers the new content too.
7. **Visual overhaul:** new art and fonts, side-panel HUD and decoration, effects (shaders, particles, screen
   shake), and UI polish. Make it look as good as possible.
8. **Placeholder cleanup:** replace the Flash-portal leftovers ("submit high score", "display high score", the
   sponsor link and "More games") with real features or remove them. Done just before Steam, because
   leaderboards are the likely replacement for submitting high scores.
9. **Steam integration:** Steamworks, achievements, cloud saves, leaderboards, Steam Deck support, and store
   builds. See [release-checklist.md](release-checklist.md).

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
| Starfield, Star, Particle, Nebula | | Fx/Starfield.cs |
| LaserEffect, EMPEffect | | Fx/Effects.cs (BeamEffect) |
| PopScore, Overflow | Entities.cs (Overflow); the score itself in Game | Fx/Effects.cs (PopScore) |
| GameUI, ShipSystem, PopWarning | Entities.cs (Hull: the repair word) | UI/GameUI.cs (HUD, HullBar) |
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
- **The typing test no longer gives an achievement.** The original set the bronze Perfection medal but announced
  it as "Explorer", a bug.
- **Escape counts as pausing "without clicking"** for the silver Explorer medal, as typing "pause" does.
- **The difficulty box stays on the stage.** Next to level 5 it hung off the right edge.
- **Flash-portal stubs** (submit and display high score, the sponsor link, More games) show the original's
  placeholder messages until phase 8. The sponsor link still gives the bronze Explorer medal.
- The score screen's count-up runs at 60 steps a second (the original's 10 ms timer, which Flash capped at about
  that). Its number column is right-aligned at x = 520; the original's autosized fields can't be reproduced exactly.
- **The "New Achievement" pop-up** shows under "FINAL SCORE:" on the score screen, where the original's top-centre
  spot would cover the "Level N Complete!" title. Elsewhere it's at the top centre as before.
- **"superman" ends.** The cheat's hero is removed once it has flown off the top. The original kept it in the object
  list forever, so a level could never end after typing "superman" (unless you then typed "kamikaze").
- **Turret explosions** happen at the turret. The original used the turret's position inside its ship, so the
  explosion went off near the top-left corner of the game view.
- **Boss 2's beam** starts from its plain frame each time. The original could keep showing a shield-hit frame from
  the previous beam if that one ended with counter-words left over.
- **Boss 2's scanner and death explosions** land on the art. The original ignored the art's 12 px offset.
- **Counter-word while the shield is fading:** typing one of boss 2's counter-words while the player's shield is
  fading out brings the shield back. The original added to the fading shield, which then vanished anyway.
- **"chameleon"** recolours the player's ship with a shader: a random hue rotation (Flash's luminance-weighted
  matrix) and saturation x2. Flash's `AdjustColor` saturation scale isn't documented, so x2 is a guess.
- Crash guards with no gameplay effect: an EMP landing on a ship that already left, a missile chasing a turret ship
  that already left, and `insert(-1)` (a no-op in AS3; levels 6 and 8 use it).

## Notes from the original worth knowing

- Distance per tick is `wpm / 24 / 3600`. Level script steps of 0.1 or more are scaled by `max(1 - wpm/300, 0.5)`.
- Enemies only actually fire a missile when an accumulator passes 1. The rate is `clamp(wpm/150, 0.3, 1)` per
  fire command, so at low difficulty most fire commands do nothing.
- Laser charge time is `6000 / wpm` ticks. At half charge the word turns red and the charge sound plays.
- The typed buffer holds 20 characters. Overflowing or pressing Backspace costs 1 point each.
- A word whose ship leaves the screen stays typeable for 30 ticks (`Overflow`), so a half-typed word isn't wasted.
- `WordList.Shift` never picks one of the last five words in a list, so returned words aren't reused immediately.
- **Boss fights** (levels 3, 7, 11): the boss flies in and stops; a Scanner ring appears on a random visible pixel.
  Each 3-letter scanner word shrinks the ring 30 px; after nine, a 10-letter word appears, and typing it hits the
  boss. Three hits kill it (+100); it fades out over 200 ticks in explosions and the level ends. Until then
  `bossMode` is on: `bossLoop` keeps sending escorts, and kills score nothing. Attacks: boss 0 launches six suicide ships from its
  hangar every 48x400/wpm ticks; boss 1 drops an invisible laser turret at one of six gun ports (shielded once the boss is
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
