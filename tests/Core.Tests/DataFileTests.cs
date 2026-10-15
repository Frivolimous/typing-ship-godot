using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip.Core.Tests;

public class TuningTests
{
    static string Real => File.ReadAllText(Path.Combine(TestData.DataDir, "tuning.txt"));

    static DataError Fails(string text) => Assert.Throws<DataError>(() => Tuning.Parse(text));

    [Fact]
    public void Real_file_matches_the_original_numbers()
    {
        var t = Tuning.Parse(Real);
        var original = new Tuning(); // the defaults are the original game's numbers
        foreach (var f in typeof(Tuning).GetFields().Where(f => f.FieldType.IsPrimitive))
            Assert.True(Equals(f.GetValue(original), f.GetValue(t)), $"{f.Name}: {f.GetValue(t)}, originally {f.GetValue(original)}");
        Assert.Equal(original.Sizes, t.Sizes);
        Assert.Equal(original.DefencePoints, t.DefencePoints);
        Assert.Equal(original.HullHealth, t.HullHealth);
    }

    [Fact]
    public void Misspelt_setting_names_the_line_and_suggests()
    {
        var e = Fails(Real.Replace("turret range = 100", "turet range = 100"));
        Assert.Contains("turet range", e.Message);
        Assert.Contains("Did you mean \"turret range\"", e.Message);
        Assert.Equal(Real.Split('\n').ToList().FindIndex(l => l.StartsWith("turret range")) + 1, e.Line);
    }

    [Fact]
    public void Numbers_can_be_fractions()
    {
        Assert.Equal(100 / 3.0, Tuning.Parse(Real).HaltScale);
        Assert.Equal(2.5, Tuning.Parse(Real.Replace("stealth fade cps = 2.5", "stealth fade cps = 5 / 2")).StealthFadeCps);
        Assert.Contains("isn't a number", Fails(Real.Replace("stealth fade cps = 2.5", "stealth fade cps = 5/0")).Message);
    }

    [Fact]
    public void Missing_setting_is_an_error()
    {
        var e = Fails(Real.Replace("laser charge = 500", ""));
        Assert.Contains("[enemy weapons] laser charge", e.Message);
    }

    [Fact]
    public void Bad_number_is_an_error()
    {
        var e = Fails(Real.Replace("overflow = 30", "overflow = thirty"));
        Assert.Contains("\"thirty\" isn't a number", e.Message);
    }

    [Fact]
    public void Whole_numbers_must_be_whole()
    {
        var e = Fails(Real.Replace("overflow = 30", "overflow = 30.5"));
        Assert.Contains("overflow", e.Message);
    }

    [Fact]
    public void Duplicate_and_unknown_section_are_errors()
    {
        Assert.Contains("set twice", Fails(Real.Replace("turret range = 100", "turret range = 100\nturret range = 90")).Message);
        Assert.Contains("Did you mean \"bosses\"", Fails(Real.Replace("[bosses]", "[boses]")).Message);
    }

    [Fact]
    public void Ship_table_rows_need_every_field()
    {
        var e = Fails(Real.Replace("s = letters 4, points 0, speed 0", "s = letters 4, speed 0"));
        Assert.Contains("Missing points", e.Message);
    }

    [Fact]
    public void Hulls_must_use_known_sizes()
    {
        var e = Fails(Real.Replace("xm = 1", "xm = 1\nqm = 2"));
        Assert.Contains("\"qm\"", e.Message);
    }
}

public class LevelFormatTests
{
    static LevelScript Parse(string text) => LevelScript.Parse(text, "test.txt");

    static DataError ParseFails(string text) => Assert.Throws<DataError>(() => Parse(text));

    static List<LevelEvent> Build(string text, int wpm = 40, ulong seed = 1) =>
        LevelBuilder.Build(Parse(text), new LevelContext(new Rng(seed), TestData.Tuning, Speed.FromWpm(wpm)));

    [Fact]
    public void Reads_the_original_helpers()
    {
        var s = Parse("""
            # a comment
            missile sn 0.05  5,0 5,6 4,12   // another comment
            mirror
            laser ss 0.1  6,0
            suicide mn 0.14  3,0 3,3
            stealth 0.05  6,7 6,0 shield
            ship lmt 0.1  3,0  move 3,3  halt 15  halt 10 fire  move 0,3
            repeat 0.01 7 1 mirrored
            insert -4
            offset 0.015  0,-2
            clone 0.01 -2
            boss 0.2 1
            """);
        Assert.Equal(11, s.Statements.Count);
        var m = Assert.IsType<Statement.Missile>(s.Statements[0]);
        Assert.Equal(2, m.Line);
        Assert.Equal("sn", m.Type);
        Assert.Equal(2, m.To.Length);
        var ship = Assert.IsType<Statement.Ship>(s.Statements[5]);
        Assert.Equal(4, ship.Steps.Length);
        Assert.True(ship.Steps[2].Fire);
        Assert.Equal(Step.Kind.Halt, ship.Steps[2].Type);
        var r = Assert.IsType<Statement.Repeat>(s.Statements[6]);
        Assert.True(r.Mirrored);
        Assert.Equal((7.0, 1), (r.Times.Eval(null!, 0), r.Count));
    }

    [Fact]
    public void Spaces_after_commas_are_fine()
    {
        var m = Assert.IsType<Statement.Missile>(Parse("missile sn 0.05  5, 0 5 ,6").Statements[0]);
        Assert.Equal("5,0", m.From.ToString());
    }

    [Fact]
    public void Unknown_command_suggests_the_right_one()
    {
        var e = ParseFails("missile sn 0.05  5,0 5,6\nmisile sn 0.05  5,0 5,6");
        Assert.Equal(2, e.Line);
        Assert.Contains("Did you mean \"missile\"", e.Message);
    }

    [Theory]
    [InlineData("missile sn 0.05  5;0 5,6", "isn't a point")]
    [InlineData("missile sn 0.05  5,0", "at least 1 point to fly to")]
    [InlineData("missile snx 0.05  5,0 5,6", "isn't a ship type")]
    [InlineData("missile sn fast  5,0 5,6", "isn't a number")]
    [InlineData("laser sn 0.1  5,5", "edge of the grid")]
    [InlineData("boss 0.2 4", "from 1 to 3")]
    [InlineData("repeat 0.1 two", "isn't a whole number")]
    [InlineData("stealth 0.05  6,7 6,0 shielded", "Expected \"shield\"")]
    [InlineData("ship lmt 0.1  3,0  fly 3,3", "isn't a step")]
    [InlineData("missile sn 0.05  5,0 5,6 1,1 2,2 3,3", "Unexpected \"3,3\"")]
    [InlineData("missile sn 0.05  5,0 3~x,6", "isn't a range")]
    public void Mistakes_say_what_is_wrong(string line, string expected)
    {
        var e = ParseFails(line);
        Assert.Equal(1, e.Line);
        Assert.Contains(expected, e.Message);
        Assert.Contains("test.txt, line 1", e.Message);
    }

    [Fact]
    public void Copying_with_nothing_above_is_an_error_with_the_line()
    {
        var e = Assert.Throws<DataError>(() => Build("\n\nmirror"));
        Assert.Equal(3, e.Line);
        Assert.Contains("only 0 events", e.Message);
        Assert.Contains("not a ship", Assert.Throws<DataError>(() => Build("boss 0.1 1\nmirror")).Message);
        Assert.Contains("insert -5", Assert.Throws<DataError>(() => Build("laser sn 0.1  6,0\ninsert -5")).Message);
    }

    [Fact]
    public void Unknown_ship_code_is_an_error_with_the_line()
    {
        var e = Assert.Throws<DataError>(() => Build("laser sn 0.1  6,0\nship qmn 0.1  3,0  move 3,3"));
        Assert.Equal(2, e.Line);
        Assert.Contains("\"qmn\"", e.Message);
    }

    [Fact]
    public void Mirror_flips_left_and_right()
    {
        var events = Build("laser sn 0.1  0,4\nmirror 0.01").Cast<SpawnEvent>().ToList();
        Assert.Equal(0, events[0].X);
        Assert.Equal(Playfield.ViewWidth, events[1].X);
        Assert.Equal(events[0].Y, events[1].Y);
        Assert.Equal(Playfield.ViewWidth - events[0].Commands[0].X, events[1].Commands[0].X, 20f); // targets jitter by up to 10 px each
    }

    [Fact]
    public void Repeat_copies_the_last_ships_keeping_their_spacing()
    {
        var events = Build("missile sn 0.1  0,3 7,3 12,3\nmissile sn 0.02  12,6 5,6 0,6\nrepeat 0.05 2 2");
        Assert.Equal(6, events.Count);
        var types = events.Cast<SpawnEvent>().Select(e => e.X).ToList();
        Assert.Equal(new[] { 0f, 600f, 0f, 600f, 0f, 600f }, types);
        // Each pair keeps the 0.02 gap between its two ships; pairs are 0.05 apart.
        Assert.Equal(0.02, events[3].Distance - events[2].Distance, 9);
        Assert.Equal(0.05, events[2].Distance - events[1].Distance, 9);
    }

    [Fact]
    public void Insert_moves_the_last_event_back()
    {
        var events = Build("laser sn 0.05  0,1\nlaser sn 0.05  0,2\nlaser sn 0.05  0,3\nlaser sn 0  12,9\ninsert -3");
        Assert.Equal(new[] { 1, 4, 2, 3 }, events.Select(e => e.Line).ToArray());
        Assert.Equal(events[2].Distance, events[1].Distance); // it takes the place (and distance) of the one it went before
    }

    [Fact]
    public void Long_steps_shrink_at_higher_speeds()
    {
        Assert.Equal(0.2, Build("laser sn 0.2  0,1", wpm: 1)[0].Distance, 2);
        Assert.Equal(0.2 * (1 - 90 / 300.0), Build("laser sn 0.2  0,1", wpm: 90)[0].Distance, 9);
        Assert.Equal(0.05, Build("laser sn 0.05  0,1", wpm: 90)[0].Distance, 9); // short steps don't
    }

    [Fact]
    public void Random_numbers_are_rolled_per_build()
    {
        var a = Build("laser ms 0.3  0,6~9", seed: 1).Cast<SpawnEvent>().Single().Y;
        var b = Build("laser ms 0.3  0,6~9", seed: 2).Cast<SpawnEvent>().Single().Y;
        Assert.NotEqual(a, b);
        Assert.InRange(a, 6 * Playfield.GridY, 9 * Playfield.GridY);
        var x = Build("stealth 0.3  1,1 0|12,5").Cast<SpawnEvent>().Single().Commands[^1].X;
        Assert.True(x is 0 or 600, $"{x}");
    }

    [Fact]
    public void Every_real_level_and_boss_section_loads()
    {
        Assert.Equal(GameData.LevelCount, TestData.Data.Main.Levels.Count);
        Assert.Equal(4, TestData.Data.Main.BossScripts.Count);
    }

    [Fact]
    public void Missing_boss_section_is_an_error()
    {
        var text = File.ReadAllText(Path.Combine(TestData.DataDir, "levels", "bosses.txt")).Replace("[boss 2 escorts]", "[boss two escorts]");
        var e = Assert.Throws<DataError>(() => TestData.With(("levels/bosses.txt", text)));
        Assert.Contains("[boss 2 escorts] is missing", e.Message);
    }
}
