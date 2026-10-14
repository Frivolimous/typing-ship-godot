# Porting status

The original AS3 is in [../reference/original-as3/](../reference/original-as3/). This file tracks what has been
ported, what's left, and where the port deliberately differs from the original.

## Milestones

1. ~~**Vertical slice:** level 0 playable end to end.~~ Done.
2. **Full gameplay:** levels 1–11, bosses (3, 7, 11) with `bossLoop`, scanner, scramble objects, superman and
   chameleon cheats. Verify shields, turrets, stealth ships, suicide ships and mines, which are ported but unused
   by level 0.
3. **Game shell:** title menu, level map, difficulty select, typing test, pause/mute, score screen, game over,
   victory, credits, transitions, tutorial pop-ups, achievements, save data (best score per level, unlocks).
4. **Refactor:** rules into a plain C# core with unit tests, level scripts and tuning numbers as data.
5. **Release:** new art and fonts, side-panel HUD and decoration, Steam integration. See
   [release-checklist.md](release-checklist.md).

## Class map

| AS3 | C# | Status |
|---|---|---|
| Facade | Core/G.cs | Done (globals, RNG, constants) |
| GameModel | Game/GameModel.cs | Run state done; save data not yet |
| GameControl | Game/GameControl.cs | Loop, levels, death, pause done; boss flow not yet |
| ActionControl | Game/ActionControl.cs | Done; achievement pop-ups not yet (counters only) |
| KeyboardControl, KeyCon | Game/KeyboardControl.cs | In-game typing done; menu/typing-test input not yet |
| LevelData | Level/LevelData.cs | Helpers done; **level 0 only**; `bossLoop`, `boss0Suicides` not yet |
| LevelEvent, SpawnEvent, ObjectCommand | Level/ | Done |
| BossEvent, BossShip, Scanner | | Not yet |
| GameObject, GameSprite, TextObject | Entities/ | Done |
| EnemyShip, PlayerShip, Missile | Entities/ | Done |
| Turret, ShieldView, Charge, StealthEffect | Entities/, Fx/Effects.cs | Ported; shields, turret and stealth untested |
| ScrambleObject | | Not yet |
| Starfield, Star, Particle, Nebula | Fx/Starfield.cs | Done |
| LaserEffect, EMPEffect | Fx/Effects.cs (BeamEffect) | Done |
| PopScore, Overflow | Fx/Effects.cs | Done |
| GameUI, ShipSystem, PopWarning | UI/GameUI.cs | Done (no on-screen pause/mute buttons) |
| SoundControl | Audio/SoundControl.cs | Done |
| SpriteSheets | Core/Sprites.cs | Gameplay sprites done; menu art not yet; `recolour` (chameleon) not yet |
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

## Notes from the original worth knowing

- Distance per tick is `wpm / 24 / 3600`. Level script steps of 0.1 or more are scaled by `max(1 - wpm/300, 0.5)`.
- Enemies only actually fire a missile when an accumulator passes 1. The rate is `clamp(wpm/150, 0.3, 1)` per
  fire command, so at low difficulty most fire commands do nothing.
- Laser charge time is `6000 / wpm` ticks. At half charge the word turns red and the charge sound plays.
- The typed buffer holds 20 characters. Overflowing or pressing Backspace costs 1 point each.
- A word whose ship leaves the screen stays typeable for 30 ticks (`Overflow`), so a half-typed word isn't wasted.
- `WordList.Shift` never picks one of the last five words in a list, so returned words aren't reused immediately.
