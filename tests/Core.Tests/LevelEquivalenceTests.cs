using System.Globalization;
using System.Text;
using TypingShip.Core.Levels;

namespace TypingShip.Core.Tests;

// TEMPORARY proof that the level files in data/levels build exactly what the old C# scripts (LegacyLevelData) did:
// every event, field and float, and the same random numbers drawn, for every level, every WPM from 1 to 150 and
// several seeds. Deleted with LegacyLevelData once it has passed.
public class LevelEquivalenceTests
{
    static string Dump(IEnumerable<LevelEvent> events, Rng rng)
    {
        static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);
        static string D(double d) => d.ToString("R", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        foreach (var e in events)
        {
            sb.Append(D(e.Distance)).Append(' ');
            switch (e)
            {
                case SpawnEvent s:
                    sb.Append($"spawn {s.Type} {F(s.X)},{F(s.Y)} A{F(s.A)} f{s.Fires} k{s.KillBy} v{s.Value} w{s.WordSize} h{s.Health} {s.Shielded} {s.HasTurret}");
                    foreach (var c in s.Commands) sb.Append($" [{F(c.X)},{F(c.Y)} t{c.Timer} {c.Move} {c.Fire}]");
                    break;
                case BossEvent b: sb.Append($"boss {b.Type}"); break;
                case MarkerEvent m: sb.Append($"marker {m.Type} {D(m.Index)}"); break;
            }
            sb.Append('\n');
        }
        sb.Append("next random: ").Append(F(rng.Rand())); // the same number of random draws
        return sb.ToString();
    }

    public static IEnumerable<TheoryDataRow<int>> Levels() => Enumerable.Range(0, 12).Select(i => new TheoryDataRow<int>(i));

    [Theory]
    [MemberData(nameof(Levels))]
    public void Level_files_build_the_same_events_as_the_old_scripts(int level)
    {
        var data = TestData.Data;
        for (int wpm = 1; wpm <= 150; wpm++)
        for (ulong seed = 1; seed <= 4; seed++)
        {
            var oldRng = new Rng(seed * 7919 + (ulong)wpm);
            var expected = Dump(LegacyLevelData.LoadData(level, new LevelContext(oldRng, data.Tuning, wpm)), oldRng);
            var newRng = new Rng(seed * 7919 + (ulong)wpm);
            var actual = Dump(data.BuildLevel(level, new LevelContext(newRng, data.Tuning, wpm)), newRng);
            if (expected != actual) Assert.Equal(expected, actual); // only build the big message on failure
        }
    }

    [Fact]
    public void Boss_files_build_the_same_events_as_the_old_scripts()
    {
        var data = TestData.Data;
        for (int wpm = 1; wpm <= 150; wpm++)
        for (ulong seed = 1; seed <= 4; seed++)
        {
            for (int boss = 0; boss < 3; boss++)
            {
                double distance = 1.234 + seed;
                var oldRng = new Rng(seed + 100 * (ulong)wpm);
                LegacyLevelData.LoadData(0, new LevelContext(new Rng(1), data.Tuning, wpm)); // sets its step multiplier
                var expected = Dump(LegacyLevelData.bossLoop(new LevelContext(oldRng, data.Tuning, wpm), boss, distance), oldRng);
                var newRng = new Rng(seed + 100 * (ulong)wpm);
                var actual = Dump(data.BuildBossLoop(boss, new LevelContext(newRng, data.Tuning, wpm), distance), newRng);
                if (expected != actual) Assert.Equal(expected, actual);
            }
            {
                var oldRng = new Rng(seed + 7 * (ulong)wpm);
                var expected = Dump(LegacyLevelData.boss0Suicides(new LevelContext(oldRng, data.Tuning, wpm)), oldRng);
                var newRng = new Rng(seed + 7 * (ulong)wpm);
                var actual = Dump(data.BuildHangar(new LevelContext(newRng, data.Tuning, wpm)), newRng);
                if (expected != actual) Assert.Equal(expected, actual);
            }
        }
    }
}

// TEMPORARY, with the above: the old SpawnEvent's hard-coded ship stats, against the same stats from tuning.txt.
public class ShipStatEquivalenceTests
{
    static (float A, int Value, int WordSize, int Fires, int KillBy, bool Shielded, bool HasTurret, int Health) Old(string t, int wpm)
    {
        float A = 0.5f;
        int Fires = 0, KillBy = Weapons.Missile, Value = 5, WordSize, Index, Health = 1;
        bool Shielded = false, HasTurret = false;
        switch (t[..2])
        {
            case "sm": Index = 0; break;
            case "sl": Index = 1; break;
            case "ss": Index = 2; break;
            case "mm": Index = 3; break;
            case "ml": Index = 4; break;
            case "ms": Index = 5; break;
            case "lm": Index = 6; break;
            case "ll": Index = 7; break;
            case "xm": Index = 8; break;
            case "nl": Index = 9; KillBy = Weapons.Laser; break;
            default: throw new ArgumentException();
        }
        switch (t[0])
        {
            case 's': WordSize = 4; break;
            case 'm': WordSize = 6; Value += 1; A -= 0.2f; break;
            case 'l': WordSize = 8; Value += 3; A -= 0.25f; break;
            case 'x': WordSize = 7; Value += 3; A -= 0.1f; break;
            case 'n': WordSize = 7; break;
            default: WordSize = -1; break;
        }
        switch (t[1])
        {
            case 'm': Fires = Weapons.Missile; break;
            case 'l': Fires = Weapons.Laser; WordSize += 1; Value += 1; A -= 0.1f; break;
            case 's':
                Fires = Weapons.Suicide;
                WordSize = 3;
                KillBy = Weapons.Laser;
                Value -= 3;
                A += t[0] == 's' ? -0.2f : 0.2f;
                break;
        }
        if (t[0] == 'x') Fires = Weapons.AutoMissile;
        switch (t[2])
        {
            case 's': Shielded = true; Value += 2; break;
            case 't': HasTurret = true; Value += 2; break;
            case 'b': Shielded = true; HasTurret = true; Value += 5; break;
        }
        A *= wpm / 100f;
        if (Index is 3 or 4) Health = 2;
        if (Index is 6 or 7) Health = 4;
        return (A, Value, WordSize, Fires, KillBy, Shielded, HasTurret, Health);
    }

    [Fact]
    public void Ship_codes_give_the_old_stats()
    {
        var tuning = TestData.Tuning;
        foreach (var hull in new[] { "sm", "sl", "ss", "mm", "ml", "ms", "lm", "ll", "xm", "nl" })
        foreach (var defence in "nstb")
        for (int wpm = 1; wpm <= 150; wpm++)
        {
            var code = hull + defence;
            var e = new SpawnEvent(new LevelContext(new Rng(1), tuning, wpm), 0, code, 0, 0);
            var expected = Old(code, wpm);
            Assert.Equal(expected, (e.A, e.Value, e.WordSize, e.Fires, e.KillBy, e.Shielded, e.HasTurret, e.Health));
            Assert.Equal(BitConverter.SingleToInt32Bits(expected.A), BitConverter.SingleToInt32Bits(e.A));
        }
    }
}
