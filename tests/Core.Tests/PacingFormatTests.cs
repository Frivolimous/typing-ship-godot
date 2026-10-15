using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip.Core.Tests;

/// <summary>The phase 5 level-format additions: pace, difficulty tags, per-difficulty numbers, density.</summary>
public class PacingFormatTests
{
    static LevelScript Parse(string text) => LevelScript.Parse(text, "test.txt");

    static List<LevelEvent> Build(string text, double cps, ulong seed = 1) =>
        LevelBuilder.Build(Parse(text), new LevelContext(new Rng(seed), TestData.Tuning, cps));

    static int Ships(string text, double cps) => Build(text, cps).OfType<SpawnEvent>().Count();

    static DataError Fails(string text) => Assert.Throws<DataError>(() => Parse(text));

    const string Waves = "missile sn 0.2  5,0 5,6\nmirror\nmissile sn 0.2  4,0 4,6\nmissile sn 0.2  3,0 3,6\nmissile sn 0.2  2,0 2,6\n";

    // ---- pace ----

    [Fact]
    public void A_paced_level_has_the_same_timeline_at_every_difficulty()
    {
        var hard = Build("pace hard\n" + Waves, Difficulty.Hard).Select(e => e.Distance).ToList();
        foreach (var cps in Difficulty.Cps)
            Assert.Equal(hard, Build("pace hard\n" + Waves, cps).Select(e => e.Distance).ToList());
        // Without a pace line, long steps shrink more at higher speeds (Classic).
        Assert.NotEqual(Build(Waves, Difficulty.Easy).Select(e => e.Distance), Build(Waves, Difficulty.Hard).Select(e => e.Distance));
    }

    [Fact]
    public void A_paced_level_lasts_the_same_time_at_every_difficulty()
    {
        var data = TestData.With(("levels/level00.txt", "pace hard\n" + File.ReadAllText(Path.Combine(TestData.DataDir, "levels", "classic", "level00.txt"))));
        // The last ship comes in on the same tick at every difficulty. (The level then ends once the ships have gone,
        // which takes longer at low difficulties, where ships are slower.)
        var lastSpawn = Difficulty.Cps.Select(cps =>
        {
            long tick = 0;
            var r = Simulator.Run(data, TestData.Art, 0, cps, seed: 1, onTick: g =>
            {
                if (tick == 0 && g.LevelData.All(e => e is not SpawnEvent)) tick = g.Ticks;
            });
            Assert.True(r.Cleared, r.Line);
            return tick;
        }).ToList();
        Assert.All(lastSpawn, t => Assert.Equal(lastSpawn[0], t));
    }

    [Fact]
    public void A_Classic_level_paced_at_hard_plays_on_hard_exactly_as_Classic()
    {
        var classic = File.ReadAllText(Path.Combine(TestData.DataDir, "levels", "classic", "level04.txt"));
        var data = TestData.With(("levels/level04.txt", "pace hard\n" + classic));
        string Trace(GameData d, bool isClassic)
        {
            var sb = new StringWriter();
            Simulator.Run(d, TestData.Art, 4, Difficulty.Hard, 3, trace: sb, classic: isClassic);
            return sb.ToString();
        }
        Assert.Equal(Trace(TestData.Data, true), Trace(data, false));
    }

    [Fact]
    public void Pace_mistakes_name_the_line()
    {
        Assert.Equal(2, Fails("pace hard\npace easy\n").Line);
        Assert.Contains("pace hard", Fails("pace fast\n").Message);
        var e = Assert.Throws<DataError>(() => LevelScript.ParseSections("[boss 1 escorts]\npace hard\n", "bosses.txt"));
        Assert.Contains("level file", e.Message);
    }

    // ---- difficulty tags ----

    [Theory]
    [InlineData("hard", 0b00100)]
    [InlineData("hard+", 0b11100)]
    [InlineData("normal-", 0b00011)]
    [InlineData("normal-extreme", 0b01110)]
    [InlineData("all", 0b11111)]
    [InlineData("HARD+", 0b11100)]
    public void Difficulty_tags(string tag, int mask) => Assert.Equal(mask, DifficultyTag.Parse(tag));

    [Theory]
    [InlineData("hardest")]
    [InlineData("extreme-normal")]
    [InlineData("+")]
    public void Not_difficulty_tags(string tag) => Assert.Null(DifficultyTag.Parse(tag));

    [Fact]
    public void Tagged_lines_happen_only_at_their_difficulties()
    {
        const string text = "missile sn 0.1  5,0 5,6\nhard+: missile sn 0.1  4,0 4,6\nnormal-: laser sn 0.1  6,0\n";
        Assert.Equal(2, Ships(text, Difficulty.Easy));
        Assert.Equal(2, Ships(text, Difficulty.Normal));
        Assert.Equal(2, Ships(text, Difficulty.Hard));
        Assert.Equal("sln", Build(text, Difficulty.Easy).OfType<SpawnEvent>().Last().Type);
        Assert.Equal("smn", Build(text, Difficulty.Insane).OfType<SpawnEvent>().Last().Type);
    }

    [Fact]
    public void Only_blocks_tag_the_lines_below_them_and_copies_follow_their_ship()
    {
        const string text = "missile sn 0.1  5,0 5,6\nonly extreme+\nmissile sn 0.1  4,0 4,6\nmirror\nonly all\n" +
                            "mirror 0.1 -3\nclone 0.1 -2\n";
        // The last mirror copies the untagged first ship; the clone copies the extreme+ one, so it's extreme+ too.
        Assert.Equal(2, Ships(text, Difficulty.Hard));
        Assert.Equal(5, Ships(text, Difficulty.Extreme));
    }

    [Fact]
    public void Leaving_out_ships_keeps_the_timeline_and_the_length()
    {
        const string text = "pace hard\nmissile sn 0.1  5,0 5,6\ninsane: missile sn 0.3  4,0 4,6\ninsane: missile sn 0.3  3,0 3,6\n";
        var easy = Build(text, Difficulty.Easy);
        var insane = Build(text, Difficulty.Insane);
        Assert.Equal(insane[0].Distance, easy[0].Distance);
        Assert.Equal(insane[^1].Distance, easy[^1].Distance); // a wait marks where the level would have ended
        Assert.IsType<MarkerEvent>(easy[^1]);
    }

    [Fact]
    public void Tag_mistakes_name_the_line()
    {
        Assert.Equal(2, Fails("missile sn 0.1  5,0 5,6\nharder: mirror\n").Line);
        Assert.Contains("only TAG", Fails("only\n").Message);
        Assert.Contains("Nothing follows", Fails("hard:\n").Message);
    }

    // ---- numbers per difficulty ----

    [Fact]
    public void Numbers_can_differ_by_difficulty()
    {
        const string text = "missile sn 0.05  5,0 5,6\nrepeat 0.05 0/1/2/3/4\n";
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, Difficulty.Cps.Select(cps => Ships(text, cps)));
        Assert.Equal(0.02, Build("pace hard\nmissile sn 0.01/0.02/0.03/0.04/0.05  5,0 5,6", Difficulty.Normal)[0].Distance, 9);
    }

    [Fact]
    public void Numbers_per_difficulty_need_five_values()
    {
        Assert.Contains("five", Fails("missile sn 0.01/0.02  5,0 5,6\n").Message);
        Assert.Contains("five", Fails("missile sn 0.05  5,0 5,6\nrepeat 0.05 1/2/3\n").Message);
    }

    // ---- density ----

    [Fact]
    public void Density_keeps_waves_evenly_and_ships_arriving_together_stay_together()
    {
        const string text = "pace hard\ndensity 50%/100%/100%/100%/100%\n" + Waves;
        var easy = Build(text, Difficulty.Easy).OfType<SpawnEvent>().ToList();
        // Waves: (5,0 + its mirror), 4,0, 3,0, 2,0. Half of them: the 1st and 3rd.
        Assert.Equal(new[] { 5f, 7f, 3f }, easy.Select(e => MathF.Round(e.X / Playfield.GridX)));
        Assert.Equal(5, Build(text, Difficulty.Normal).OfType<SpawnEvent>().Count());
        Assert.Equal(Build(text, Difficulty.Normal)[^1].Distance, Build(text, Difficulty.Easy)[^1].Distance, 9);
    }

    [Fact]
    public void Density_mistakes_name_the_line()
    {
        Assert.Equal(1, Fails("density 0%\n").Line);
        Assert.Contains("five percentages", Fails("density 50%/60%\n").Message);
        Assert.Contains("five percentages", Fails("density 150%\n").Message);
        Assert.Equal(new[] { 0.6, 0.6, 0.6, 0.6, 0.6 }, Parse("density 60\n").Density);
    }

    [Fact]
    public void Both_level_sets_load_and_the_Classic_set_is_the_original()
    {
        Assert.Equal(GameData.LevelCount, TestData.Data.Classic.Levels.Count);
        Assert.Equal("levels/classic/level03.txt", TestData.Data.Classic.LevelFile(3));
        Assert.Equal("levels/level03.txt", TestData.Data.Main.LevelFile(3));
        Assert.All(TestData.Data.Classic.Levels, l => Assert.Null(l.Pace));
    }
}
