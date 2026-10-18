using TypingShip.Core.Data;

namespace TypingShip.Core;

/// <summary>
/// Measures how much typing a level asks for over time: the bot plays it, and every second we record the letters
/// of the words that appeared (on ships, missiles, turrets, boss scanners and counter-words) and what's on screen.
/// The hull's repair word isn't counted: it's optional, and the perfect bot never takes damage.
/// </summary>
public static class Pacing
{
    public const int Window = 10; // seconds, for the rolling demand figure

    /// <summary>The realistic bot: this share of the difficulty's speed, mistyping this share of its keystrokes.</summary>
    public const double RealisticSpeed = 0.8, RealisticErrors = 0.035;

    // The part of the game view the player sees (the 550 px stage sits at 25,25 in game-view space).
    const float ViewLeft = 25, ViewRight = 575, ViewTop = 25, ViewBottom = 575;

    /// <param name="NewChars">Letters of words that appeared during this second.</param>
    /// <param name="DemandCps">Letters that appeared over the last <see cref="Window"/> seconds, per second,
    /// i.e. the typing speed needed to keep up with the level at this point.</param>
    /// <param name="WordsOnScreen">Words waiting to be typed at the end of the second, including ships that haven't
    /// flown into view yet (their words can already be typed).</param>
    /// <param name="CharsOnScreen">Their letters.</param>
    /// <param name="VisibleWords">Words the player can see: inside the view, and not still fading in.</param>
    public sealed record Sample(int Second, int Progress, int NewChars, double DemandCps, int WordsOnScreen, int CharsOnScreen,
        int VisibleWords);

    /// <param name="Tail">Seconds after the last spawn that still count as the level (the pacing rules' "duration tail").</param>
    /// <param name="Pressure">Per second (index 0 = second 1): the typing speed needed to stop every threat in time, as a
    /// share of cps (see <see cref="Core.Pressure"/>), from a run where nobody types.</param>
    /// <param name="PressureLines">Per second: the level file lines behind that second's pressure.</param>
    public sealed record Run(int Level, double Cps, bool Cleared, string Line, IReadOnlyList<Sample> Samples, double Tail = 5,
        double[]? Pressure = null, int[][]? PressureLines = null)
    {
        /// <summary>The highest pressure in the level, and the second (1-based) it comes at.</summary>
        public (double Value, int Second) PeakPressure => Pressure is { Length: > 0 } p
            ? (p.Max(), Array.IndexOf(p, p.Max()) + 1) : (0, 0);

        public int Seconds => Samples.Count;
        public int TotalChars => Samples.Sum(s => s.NewChars);
        /// <summary>The level's length for averages: start to the last spawn plus the tail (not the time the last ships
        /// take to leave).</summary>
        public int AverageSeconds => Math.Max(1, Math.Min(Seconds, LastSpawnSecond + (int)Math.Round(Tail)));
        /// <summary>
        /// Average typing speed the level asks for: the letters of every word that appears, start to last spawn + tail,
        /// over that time. The one definition of "average", used by the report and the pacing rules alike.
        /// </summary>
        public double AverageCps => Samples.Take(AverageSeconds).Sum(s => s.NewChars) / (double)AverageSeconds;
        public double PeakCps => Samples.Count == 0 ? 0 : Samples.Max(s => s.DemandCps);
        /// <summary>Average and peak demand as a share of the difficulty's speed (1 = typing flat out at it).</summary>
        public double AverageRatio => AverageCps / Cps;
        public double PeakRatio => PeakCps / Cps;
        /// <summary>The second in which the level's last new word appeared (0 if none did).</summary>
        public int LastSpawnSecond => Samples.LastOrDefault(s => s.NewChars > 0)?.Second ?? 0;
        /// <summary>Demand as a share of the difficulty's speed, second by second (index 0 = second 1).</summary>
        public double[] Load => Samples.Select(s => s.DemandCps / Cps).ToArray();
    }

    /// <summary>How a run by a less-than-perfect bot went: cleared or not, how long, and how often it was hit.</summary>
    public sealed record Outcome(int Level, double Cps, double BotCps, double ErrorRate, bool Cleared, int Seconds, int Hits, int Health);

    /// <summary>
    /// Plays a level with a bot closer to a real player: it types at a share of the difficulty's speed and mistypes
    /// some letters (each mistake costs a Backspace too). Shows where real players are likely to struggle.
    /// </summary>
    public static Outcome Realistic(GameData data, ArtLibrary art, int level, double cps, ulong seed = 1,
        double speedShare = RealisticSpeed, double errorRate = RealisticErrors, bool classic = false)
    {
        int hits = 0, health = -1;
        var result = Simulator.Run(data, art, level, cps, seed, botCps: cps * speedShare, classic: classic, errorRate: errorRate,
            onTick: game =>
            {
                if (health >= 0 && game.Player.Health < health) hits += health - game.Player.Health;
                health = game.Player.Health;
            });
        var g = result.Game;
        return new Outcome(level, cps, cps * speedShare, errorRate, result.Cleared, (int)(g.Ticks / Playfield.TickRate), hits,
            Math.Max(0, g.Player.Health));
    }

    public static Run Measure(GameData data, ArtLibrary art, int level, double cps, ulong seed = 1, bool classic = false,
        double tail = 5, bool pressure = true)
    {
        var samples = new List<Sample>();
        var seen = new HashSet<(object, string)>();
        var current = new HashSet<(object, string)>();
        var perSecond = new List<int>();
        int newChars = 0;

        var result = Simulator.Run(data, art, level, cps, seed, classic: classic, onTick: game =>
        {
            current.Clear();
            int chars = 0, visible = 0;
            void Add(Entity? e, bool inSight = true)
            {
                if (e?.Word is not { Length: > 0 } w) return;
                current.Add((e, w));
                chars += w.Length;
                if (inSight) visible++;
                if (seen.Add((e, w))) newChars += w.Length;
            }
            foreach (var o in game.Objects)
            {
                bool inView = o.Alpha >= 0.5f && o.Position.X >= ViewLeft && o.Position.X <= ViewRight &&
                              o.Position.Y >= ViewTop && o.Position.Y <= ViewBottom;
                Add(o, inView);
                Add(o.Turret, inView);
            }
            Add(game.Boss?.Scanner);
            seen.IntersectWith(current); // forget words that are gone, so a recycled word counts again

            if (game.Ticks % Playfield.TickRate != 0) return;
            perSecond.Add(newChars);
            int window = perSecond.Skip(Math.Max(0, perSecond.Count - Window)).Sum();
            double demand = window / (double)Math.Min(Window, perSecond.Count);
            int progress = game.MaxTime > 0 ? (int)Math.Min(100, game.Time / game.MaxTime * 100) : 0;
            samples.Add(new Sample(perSecond.Count, progress, newChars, demand, current.Count, chars, visible));
            newChars = 0;
        });
        if (!pressure) return new Run(level, cps, result.Cleared, result.Line, samples, tail);
        var threats = Core.Pressure.Collect(data, art, level, cps, seed, classic, maxSeconds: Math.Max(samples.Count + 60, 120));
        int seconds = Math.Max(samples.Count, threats.Count == 0 ? 0 : (int)Math.Ceiling(threats.Max(t => t.Hit)));
        var (ratio, lines) = Core.Pressure.Curve(threats, cps, seconds);
        return new Run(level, cps, result.Cleared, result.Line, samples, tail, ratio, lines);
    }
}
