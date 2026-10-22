using TypingShip.Core.Data;

namespace TypingShip.Core;

/// <summary>
/// How hard a level presses on its deadlines, which the demand curve (letters per second) can't see: a wave of mines
/// with 3-letter words barely moves demand, but if they all dive at once the player has to type them all in the same
/// few seconds.
///
/// The level is played with nobody typing and the player unhurt, so every threat plays out: each enemy missile, suicide
/// ship (mines too) and laser shot that would hurt the player. For each one we note when its word first shows on
/// screen, when it hits, and the letters it takes to stop (its word, plus the main word under a shield). Pressure is
/// then the slowest steady typing speed that stops every threat before it hits, typing the most urgent first: over
/// every stretch of time, the letters of the threats that both appear and hit inside it, divided by its length. The
/// curve gives, for each second, the highest such need over the stretches that include it, as a share of the
/// difficulty's speed (like load). Above 1, even a perfect typist at the stated speed can't stop everything in time.
///
/// It's an upper bound in one way (ships aren't shot down before they fire, so every missile they'd launch is
/// counted) and generous in another (no reaction time, no missile flight time after a laser ship's word is typed).
/// </summary>
public static class Pressure
{
    /// <param name="Line">The level file line it came from (a missile: the line of the ship that fired it), or 0.</param>
    /// <param name="Appear">Seconds from the start of the level when its word first showed on screen.</param>
    /// <param name="Hit">When it hit (or would have hit) the player.</param>
    /// <param name="Letters">Letters to type to stop it.</param>
    public sealed record Threat(int Line, double Appear, double Hit, int Letters);

    /// <summary>Stretches shorter than this count as this long, so a lone threat seen for an instant isn't a spike.</summary>
    public const double MinWindow = 1;
    /// <summary>The longest stretch the curve looks at, so a hard moment shows where it is rather than smeared across
    /// the level (the same length as the demand curve's window).</summary>
    public const double MaxWindow = 20;

    // The visible stage in game-view space.
    const float Left = Playfield.VisibleLeft, Right = Playfield.VisibleRight, Top = Playfield.VisibleTop, Bottom = Playfield.VisibleBottom;

    /// <summary>Plays the level untouched and lists every threat that hit.</summary>
    public static List<Threat> Collect(GameData data, ArtLibrary art, int level, double cps, ulong seed = 1,
        int maxSeconds = 600)
    {
        var game = new Game(data, new Rng(seed), art) { Cps = cps, Invincible = true };
        game.TutOff();
        var seen = new Dictionary<Sprite, (double Appear, int Letters)>();
        var threats = new List<Threat>();
        bool over = false;
        game.Events = new Recorder(by =>
        {
            if (by == null || !seen.TryGetValue(by, out var s)) return; // nothing typeable hit (e.g. a boss attack)
            int line = by is Missile m ? m.SourceLine : by is EnemyShip e ? e.Line : 0;
            threats.Add(new Threat(line, s.Appear, game.Time, s.Letters));
            seen.Remove(by); // each hit is one threat; a ship that hits again starts over
        }, () => over = true);
        game.NewGame(level);
        while (!over && game.Ticks < maxSeconds * Playfield.TickRate)
        {
            game.Step();
            foreach (var o in game.Objects)
            {
                if (o.Word is not { Length: > 0 } w || seen.ContainsKey(o)) continue;
                if (o.Alpha < 0.5f || o.Position.X < Left || o.Position.X > Right || o.Position.Y < Top || o.Position.Y > Bottom) continue;
                int letters = w.Length + (o.ShieldOn ? Math.Max(0, o.WordSize) : 0);
                seen[o] = (game.Time, letters);
            }
        }
        return threats;
    }

    /// <summary>
    /// The pressure curve: for each second (index 0 = second 1), the typing speed needed (as a share of cps) over the
    /// most demanding stretch that includes it, and the lines of the threats in that stretch.
    /// </summary>
    public static (double[] Ratio, int[][] Lines) Curve(IReadOnlyList<Threat> threats, double cps, int seconds)
    {
        var ratio = new double[seconds];
        var best = new (double Start, double End)[seconds];
        var sorted = threats.OrderBy(t => t.Appear).ToList();
        foreach (double start in sorted.Select(t => t.Appear).Distinct())
        {
            // Threats that appear at or after this start, by when they hit: each prefix is a stretch [start, hit].
            double letters = 0;
            foreach (var group in sorted.Where(t => t.Appear >= start).OrderBy(t => t.Hit).GroupBy(t => t.Hit))
            {
                letters += group.Sum(t => t.Letters);
                double end = group.Key;
                if (end - start > MaxWindow) break;
                double need = letters / Math.Max(MinWindow, end - start) / cps;
                for (int s = Math.Max(0, (int)Math.Floor(start)); s < Math.Min(seconds, (int)Math.Ceiling(end)); s++)
                    if (need > ratio[s])
                    {
                        ratio[s] = need;
                        best[s] = (start, end);
                    }
            }
        }
        var lines = new int[seconds][];
        for (int s = 0; s < seconds; s++)
            lines[s] = ratio[s] == 0 ? Array.Empty<int>() :
                threats.Where(t => t.Appear >= best[s].Start && t.Hit <= best[s].End && t.Line > 0).Select(t => t.Line).Distinct().Order().ToArray();
        return (ratio, lines);
    }

    sealed class Recorder(Action<Sprite?> hit, Action over) : IGameEvents
    {
        public void PlayerHit(Sprite? by) => hit(by);
        public void LevelOver(bool cleared) => over();
    }
}
