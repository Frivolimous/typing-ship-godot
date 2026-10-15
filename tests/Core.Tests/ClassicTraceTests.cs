using System.Security.Cryptography;
using System.Text;

namespace TypingShip.Core.Tests;

/// <summary>
/// The Classic levels (the original 12) must keep playing exactly as they did: every level at every difficulty, a
/// perfect bot on two seeds and a slow bot on one, gives the same per-tick trace as the file classic-traces.txt
/// records (a hash per run). If a change is meant to alter how the Classic levels play, regenerate the file by
/// running the tests with UPDATE_CLASSIC_TRACES=1 and say why in the commit.
/// </summary>
public class ClassicTraceTests
{
    static string GoldenPath => Path.Combine(TestData.RepoRoot, "tests", "Core.Tests", "classic-traces.txt");

    static IEnumerable<(string Key, Func<string> Trace)> Runs() =>
        from level in Enumerable.Range(0, 12)
        from wpm in Difficulty.Wpm
        from run in new[] { (seed: 1UL, slow: false), (seed: 2UL, slow: false), (seed: 1UL, slow: true) }
        select ($"level {level} wpm {wpm} seed {run.seed}{(run.slow ? " slow" : "")}",
            (Func<string>)(() => TraceOf(level, wpm, run.seed, run.slow)));

    static string TraceOf(int level, int wpm, ulong seed, bool slow)
    {
        var sb = new StringWriter();
        Simulator.Run(TestData.Data, TestData.Art, level, Speed.FromWpm(wpm), seed, slow ? Speed.FromWpm(wpm * 6 / 10) : 0, sb);
        return sb.ToString();
    }

    static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..16];

    [Fact]
    public void Classic_levels_play_tick_for_tick_as_recorded()
    {
        var actual = Runs().AsParallel().AsOrdered().Select(r => $"{r.Key}: {Hash(r.Trace())}").ToList();
        if (Environment.GetEnvironmentVariable("UPDATE_CLASSIC_TRACES") == "1")
        {
            File.WriteAllLines(GoldenPath, actual);
            return;
        }
        var expected = File.ReadAllLines(GoldenPath);
        var diffs = actual.Zip(expected).Where(p => p.First != p.Second).Select(p => $"now {p.First}, was {p.Second}").ToList();
        Assert.True(expected.Length == actual.Count && diffs.Count == 0,
            $"{diffs.Count} of {actual.Count} Classic runs play differently:\n" + string.Join("\n", diffs.Take(20)));
    }
}
