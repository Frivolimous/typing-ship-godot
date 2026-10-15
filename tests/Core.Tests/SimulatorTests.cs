namespace TypingShip.Core.Tests;

public class SimulatorTests
{
    public static IEnumerable<TheoryDataRow<int, int>> LevelsAndDifficulties() =>
        from level in Enumerable.Range(0, 12)
        from wpm in Difficulty.Wpm
        select new TheoryDataRow<int, int>(level, wpm);

    [Theory]
    [MemberData(nameof(LevelsAndDifficulties))]
    public void Perfect_bot_clears_every_level_at_every_difficulty(int level, int wpm)
    {
        var r = Simulator.Run(TestData.Data, TestData.Art, level, wpm, seed: 1);
        Assert.True(r.Cleared, r.Line);
    }

    [Theory]
    [InlineData(5, 25)]
    [InlineData(10, 40)]
    public void Slow_bot_takes_damage(int level, int wpm)
    {
        var r = Simulator.Run(TestData.Data, TestData.Art, level, wpm, seed: 1, botWpm: wpm * 6 / 10);
        Assert.True(!r.Cleared || r.Game.Player.Injured, r.Line);
    }
}
