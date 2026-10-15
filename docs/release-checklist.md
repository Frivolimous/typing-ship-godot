# Release checklist

Things that must be settled before a commercial Steam release.

## Rights and licensing

- [ ] **Fonts.** The original used Consolas, Arial, Rosewood, Rocket Script, Quixley LET and Lily UPC, none
      licensed for embedding in a paid game. The port reads Consolas and Arial from the system as placeholders
      (game/scripts/Core/Fonts.cs). Replace them with bundled fonts under a suitable licence (e.g. OFL), and keep each
      licence file in the repo.
- [ ] **Art.** The original art is by Avi Kentridge. Get permission to use it, or replace it all.
- [ ] **Music.** The original music is by Binyamin Hornick. Get permission, and decide whether to keep or
      replace the tracks.
- [ ] **Nebula and space images.** Some backgrounds look like they come from astronomy photos (e.g. Hubble).
      Confirm the source and licence of each, or replace them.
- [ ] Credits screen updated for the new release.

## Build and export

- [ ] Export presets (Windows; Linux for Steam Deck).
- [ ] The data files (words, tuning, levels) are embedded in the game assembly (game/TypingShip.csproj). Check an
      exported build reads them: run it with `-- --check`.
- [ ] Music is git-ignored. Make sure the release build machine has it (see game/assets/music/README.md).
- [ ] Music: the WAVs are about 81 MB. Re-import as compressed (Ogg/QOA) or convert, to keep the download small.
- [ ] Window and fullscreen settings, and a settings menu (volume, fullscreen, keyboard layout notes).
- [ ] Remove or gate the dev flags (`--autotype`, `--level`, `--wpm`, `--screenshot`, `--keys`, `--profile`, `--edit`, `--from`,
      `--line`, `--trace`, ...) and the
      original's dev keys outside a level (Ctrl+Home invincible, Ctrl+Page Up unlock all, Ctrl+Page Down wipe the save).
- [ ] The save (user://save.json) uses System.Text.Json reflection. Check it still works in a trimmed export build.

## Steam

- [ ] Steamworks integration (GodotSteam or Steamworks.NET): achievements mapped from the six in-game ones
      (Soldier, Conqueror, Riddler, Defender, Perfection, Explorer, three tiers each), cloud saves. Map the save (scripts/Core/SaveData.cs) to Steam Cloud.
- [ ] Store page assets, capsule art, trailer.
- [ ] Steam Deck: the game needs a keyboard. Decide on on-screen keyboard support or a "keyboard required" note.
- [ ] Replace "More games" and "Visit our sponsors" (Explorer achievement tier 1) from the Flash-portal era.
