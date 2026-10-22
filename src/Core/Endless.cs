using System.Text.RegularExpressions;
using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip.Core;

/// <summary>One of endless mode's patterns: a [level N name] section of levels/endless.txt.</summary>
public sealed record EndlessWave(string Name, int Level, LevelScript Script)
{
    public const int MaxLevel = 15;
    /// <summary>The letters a diving suicide ship counts for in the guidelines on top of its word.</summary>
    public const int DiverPremium = 3;
    /// <summary>How far (in grid squares) a diving suicide ship should move before it dives, from the side or the bottom
    /// of the grid, so the player has time to see it coming: at least half of a pattern's should.</summary>
    public const double DiverMinMove = 5;
    static readonly Regex Section = new(@"^level (\d+)(?: .+)?$", RegexOptions.Compiled);

    public static IReadOnlyList<EndlessWave> Parse(string text, string file)
    {
        var waves = new List<EndlessWave>();
        foreach (var (name, script) in LevelScript.ParseSections(text, file))
        {
            if (name == "")
            {
                if (script.Statements.Count > 0)
                    throw new DataError(file, script.Statements[0].Line,
                        "Endless patterns go in sections: put a [level N name] line (N from 1 to 15) above the first one.");
                continue;
            }
            var m = Section.Match(name);
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int level) || level is < 1 or > MaxLevel)
                throw new DataError(file, 0, $"[{name}] isn't a pattern: endless sections are [level N name], with N from 1 to {MaxLevel}, " +
                    "like [level 4 shielded drops].");
            if (script.Statements.Count == 0) throw new DataError(file, 0, $"[{name}] has no ships.");
            waves.Add(new EndlessWave(name, level, script));
        }
        if (waves.Count == 0) throw new DataError(file, 0, "Endless needs at least one pattern.");
        return waves;
    }

    /// <summary>
    /// Builds every pattern at every difficulty, so mistakes show up at load rather than mid-run, and checks each plays
    /// the same everywhere: endless picks patterns by level, so they take no difficulty tags or per-difficulty numbers.
    /// </summary>
    internal static void Check(IReadOnlyList<EndlessWave> waves, Tuning tuning)
    {
        foreach (var w in waves)
        {
            string? first = null;
            foreach (double cps in Difficulty.Cps)
            {
                var ships = LevelBuilder.Build(w.Script, new LevelContext(new Rng(1), tuning, cps)).OfType<SpawnEvent>().ToList();
                if (ships.Count == 0) throw new DataError(w.Script.File, w.Script.Statements[0].Line, $"[{w.Name}] has no ships.");
                string shape = string.Join(" ", ships.Select(s => $"{s.Type}@{s.Time:0.###}"));
                if (first == null) first = shape;
                else if (shape != first)
                    throw new DataError(w.Script.File, w.Script.Statements[0].Line, $"[{w.Name}] changes with the difficulty. " +
                        "Endless patterns play the same everywhere: take out difficulty tags (normal+:) and per-difficulty numbers (1/2/3/4/5).");
            }
        }
    }

    bool? missileOnly;

    /// <summary>Whether the pattern is only missile ships (no lasers, suicide ships, mines or stealth ships): the ships
    /// you can let go without being hit unless they fire. A wave has few of these (tuning.txt [endless]).</summary>
    public bool MissileOnly(Tuning tuning) => missileOnly ??=
        LevelBuilder.Build(Script, new LevelContext(new Rng(1), tuning, Difficulty.Hard)).OfType<SpawnEvent>()
            .All(s => s.Type[1] == 'm' && s.Type[0] != 'x');

    /// <summary>A pattern measured against the guidelines for writing them (docs/level-format.md): its letters and
    /// words, its spawn time (first ship to last), and the level those give.</summary>
    /// <summary>It also counts the diving suicide ships, and those that come in from the side or the bottom and move at
    /// least <see cref="DiverMinMove"/> squares before diving.</summary>
    public sealed record Measure(int Letters, int Words, double SpawnTime, int GuideLevel, int MinLetters, int MaxLetters,
        int Divers, int DiversWithRoom)
    {
        public bool DiversOk => DiversWithRoom * 2 >= Divers;
    }

    public Measure Guide(Tuning tuning)
    {
        var ships = LevelBuilder.Build(Script, new LevelContext(new Rng(1), tuning, Difficulty.Hard)).OfType<SpawnEvent>().ToList();
        int letters = 0, words = 0;
        foreach (var s in ships)
        {
            var (l, w) = Cost(s);
            letters += l;
            words += w;
        }
        double time = ships.Max(s => s.Time) - ships.Min(s => s.Time);
        var divers = ships.Where(s => s.Hull == "ms").ToList();
        return new Measure(letters, words, time, LevelFor(letters + words, time), (Level + 1) * 3, (Level + 1) * 5,
            divers.Count, divers.Count(HasRoom));
    }

    /// <summary>Does a diving suicide ship come in from the side or the bottom, and move far enough before it dives?</summary>
    static bool HasRoom(SpawnEvent s)
    {
        double x = s.X / Playfield.GridX, y = s.Y / Playfield.GridY;
        var to = s.Commands[0];
        double move = Math.Sqrt(Math.Pow(to.WrittenX / Playfield.GridX - x, 2) + Math.Pow(to.WrittenY / Playfield.GridY - y, 2));
        return (x <= 0 || x >= 12 || y >= 12) && move >= DiverMinMove - 1e-6;
    }

    /// <summary>The guideline level of a pattern: floor((letters + words) / (spawn time + 3) / 0.8 - 1.4).</summary>
    public static int LevelFor(int lettersAndWords, double spawnTime) => (int)Math.Floor(lettersAndWords / (spawnTime + 3) / 0.8 - 1.4);

    /// <summary>The guideline spawn time for a level: (letters + words) / (0.8 x level + 1.12) - 3.</summary>
    public static double SpawnTimeFor(int lettersAndWords, int level) => lettersAndWords / (0.8 * level + 1.12) - 3;

    /// <summary>What a ship counts for in the guidelines: its word, its shield's (the same word again), its turret's
    /// (4 letters), and for a stealth ship one of its missiles (3 letters). Missile ships' missiles don't count (the
    /// player should get them first). A diving suicide ship (medium) counts 3 letters more: it's the one
    /// ship that hits the player if it isn't typed in time, where others just leave (landmines are slow enough).</summary>
    public static (int Letters, int Words) Cost(SpawnEvent s)
    {
        int letters = s.WordSize, words = 1;
        if (s.Hull == "ms") letters += DiverPremium;
        if (s.Shielded) { letters += s.WordSize; words++; }
        if (s.HasTurret) { letters += 4; words++; }
        if (s.Fires == Weapons.AutoMissile) { letters += 3; words++; }
        return (letters, words);
    }
}

/// <summary>
/// An endless run, in numbered waves. Each wave has a budget of pattern levels: it starts a level below the
/// difficulty's own (the guideline level of its typing speed) and grows each wave. The budget is split at random
/// across a random number of patterns from levels/endless.txt (few of them only missile ships), started a set delay apart (shrinking each wave, so
/// they overlap more); the next wave waits until every enemy of the last is dead or gone, is announced ("WAVE 4"),
/// and starts as the announcement fades. The typing speed the waves are balanced for also rises, so everything
/// moves faster and enemies fire more often. All the numbers are in tuning.txt
/// [endless]. The patterns come from the run's own random numbers, so the same seed gives the same waves whatever
/// the player types (the daily challenge needs that).
/// </summary>
public sealed class EndlessRun
{
    readonly Game game;
    readonly Rng rng;
    double nextWaveAt;

    public readonly ulong Seed;
    /// <summary>The wave being played (0 before the first).</summary>
    public int Wave { get; private set; }
    /// <summary>The patterns of the current wave, by name (for tests and the simulator).</summary>
    public IReadOnlyList<string> Patterns { get; private set; } = Array.Empty<string>();

    public EndlessRun(Game game, ulong seed)
    {
        this.game = game;
        Seed = seed;
        rng = new Rng(seed);
        nextWaveAt = T.EndlessFirstWave;
    }

    Tuning T => game.Tuning;

    /// <summary>The guideline level of a typing speed: the patterns a typist at 80% of it keeps up with.</summary>
    public static double LevelOf(double cps) => cps - 1.4;

    /// <summary>Wave 1's budget: a level below the difficulty's own ("start offset"), at least 1.</summary>
    public double StartBudget => Math.Max(1, Math.Round(LevelOf(game.Cps)) + T.EndlessStartOffset);

    /// <summary>Wave n's budget of pattern levels.</summary>
    public double BudgetOf(int wave) => StartBudget + T.EndlessBudgetPerWave * (wave - 1);

    /// <summary>The fewest and most patterns wave n can have: enough that none needs to be above level 15, and one
    /// more every few waves.</summary>
    public (int Min, int Max) PatternsIn(int wave)
    {
        int min = Math.Max(1, (int)Math.Ceiling(BudgetOf(wave) / EndlessWave.MaxLevel));
        return (min, Math.Max(min, (int)Math.Ceiling(wave / T.EndlessWavesPerPattern)));
    }

    /// <summary>Seconds between the starts of wave n's patterns, when it has <paramref name="patterns"/> of them:
    /// shrinking each wave down to a floor, plus a little more for each pattern past the first.</summary>
    public double DelayIn(int wave, int patterns) =>
        Math.Max(T.EndlessMinPatternDelay, T.EndlessPatternDelay - T.EndlessPatternDelayPerWave * wave) +
        T.EndlessPatternDelayPerPattern * (patterns - 1);

    /// <summary>The typing speed wave n is balanced for: the difficulty's, plus "cps per wave" each wave, up to
    /// Insane's (so Insane never ramps). It sets how fast everything moves and how often enemies fire; the score
    /// still uses the difficulty's own speed.</summary>
    public double CpsOf(int wave) =>
        Math.Max(game.Cps, Math.Min(Difficulty.Insane, game.Cps + T.EndlessCpsPerWave * (Math.Max(wave, 1) - 1)));

    /// <summary>How much faster than the difficulty everything moves in wave n: the pace of its typing speed.</summary>
    public double SpeedOf(int wave) => T.Pace(CpsOf(wave)) / T.Pace(game.Cps);

    /// <summary>Endless scores are the ships' points times the difficulty's typing speed over Easy's (its WPM / 25:
    /// Easy x1 to Insane x4.8), so a run counts for more on a harder difficulty and playing below your speed doesn't
    /// pay. Best scores are still kept per difficulty.</summary>
    public double ScoreFactor => game.Cps / Difficulty.Easy;

    double points;

    /// <summary>The score once these points are added: all the ships' points so far, times the factor.</summary>
    public int ScoreFor(int amount)
    {
        points += amount;
        return (int)Math.Round(points * ScoreFactor);
    }

    /// <summary>The current speed-up.</summary>
    public double Speed => SpeedOf(Wave);

    /// <summary>Called each tick: once the last wave is over (all its ships spawned, and every enemy dead or gone),
    /// announces the next and lines up its patterns.</summary>
    public void Feed()
    {
        if (game.LevelData.Count > 0 || game.Time < nextWaveAt) return;
        if (game.Objects.Any(o => o is EnemyShip || o is Missile { FromPlayer: false })) return;
        Wave++;
        game.MRate = game.FireRate(CpsOf(Wave));
        game.Events.WaveStarting(Wave);

        // The budget, split at random (unevenly, on purpose) across a random number of patterns.
        var (min, max) = PatternsIn(Wave);
        int count = min + (int)(rng.Rand() * (max - min + 1));
        var weights = Enumerable.Range(0, count).Select(_ => rng.Rand() + 0.05).ToList();
        double budget = BudgetOf(Wave), total = weights.Sum();
        double start = game.Time + T.EndlessAnnounce;
        var names = new List<string>();
        int missile = Math.Max(1, count / (int)T.EndlessPatternsPerMissilePattern); // missile-only patterns it can still have
        foreach (double w in weights)
        {
            var pattern = AddPattern((int)Math.Round(budget * w / total), start, missile > 0);
            if (pattern.MissileOnly(T)) missile--;
            names.Add(pattern.Name);
            start += DelayIn(Wave, count);
        }
        Patterns = names;
        // Patterns overlap, and the game runs events in list order. The sort is stable.
        game.LevelData = game.LevelData.OrderBy(e => e.Time).ToList();
        nextWaveAt = game.Time;
    }

    /// <summary>A pattern of the given level (or the nearest level there is), mirrored half the time; only missile
    /// ships if <paramref name="missile"/> allows.</summary>
    EndlessWave AddPattern(int level, double start, bool missile)
    {
        var all = game.Data.EndlessWaves.Where(w => missile || !w.MissileOnly(T)).ToList();
        int nearest = all.Min(w => Math.Abs(w.Level - level));
        var pool = all.Where(w => Math.Abs(w.Level - level) == nearest).ToList();
        var pattern = pool[Math.Min(pool.Count - 1, (int)(rng.Rand() * pool.Count))];

        var events = LevelBuilder.Build(pattern.Script, new LevelContext(rng, T, game.Cps, SpeedOf(Wave)), start);
        if (rng.Rand() < 0.5)
            foreach (var e in events.OfType<SpawnEvent>())
            {
                e.X = Playfield.ViewWidth - e.X;
                e.Commands = e.Commands.Select(cmd => cmd.Mirror()).ToArray();
            }
        game.LevelData.AddRange(events);
        return pattern;
    }
}
