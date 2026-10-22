# Music (not in git)

The music tracks are too big to keep in the repo, so they're git-ignored and backed up elsewhere.
Without them the game runs silently, with sound effects only.

Put these files here:

| File | Original |
|---|---|
| `Music0.ogg` | Binyamin track 0 (menu) |
| `Music1.ogg` – `Music4.ogg` | Binyamin tracks 1–4 (levels cycle through them) |

The originals are in `C:\JS Projects\Typing Ship\Audio\Music\` (as "Binyamin track N.wav"), or can be
extracted from the original `Typing Ship.fla`, where they're the `Music0`–`Music4` linkage classes.
Ship them as Ogg Vorbis (about 6 MB for all five, against 81 MB of WAV); `MusicN.wav` still works if there's no Ogg.
After adding them, re-import assets (`ts 4`).
