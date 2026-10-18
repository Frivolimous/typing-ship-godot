using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip.Core.Tests;

/// <summary>The level file format: times in seconds (as on Hard), header constants, and the commands.</summary>
public class LevelFormatTests
{
    static LevelScript Parse(string text) => LevelScript.Parse(text, "test.txt");

    static DataError ParseFails(string text) => Assert.Throws<DataError>(() => Parse(text));

    static List<LevelEvent> Build(string text, int wpm = 65, ulong seed = 1) =>
        LevelBuilder.Build(Parse(text), new LevelContext(new Rng(seed), TestData.Tuning, Speed.FromWpm(wpm)));

    static double[] Times(string text, int wpm = 65) => Build(text, wpm).Select(e => e.Time).ToArray();

    static void Close(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i], 9);
    }

    [Fact]
    public void Reads_the_commands()
    {
        var s = Parse("""
            # a comment
            @0:02.77 missile sn  5,0 5,6 4,12   // another comment
            mirror
            +0.55 laser ss  6,0
            suicide mn  3,0 3,3
            stealth  6,7 6,0 shield
            ship lmt  3,0  move 3,3  halt 3.8  halt 2.5 fire  move 0,3
            repeat 0.55 7 1 mirrored
            insert -4
            +0.8 offset  0,-2
            clone -2
            @1:10 boss 1
            """);
        Assert.Equal(11, s.Statements.Count);
        var m = Assert.IsType<Statement.Missile>(s.Statements[0]);
        Assert.Equal(2, m.Line);
        Assert.Equal("sn", m.Type);
        Assert.Equal(2, m.To.Length);
        Assert.Equal(new Timing(Timing.Kind.At, At: 2.77), m.When);
        Assert.Equal(Timing.Same, Assert.IsType<Statement.Mirror>(s.Statements[1]).When);
        var ship = Assert.IsType<Statement.Ship>(s.Statements[5]);
        Assert.Equal(4, ship.Steps.Length);
        Assert.True(ship.Steps[2].Fire);
        Assert.Equal(3.8, ship.Steps[1].Halt!.Eval(null!, 0));
        var r = Assert.IsType<Statement.Repeat>(s.Statements[6]);
        Assert.True(r.Mirrored);
        Assert.Equal((0.55, 7.0, 1), (r.Gap.Eval(null!, 0), r.Times.Eval(null!, 0), r.Count));
    }

    [Fact]
    public void Times_come_from_anchors_and_gaps()
    {
        // @ is from the start; + is after the line above; nothing is the same moment as the line above.
        Close(new[] { 2.5, 2.5, 3.1, 70.25 }, Times("@0:02.5 laser sn  0,1\nmirror\n+0.6 laser sn  0,2\n@1:10.25 laser sn  0,3"));
    }

    [Fact]
    public void Timing_is_the_same_at_every_difficulty()
    {
        const string level = "@0:05 missile sn  5,0 5,6\n+0.55 clone\nrepeat 1.1 3 2\n@0:20 laser sn  0,4";
        var hard = Times(level);
        foreach (var wpm in Difficulty.Wpm) Close(hard, Times(level, wpm));
    }

    [Fact]
    public void Overlapping_waves_come_in_time_order()
    {
        // The repeat runs on past the next @ time; the next wave's ships still arrive at their own times, not after it.
        var events = Build("@0:10 missile sn  3,0 6,7 12,8\nrepeat 1.2 9 1 mirrored\n@0:19 suicide mn  0,2 6,3\nrepeat 1 3 1 mirrored");
        Assert.Equal(events.Select(e => e.Time).OrderBy(t => t), events.Select(e => e.Time));
        Assert.Contains(events, e => e.Line == 3 && Math.Abs(e.Time - 19) < 1e-9);
    }

    [Fact]
    public void Anchors_must_be_in_order()
    {
        var e = ParseFails("@0:20 laser sn  0,1\n@0:10 laser sn  0,2");
        Assert.Equal(2, e.Line);
        Assert.Contains("earlier than the @ time on line 1", e.Message);
        Assert.Contains("isn't a time", ParseFails("@1:5 laser sn  0,1").Message);
        Assert.Contains("under 60", ParseFails("@0:75 laser sn  0,1").Message);
    }

    [Fact]
    public void Spaces_after_commas_are_fine()
    {
        var m = Assert.IsType<Statement.Missile>(Parse("missile sn  5, 0 5 ,6").Statements[0]);
        Assert.Equal("5,0", m.From.ToString());
    }

    [Fact]
    public void Unknown_command_suggests_the_right_one()
    {
        var e = ParseFails("missile sn  5,0 5,6\nmisile sn  5,0 5,6");
        Assert.Equal(2, e.Line);
        Assert.Contains("Did you mean \"missile\"", e.Message);
    }

    [Theory]
    [InlineData("missile sn  5;0 5,6", "isn't a point")]
    [InlineData("missile sn  5,0", "at least 1 point to fly to")]
    [InlineData("missile snx  5,0 5,6", "isn't a ship type")]
    [InlineData("+fast missile sn  5,0 5,6", "isn't a number or a constant")]
    [InlineData("laser sn  5,5", "edge of the grid")]
    [InlineData("boss 4", "from 1 to 3")]
    [InlineData("repeat 0.5 2.5", "whole number from 0 to 1000")]
    [InlineData("stealth  6,7 6,0 shielded", "Expected \"shield\"")]
    [InlineData("ship lmt  3,0  fly 3,3", "isn't a step")]
    [InlineData("missile sn  5,0 5,6 1,1 2,2 3,3", "Unexpected \"3,3\"")]
    [InlineData("missile sn  5,0 3~x,6", "isn't a range")]
    [InlineData("@0:05 repeat 0.5 2", "can't have a time in front")]
    [InlineData("+-1 laser sn  0,1", "can't be negative")]
    [InlineData("@0:05", "Nothing follows")]
    public void Mistakes_say_what_is_wrong(string line, string expected)
    {
        var e = ParseFails(line);
        Assert.Equal(1, e.Line);
        Assert.Contains(expected, e.Message);
        Assert.Contains("test.txt, line 1", e.Message);
    }

    [Theory]
    [InlineData("pace hard", "no longer need a pace line")]
    [InlineData("duration 2:15", "length is now its content")]
    [InlineData("density 50%", "density was removed")]
    [InlineData("only hard+", "were removed")]
    public void Removed_features_say_what_to_do(string line, string expected) =>
        Assert.Contains(expected, ParseFails(line).Message);

    [Fact]
    public void Copies_of_copies_jitter_around_the_written_point_without_drifting()
    {
        // 40 generations of copies (each repeat copies the copy before it) and an offset chain: every point stays
        // within the jitter of where it was written, and every halt is as long as the first.
        var ships = Build("@0:01 ship mmn  3,0  move 3,4  halt 2  move 6,8\nrepeat 0.2 40 1 mirrored\n" +
                          "@0:20 ship mmn  1,0  move 1,4  move 1,13\n+0.5 offset  1,0\n+0.5 offset  1,0\n+0.5 offset  1,0\n+0.5 offset  1,0")
            .Cast<SpawnEvent>().ToList();
        float j = TestData.Tuning.Jitter;
        for (int i = 0; i <= 40; i++)
        {
            var cmd = ships[i].Commands;
            float x = (i % 2 == 0 ? 3 : 9) * Playfield.GridX;
            Assert.InRange(cmd[0].X, x - j, x + j);
            Assert.InRange(cmd[0].Y, 4 * Playfield.GridY - j, 4 * Playfield.GridY + j);
            Assert.Equal(ships[0].Commands[1].Timer, cmd[1].Timer);
        }
        for (int k = 0; k < 5; k++)
            Assert.InRange(ships[41 + k].Commands[0].X, (1 + k) * Playfield.GridX - j, (1 + k) * Playfield.GridX + j);
    }

    [Fact]
    public void Repeat_over_spreads_the_copies_so_the_last_comes_at_the_time_given()
    {
        Close(new[] { 1.0, 1.5, 2, 2.5, 3 }, Times("@0:01 missile sn  5,0 5,6\nrepeat_over 2 4"));
        // A pair keeps its spacing; the last ship comes 3 s after the line above, as with repeat 0.5 3 2.
        const string pairs = "@0:08 missile sn  0,3 7,3 12,3\n+0.5 missile sn  12,6 5,6 0,6\n";
        Close(Times(pairs + "repeat 0.5 3 2"), Times(pairs + "repeat_over 3 3 2"));
        Close(new[] { 8, 8.5, 11, 11.5 }, Times(pairs + "repeat_over 3 1/2/3/3/3 2", 25));
        Close(new[] { 8, 8.5 }, Times(pairs + "repeat_over 3 0/2/3/3/3 2", 25));
        var mirrored = Build("@0:01 missile sn  2,0 2,4\nrepeat_over 2 2 1 mirrored").Cast<SpawnEvent>().Select(e => e.X).ToArray();
        Assert.Equal(new[] { 100f, 500f, 100f }, mirrored);
    }

    [Fact]
    public void Repeat_over_too_short_for_its_copies_says_so()
    {
        var e = Assert.Throws<DataError>(() => Build("@0:08 missile sn  0,3 7,3\n+0.5 missile sn  12,6 5,6\nrepeat_over 0.6 3 2"));
        Assert.Contains("at least 0.5 s", e.Message);
        Assert.Equal(3, e.Line);
        Assert.Contains("whole number", ParseFails("@0:01 laser sn  0,1\nrepeat_over 2 1.5").Message);
        Assert.Contains("number of times", ParseFails("@0:01 laser sn  0,1\nrepeat_over 2").Message);
    }

    [Fact]
    public void Numbers_per_difficulty_work_in_a_line()
    {
        const string level = "@0:01 missile sn  5,0 5,6\nrepeat 2/1/0.5/0.5/0.5 1/2/4/4/4\n+1/1/0.5/0.5/0.5 laser sn  0,1/2/3/3/3";
        Close(new[] { 1.0, 3, 4 }, Times(level, 25));
        Close(new[] { 1.0, 2, 3, 4 }, Times(level, 40));
        Close(new[] { 1.0, 1.5, 2, 2.5, 3, 3.5 }, Times(level, 65));
        Assert.Equal(1 * Playfield.GridY, Build(level, 25).Cast<SpawnEvent>().Last().Y);
        Assert.Equal(3 * Playfield.GridY, Build(level, 90).Cast<SpawnEvent>().Last().Y);
    }

    [Theory]
    [InlineData("@0:01 laser sn  0,1\nrepeat 0.5 1/2/3/4", "one number per difficulty")]
    [InlineData("@0:01 laser sn  0,1\nrepeat 0.5 1/2/2.5/4/4", "whole number")]
    [InlineData("@0:01 laser sn  0,1\n+1/1/-1/1/1 mirror", "can't be negative")]
    public void Bad_numbers_per_difficulty_say_what_is_wrong(string text, string expected) =>
        Assert.Contains(expected, ParseFails(text).Message);

    [Fact]
    public void Difficulty_tags_leave_ships_out_below_their_difficulty()
    {
        const string level = "@0:01 laser sn  0,1\nnormal+: +1 laser sn  12,1\n+1 laser sn  0,2\nhard+: +1 laser sn  12,2";
        Close(new[] { 1.0, 3 }, Times(level, 25)); // the line after a tagged one keeps its time
        Close(new[] { 1.0, 2, 3 }, Times(level, 40));
        Close(new[] { 1.0, 2, 3, 4 }, Times(level, 65));
        Close(new[] { 1.0, 2, 3, 4 }, Times(level, 90));
    }

    [Fact]
    public void Copies_of_a_tagged_ship_go_with_it_and_a_tagged_copy_goes_alone()
    {
        Assert.Empty(Build("normal+: @0:01 missile sn  5,0 5,6\nmirror\nrepeat 0.5 2", 25));
        Assert.Equal(4, Build("normal+: @0:01 missile sn  5,0 5,6\nmirror\nrepeat 0.5 2", 40).Count);
        Assert.Single(Build("@0:01 laser sn  0,1\nnormal+: mirror", 25));
        Assert.Equal(2, Build("@0:01 laser sn  0,1\nnormal+: mirror", 40).Count);
        // A tagged repeat leaves out its copies, not the ship it copies.
        Assert.Single(Build("@0:01 missile sn  5,0 5,6\nnormal+: repeat 0.5 3", 25));
    }

    [Fact]
    public void Tags_dont_change_the_ships_that_stay()
    {
        // Tagged ships are still built (random numbers drawn) on every difficulty, so the rest fly the same paths.
        const string level = "@0:01 missile sn  5,0 5,6 4,12\nhard+: missile sn  3,0 3,6\n+1 missile sn  7,0 7,6 4,12";
        var easy = Build(level, 25).Cast<SpawnEvent>().ToList();
        var hard = Build(level, 65).Cast<SpawnEvent>().ToList();
        Assert.Equal(2, easy.Count);
        foreach (var (e, h) in easy.Zip(new[] { hard[0], hard[2] }))
        {
            Assert.Equal(h.Time, e.Time);
            Assert.Equal(h.Commands.Select(c => (c.X, c.Y)), e.Commands.Select(c => (c.X, c.Y)));
        }
    }

    [Theory]
    [InlineData("normal: laser sn  0,1", "isn't a difficulty tag")]
    [InlineData("hard: laser sn  0,1", "Did you mean \"hard+:\"")]
    [InlineData("easy+: laser sn  0,1", "every difficulty")]
    [InlineData("normal+:", "Nothing follows the tag")]
    public void Bad_tags_say_what_is_wrong(string line, string expected) =>
        Assert.Contains(expected, ParseFails(line).Message);

    [Fact]
    public void Header_constants_work_anywhere_a_number_goes()
    {
        var events = Build("HOVER = 3.8\nGAP = 0.5\nCOL = 4\n@0:01 ship lmn  COL,0  move COL,3  halt HOVER  move 0,3\n+GAP clone\nrepeat GAP 2");
        Close(new[] { 1, 1.5, 2, 2.5 }, events.Select(e => e.Time).ToArray());
        var ship = events.Cast<SpawnEvent>().First();
        Assert.Equal(4 * Playfield.GridX, ship.X);
        Assert.Equal(Speed.FloorTicks(3.8 * Playfield.TickRate), ship.Commands[1].Timer); // 3.8 s on Hard
    }

    [Fact]
    public void Per_difficulty_constants_are_header_only()
    {
        const string level = "GAP = 1/1/0.5/0.25/0.25\n@0:01 laser sn  0,1\n+GAP laser sn  0,2";
        Assert.Equal(2, Times(level, 25)[1], 9);
        Assert.Equal(1.5, Times(level, 65)[1], 9);
        Assert.Equal(1.25, Times(level, 90)[1], 9);
        Assert.Contains("in the header", ParseFails("@0:01 laser sn  0,1\nGAP = 2").Message);
        Assert.Contains("five, Easy to Insane", ParseFails("GAP = 1/2").Message);
        Assert.Contains("already set", ParseFails("GAP = 1\nGAP = 2").Message);
        Assert.Contains("The header has: GAP", ParseFails("GAP = 1\n+GPA laser sn  0,1").Message);
    }

    [Fact]
    public void Halts_are_seconds_on_Hard_and_scale_with_cps_elsewhere()
    {
        int Timer(int wpm) => Build("ship lmn  3,0  halt 2  move 0,3", wpm).Cast<SpawnEvent>().Single().Commands[0].Timer;
        Assert.Equal(48, Timer(65)); // 2 s at 24 ticks a second
        Assert.Equal(Speed.FloorTicks(2 * 24 * 65 / 25.0), Timer(25)); // longer on Easy, where ships are slower too
        Assert.Equal(Speed.FloorTicks(2 * 24 / Math.Sqrt(90 / 65.0)), Timer(90)); // above Hard, the flattened pace (tuning: pace above hard 0.5)
    }

    [Fact]
    public void Copying_with_nothing_above_is_an_error_with_the_line()
    {
        var e = Assert.Throws<DataError>(() => Build("\n\nmirror"));
        Assert.Equal(3, e.Line);
        Assert.Contains("only 0 events", e.Message);
        Assert.Contains("not a ship", Assert.Throws<DataError>(() => Build("boss 1\nmirror")).Message);
        Assert.Contains("insert -5", Assert.Throws<DataError>(() => Build("laser sn  6,0\ninsert -5")).Message);
    }

    [Fact]
    public void Unknown_ship_code_is_an_error_with_the_line()
    {
        var e = Assert.Throws<DataError>(() => Build("laser sn  6,0\nship qmn  3,0  move 3,3"));
        Assert.Equal(2, e.Line);
        Assert.Contains("\"qmn\"", e.Message);
    }

    [Fact]
    public void Mirror_flips_left_and_right()
    {
        var events = Build("laser sn  0,4\n+0.55 mirror").Cast<SpawnEvent>().ToList();
        Assert.Equal(0, events[0].X);
        Assert.Equal(Playfield.ViewWidth, events[1].X);
        Assert.Equal(events[0].Y, events[1].Y);
        Assert.Equal(Playfield.ViewWidth - events[0].Commands[0].X, events[1].Commands[0].X, 20f); // targets jitter by up to 10 px each
    }

    [Fact]
    public void Repeat_copies_the_last_ships_keeping_their_spacing()
    {
        var events = Build("@0:05 missile sn  0,3 7,3 12,3\n+1.1 missile sn  12,6 5,6 0,6\nrepeat 2.75 2 2");
        Assert.Equal(6, events.Count);
        Assert.Equal(new[] { 0f, 600f, 0f, 600f, 0f, 600f }, events.Cast<SpawnEvent>().Select(e => e.X).ToArray());
        // Each pair keeps the 1.1 s between its two ships; each pair starts 2.75 s after the one before ends.
        Close(new[] { 5, 6.1, 8.85, 9.95, 12.7, 13.8 }, events.Select(e => e.Time).ToArray());
    }

    [Fact]
    public void Insert_moves_the_last_event_back()
    {
        var events = Build("@0:02 laser sn  0,1\n+2 laser sn  0,2\n+2 laser sn  0,3\nlaser sn  12,9\ninsert -3");
        Assert.Equal(new[] { 1, 4, 2, 3 }, events.Select(e => e.Line).ToArray());
        Assert.Equal(events[2].Time, events[1].Time); // it takes the place (and time) of the one it went before
    }

    [Fact]
    public void Boss_escorts_count_from_when_the_batch_starts()
    {
        var events = LevelBuilder.Build(Parse("@0:02 laser sn  0,1\n+1 mirror"), new LevelContext(new Rng(1), TestData.Tuning, Difficulty.Hard), 100);
        Close(new[] { 102.0, 103.0 }, events.Select(e => e.Time).ToArray());
    }

    [Fact]
    public void Random_numbers_are_rolled_per_build()
    {
        var a = Build("laser ms  0,6~9", seed: 1).Cast<SpawnEvent>().Single().Y;
        var b = Build("laser ms  0,6~9", seed: 2).Cast<SpawnEvent>().Single().Y;
        Assert.NotEqual(a, b);
        Assert.InRange(a, 6 * Playfield.GridY, 9 * Playfield.GridY);
        var x = Build("stealth  1,1 0|12,5").Cast<SpawnEvent>().Single().Commands[^1].X;
        Assert.True(x is 0 or 600, $"{x}");
    }

    [Fact]
    public void Every_real_level_and_boss_section_loads()
    {
        Assert.Equal(GameData.LevelCount, TestData.Data.Main.Levels.Count);
        Assert.Equal(GameData.LevelCount, TestData.Data.Classic.Levels.Count);
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
