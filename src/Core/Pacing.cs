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

    /// <param name="NewChars">Letters of words that appeared during this second.</param>
    /// <param name="DemandCps">Letters that appeared over the last <see cref="Window"/> seconds, per second,
    /// i.e. the typing speed needed to keep up with the level at this point.</param>
    /// <param name="WordsOnScreen">Words waiting to be typed at the end of the second.</param>
    /// <param name="CharsOnScreen">Their letters.</param>
    public sealed record Sample(int Second, int Progress, int NewChars, double DemandCps, int WordsOnScreen, int CharsOnScreen);

    public sealed record Run(int Level, double Cps, bool Cleared, string Line, IReadOnlyList<Sample> Samples)
    {
        public int Seconds => Samples.Count;
        public int TotalChars => Samples.Sum(s => s.NewChars);
        /// <summary>Average typing speed the whole level asks for.</summary>
        public double AverageCps => Seconds == 0 ? 0 : TotalChars / (double)Seconds;
        public double PeakCps => Samples.Count == 0 ? 0 : Samples.Max(s => s.DemandCps);
        /// <summary>Average and peak demand as a share of the difficulty's speed (1 = typing flat out at it).</summary>
        public double AverageRatio => AverageCps / Cps;
        public double PeakRatio => PeakCps / Cps;
    }

    public static Run Measure(GameData data, ArtLibrary art, int level, double cps, ulong seed = 1, bool classic = false)
    {
        var samples = new List<Sample>();
        var seen = new HashSet<(object, string)>();
        var current = new HashSet<(object, string)>();
        var perSecond = new List<int>();
        int newChars = 0;

        var result = Simulator.Run(data, art, level, cps, seed, classic: classic, onTick: game =>
        {
            current.Clear();
            int chars = 0;
            void Add(Entity? e)
            {
                if (e?.Word is not { Length: > 0 } w) return;
                current.Add((e, w));
                chars += w.Length;
                if (seen.Add((e, w))) newChars += w.Length;
            }
            foreach (var o in game.Objects)
            {
                Add(o);
                Add(o.Turret);
            }
            Add(game.Boss?.Scanner);
            seen.IntersectWith(current); // forget words that are gone, so a recycled word counts again

            if (game.Ticks % Playfield.TickRate != 0) return;
            perSecond.Add(newChars);
            int window = perSecond.Skip(Math.Max(0, perSecond.Count - Window)).Sum();
            double demand = window / (double)Math.Min(Window, perSecond.Count);
            int progress = game.MaxDistance > 0 ? (int)Math.Min(100, game.Distance / game.MaxDistance * 100) : 0;
            samples.Add(new Sample(perSecond.Count, progress, newChars, demand, current.Count, chars));
            newChars = 0;
        });
        return new Run(level, cps, result.Cleared, result.Line, samples);
    }
}
