using TypingShip.Core.Data;

namespace TypingShip.Core.Tests;

public class TypingTestTests
{
    [Fact]
    public void The_passages_are_prose_of_about_250_characters()
    {
        var passages = TestData.Data.TestPassages;
        Assert.InRange(passages.Count, 1, 10);
        foreach (var p in passages)
        {
            Assert.InRange(p.Length, 200, 300);
            Assert.DoesNotContain("  ", p);
        }
    }

    [Fact]
    public void Passage_file_mistakes_name_the_line()
    {
        var e = Assert.Throws<DataError>(() => TypingTestRun.ParsePassages("# note\n\nToo short.\n", "typing-test.txt"));
        Assert.Equal(3, e.Line);
        var long1 = string.Join(" ", Enumerable.Repeat("word", 40));
        e = Assert.Throws<DataError>(() => TypingTestRun.ParsePassages($"{long1}\n{long1}\ncafé", "typing-test.txt"));
        Assert.Equal(3, e.Line);
        Assert.Contains("é", e.Message);
    }

    [Fact]
    public void Lines_join_into_one_passage_and_blank_lines_separate_them()
    {
        var a = string.Join(" ", Enumerable.Repeat("alpha", 15));
        var p = TypingTestRun.ParsePassages($"{a}\n  {a}  # a note\n\n\n{a} {a}\n", "t");
        Assert.Equal(2, p.Count);
        Assert.Equal(a + " " + a, p[0]);
    }

    [Fact]
    public void The_clock_starts_on_the_first_key_and_net_wpm_counts_correct_characters()
    {
        var t = new TypingTestRun("ab cd");
        Assert.Equal(90, t.Remaining(1000));
        Assert.False(t.Type('x', 10)); // a wrong first key starts the clock
        Assert.Equal(1, t.Errors);
        Assert.True(t.Type('A', 11)); // case doesn't matter
        foreach (var c in "b cd") t.Type(c, 12);
        Assert.True(t.Finished);
        Assert.Equal(2, t.Seconds);
        Assert.Equal(5 / 2.0 * 12, t.NetWpm, 9); // 5 characters in 2 seconds = 30 WPM
        Assert.Equal(5 / 6.0, t.Accuracy, 9);
    }

    [Fact]
    public void The_test_ends_at_90_seconds()
    {
        var t = new TypingTestRun(new string('a', 300));
        for (int i = 0; i < 100; i++) t.Type('a', i * 0.5); // 2 characters a second for 50 s
        Assert.False(t.Tick(89));
        Assert.True(t.Tick(90));
        Assert.False(t.Type('a', 91));
        Assert.Equal(100, t.Typed);
        Assert.Equal(90, t.Seconds);
        Assert.Equal(100 / 90.0 * 12, t.NetWpm, 9);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(10, 0)]  // below Easy still suggests Easy
    [InlineData(25, 0)]
    [InlineData(64, 1)]
    [InlineData(65, 2)]
    [InlineData(89, 3)]
    [InlineData(150, 4)]
    public void The_suggestion_is_the_hardest_difficulty_at_or_below_the_result(int wpm, int difficulty) =>
        Assert.Equal(difficulty, TypingTestRun.Suggest(wpm));
}
