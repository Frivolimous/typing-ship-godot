# Roadmap and porting status

The original AS3 is in [../reference/original-as3/](../reference/original-as3/). This file tracks what has been
ported, what's left, where the port deliberately differs from the original, and the phases after the port.

## Milestones

1. ~~**Vertical slice:** level 0 playable end to end.~~ Done.
2. ~~**Full gameplay:** levels 1–11, bosses (3, 7, 11) with `bossLoop`, scanner, scramble objects, superman and
   chameleon cheats; shields, turrets, stealth ships, suicide ships and mines verified.~~ Done: every level clears
   with the bot at all five difficulties (seeds 1–5). Difficulty and feel still need Jeremy to play them.
3. **Game shell:** title menu, level map, difficulty select, typing test, pause/mute, score screen, game over,
   victory, credits, transitions, tutorial pop-ups, achievements, save data (best score per level, unlocks).
4. **Refactor and level editor:** rules move into a plain C# core with unit tests. Level scripts and tuning
   numbers move into data files that Jeremy can edit without touching code. Add tools to make editing quick:
   reload a level without restarting, start from any point in a level, and show the 12x12 spawn grid and ship
   paths in game.
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

| AS3 | C# | Status |
|---|---|---|
| Facade | Core/G.cs | Done (globals, RNG, constants) |
| GameModel | Game/GameModel.cs | Run state done; save data not yet |
| GameControl | Game/GameControl.cs | Done (boss flow: `bossMode` escorts, `killBoss`, fade-out with explosions); score/game-over screens not yet |
| ActionControl | Game/ActionControl.cs | Done (incl. `shootInstant`); achievement pop-ups not yet (counters and tiers only) |
| KeyboardControl, KeyCon | Game/KeyboardControl.cs | In-game typing and cheat words done; menu/typing-test input not yet |
| LevelData | Level/LevelData.cs | Done: levels 0–11, `bossLoop`, `boss0Suicides` |
| LevelEvent, SpawnEvent, ObjectCommand | Level/ | Done |
| BossEvent, BossShip, Scanner | Level/BossEvent.cs, Entities/BossShip.cs, Entities/Scanner.cs | Done; boss tutorial pop-ups (tutFlags 9, 10) come with the pop-ups |
| GameObject, GameSprite, TextObject | Entities/ | Done |
| EnemyShip, PlayerShip, Missile | Entities/ | Done |
| Turret, ShieldView, Charge, StealthEffect | Entities/, Fx/Effects.cs | Done, verified in levels 1–11 |
| ScrambleObject | Entities/ScrambleObject.cs | Ported, but unused, as in the original |
| Starfield, Star, Particle, Nebula | Fx/Starfield.cs | Done |
| LaserEffect, EMPEffect | Fx/Effects.cs (BeamEffect) | Done |
| PopScore, Overflow | Fx/Effects.cs | Done |
| GameUI, ShipSystem, PopWarning | UI/GameUI.cs | Done (no on-screen pause/mute buttons) |
| SoundControl | Audio/SoundControl.cs | Done |
| SpriteSheets | Core/Sprites.cs | Gameplay and boss sprites done; `recolour` (chameleon) is a shader; menu art not yet |
| WordList | Core/WordList.cs + data/words.txt | Done |
| StringData, GameStrings | | Inlined where used so far; menus will need them |
| MenuUI, MapUI, LevelButton, DiffSelect, BoxButton, UIButton, BitButton, MenuBt, MenuBox, ConfirmMenu | | Not yet |
| TypingTest, ScoreUI, GameOverUI, VictoryUI, AchieveUI, Award, PopAchieve, PopBox, PauseUI, PauseB, MuteB, Transition | | Not yet |
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
- Level end and death show a placeholder message and Space restarts, until the score and game-over screens are ported.
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
  survives them by healing beforehand and still ends level 10 at 3–4 health. The original sets the WPM from your
  typing test, so this is how it played.
- The player's EMP is a single charge: typing a second shield word within 5 ticks of the first re-aims it, and the
  first ship stays shielded with no word. Only reachable above ~140 WPM; kept as in the original.
