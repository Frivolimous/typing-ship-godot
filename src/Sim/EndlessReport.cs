using TypingShip.Core;
using TypingShip.Core.Levels;
using TypingShip.Core.Data;

// dotnet run --project src/Sim -- endless [--runs=10] [--minutes=30] [--difficulty=Hard]
//   Plays endless runs at every difficulty with three bots (perfect, and the realistic one at 90% and 80% of the
//   speed with typing mistakes), plus a world-class typist (140 WPM on Insane, 2% mistakes), one run per seed, and
//   prints the wave each reached and how long it lasted. Then the waves themselves (tuning.txt [endless]).
//   --cross instead plays each difficulty's 80% typist one difficulty down and up, and Insane typists on Easy.
// dotnet run --project src/Sim -- endless --patterns
//   Lists every pattern in data/levels/endless.txt against the guidelines for writing them (docs/level-format.md):
//   letters, words, spawn time and the level those give, and flags the ones that miss.
static class EndlessReport
{
    public static int Run(GameData data, ArtLibrary art, Dictionary<string, string> opts)
    {
        if (opts.ContainsKey("patterns")) return Patterns(data);
        if (opts.ContainsKey("solo")) return Solo(data, art, opts);
        if (opts.ContainsKey("detail")) return Detail(data, art, opts);
        int runs = int.Parse(opts.GetValueOrDefault("runs", "10"));
        int maxTicks = int.Parse(opts.GetValueOrDefault("minutes", "30")) * 60 * Playfield.TickRate;
        static string Clock(double s) => $"{(int)s / 60}:{(int)s % 60:00}";

        var rows = Difficulty.Cps.SelectMany(cps => new[]
        {
            (Difficulty.Name(cps), "perfect", cps, Bot: cps, Errors: 0.0),
            (Difficulty.Name(cps), "90%", cps, Bot: cps * 0.9, Errors: Pacing.RealisticErrors),
            (Difficulty.Name(cps), "80%", cps, Bot: cps * 0.8, Errors: Pacing.RealisticErrors),
        }).Append(("Insane", "140 WPM", Difficulty.Insane, Bot: Speed.FromWpm(140), Errors: 0.02));
        if (opts.ContainsKey("cross"))
        {
            // Players on the wrong difficulty: each difficulty's 80% typist one difficulty down, on its own and one up,
            // and Insane typists on Easy.
            var cps = Difficulty.Cps;
            rows = Enumerable.Range(0, cps.Length).SelectMany(p => Enumerable.Range(p - 1, 3).Where(d => d >= 0 && d < cps.Length)
                    .Select(d => (Difficulty.Name(cps[d]), $"{Difficulty.Name(cps[p])} 80%", cps[d], Bot: cps[p] * 0.8, Errors: Pacing.RealisticErrors)))
                .Append(("Easy", "Insane 80%", Difficulty.Easy, Bot: Difficulty.Insane * 0.8, Errors: Pacing.RealisticErrors))
                .Append(("Easy", "Insane", Difficulty.Easy, Bot: Difficulty.Insane, Errors: 0.0));
        }

        Console.WriteLine($"Endless runs, {runs} seeds each, medians (lowest-highest) of: the clears (every ship of a wave shot down),");
        Console.WriteLine("the last wave of an unbroken run of them from wave 1 and the last wave cleared at all, each with when it ended;");
        Console.WriteLine("the wave reached and the time survived (and the middle 80% of times); and the score.");
        Console.WriteLine($"{"Difficulty",-10} {"Bot",-11} {"Clears to",-16} {"Last clear",-20} {"Wave reached",-14} {"Time survived",-20} {"Middle 80%",-11} {"Score",6}");
        string? only = opts.GetValueOrDefault("difficulty");
        foreach (var (name, botName, cps, bot, errors) in rows)
        {
            if (only != null && !name.Equals(only, StringComparison.OrdinalIgnoreCase)) continue;
            var results = Enumerable.Range(1, runs).AsParallel().AsOrdered().Select(seed =>
            {
                var clears = new Clears();
                var r = Simulator.Run(data, art, 0, cps, (ulong)seed, botCps: bot, maxTicks: maxTicks, errorRate: errors, endless: true,
                    onTick: clears.Tick);
                return (r.Game.Endless!.Wave, Seconds: r.Game.Time, r.Game.Score, Over: r.Game.Player.Destroyed,
                    clears.Streak, clears.StreakTime, clears.Last, clears.LastTime);
            }).ToList();
            T Median<T>(Func<(int Wave, double Seconds, int Score, bool Over, int Streak, double StreakTime, int Last, double LastTime), T> f) =>
                results.Select(f).Order().ElementAt(runs / 2);
            string Waves(Func<(int Wave, double Seconds, int Score, bool Over, int Streak, double StreakTime, int Last, double LastTime), int> f) =>
                $"{Median(f)} ({results.Min(f)}-{results.Max(f)})";
            var times = results.Select(r => r.Seconds).Order().ToList();
            string wave = Waves(r => r.Wave);
            if (results.Any(r => !r.Over)) wave += $" {results.Count(r => !r.Over)} alive";
            Console.WriteLine($"{name,-10} {botName,-11} {$"{Median(r => r.Streak)} @{Clock(Median(r => r.StreakTime))}",-16} " +
                $"{$"{Waves(r => r.Last)} @{Clock(Median(r => r.LastTime))}",-20} {wave,-14} " +
                $"{$"{Clock(times[runs / 2])} ({Clock(times[0])}-{Clock(times[^1])})",-20} " +
                $"{$"{Clock(times[runs / 10])}-{Clock(times[runs - 1 - runs / 10])}",-11} {Median(r => r.Score),6}");
        }

        Console.WriteLine();
        Console.WriteLine("The waves on Hard (tuning.txt [endless]): budget of pattern levels, patterns, seconds between them, the typing speed they're balanced for, and the speed-up and fire rate that gives.");
        var g = new Game(data, new Rng(1), art) { Cps = Difficulty.Hard };
        var run = new EndlessRun(g, 1);
        foreach (int w in new[] { 1, 2, 3, 4, 5, 6, 8, 10, 12, 15, 20, 25, 30 })
        {
            var (min, max) = run.PatternsIn(w);
            Console.WriteLine($"  wave {w,2}  budget {run.BudgetOf(w),4:0.#}  patterns {min}-{max}  delay {run.DelayIn(w, min):0.0}-{run.DelayIn(w, max):0.0} s  cps {run.CpsOf(w):0.00}  speed {run.SpeedOf(w):0.00}x  fire {g.FireRate(run.CpsOf(w)):0.00}");
        }
        Console.WriteLine("  (wave 1's budget on each difficulty: " +
            string.Join(", ", Difficulty.Cps.Select(c => $"{Difficulty.Name(c)} {new EndlessRun(new Game(data, new Rng(1), art) { Cps = c }, 1).StartBudget:0}")) + ")");
        return 0;
    }


    // --detail: what each run's waves held and the damage each did, then where the damage comes from: short runs
    // against long ones, damage by the number of patterns and by the letters a wave holds, and which patterns are
    // in the waves that hurt more than others with as many patterns.
    static int Detail(GameData data, ArtLibrary art, Dictionary<string, string> opts)
    {
        int runs = int.Parse(opts.GetValueOrDefault("runs", "12"));
        double cps = Difficulty.Cps.First(c => Difficulty.Name(c).Equals(opts.GetValueOrDefault("difficulty", "Hard"), StringComparison.OrdinalIgnoreCase));
        double share = double.Parse(opts.GetValueOrDefault("bot", "0.9"), System.Globalization.CultureInfo.InvariantCulture);
        var load = data.EndlessWaves.ToDictionary(w => w.Name, w => { var m = w.Guide(data.Tuning); return m.Letters + m.Words; });
        var all = new List<(int Seed, int Reached, List<WaveLog> Waves)>();
        for (int seed = 1; seed <= runs; seed++)
        {
            var waves = new List<WaveLog>();
            WaveLog? cur = null;
            int lastHealth = 0;
            var r = Simulator.Run(data, art, 0, cps, (ulong)seed, botCps: cps * share, maxTicks: 30 * 60 * Playfield.TickRate,
                errorRate: share < 1 ? Pacing.RealisticErrors : 0, endless: true, onTick: g =>
                {
                    var e = g.Endless!;
                    if (e.Wave > 0 && (cur == null || e.Wave != cur.Wave))
                    {
                        cur = new WaveLog(e.Wave, e.Patterns.ToList(), e.Patterns.Sum(p => load[p]), g.Player.Health);
                        waves.Add(cur);
                    }
                    if (g.Player.Health < lastHealth && cur != null) cur.Hits += lastHealth - g.Player.Health;
                    lastHealth = g.Player.Health;
                });
            all.Add((seed, r.Game.Endless!.Wave, waves));
            Console.WriteLine($"seed {seed,2}: wave {r.Game.Endless.Wave,2} at {(int)r.Game.Time / 60}:{(int)r.Game.Time % 60:00}  " +
                string.Join(" ", waves.Where(w => w.Hits > 0).Select(w => $"w{w.Wave}[{string.Join("+", w.Levels)}]-{w.Hits}")) +
                $"  (worst: {string.Join(", ", waves.MaxBy(w => w.Hits)!.Names)})");
        }
        var every = all.SelectMany(a => a.Waves).ToList();

        // Short runs against long ones, over the waves both lived through.
        var byLength = all.OrderBy(a => a.Reached).ToList();
        int third = Math.Max(1, runs / 3), band = byLength[0].Reached - 1;
        Console.WriteLine($"\nShortest third of runs against longest third, waves 1-{band} (all of them lived that long):");
        foreach (var (label, group) in new[] { ("short", byLength.Take(third)), ("long", byLength.TakeLast(third)) })
        {
            var ws = group.SelectMany(a => a.Waves.Where(w => w.Wave <= band)).ToList();
            Console.WriteLine($"  {label,-6} reached {string.Join(",", group.Select(a => a.Reached))}  patterns a wave {ws.Average(w => w.Count):0.00}  " +
                $"letters a wave {ws.Average(w => w.Load):0}  biggest pattern {ws.Average(w => w.Levels.Max()):0.0}  hits a wave {ws.Average(w => w.Hits):0.00}");
        }

        Console.WriteLine("\nHits a wave by the number of patterns (waves), in bands of waves:");
        foreach (var (lo, hi) in new[] { (1, 9), (10, 15), (16, 21), (22, 40) })
        {
            var ws = every.Where(w => w.Wave >= lo && w.Wave <= hi).ToList();
            if (ws.Count == 0) continue;
            Console.WriteLine($"  waves {lo,2}-{hi,-2}  " + string.Join("  ", ws.GroupBy(w => w.Count).OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Average(w => w.Hits):0.00} ({g.Count()})")));
        }

        Console.WriteLine("\nLetters a wave against its budget (letters + words per budget level), waves 10-21:");
        var mid = every.Where(w => w.Wave is >= 10 and <= 21).ToList();
        Console.WriteLine("  " + string.Join("  ", mid.GroupBy(w => w.Count).OrderBy(g => g.Key)
            .Select(g => $"{g.Key} patterns: {g.Average(w => w.Load / (double)w.Levels.Sum()):0.0}")));
        foreach (var g in mid.GroupBy(w => (int)(w.Load / (double)w.Levels.Sum())).OrderBy(g => g.Key))
            Console.WriteLine($"  {g.Key,2}-{g.Key + 1,-2} letters a level  {g.Count(),3} waves  hits {g.Average(w => w.Hits):0.00}");

        // Waves that repeat a pattern (the missile cap leaves some levels with one other pattern) against waves that don't.
        Console.WriteLine("\nWaves with a pattern twice or more against waves without, by number of patterns: hits a wave (waves):");
        foreach (var g in every.Where(w => w.Count >= 2).GroupBy(w => w.Count).OrderBy(g => g.Key))
        {
            var rep = g.Where(w => w.Names.Distinct().Count() < w.Count).ToList();
            var not = g.Except(rep).ToList();
            Console.WriteLine($"  {g.Key} patterns: repeated {(rep.Count > 0 ? rep.Average(w => w.Hits) : 0):0.00} ({rep.Count})  not {(not.Count > 0 ? not.Average(w => w.Hits) : 0):0.00} ({not.Count})");
        }

        // Each pattern: how much more its waves hurt than waves of the same band and pattern count.
        double Expected(WaveLog w) => every.Where(o => o.Count == w.Count && Math.Abs(o.Wave - w.Wave) <= 3).Average(o => o.Hits);
        Console.WriteLine("\nPatterns by the extra hits of their waves (over waves like them: as many patterns, within 3 waves):");
        var blame = every.SelectMany(w => w.Names.Distinct().Select(n => (Name: n, Extra: w.Hits - Expected(w))))
            .GroupBy(x => x.Name).Select(g => (g.Key, Times: g.Count(), Extra: g.Average(x => x.Extra))).OrderByDescending(x => x.Extra).ToList();
        foreach (var (name, times, extra) in blame) Console.WriteLine($"  {name,-34} {times,4}  {extra,5:+0.00;-0.00}");
        return 0;
    }

    /// <summary>Follows a run's clears, waves where every ship was shot down (none flew off or crashed into the player).
    /// A wave is over when the next starts, so the one the run ends in never counts.</summary>
    sealed class Clears
    {
        int wave, missedAtStart;
        /// <summary>The last wave of an unbroken run of clears from wave 1, and the last wave cleared at all.</summary>
        public int Streak, Last;
        /// <summary>When those waves ended (the next was announced), in seconds.</summary>
        public double StreakTime, LastTime;

        public void Tick(Game g)
        {
            if (g.Endless!.Wave == wave) return;
            if (wave > 0 && g.MissedShips == missedAtStart)
            {
                (Last, LastTime) = (wave, g.Time);
                if (Streak == wave - 1) (Streak, StreakTime) = (wave, g.Time);
            }
            wave = g.Endless.Wave;
            missedAtStart = g.MissedShips;
        }
    }

    sealed class WaveLog(int wave, List<string> names, int load, int health)
    {
        public int Wave = wave, Load = load, Health = health, Hits;
        public List<string> Names = names;
        public int Count => Names.Count;
        public List<int> Levels => Names.Select(n => int.Parse(n.Split(' ')[1])).ToList();
    }


    // --solo=NAME: plays each pattern whose name holds NAME on its own, many times, with the bot (--bot share of the
    // difficulty's speed, mistakes below 1) at --speed (the wave speed-up) or as in --wave=N, and prints the hits it does each time.
    static int Solo(GameData data, ArtLibrary art, Dictionary<string, string> opts)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int runs = int.Parse(opts.GetValueOrDefault("runs", "20"));
        double cps = Difficulty.Cps.First(c => Difficulty.Name(c).Equals(opts.GetValueOrDefault("difficulty", "Hard"), StringComparison.OrdinalIgnoreCase));
        double share = double.Parse(opts.GetValueOrDefault("bot", "0.9"), inv), speed = double.Parse(opts.GetValueOrDefault("speed", "1.1"), inv);
        if (opts.TryGetValue("botcps", out var bc)) share = double.Parse(bc, inv) / cps; // a set typing speed instead of a share
        foreach (var w in data.EndlessWaves.Where(w => w.Name.Contains(opts["solo"])).OrderBy(w => w.Level))
        {
            var hits = new List<int>();
            for (int seed = 1; seed <= runs; seed++)
            {
                var g = new Game(data, new Rng((ulong)seed), art) { Cps = cps };
                g.TutOff();
                g.NewGame(0);
                if (opts.TryGetValue("wave", out var wv)) // as in that wave of a run: its speed-up and fire rate
                {
                    var run = new EndlessRun(g, 1);
                    speed = run.SpeedOf(int.Parse(wv));
                    g.MRate = g.FireRate(run.CpsOf(int.Parse(wv)));
                }
                while (g.Objects.Count > 0) g.Objects[0].Remove();
                g.LevelData = LevelBuilder.Build(w.Script, new LevelContext(new Rng((ulong)seed), data.Tuning, cps, speed), g.Time + 1).ToList();
                var bot = new AutoTyper(g, cps * share, share < 1 ? Pacing.RealisticErrors : 0, errorSeed: (ulong)seed);
                int health = g.Player.Health, lost = 0;
                for (int t = 0; t < 60 * Playfield.TickRate; t++)
                {
                    g.Step();
                    bot.Step();
                    if (g.Player.Health < health) lost += health - g.Player.Health;
                    g.Player.Health = health = g.Player.MaxHealth; // count every hit, never die
                    if (g.LevelData.Count == 0 && !g.Objects.Any(o => o is EnemyShip || o is Missile { FromPlayer: false })) break;
                }
                hits.Add(lost);
            }
            Console.WriteLine($"{w.Name,-34} mean {hits.Average(),4:0.0}  {string.Join(" ", hits)}");
        }
        return 0;
    }

    static int Patterns(GameData data)
    {
        Console.WriteLine("Endless patterns against the guidelines: letters (the range for the level), words, spawn time (the");
        Console.WriteLine("guideline time for the level at these letters), and the level they work out to. ! marks a miss.");
        Console.WriteLine($"{"Level",5} {"Pattern",-34} {"Letters",-14} {"Words",5} {"Spawn time",-16} {"Works out",9}");
        int misses = 0;
        foreach (var w in data.EndlessWaves.OrderBy(w => w.Level))
        {
            var m = w.Guide(data.Tuning);
            bool lettersOff = m.Letters < m.MinLetters || m.Letters > m.MaxLetters, levelOff = m.GuideLevel != w.Level;
            if (lettersOff || levelOff || !m.DiversOk) misses++;
            string name = w.Name.Length > 34 ? w.Name[..34] : w.Name;
            double guideTime = Math.Max(0, EndlessWave.SpawnTimeFor(m.Letters + m.Words, w.Level));
            Console.WriteLine($"{w.Level,5} {name,-34} {$"{m.Letters} ({m.MinLetters}-{m.MaxLetters}){(lettersOff ? " !" : "")}",-14} {m.Words,5} " +
                $"{$"{m.SpawnTime:0.0#} s ({guideTime:0.0#})",-16} {m.GuideLevel,7}{(levelOff ? " !" : "")}{(w.MissileOnly(data.Tuning) ? "  missile" : "")}{(m.Divers > 0 ? $"  divers {m.DiversWithRoom}/{m.Divers} with room{(m.DiversOk ? "" : " !")}" : "")}");
        }
        Console.WriteLine();
        Console.WriteLine($"{data.EndlessWaves.Count} patterns, {misses} outside the guidelines. Per level: " +
            string.Join(" ", Enumerable.Range(1, EndlessWave.MaxLevel).Select(l => $"{l}:{data.EndlessWaves.Count(w => w.Level == l)}")) +
            $". Only missile ships: {data.EndlessWaves.Count(w => w.MissileOnly(data.Tuning))}; levels with none of the others: " +
            string.Join(" ", Enumerable.Range(1, EndlessWave.MaxLevel).Where(l => data.EndlessWaves.Where(w => w.Level == l).All(w => w.MissileOnly(data.Tuning)))));
        return 0;
    }
}
