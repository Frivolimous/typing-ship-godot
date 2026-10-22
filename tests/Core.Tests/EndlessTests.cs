using TypingShip.Core.Data;

namespace TypingShip.Core.Tests;

/// <summary>Endless mode: levels/endless.txt, the wave generator, and its best scores.</summary>
public class EndlessTests
{
    [Fact]
    public void The_endless_file_has_patterns_at_every_level_within_the_guidelines()
    {
        var waves = TestData.Data.EndlessWaves;
        Assert.Equal(Enumerable.Range(1, EndlessWave.MaxLevel), waves.Select(w => w.Level).Distinct().Order());
        // The guidelines aren't rules (a pattern may stray), but the shipped library keeps to them.
        foreach (var w in waves)
        {
            var m = w.Guide(TestData.Tuning);
            Assert.True(m.GuideLevel == w.Level, $"{w.Name}: works out to level {m.GuideLevel}");
            Assert.InRange(m.Letters, m.MinLetters, m.MaxLetters);
            Assert.True(m.DiversOk, $"{w.Name}: {m.DiversWithRoom} of {m.Divers} diving suicide ships come from the side or bottom and move {EndlessWave.DiverMinMove}");
        }
    }

    [Fact]
    public void The_guidelines_count_words_shields_turrets_and_a_stealth_missile()
    {
        var w = EndlessWave.Parse("[level 3 test]\n@0:00 missile ss  6,0 6,5 0,10\n+1 missile lt  0,1 9,3 0,6\n+1 stealth  3,5 0,12", "endless.txt")[0];
        var m = w.Guide(TestData.Tuning);
        Assert.Equal(4 * 2 + (8 + 4) + (7 + 3), m.Letters); // shielded small; large with a turret; stealth and a missile
        Assert.Equal(2 + 2 + 2, m.Words);
        Assert.Equal(2, m.SpawnTime, 9);
        Assert.Equal(EndlessWave.LevelFor(m.Letters + m.Words, 2), m.GuideLevel);
        // A diving suicide ship counts its word and 3 letters more; a landmine just its word.
        var d = EndlessWave.Parse("[level 3 test]\n@0:00 suicide mn  3,0 3,3\n+1 suicide sn  1.5,0 1.5,6", "endless.txt")[0].Guide(TestData.Tuning);
        Assert.Equal(3 + EndlessWave.DiverPremium + 3, d.Letters);
        Assert.Equal(2, d.Words);
        Assert.Equal(1, d.Divers);
        Assert.Equal(0, d.DiversWithRoom); // from the top, and only 3 squares
        Assert.False(d.DiversOk);
        var ok = EndlessWave.Parse("[level 3 test]\n@0:00 suicide mn  0,2 5,4\n+1 suicide mn  1,12 4,6\n+1 suicide mn  3,0 3,3", "endless.txt")[0].Guide(TestData.Tuning);
        Assert.Equal((3, 2), (ok.Divers, ok.DiversWithRoom)); // from the side and the bottom, 5 squares or more
        Assert.True(ok.DiversOk);
        // The inverse: at the guideline spawn time for a level, the pattern is that level; any slower, the one below.
        Assert.Equal(3, EndlessWave.LevelFor(20, EndlessWave.SpawnTimeFor(20, 3) - 0.01));
        Assert.Equal(2, EndlessWave.LevelFor(20, EndlessWave.SpawnTimeFor(20, 3) + 0.1));
    }

    [Fact]
    public void A_run_keeps_sending_waves_and_only_ends_when_the_ship_is_destroyed()
    {
        var g = TestData.NewGame(wpm: 65);
        g.Invincible = true;
        g.NewEndless(7);
        for (int i = 0; i < 6 * 60 * Playfield.TickRate; i++) g.Step(); // six minutes, nobody typing
        Assert.False(g.LevelEnded);
        Assert.True(g.Endless!.Wave > 8, $"wave {g.Endless.Wave}");
        Assert.Equal(g.Endless.SpeedOf(g.Endless.Wave), g.PaceScale); // things speed up with the waves...
        Assert.True(g.PaceScale > 1);
        Assert.Equal(g.FireRate(g.Endless.CpsOf(g.Endless.Wave)), g.MRate); // ...and fire more often
        Assert.True(g.MRate > g.FireRate(g.Cps));

        // And a ship that isn't invincible goes down in the end.
        var lost = Simulator.Run(TestData.Data, TestData.Art, 0, Difficulty.Hard, 7, botCps: Speed.FromWpm(20), endless: true);
        Assert.False(lost.Cleared);
        Assert.StartsWith("Endless run over", lost.Line);
    }

    [Fact]
    public void The_same_seed_sends_the_same_waves_whatever_the_player_types()
    {
        // Typing changes when each wave starts (the next waits for the last to be cleared), not what it holds.
        List<string> Waves(bool typing)
        {
            var g = TestData.NewGame(wpm: 65);
            g.Invincible = true;
            g.NewEndless(42);
            var bot = new AutoTyper(g, 0);
            var waves = new List<string>();
            for (int i = 0; i < 4 * 60 * Playfield.TickRate && g.Endless!.Wave < 8; i++)
            {
                int before = g.Endless!.Wave;
                g.Step();
                if (typing) bot.Step();
                if (g.Endless.Wave != before) waves.Add($"{g.Endless.Wave}: {string.Join(", ", g.Endless.Patterns)}");
            }
            return waves;
        }
        var typed = Waves(true);
        Assert.Equal(8, typed.Count); // waves 1 to 8
        Assert.Equal(typed, Waves(false).Take(typed.Count));
    }

    [Fact]
    public void A_wave_has_one_missile_only_pattern_or_one_for_every_three()
    {
        var g = TestData.NewGame(wpm: 65);
        g.Invincible = true;
        g.NewEndless(5);
        var byName = TestData.Data.EndlessWaves.ToDictionary(w => w.Name);
        int wave = 0, missile = 0;
        for (int i = 0; i < 8 * 60 * Playfield.TickRate; i++)
        {
            g.Step();
            if (g.Endless!.Wave == wave) continue;
            wave = g.Endless.Wave;
            var patterns = g.Endless.Patterns.Select(n => byName[n]).ToList();
            int m = patterns.Count(p => p.MissileOnly(TestData.Tuning));
            Assert.True(m <= Math.Max(1, patterns.Count / 3), $"wave {wave}: {m} of {patterns.Count} only missile ships");
            missile += m;
        }
        Assert.True(missile > 0); // but they do come
        Assert.True(byName["level 1 slow pair"].MissileOnly(TestData.Tuning));
        Assert.False(byName["level 1 laser and drop"].MissileOnly(TestData.Tuning));
        Assert.False(byName["level 8 stealth pair"].MissileOnly(TestData.Tuning));
    }

    [Fact]
    public void A_wave_waits_for_the_last_to_be_cleared_and_announces_itself()
    {
        var announced = new List<double>(); // the time each wave was announced
        int waves = 0;
        var g = TestData.NewGame(wpm: 65, events: new WaveWatcher(() => waves++));
        g.NewEndless(3);
        for (int i = 0; i < 90 * Playfield.TickRate && announced.Count < 3; i++)
        {
            int before = waves;
            g.Step();
            if (waves != before)
            {
                announced.Add(g.Time);
                // Nothing of the last wave is left when the next is announced.
                Assert.DoesNotContain(g.Objects, o => o is EnemyShip || o is Missile { FromPlayer: false });
                Assert.Equal(announced.Count, g.Endless!.Wave);
                // The wave's first ship comes "announce" seconds later.
                Assert.Equal(g.Time + TestData.Tuning.EndlessAnnounce, g.LevelData.Min(e => e.Time), 6);
            }
            // Nobody types: clear the screen by hand so the next wave can come.
            if (g.LevelData.Count == 0) foreach (var o in g.Objects.Where(o => o is EnemyShip || o is Missile { FromPlayer: false }).ToList()) o.Remove();
        }
        Assert.Equal(3, announced.Count);
        Assert.Equal(TestData.Tuning.EndlessFirstWave, announced[0], 1);
    }

    sealed class WaveWatcher(Action onWave) : IGameEvents
    {
        public void WaveStarting(int wave) => onWave();
    }

    [Fact]
    public void The_waves_grow_in_budget_patterns_closeness_and_speed()
    {
        var run = new EndlessRun(TestData.NewGame(wpm: 65), 1);
        var t = TestData.Tuning;
        Assert.Equal(Math.Round(Difficulty.Hard - 1.4) + t.EndlessStartOffset, run.StartBudget, 9); // Hard: 4 - 1
        Assert.Equal(run.StartBudget + 9 * t.EndlessBudgetPerWave, run.BudgetOf(10), 9);
        Assert.Equal(new[] { 1, 1, 1, 2, 2, 2, 3 }, Enumerable.Range(1, 7).Select(w => run.PatternsIn(w).Max));
        Assert.True(run.PatternsIn(200).Min * EndlessWave.MaxLevel >= run.BudgetOf(200)); // never a pattern above 15
        Assert.Equal(1, new EndlessRun(TestData.NewGame(wpm: 25), 1).StartBudget, 9); // at least 1
        Assert.Equal(t.EndlessPatternDelay - t.EndlessPatternDelayPerWave, run.DelayIn(1, 1), 9);
        Assert.Equal(t.EndlessMinPatternDelay, run.DelayIn(1000, 1), 9);
        Assert.Equal(Difficulty.Hard, run.CpsOf(1), 9);
        Assert.Equal(Difficulty.Hard + 9 * t.EndlessCpsPerWave, run.CpsOf(10), 9);
        Assert.Equal(Difficulty.Insane, run.CpsOf(1000), 9); // capped at Insane...
        Assert.Equal(1, run.SpeedOf(1), 9);
        Assert.Equal(t.Pace(Difficulty.Insane) / t.Pace(Difficulty.Hard), run.SpeedOf(1000), 9);
        var insane = new EndlessRun(TestData.NewGame(wpm: 120), 1);
        Assert.Equal(1, insane.SpeedOf(50), 9); // ...so Insane never ramps
        Assert.Equal(Difficulty.Insane, insane.CpsOf(50), 9);
    }

    [Fact]
    public void Endless_scores_are_ship_points_times_the_difficulty_speed_over_easy()
    {
        var g = TestData.NewGame(wpm: 65);
        g.NewEndless(1);
        Assert.Equal(65 / 25.0, g.Endless!.ScoreFactor, 9);
        g.AddScore(10, default);
        Assert.Equal(26, g.Score);
        g.AddScore(1, default); // 11 points x 2.6 = 28.6
        Assert.Equal(29, g.Score);
        Assert.Equal(1, new EndlessRun(TestData.NewGame(wpm: 25), 1).ScoreFactor, 9);
        Assert.Equal(120 / 25.0, new EndlessRun(TestData.NewGame(wpm: 120), 1).ScoreFactor, 9);

        var level = TestData.NewGame(wpm: 65); // the campaign keeps its points as they are
        level.NewGame(0);
        level.AddScore(10, default);
        Assert.Equal(10, level.Score);
    }

    [Fact]
    public void Best_scores_and_times_are_kept_per_difficulty_and_survive_old_saves()
    {
        var save = new SaveData();
        Assert.True(save.RecordEndless(2, 500, 120));
        Assert.False(save.RecordEndless(2, 300, 200)); // a longer run, but not a better score
        Assert.Equal(500, save.EndlessBest[2]);
        Assert.Equal(200, save.EndlessSeconds[2]);
        Assert.Equal(0, save.EndlessBest[0]);

        // A save from before endless mode loads, with no bests.
        var old = SaveData.FromJson(new SaveData { TestWpm = 55 }.ToJson().Replace("\"EndlessBest\"", "\"Unused1\"").Replace("\"EndlessSeconds\"", "\"Unused2\""));
        Assert.Equal(55, old.TestWpm);
        Assert.Equal(new int[5], old.EndlessBest);
    }

    [Theory]
    [InlineData("[waves]\n@0:00 laser sn  6,0", "isn't a pattern")]
    [InlineData("[level 16 too high]\n@0:00 laser sn  6,0", "isn't a pattern")]
    [InlineData("@0:00 laser sn  6,0", "go in sections")]
    [InlineData("[level 1 empty]\n", "has no ships")]
    [InlineData("[level 2 tagged]\n@0:00 laser sn  6,0\nnormal+: mirror", "changes with the difficulty")]
    [InlineData("[level 2 numbers]\n@0:00 missile sn  0,3 5,4 12,8\nrepeat 0.5 1/1/2/2/3", "changes with the difficulty")]
    public void Mistakes_in_the_endless_file_say_what_is_wrong(string text, string expected)
    {
        var e = Assert.Throws<DataError>(() => TestData.With((GameData.EndlessFile, text)));
        Assert.Contains(expected, e.Message);
        Assert.Contains("endless.txt", e.Message);
    }
}
