# Release checklist

Things that must be settled before a commercial Steam release.

## Rights and licensing

- [ ] **Fonts.** The original used Consolas, Arial, Rosewood, Rocket Script, Quixley LET and Lily UPC, none
      licensed for embedding in a paid game. The port reads Consolas and Arial from the system as placeholders
      (game/scripts/Core/Fonts.cs). Replace them with bundled fonts under a suitable licence (e.g. OFL), and keep each
      licence file in the repo. **Done for everything players see:** bundled OFL fonts, Orbitron and JetBrains Mono,
      with their licences in game/assets/fonts/ and credited on the credits screen. Only the dev level editor still
      reads Arial from the system.
- [ ] **Art.** The original art is by Avi Kentridge. Get permission to use it, or replace it all.
- [ ] **Music.** The original music is by Binyamin Hornick. Get permission, and decide whether to keep or
      replace the tracks.
- [ ] **Nebula and space images.** Some backgrounds look like they come from astronomy photos (e.g. Hubble).
      Confirm the source and licence of each, or replace them.
- [ ] Credits screen updated for the new release.

## Build and export

- [x] Export presets (Windows; Linux for Steam Deck): game/export_presets.cfg; `steam/build.ps1` (ts 17) exports
      both with the release template. The Linux build hasn't been run yet (no Linux machine here).
- [x] The data files (words, tuning, levels) are embedded in the game assembly (game/TypingShip.csproj). Check an
      exported build reads them: run it with `-- --check`. The build script does this every time.
- [ ] Music is git-ignored. Make sure the release build machine has it (see game/assets/music/README.md).
- [ ] Music: the WAVs are about 81 MB. Re-import as compressed (Ogg/QOA) or convert, to keep the download small.
- [ ] Window and fullscreen settings, and a settings menu (volume, fullscreen, keyboard layout notes).
- [x] Remove or gate the dev flags (`--autotype`, `--level`, `--wpm`, `--cps`, `--screenshot`, `--keys`, `--profile`, `--edit`, `--from`,
      `--line`, `--trace`, ...) and the
      original's dev keys outside a level (Ctrl+Home invincible, Ctrl+Page Up unlock all, Ctrl+Page Down wipe the save).
      Release exports ignore all but `--check` and `--nosteam` (`G.Dev`); checked on an exported build.
- [x] The save (user://save.json) used System.Text.Json reflection. It's source-generated now (`SaveJson` in
      SaveData.cs), so trimming can't break it; Godot doesn't trim exports anyway. An exported build loads and
      writes the save.
- [ ] Windows exe metadata: company name, version and copyright in the Windows preset (game/export_presets.cfg).
- [x] Remove the Classic levels: data/levels/classic/, `--classic`, `ts 12`, Game.Classic, ClassicTraceTests and its
      classic-traces.txt, and the Classic half of the pacing report. Done (2026-10-22).

## Steam

- [x] Steamworks integration (Steamworks.NET, game/scripts/Platform/): achievements mapped from the six in-game ones
      (Soldier, Conqueror, Riddler, Defender, Perfection, Explorer, three tiers each), endless leaderboards, cloud
      saves (Auto-Cloud), SteamPipe uploads (ts 18). Tested against app 5450910: Steam starts and the leaderboards
      take scores and give ranks. Partner-site setup in [steam/README.md](../steam/README.md).
- [ ] Partner site: create and publish the 18 achievements (with icons), set up Steam Cloud, create the `beta`
      branch, reset the test scores on `endless_hard` and `endless_all` (steam/README.md). Then check that
      achievements unlock and the save syncs between two machines.
- [ ] Daily challenge leaderboard (a board per day; room left in `Boards`).
- [ ] Decide on Steam Playtest vs. keys + beta branch for playtesters (both supported by steam/build.ps1).
- [ ] Store page assets, capsule art, trailer.
- [ ] Steam Deck: the game needs a keyboard. Decide on on-screen keyboard support or a "keyboard required" note.
- [x] Replace "More games" and "Visit our sponsors" (Explorer achievement tier 1) from the Flash-portal era: removed;
      the credits screen gives the Explorer medal now.
