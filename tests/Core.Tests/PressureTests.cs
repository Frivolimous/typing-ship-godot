namespace TypingShip.Core.Tests;

/// <summary>Pressure: the typing speed needed to stop every threat before it hits (src/Core/Pressure.cs).</summary>
public class PressureTests
{
    static Pressure.Threat T(double appear, double hit, int letters, int line = 0) => new(line, appear, hit, letters);

    [Fact]
    public void Threats_due_together_add_up()
    {
        // Two 6-letter threats on screen from 0 s, both hitting at 2 s: 12 letters in 2 s = 6 letters a second.
        var (ratio, lines) = Pressure.Curve(new[] { T(0, 2, 6, 10), T(0, 2, 6, 11) }, cps: 6, seconds: 4);
        Assert.Equal(1.0, ratio[0], 9);
        Assert.Equal(1.0, ratio[1], 9);
        Assert.Equal(0, ratio[3]);
        Assert.Equal(new[] { 10, 11 }, lines[0]);
    }

    [Fact]
    public void Spread_out_threats_need_less()
    {
        // The same letters, but due 4 s apart: the most demanding stretch is one threat in 2 s.
        var (ratio, _) = Pressure.Curve(new[] { T(0, 2, 6), T(4, 6, 6) }, cps: 6, seconds: 8);
        Assert.Equal(0.5, ratio.Max(), 9);
    }

    [Fact]
    public void A_threat_seen_for_an_instant_counts_over_at_least_a_second()
    {
        var (ratio, _) = Pressure.Curve(new[] { T(5, 5.1, 3) }, cps: 3, seconds: 8);
        Assert.Equal(1.0, ratio.Max(), 9); // 3 letters over 1 s (not 0.1 s) at 3 letters a second
    }

    [Fact]
    public void Stretches_longer_than_the_window_are_not_combined()
    {
        // Two slow threats 25 s apart never form one stretch, so neither borrows the other's letters.
        var (ratio, _) = Pressure.Curve(new[] { T(0, 10, 10), T(25, 35, 10) }, cps: 1, seconds: 40);
        Assert.Equal(1.0, ratio.Max(), 9);
    }

    [Fact]
    public void A_laser_ship_is_one_threat_whose_letters_include_its_shield()
    {
        var plain = Pressure.Collect(TestData.With(("levels/level00.txt", "@0:01 laser sn  6,0")), TestData.Art, 0, Difficulty.Hard);
        var shielded = Pressure.Collect(TestData.With(("levels/level00.txt", "@0:01 laser ss  6,0")), TestData.Art, 0, Difficulty.Hard);
        var p = Assert.Single(plain);
        var s = Assert.Single(shielded);
        Assert.Equal(1, p.Line);
        Assert.True(p.Hit > p.Appear + 3, $"appears {p.Appear}, hits {p.Hit}"); // it flies in, halts and charges first
        Assert.Equal(3 + p.Letters, s.Letters); // the 3-letter shield word, then its own word
    }

    [Fact]
    public void Missiles_count_as_threats_from_the_line_of_the_ship_that_fired_them()
    {
        var threats = Pressure.Collect(TestData.With(("levels/level00.txt", "@0:01 missile mn  6,0 6,4 6,12\n+1 clone\n+1 clone\n+1 clone")),
            TestData.Art, 0, Difficulty.Insane);
        Assert.NotEmpty(threats);
        Assert.All(threats, t => Assert.Equal(3, t.Letters)); // missile ships themselves don't hurt; their missiles do
        Assert.All(threats, t => Assert.InRange(t.Line, 1, 4));
    }

    [Fact]
    public void The_rule_check_fails_above_the_max_and_warns_above_the_warning_level()
    {
        var rules = PacingRules.Parse(File.ReadAllText(Path.Combine(TestData.DataDir, PacingRules.FileName)));
        var samples = Enumerable.Range(1, 90).Select(s => new Pacing.Sample(s, 0, s <= 85 ? 4 : 0, 0.7 * Difficulty.Hard, 1, 5, 1)).ToList();
        var pressure = new double[90];
        for (int s = 30; s < 35; s++) pressure[s] = 1.1; // over the max at 0:30-0:34
        for (int s = 60; s < 63; s++) pressure[s] = 0.9; // over the warning level at 1:00-1:02
        var run = new Pacing.Run(0, Difficulty.Hard, true, "", samples, rules.TailSeconds, pressure,
            Enumerable.Range(0, 90).Select(s => new[] { s }).ToArray());
        var found = rules.Check(run, 1).Where(f => f.Rule == "pressure").ToList();
        Assert.Contains(found, f => !f.Warning && f.At == "0:30" && f.Message.Contains("1.10P"));
        Assert.Contains(found, f => f.Warning && f.At == "1:00" && f.Message.Contains("0.90P"));
        Assert.Equal(2, found.Count);
    }
}
