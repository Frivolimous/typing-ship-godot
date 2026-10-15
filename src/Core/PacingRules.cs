using System.Globalization;
using TypingShip.Core.Data;

namespace TypingShip.Core;

/// <summary>
/// The level pacing rules (docs/pacing-rules.md), with their numbers read from data/pacing-rules.txt, and a checker
/// that runs them over a <see cref="Pacing.Run"/>. Everything is measured on load = demand ÷ the difficulty's speed,
/// one value per second. Levels are numbered as on the map (1-12).
/// </summary>
public sealed class PacingRules
{
    public const string FileName = "pacing-rules.txt";

    /// <summary>The rule names an "exempt" line can use, in the order they're checked.</summary>
    public static readonly string[] RuleNames = { "duration", "average", "lulls", "earned", "intensity", "peaks", "arc" };

    public double Burst = 0.5;
    public double LullFloor = 0.3, LullFloorSeconds = 7, EmptySeconds = 3, LullShare = 0.25;
    public double EarnedPeak = 1.0, LullBaseSeconds = 4, LullMaxSeconds = 10;
    public double Intense = 1.0, IntenseSeconds = 13, VeryIntense = 1.2, VeryIntenseShare = 0.2;
    public double PeakCap = 1.5, SpikeCap = 1.6, SpikeSeconds = 5, Release = 0.65, ReleaseWithin = 5, ReleaseHold = 5;
    public double Intro = 8;
    public double DurationBase = 90, DurationPerLevel = 9, DurationTolerance = 3;
    public double AverageMin = 0.65, AverageMax = 0.8, AverageTargetBase = 0.65, AverageTargetPerLevel = 0.015,
        AverageTargetTolerance = 0.03;

    /// <summary>Map level (1-12) -> the rules it's exempt from ("all" = every rule).</summary>
    public readonly Dictionary<int, HashSet<string>> Exempt = new();

    public bool IsExempt(int mapLevel, string rule) =>
        Exempt.TryGetValue(mapLevel, out var set) && (set.Contains("all") || set.Contains(rule));

    public double TargetDuration(int mapLevel) => DurationBase + DurationPerLevel * (mapLevel - 1);
    public double TargetAverage(int mapLevel) => AverageTargetBase + AverageTargetPerLevel * (mapLevel - 1);

    // ---- the file ----

    static readonly (string Name, Action<PacingRules, double> Set)[] Keys =
    {
        ("burst", (r, v) => r.Burst = v),
        ("lull floor", (r, v) => r.LullFloor = v),
        ("lull floor seconds", (r, v) => r.LullFloorSeconds = v),
        ("empty screen seconds", (r, v) => r.EmptySeconds = v),
        ("lull share", (r, v) => r.LullShare = v),
        ("earned by peak", (r, v) => r.EarnedPeak = v),
        ("lull base seconds", (r, v) => r.LullBaseSeconds = v),
        ("lull max seconds", (r, v) => r.LullMaxSeconds = v),
        ("intense", (r, v) => r.Intense = v),
        ("intense seconds", (r, v) => r.IntenseSeconds = v),
        ("very intense", (r, v) => r.VeryIntense = v),
        ("very intense share", (r, v) => r.VeryIntenseShare = v),
        ("peak cap", (r, v) => r.PeakCap = v),
        ("spike cap", (r, v) => r.SpikeCap = v),
        ("spike seconds", (r, v) => r.SpikeSeconds = v),
        ("release", (r, v) => r.Release = v),
        ("release within", (r, v) => r.ReleaseWithin = v),
        ("release hold", (r, v) => r.ReleaseHold = v),
        ("intro seconds", (r, v) => r.Intro = v),
        ("duration base", (r, v) => r.DurationBase = v),
        ("duration per level", (r, v) => r.DurationPerLevel = v),
        ("duration tolerance", (r, v) => r.DurationTolerance = v),
        ("average min", (r, v) => r.AverageMin = v),
        ("average max", (r, v) => r.AverageMax = v),
        ("average target base", (r, v) => r.AverageTargetBase = v),
        ("average target per level", (r, v) => r.AverageTargetPerLevel = v),
        ("average target tolerance", (r, v) => r.AverageTargetTolerance = v),
    };

    /// <summary>
    /// Reads "name = value" lines (shares can be written as 30%) and "exempt LEVEL: RULE, RULE" lines. Every name
    /// must appear once, so a typo can't silently leave a rule at a default. Throws a <see cref="DataError"/>.
    /// </summary>
    public static PacingRules Parse(string text, string file = FileName)
    {
        var r = new PacingRules();
        var seen = new Dictionary<string, int>();
        int lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            int hash = raw.IndexOf('#');
            var line = (hash >= 0 ? raw[..hash] : raw).Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("exempt ", StringComparison.OrdinalIgnoreCase))
            {
                var rest = line[7..];
                int colon = rest.IndexOf(':');
                if (colon < 0 || !int.TryParse(rest[..colon].Trim(), out var level) || level is < 1 or > GameData.LevelCount)
                    throw new DataError(file, lineNo, $"Expected \"exempt LEVEL: RULE, RULE\" with a level from 1 to {GameData.LevelCount}, like \"exempt 3: arc\".");
                var rules = rest[(colon + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.ToLowerInvariant()).ToList();
                foreach (var rule in rules)
                    if (rule != "all" && !RuleNames.Contains(rule))
                        throw new DataError(file, lineNo, $"\"{rule}\" isn't a rule. Use all, or: {string.Join(", ", RuleNames)}.");
                if (rules.Count == 0) throw new DataError(file, lineNo, "Name at least one rule after the colon, or all.");
                if (!r.Exempt.TryGetValue(level, out var set)) r.Exempt[level] = set = new HashSet<string>();
                set.UnionWith(rules);
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq < 0) throw new DataError(file, lineNo, $"Expected \"name = value\" or \"exempt LEVEL: RULE\", not \"{line}\".");
            var name = string.Join(' ', line[..eq].Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            var valueText = line[(eq + 1)..].Trim();
            var key = Keys.FirstOrDefault(k => k.Name == name);
            if (key.Name == null) throw new DataError(file, lineNo, $"\"{name}\" isn't a pacing rule setting.");
            if (seen.TryGetValue(name, out var first)) throw new DataError(file, lineNo, $"\"{name}\" is already set on line {first}.");
            bool percent = valueText.EndsWith('%');
            if (!double.TryParse(valueText.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v < 0)
                throw new DataError(file, lineNo, $"\"{name}\" needs a number (0 or more), not \"{valueText}\".");
            key.Set(r, percent ? v / 100 : v);
            seen[name] = lineNo;
        }
        var missing = Keys.Select(k => k.Name).Where(n => !seen.ContainsKey(n)).ToList();
        if (missing.Count > 0) throw new DataError(file, lineNo, $"Missing: {string.Join(", ", missing)}.");
        return r;
    }

    // ---- the checks ----

    /// <param name="Second">The 1-based second of the run where it happens (0 for the whole level).</param>
    /// <param name="Warning">True for advice that doesn't fail the check (the average's target).</param>
    public sealed record Finding(int Level, string Difficulty, string Rule, int Second, string Message, bool Warning = false)
    {
        /// <summary>When it starts, m:ss from the start of the level ("" for the whole level).</summary>
        public string At => Second <= 0 ? "" : Time(Second - 1);
        public override string ToString() => $"{At,5}  {Rule,-9}  {Message}";
    }

    public static string Time(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

    /// <summary>A contiguous run of seconds (1-based, inclusive).</summary>
    readonly record struct Span(int Start, int End)
    {
        public int Length => End - Start + 1;
    }

    /// <summary>Runs of seconds in [from, to] where the predicate holds.</summary>
    static List<Span> Runs(int from, int to, Func<int, bool> holds)
    {
        var runs = new List<Span>();
        int start = -1;
        for (int s = from; s <= to + 1; s++)
        {
            bool h = s <= to && holds(s);
            if (h && start < 0) start = s;
            else if (!h && start >= 0)
            {
                runs.Add(new Span(start, s - 1));
                start = -1;
            }
        }
        return runs;
    }

    /// <summary>Checks one level at one difficulty (perfect-bot run). mapLevel is 1-12.</summary>
    public List<Finding> Check(Pacing.Run run, int mapLevel)
    {
        var found = new List<Finding>();
        var diff = Difficulty.Name(run.Cps);
        void Add(string rule, int second, string message, bool warning = false)
        {
            if (!IsExempt(mapLevel, rule)) found.Add(new Finding(mapLevel, diff, rule, second, message, warning));
        }
        if (IsExempt(mapLevel, "all") || run.Samples.Count == 0) return found;

        var load = run.Load;
        int n = load.Length;
        double L(int s) => s >= 1 && s <= n ? load[s - 1] : 0; // second s, 1-based
        int Visible(int s) => run.Samples[s - 1].VisibleWords;
        string P(double v) => $"{v:0.00}P";

        int lastSpawn = run.LastSpawnSecond;
        int duration = (int)Math.Round(lastSpawn + TailSeconds);
        int from = (int)Intro + 1, to = lastSpawn; // the checked stretch: after the intro, up to the last spawn
        int checkedSeconds = Math.Max(0, to - from + 1);

        // 1. Duration
        double target = TargetDuration(mapLevel);
        if (Math.Abs(duration - target) > DurationTolerance)
            Add("duration", 0, $"lasts {Time(duration)} (last spawn {Time(lastSpawn)} + {TailSeconds:0} s); target {Time((int)target)} ± {DurationTolerance:0} s");

        // 2. Average load over the whole level (start to last spawn + tail)
        int avgEnd = Math.Min(n, duration);
        double avg = Enumerable.Range(1, avgEnd).Average(L);
        if (avg < AverageMin - 1e-9 || avg > AverageMax + 1e-9)
            Add("average", 0, $"average load {P(avg)}; allowed {P(AverageMin)}–{P(AverageMax)} (target {P(TargetAverage(mapLevel))})");
        else if (Math.Abs(avg - TargetAverage(mapLevel)) > AverageTargetTolerance + 1e-9)
            Add("average", 0, $"average load {P(avg)}; target for level {mapLevel} is {P(TargetAverage(mapLevel))}", warning: true);

        if (checkedSeconds <= 0) return found;

        // 3. Lulls
        foreach (var r in Runs(from, to, s => L(s) < LullFloor))
            if (r.Length > LullFloorSeconds)
                Add("lulls", r.Start, $"{r.Length} s below {P(LullFloor)} (max {LullFloorSeconds:0} s)");
        foreach (var r in Runs(from, to, s => Visible(s) == 0))
            if (r.Length > EmptySeconds)
                Add("lulls", r.Start, $"empty screen for {r.Length} s (max {EmptySeconds:0} s)");
        int below = Enumerable.Range(from, checkedSeconds).Count(s => L(s) < Burst);
        if (below > LullShare * checkedSeconds + 1e-9)
            Add("lulls", from, $"{below * 100.0 / checkedSeconds:0}% of the level below {P(Burst)} (max {LullShare * 100:0}%)");

        // 4. Earned lulls: each lull against the burst just before it (bursts may start in the intro).
        var bursts = Runs(1, to, s => L(s) >= Burst);
        foreach (var lull in Runs(1, to, s => L(s) < Burst))
        {
            if (lull.Start < from) continue; // started in the intro
            var prev = bursts.LastOrDefault(b => b.End == lull.Start - 1);
            if (prev == default) continue;
            double prevPeak = Enumerable.Range(prev.Start, prev.Length).Max(L);
            int above = Enumerable.Range(prev.Start, prev.Length).Count(s => L(s) > EarnedPeak);
            double lowest = Enumerable.Range(lull.Start, lull.Length).Min(L);
            if (lowest < LullFloor && prevPeak <= EarnedPeak)
                Add("earned", lull.Start, $"dips to {P(lowest)} after a burst peaking at only {P(prevPeak)} (needs > {P(EarnedPeak)} to drop below {P(LullFloor)})");
            double max = Math.Min(LullMaxSeconds, LullBaseSeconds + above);
            if (lull.Length > max)
                Add("earned", lull.Start, $"lull of {lull.Length} s; the burst before spent {above} s above {P(EarnedPeak)}, so max {max:0} s");
        }

        // 5. Sustained intensity
        foreach (var r in Runs(from, to, s => L(s) > Intense))
            if (r.Length > IntenseSeconds)
                Add("intensity", r.Start, $"{r.Length} s above {P(Intense)} (max {IntenseSeconds:0} s)");
        int veryIntense = Enumerable.Range(from, checkedSeconds).Count(s => L(s) > VeryIntense);
        if (veryIntense > VeryIntenseShare * checkedSeconds + 1e-9)
            Add("intensity", from, $"{veryIntense * 100.0 / checkedSeconds:0}% of the level above {P(VeryIntense)} (max {VeryIntenseShare * 100:0}%)");

        // Peaks of the bursts in the checked stretch (the part of each burst inside it).
        var peaks = new List<(int Second, double Value, Span Burst)>();
        foreach (var b in Runs(from, to, s => L(s) >= Burst))
        {
            int at = Enumerable.Range(b.Start, b.Length).Aggregate((x, y) => L(y) >= L(x) ? y : x); // on a plateau, its last second
            peaks.Add((at, L(at), b));
        }

        // 6. Peaks: one spike above the cap is allowed, with a release after it.
        // Each stretch above the peak cap is one spike (two can sit in the same burst).
        bool spikeUsed = false;
        foreach (var spike in Runs(from, to, s => L(s) > PeakCap + 1e-9))
        {
            int at = Enumerable.Range(spike.Start, spike.Length).Aggregate((x, y) => L(y) > L(x) ? y : x);
            double value = L(at);
            if (value > SpikeCap + 1e-9)
            {
                Add("peaks", at, $"peak {P(value)} (max {P(PeakCap)}, or one spike up to {P(SpikeCap)})");
                continue;
            }
            if (spikeUsed)
            {
                Add("peaks", at, $"a second spike above {P(PeakCap)} ({P(value)}); only one per level");
                continue;
            }
            spikeUsed = true;
            if (spike.Length > SpikeSeconds)
                Add("peaks", at, $"spike stays above {P(PeakCap)} for {spike.Length} s (max {SpikeSeconds:0} s)");
            int spikeEnd = spike.End;
            // Release: below the release level within N seconds after the spike, then held there.
            bool released = false;
            for (int s = spikeEnd + 1; s <= spikeEnd + ReleaseWithin && !released; s++)
                if (Enumerable.Range(s, (int)ReleaseHold).All(x => x <= n && L(x) < Release)) released = true;
            if (!released)
                Add("peaks", at, $"no release after the {P(value)} spike: load must drop below {P(Release)} within {ReleaseWithin:0} s and stay there {ReleaseHold:0} s");
        }

        // 7. Arc: the highest peak in the second half, and the final peak above every first-half peak.
        if (peaks.Count > 0)
        {
            double half = duration / 2.0;
            var highest = peaks.OrderByDescending(p => p.Value).ThenByDescending(p => p.Second).First();
            if (highest.Second <= half)
                Add("arc", highest.Second, $"highest peak ({P(highest.Value)}) is in the first half (half-way is {Time((int)half)})");
            var final = peaks[^1];
            var early = peaks.Where(p => p.Second <= half).ToList();
            if (early.Count > 0 && final.Second > half && final.Value <= early.Max(p => p.Value))
            {
                var top = early.OrderByDescending(p => p.Value).First();
                Add("arc", final.Second, $"final peak {P(final.Value)} doesn't top the first half's {P(top.Value)} at {Time(top.Second - 1)}");
            }
            else if (early.Count > 0 && final.Second <= half)
                Add("arc", final.Second, $"the last peak is in the first half (half-way is {Time((int)half)})");
        }
        return found;
    }

    /// <summary>Set from the tuning's "duration tail" when the rules are loaded with the game data.</summary>
    public double TailSeconds = 5;
}
