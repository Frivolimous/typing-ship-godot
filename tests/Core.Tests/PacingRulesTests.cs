using TypingShip.Core.Data;

namespace TypingShip.Core.Tests;

/// <summary>The pacing rules (docs/pacing-rules.md, data/pacing-rules.txt) on made-up load curves.</summary>
public class PacingRulesTests
{
    static readonly PacingRules Rules = PacingRules.Parse(File.ReadAllText(Path.Combine(TestData.DataDir, PacingRules.FileName)));
    static readonly double Cps = Difficulty.Hard;

    /// <summary>
    /// A run whose load (demand ÷ speed) is the given value each second, and whose new letters each second add up to
    /// the same load (spread so whole letters carry the remainder), so averages and the rolling demand agree. Every
    /// second up to the last spawns something, and one word is visible unless the visible list says otherwise.
    /// </summary>
    static Pacing.Run RunOf(double[] load, int[]? visible = null, int lastSpawn = 0)
    {
        if (lastSpawn == 0) lastSpawn = load.Length - 5;
        double carry = 0;
        var samples = load.Select((l, i) =>
        {
            int letters = 0;
            if (i + 1 <= lastSpawn)
            {
                carry += l * Cps;
                letters = Math.Max(1, (int)Math.Floor(carry + 1e-9));
                carry -= letters;
            }
            return new Pacing.Sample(i + 1, 0, letters, l * Cps, 1, 5, visible?[i] ?? 1);
        }).ToList();
        return new Pacing.Run(0, Cps, true, "", samples, Rules.TailSeconds);
    }

    /// <summary>A level-1 length run (85 s to the last spawn + 5 s tail) at a steady load, to edit bits of.</summary>
    static double[] Steady(double load = 0.7) => Enumerable.Repeat(load, 90).ToArray();

    static void Set(double[] load, int fromSecond, int toSecond, double value)
    {
        for (int s = fromSecond; s <= toSecond; s++) load[s - 1] = value;
    }

    static List<PacingRules.Finding> Check(double[] load, int level = 1, int[]? visible = null, int lastSpawn = 0) =>
        Rules.Check(RunOf(load, visible, lastSpawn), level).Where(f => !f.Warning).ToList();

    static List<string> RulesBroken(double[] load, int level = 1, int[]? visible = null) =>
        Check(load, level, visible).Select(f => f.Rule).Distinct().ToList();

    [Fact]
    public void The_rules_file_loads_and_mistakes_name_the_line()
    {
        Assert.Equal(1.5, Rules.PeakCap);
        Assert.Equal(0.25, Rules.LullShare); // written as 25%
        Assert.True(Rules.IsExempt(4, "lulls") && Rules.IsExempt(3, "arc") && !Rules.IsExempt(3, "lulls"));
        var text = File.ReadAllText(Path.Combine(TestData.DataDir, PacingRules.FileName));
        Assert.Contains("Missing: peak cap", Assert.Throws<DataError>(() => PacingRules.Parse(text.Replace("peak cap = 1.5", ""))).Message);
        Assert.Contains("isn't a rule", Assert.Throws<DataError>(() => PacingRules.Parse(text + "\nexempt 2: wobble\n")).Message);
        Assert.Contains("already set", Assert.Throws<DataError>(() => PacingRules.Parse(text + "\nburst = 0.4\n")).Message);
    }

    [Fact]
    public void A_steady_level_of_the_right_length_passes() => Assert.Empty(Check(Steady()));

    [Fact]
    public void Exempt_levels_are_not_checked()
    {
        var load = Steady(0.2);
        Assert.NotEmpty(Check(load, level: 2));
        Assert.Empty(Check(load, level: 4));
    }

    [Fact]
    public void Duration_is_the_last_spawn_plus_the_tail()
    {
        Assert.DoesNotContain("duration", RulesBroken(Steady())); // 85 + 5 = 1:30, level 1's target
        Assert.Contains("duration", RulesBroken(Enumerable.Repeat(0.7, 110).ToArray())); // 1:50
        Assert.DoesNotContain("duration", RulesBroken(Enumerable.Repeat(0.7, 99).ToArray(), level: 2)); // 1:39
    }

    [Fact]
    public void Average_load_must_be_in_range()
    {
        Assert.Contains("average", RulesBroken(Steady(0.6)));
        Assert.Contains("average", RulesBroken(Steady(0.85)));
        // Off the level's target but in range: a warning only.
        var findings = Rules.Check(RunOf(Steady(0.78)), 1);
        Assert.Contains(findings, f => f.Rule == "average" && f.Warning);
    }

    [Fact]
    public void Lulls_below_the_floor_and_empty_screens_have_time_limits()
    {
        var load = Steady();
        Set(load, 20, 25, 1.2); // a big burst first, so the lull is earned (6 s above 1.0P: lulls up to 10 s)
        Set(load, 26, 33, 0.2); // 8 s below 0.3P: one too many
        Assert.Contains(Check(load), f => f.Rule == "lulls" && f.Message.StartsWith("8 s below"));
        Set(load, 33, 33, 0.4); // 7 s: fine
        Assert.DoesNotContain(Check(load), f => f.Rule == "lulls" && f.Message.Contains("below 0.30P"));

        var visible = Enumerable.Repeat(1, 90).ToArray();
        for (int s = 40; s <= 43; s++) visible[s - 1] = 0; // 4 s with nothing to see
        Assert.Contains(Check(Steady(), visible: visible), f => f.Rule == "lulls" && f.Message.Contains("empty screen for 4 s"));
    }

    [Fact]
    public void A_lull_must_be_earned_by_the_burst_before_it()
    {
        var load = Steady();
        Set(load, 30, 33, 0.9); // a burst that never passes 1.0P...
        Set(load, 34, 36, 0.2); // ...doesn't earn a dip below 0.3P
        Assert.Contains(Check(load), f => f.Rule == "earned" && f.Message.StartsWith("dips to 0.20P"));
        Set(load, 31, 33, 1.1); // 3 s above 1.0P: dips are fine, and lulls up to 4 + 3 = 7 s
        Assert.DoesNotContain(Check(load), f => f.Rule == "earned");
        Set(load, 34, 41, 0.4); // an 8 s lull
        Assert.Contains(Check(load), f => f.Rule == "earned" && f.Message.Contains("so max 7 s"));
    }

    [Fact]
    public void Intensity_above_the_player_is_limited()
    {
        var load = Steady();
        Set(load, 50, 63, 1.1); // 14 s above 1.0P
        Assert.Contains(Check(load), f => f.Rule == "intensity" && f.Message.StartsWith("14 s above"));
    }

    [Fact]
    public void One_spike_is_allowed_if_it_is_short_and_released()
    {
        var load = Steady(0.6);
        Set(load, 60, 61, 1.55); // a short spike...
        Set(load, 62, 68, 0.5);  // ...with a release (below 0.65P straight after, for 7 s)
        Assert.DoesNotContain("peaks", RulesBroken(load));

        Set(load, 62, 68, 0.7); // no release
        Assert.Contains(Check(load), f => f.Rule == "peaks" && f.Message.StartsWith("no release"));

        Set(load, 62, 68, 0.5);
        Set(load, 30, 30, 1.55); // a second spike
        Assert.Contains(Check(load), f => f.Rule == "peaks" && f.Message.StartsWith("a second spike"));

        var tooHigh = Steady(0.6);
        Set(tooHigh, 60, 60, 1.7);
        Assert.Contains(Check(tooHigh), f => f.Rule == "peaks" && f.Message.StartsWith("peak 1.70P"));
    }

    [Fact]
    public void The_level_builds_to_its_highest_peak_in_the_second_half()
    {
        // Peaks are per burst, so separate the bursts with short dips below 0.5P.
        var load = Steady(0.6);
        Set(load, 18, 18, 0.45);
        Set(load, 20, 21, 1.3); // the highest peak early...
        Set(load, 23, 23, 0.45);
        Set(load, 68, 68, 0.45);
        Set(load, 70, 71, 1.2); // ...and a final one that doesn't top it
        var arc = Check(load).Where(f => f.Rule == "arc").ToList();
        Assert.Contains(arc, f => f.Message.StartsWith("highest peak (1.30P)"));
        Assert.Contains(arc, f => f.Message.StartsWith("final peak 1.20P"));

        Set(load, 70, 71, 1.4);
        Assert.DoesNotContain("arc", RulesBroken(load));
        Assert.DoesNotContain("arc", RulesBroken(load.Select((l, i) => i < 30 ? 1.45 : l).ToArray(), level: 3)); // exempt
    }

    [Fact]
    public void The_report_matches_the_spec_s_measurements_of_the_Classic_levels()
    {
        // docs/pacing-rules.md was written from the Hard runs of the original levels (seed 1). Two of its notes:
        // level 10 spikes to about 1.69P at 1:10, and level 5's lull at 1:17 follows a bump that peaks at 0.77P.
        var l10 = Rules.Check(Pacing.Measure(TestData.Data, TestData.Art, 9, Difficulty.Hard, 1, classic: true), 10);
        Assert.Contains(l10, f => f.Rule == "peaks" && f.At == "1:10" && f.Message.StartsWith("peak 1.70P"));
        var l5 = Rules.Check(Pacing.Measure(TestData.Data, TestData.Art, 4, Difficulty.Hard, 1, classic: true), 5);
        Assert.Contains(l5, f => f.Rule == "earned" && f.At == "1:17" && f.Message.Contains("0.78P"));
    }

    [Fact]
    public void The_realistic_bot_mistypes_from_its_own_random_numbers()
    {
        // Same seed, same game; mistakes make the bot slower but don't change the level's random numbers.
        var a = Pacing.Realistic(TestData.Data, TestData.Art, 0, Difficulty.Normal, seed: 1, errorRate: 0.05);
        var b = Pacing.Realistic(TestData.Data, TestData.Art, 0, Difficulty.Normal, seed: 1, errorRate: 0.05);
        Assert.Equal(a, b);
        var clean = Pacing.Realistic(TestData.Data, TestData.Art, 0, Difficulty.Normal, seed: 1, speedShare: 1, errorRate: 0);
        Assert.True(clean.Cleared && clean.Hits == 0);
    }
}
