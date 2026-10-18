namespace TypingShip.Core.Tests;

public class SimulatorTests
{
    // Hard is the reference (docs/porting.md): the levels' timing is written for it, so a typist at Hard's speed or
    // faster must be able to clear them. Easy and Normal are thinned from Hard in the level files.
    public static IEnumerable<TheoryDataRow<int, int>> LevelsAndDifficulties() =>
        from level in Enumerable.Range(0, 12)
        from wpm in new[] { Difficulty.Hard, Difficulty.Extreme, Difficulty.Insane }.Select(c => (int)Speed.ToWpm(c))
        select new TheoryDataRow<int, int>(level, wpm);

    [Theory]
    [MemberData(nameof(LevelsAndDifficulties))]
    public void Perfect_bot_clears_every_level_on_Hard_and_up(int level, int wpm)
    {
        var r = Simulator.Run(TestData.Data, TestData.Art, level, Speed.FromWpm(wpm), seed: 1);
        Assert.True(r.Cleared, r.Line);
    }

    [Theory]
    [InlineData(5, 65)]
    [InlineData(10, 40)]
    public void Slow_bot_takes_damage(int level, int wpm)
    {
        var r = Simulator.Run(TestData.Data, TestData.Art, level, Speed.FromWpm(wpm), seed: 1, botCps: Speed.FromWpm(wpm * 6 / 10));
        Assert.True(!r.Cleared || r.Game.Player.Injured, r.Line);
    }
}
