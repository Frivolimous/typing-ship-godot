# Steam

How Millennium Typer talks to Steam, what has to be set up on the Steamworks partner site, and how to push a
build to playtesters.

| | |
|---|---|
| Main app | 5450910 (depot 5450911, Windows) |
| Playtest app | 5490840 (depot 5490841) |
| Demo app | 5490830 (depot 5490831), for a later demo build |

## In the game

- **Binding:** [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) 2024.8.0 (NuGet, MIT), a plain C#
  wrapper of Valve's `steam_api` library. It works with Godot's .NET exports as it is (no GDExtension, nothing
  tied to the Godot version). Valve's libraries are in `redist/`: `steam_api64.dll` (Windows, signed by Valve) and
  `libsteam_api.so` (Linux), from the Steamworks.NET 2024.8.0 standalone release (Steamworks SDK 1.60). To update
  them, take both from the same Steamworks.NET release as the NuGet package.
- **Optional:** `game/scripts/Platform/IPlatform.cs` is all the game knows: `G.Platform` is `SteamPlatform` when Steam
  starts, otherwise `NoPlatform`, and the game plays the same without it. Steam starts in an exported build that
  Steam launched (or with a `steam_appid.txt` next to the exe), and in a checkout only with `--steam` (so dev runs
  don't award achievements or post scores). Never with the bot. `--nosteam` turns it off anywhere. The console
  says `Steam: on, app 5450910` or `Steam: off (why)`.
- The game doesn't make players start it from Steam (no `RestartAppIfNecessary`): the exe runs without Steam.

## Partner site setup

### Achievements (Stats & Achievements > Achievements)

Create these 18, then **Publish** the changes. Each needs a 256x256 icon, achieved and unachieved (still to be
drawn). The game unlocks a tier when it's earned, and on start-up unlocks every tier the save already has (earned
before Steam, or with Steam closed).

| API name | Display name | Description |
|---|---|---|
| SOLDIER_BRONZE | Soldier (Bronze) | Destroy 10 enemy ships |
| SOLDIER_SILVER | Soldier (Silver) | Destroy 1,000 enemy ships |
| SOLDIER_GOLD | Soldier (Gold) | Destroy 20,000 enemy ships |
| CONQUEROR_BRONZE | Conqueror (Bronze) | Defeat the first boss |
| CONQUEROR_SILVER | Conqueror (Silver) | Defeat the second boss |
| CONQUEROR_GOLD | Conqueror (Gold) | Finish the game |
| RIDDLER_BRONZE | Riddler (Bronze) | It's a bird, it's a plane, it's... |
| RIDDLER_SILVER | Riddler (Silver) | Japanese death pilot's scream |
| RIDDLER_GOLD | Riddler (Gold) | Nature's master of disguise |
| DEFENDER_BRONZE | Defender (Bronze) | Complete any level with perfect health |
| DEFENDER_SILVER | Defender (Silver) | Achieve 5 gold health medals |
| DEFENDER_GOLD | Defender (Gold) | Finish every level with perfect health |
| PERFECTIONIST_BRONZE | Perfection (Bronze) | Complete any level killing every ship |
| PERFECTIONIST_SILVER | Perfection (Silver) | Achieve 5 gold accuracy medals |
| PERFECTIONIST_GOLD | Perfection (Gold) | Finish every level with perfect accuracy |
| EXPLORER_BRONZE | Explorer (Bronze) | View the credits |
| EXPLORER_SILVER | Explorer (Silver) | Pause the game without clicking |
| EXPLORER_GOLD | Explorer (Gold) | Kill Superman |

The descriptions are the in-game ones (`Strings.AchieveText`); the Riddler ones are riddles, so consider marking
those hidden. Until they're published the console warns `Steam: no achievements (k_EResultFail)` and nothing unlocks.

### Leaderboards (Stats & Achievements > Leaderboards)

Nothing to create: the game makes each board the first time it sends a score (highest first, shown as a number), and
keeps each player's best. Endless mode sends every ended run to two boards:

| Board | Score |
|---|---|
| `endless_easy`, `endless_normal`, `endless_hard`, `endless_extreme`, `endless_insane` | the run's score on that difficulty |
| `endless_all` | the same score, every difficulty together (scores already count x WPM / 25, so they compare fairly) |

Each entry carries the wave reached, the time in seconds and the difficulty (0-4). The game-over screen shows the
player's rank on both (their best score's rank). You can give the boards display names on the partner site. The
daily challenge will add a board per day (`daily_2026-10-22`).

`endless_hard` and `endless_all` already exist, each holding a test score of 78 from Claude's check (Jeremy's account).
Reset them before playtesting (the leaderboard's page on the partner site).

### Steam Cloud (Steam Cloud > Settings)

The saves sync without code (Auto-Cloud). The game keeps them in its own folder: `%APPDATA%\Millennium Typer\`
on Windows, `~/.local/share/Millennium Typer/` on Linux. (It used to be `%APPDATA%\Godot\app_userdata\Millennium Typer\`;
the game brings old saves over once.)

- Byte quota per user: `1048576` (1 MB; the save is under 1 KB). Number of files per user: `10`.
- Auto-Cloud root paths, two entries:
  - Root `WinAppDataRoaming`, subdirectory `Millennium Typer`, pattern `save.json`, OS: all, not recursive.
  - Root `WinAppDataRoaming`, subdirectory `Millennium Typer`, pattern `settings.json`, OS: all, not recursive.
    (Leave this one out if the window settings shouldn't follow players between a PC and a Steam Deck.)
- Root override: original root `WinAppDataRoaming`, OS `Linux`, new root `LinuxXdgDataHome` (no path change), for
  the native Linux build. The Windows build on a Steam Deck (Proton) needs nothing.
- Save, then **Publish**.

## Pushing a build to playtesters

The release build is made on Jeremy's machine (the music is git-ignored and only there).

1. One time: SteamPipe > Builds > create a branch, e.g. `beta` (with a password if it should be private).
2. `ts 18` (or `powershell -ExecutionPolicy Bypass -File steam\build.ps1 -Upload main`): builds with the release
   template, checks the exported game reads its data, writes the SteamPipe scripts into `builds\steam\`, and uploads
   with steamcmd, live on the `beta` branch. steamcmd is downloaded from Valve into `steam\steamcmd\` the first
   time. It asks for the Steam login (or set `STEAM_USER`), password and Steam Guard code, which stay in steamcmd.
   `-Branch NAME` uploads to another branch; `-Description "..."` adds a note to the build.
3. Testers own the game (Steamworks keys for the main app) and pick the branch in the game's Properties > Betas.

**Steam Playtest instead** (testers request access on the store page, no keys): `-Upload playtest` uploads to the
Playtest app. Steam won't set a build live on a default branch from steamcmd, so set it live on the Playtest app's
SteamPipe > Builds page. The Playtest app has its own achievements and leaderboards: create the achievements there
too (the leaderboards make themselves).

**Linux / Steam Deck:** the Deck runs the Windows build through Proton. For a native Linux build, add a Linux depot to
the app (operating system Linux; the Windows depot set to Windows), put its ID in `steam\build.ps1`, and upload with
`-Linux`. The Linux export works but hasn't been run here (no Linux machine).

## Dev and release builds

A release export (`--export-release`, which the script uses) turns off every dev option except `--check` and
`--nosteam`, and the original's dev keys (Ctrl+Home, Ctrl+Page Up, Ctrl+Page Down). Godot's editor and debug exports
keep them. F2 (the next art set) stays, as the options screen has the same choice.
