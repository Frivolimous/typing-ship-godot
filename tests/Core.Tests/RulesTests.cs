using System.Text;
using TypingShip.Core.Levels;

namespace TypingShip.Core.Tests;

/// <summary>Records what the rules report.</summary>
sealed class Recorder : IGameEvents
{
    public readonly List<string> Log = new();
    public bool? Ended;
    public readonly List<(int Achievement, int Tier)> Awards = new();
    public int? Tutorial;

    public void LevelOver(bool cleared) => Ended = cleared;
    public void AchievementAwarded(int achievement, int tier) => Awards.Add((achievement, tier));
    public void TutorialShown(int index, Vec2? pointer, PointerDir dir) => Tutorial = index;
    public void Explosion(Vec2 at, int size) => Log.Add($"explosion {size}");
    public void Sound(GameSound sound) => Log.Add($"sound {sound}");
}

static class Play
{
    public static void Ticks(this Game g, int n)
    {
        for (int i = 0; i < n; i++) g.Step();
    }

    /// <summary>Ticks until the condition holds (or fails the test after a limit).</summary>
    public static void Until(this Game g, Func<bool> condition, int limit = 5000)
    {
        for (int i = 0; i < limit && !condition(); i++) g.Step();
        Assert.True(condition(), "condition never came true");
    }

    public static void Type(this Game g, string text)
    {
        foreach (var c in text) g.TypeLetter(c);
    }

    public static EnemyShip OnlyShip(this Game g) => Assert.Single(g.Objects.OfType<EnemyShip>());
}

public class WeaponTests
{
    // A small missile ship flying in from the top: 5 points, a 4-letter word.
    const string OneShip = "@0:00.55 missile sn  6,0 6,4 6,12";

    [Fact]
    public void Typing_a_ships_word_fires_a_missile_and_the_hit_scores()
    {
        var g = TestData.WithLevel0(OneShip);
        g.NewGame(0);
        g.Until(() => g.Objects.Count == 1);
        var ship = g.OnlyShip();
        Assert.Equal(4, ship.Word!.Length);
        g.Type(ship.Word);
        Assert.Null(ship.Word);
        Assert.Contains(g.Objects, o => o is Missile { FromPlayer: true });
        g.Until(() => ship.Removed);
        Assert.Equal(5, g.Score);
        Assert.Equal(0, g.MissedShips);
    }

    [Fact]
    public void A_shield_needs_an_emp_first()
    {
        var g = TestData.WithLevel0("@0:00.55 missile ss  6,0 6,4 6,12");
        g.NewGame(0);
        g.Until(() => g.Objects.Count == 1);
        var ship = g.OnlyShip();
        Assert.True(ship.ShieldOn);
        Assert.Equal(3, ship.Word!.Length); // shielded ships carry a 3-letter EMP word
        g.Type(ship.Word);
        Assert.True(g.Player.Charge.Active);
        g.Ticks(g.Tuning.EmpCharge);
        Assert.False(ship.ShieldOn);
        Assert.Equal(4, ship.Word!.Length); // the ship's own word is back
    }

    [Fact]
    public void Big_ships_take_one_missile_per_health()
    {
        var g = TestData.WithLevel0("@0:00.55 missile ln  6,0 6,4 6,12");
        g.NewGame(0);
        g.Until(() => g.Objects.Count == 1);
        var ship = g.OnlyShip();
        Assert.Equal(4, ship.Health);
        g.Type(ship.Word!);
        Assert.Equal(4, g.Objects.Count(o => o is Missile { FromPlayer: true }));
        g.Until(() => ship.Removed);
        Assert.Equal(1, ship.Health);
    }

    [Fact]
    public void Enemy_fire_builds_up_slowly_at_low_speed()
    {
        var g = TestData.WithLevel0(OneShip, wpm: 25);
        g.NewGame(0);
        g.Until(() => g.Objects.Count == 1);
        var ship = g.OnlyShip();
        Assert.Equal(0.3f, g.MRate); // 25 / 150, raised to the minimum
        for (int i = 0; i < 3; i++) g.ShootMissile(ship, g.Player);
        Assert.DoesNotContain(g.Objects, o => o is Missile);
        g.ShootMissile(ship, g.Player); // 4 x 0.3 > 1
        var missile = Assert.Single(g.Objects.OfType<Missile>());
        Assert.False(missile.FromPlayer);
        Assert.Equal(3, missile.Word!.Length);
        Assert.Equal(4, ship.Value); // a ship that fired is worth a point less
    }

    [Fact]
    public void A_laser_charges_for_500_over_cps_ticks_then_hits()
    {
        var g = TestData.WithLevel0("@0:00.55 laser sn  6,0");
        g.NewGame(0);
        g.Until(() => g.Objects.Count == 1);
        var ship = g.OnlyShip();
        g.Until(() => ship.Charge!.Active);
        Assert.Equal(500 * 12 / 40 - 1, ship.Charge!.Remaining); // it starts charging the tick it fires
        g.Until(() => !ship.Charge.Active);
        Assert.Equal(g.Player.MaxHealth - 1, g.Player.Health);
        Assert.True(g.Player.Injured);
    }

    [Fact]
    public void A_turret_shoots_down_missiles_near_its_ship()
    {
        var g = TestData.WithLevel0("@0:00.55 missile st  6,0 6,3 6,12");
        g.NewGame(0);
        g.Until(() => g.Objects.Count == 1 && g.OnlyShip().Position.Y > 100);
        var ship = g.OnlyShip();
        Assert.NotNull(ship.Turret);
        g.Type(ship.Word!);
        var missile = Assert.Single(g.Objects.OfType<Missile>());
        g.Until(() => missile.Removed);
        Assert.False(ship.Removed); // shot down before it got there
        Assert.NotNull(ship.Word); // and the ship has a new word
    }
}

public class TypingTests
{
    static Game Started(string level = "@5:00 missile sn  6,0 6,4 6,12")
    {
        var g = TestData.WithLevel0(level);
        g.NewGame(0);
        return g;
    }

    [Fact]
    public void The_typing_bar_holds_twenty_letters_and_each_one_more_costs_a_point()
    {
        var g = Started();
        g.Type(new string('q', 20));
        Assert.Equal(0, g.Score);
        g.TypeLetter('q');
        Assert.Equal(-1, g.Score);
        Assert.Equal(20, g.Typed.Length);
    }

    [Fact]
    public void Backspace_costs_a_point_and_spoils_a_perfect_level()
    {
        var g = Started();
        g.Type("qq");
        g.Backspace();
        Assert.Equal("q", g.Typed);
        Assert.Equal(-1, g.Score);
        Assert.False(g.Perfect);
        g.Backspace();
        g.Backspace(); // nothing left: free
        Assert.Equal(-2, g.Score);
    }

    [Fact]
    public void Damage_puts_a_repair_word_on_the_hull_and_typing_it_repairs()
    {
        var g = Started();
        Assert.Null(g.Hull.Word);
        g.DamagePlayer();
        Assert.Equal(4, g.Player.Health);
        var word = g.Hull.Word!;
        Assert.InRange(word.Length, g.Tuning.HealWordShortest, g.Tuning.HealWordLongest);
        g.Type(word);
        Assert.Equal(5, g.Player.Health);
        Assert.Null(g.Hull.Word);
    }

    [Fact]
    public void A_word_whose_ship_left_stays_typeable_for_a_while()
    {
        var g = Started("@0:00.55 missile sn  6,0 6,1 6,-2");
        g.Until(() => g.Objects.Count == 1);
        var ship = g.OnlyShip();
        g.Until(() => ship.Removed);
        Assert.Equal(1, g.MissedShips);
        var overflow = Assert.Single(g.Overflows);
        g.Type(overflow.Word);
        Assert.Empty(g.Overflows);

        g = Started("@0:00.55 missile sn  6,0 6,1 6,-2");
        g.Until(() => g.Overflows.Count == 1);
        g.Ticks(g.Tuning.OverflowTicks + 1);
        Assert.Empty(g.Overflows);
    }

    [Fact]
    public void Losing_all_health_loses_the_level_after_the_explosion()
    {
        var rec = new Recorder();
        var g = TestData.WithLevel0("@5:00 missile sn  6,0 6,4 6,12", events: rec);
        g.NewGame(0);
        for (int i = 0; i < 5; i++) g.DamagePlayer();
        g.Step();
        Assert.False(g.CanType);
        Assert.True(g.Player.Destroyed);
        g.Ticks(98);
        Assert.Null(rec.Ended);
        g.Step();
        Assert.False(rec.Ended);
    }

    [Fact]
    public void Cheat_words_and_the_riddler_medal_in_order()
    {
        var rec = new Recorder();
        var g = TestData.WithLevel0("@5:00 missile sn  6,0 6,4 6,12", events: rec);
        g.NewGame(0);
        g.Type("kamikaze"); // out of order: no medal, but it still blows up the ship
        Assert.Empty(rec.Awards);
        Assert.True(g.Player.Destroyed);

        g.NewGame(0);
        g.Type("superman");
        Assert.Contains(g.Objects, o => o is Superman);
        g.Type("kamikaze");
        Assert.Equal(new[] { (Achievement.Riddler, 1), (Achievement.Riddler, 2) }, rec.Awards);
        Assert.Equal(99, g.Score); // superman's last flight
    }

    [Fact]
    public void Typing_pause_pauses()
    {
        var g = Started();
        g.Type("pause");
        Assert.False(g.Running);
        g.Resume();
        Assert.True(g.Running);
    }

    [Fact]
    public void A_tutorial_pauses_until_closed_and_shows_once()
    {
        var rec = new Recorder();
        var g = TestData.WithLevel0("@5:00 missile sn  6,0 6,4 6,12", wpm: 40, events: rec);
        g.NewGame(0);
        Array.Fill(g.TutFlags, false); // a fresh game below 50 wpm
        g.HitTarget(g.Player, Weapons.Suicide);
        Assert.Equal(8, rec.Tutorial);
        Assert.Equal(8, g.Tutorial);
        Assert.False(g.Running);
        g.CloseTutorial();
        Assert.True(g.Running);
        rec.Tutorial = null;
        g.HitTarget(g.Player, Weapons.Suicide);
        Assert.Null(rec.Tutorial);
    }
}

public class BossTests
{
    [Fact]
    public void The_scanner_takes_short_words_then_a_long_one_to_hit_the_boss()
    {
        var g = TestData.WithLevel0("@0:00.55 boss 2");
        g.NewGame(0);
        g.Until(() => g.Boss?.Scanner != null);
        var boss = g.Boss!;
        Assert.True(g.BossMode);
        int shortWords = 0;
        while (boss.Health == 3)
        {
            var word = boss.Scanner!.Word!;
            if (word.Length == 3) shortWords++;
            else Assert.Equal(10, word.Length);
            g.Type(word);
        }
        Assert.Equal(10, shortWords); // typed with no time between them
        Assert.Equal(2, boss.Health);
        Assert.NotNull(boss.Scanner); // a fresh one for the next hit
    }

    [Fact]
    public void Boss_3s_counter_words_take_the_same_share_of_the_typing_speed_on_every_difficulty()
    {
        int Window(double cps)
        {
            var g = TestData.WithLevel0("@0:00.55 boss 3");
            g.Cps = cps;
            g.NewGame(0);
            g.Until(() => g.Boss != null);
            return g.Boss!.CounterWindow;
        }
        Assert.Equal(31 * 4, Window(Difficulty.Hard)); // as the original had it on Hard
        double hardLetters = Window(Difficulty.Hard) * Difficulty.Hard;
        foreach (double cps in Difficulty.Cps)
            Assert.InRange(Window(cps) * cps, hardLetters - cps, hardLetters + cps); // within a tick's typing
    }

    [Fact]
    public void Boss_escorts_keep_coming_during_the_fight()
    {
        var g = TestData.WithLevel0("@0:00.55 boss 1");
        g.NewGame(0);
        g.Until(() => g.BossMode);
        g.Until(() => g.Objects.OfType<EnemyShip>().Any(), limit: 20000);
    }
}

public class ScoringTests
{
    [Theory]
    [InlineData(false, 5, 2f, 3)]
    [InlineData(true, 5, 1.2f, 2)]
    [InlineData(true, 3, 1.12f, 1)]
    [InlineData(true, 2, 1.08f, 0)]
    public void Health_bonus_and_medal(bool injured, int health, float bonus, int medal)
    {
        Assert.Equal(bonus, Scoring.HealthBonus(injured, health, 5), 5);
        Assert.Equal(medal, Scoring.HealthMedal(injured, health, 5));
    }

    [Theory]
    [InlineData(1f, 2f, 3)]
    [InlineData(0.8f, 1.4f, 2)]
    [InlineData(0.6f, 1.3f, 1)]
    [InlineData(0.5f, 1.25f, 0)]
    public void Accuracy_bonus_and_medal(float ratio, float bonus, int medal)
    {
        Assert.Equal(bonus, Scoring.AccuracyBonus(ratio), 5);
        Assert.Equal(medal, Scoring.AccuracyMedal(ratio));
    }

    [Fact]
    public void Bonuses_multiply_and_count_up_in_about_72_steps()
    {
        Assert.Equal(1.4f, Scoring.DifficultyBonus(Difficulty.Normal)); // 1 + 40 wpm / 100
        Assert.Equal((408, 1), Scoring.ApplyBonus(292, 1.4f));
        Assert.Equal((2000, 13), Scoring.ApplyBonus(1000, 2));
    }

    [Fact]
    public void A_lost_level_keeps_its_best_score()
    {
        var g = TestData.NewGame();
        g.NewGame(2);
        g.Score = 50;
        g.RecordLoss();
        Assert.Equal(50, g.Save.LevelScore[2][0]);
        g.Score = 20;
        g.RecordLoss();
        Assert.Equal(50, g.Save.LevelScore[2][0]);
        Assert.Equal(0, g.Save.LevelScore[2][1]); // not cleared
    }

    [Fact]
    public void Save_data_round_trips_and_wipes_other_versions()
    {
        var s = new SaveData();
        s.LevelScore[3] = new[] { 120, 2, 3, 1 };
        s.Achieve[Achievement.Soldier] = 2;
        var back = SaveData.FromJson(s.ToJson());
        Assert.Equal(s.LevelScore[3], back.LevelScore[3]);
        Assert.Equal(2, back.Achieve[0]);
        Assert.Equal(0, SaveData.FromJson(s.ToJson().Replace("\"Version\":10", "\"Version\":9")).Achieve[0]);
        Assert.Equal(0, SaveData.FromJson("not json").Achieve[0]);
    }
}

public class DeterminismTests
{
    static string TraceOf(int level, int wpm, ulong seed, int botWpm = 0)
    {
        var sb = new StringWriter();
        Simulator.Run(TestData.Data, TestData.Art, level, Speed.FromWpm(wpm), seed, Speed.FromWpm(botWpm), sb);
        return sb.ToString();
    }

    [Theory]
    [InlineData(3, 40)] // a boss
    [InlineData(8, 75)]
    public void Same_seed_and_inputs_give_the_same_game(int level, int wpm)
    {
        var a = TraceOf(level, wpm, seed: 9);
        var b = TraceOf(level, wpm, seed: 9);
        Assert.True(a.Length > 10000);
        Assert.Equal(a, b);
        Assert.NotEqual(a, TraceOf(level, wpm, seed: 10));
    }

    [Fact]
    public void Same_seed_and_typing_give_the_same_game_with_a_slow_typist()
    {
        Assert.Equal(TraceOf(5, 65, 4, botWpm: 30), TraceOf(5, 65, 4, botWpm: 30));
    }

    [Fact]
    public void Scripted_keys_replay_exactly()
    {
        string Run()
        {
            var g = TestData.NewGame(seed: 21);
            var trace = new StringWriter();
            g.Trace = trace;
            g.NewGame(1);
            var keys = new StringBuilder();
            for (int t = 0; t < 2000; t++)
            {
                g.Step();
                if (t % 7 == 0 && g.Typing)
                {
                    // Type the first letter still needed by the first ship's word, as a person reading the screen might.
                    var word = g.Objects.OfType<EnemyShip>().Select(o => o.Word).FirstOrDefault(w => w != null);
                    if (word != null) g.TypeLetter(word[Math.Min(g.Typed.Length, word.Length - 1)]);
                }
            }
            return trace.ToString();
        }
        Assert.Equal(Run(), Run());
    }
}
