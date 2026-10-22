using System.Globalization;
using TypingShip.Core;
using TypingShip.Core.Data;
using TypingShip.Core.Levels;

// dotnet run --project src/Sim -- pacing [--check] [--out=folder] [--seed=N] [--level=N] [--bot-speed=80%] [--errors=3.5%]
//   Plays every level of both level sets (the game's, data/levels, and Classic, data/levels/classic) at every
//   difficulty with the perfect bot and writes:
//     <folder>/pacing.csv      one row per set, level, difficulty and second (for charts and spreadsheets)
//     <folder>/summary.csv     one row per set, level and difficulty, with demand as a share of the difficulty's speed
//     <folder>/lines.csv       the time (as on Hard) each line of each level file happens, like L42 -> 1:37
//     <folder>/violations.csv  the pacing rules (data/pacing-rules.txt, docs/pacing-rules.md) each game level breaks
//     <folder>/realistic.csv   how a bot at 80% of the speed, with typing mistakes, fares on each game level
//     <folder>/pacing.html     charts of the same (open in a browser)
//   Default folder: pacing/ (git-ignored).
//   --check: print every rule a game level breaks, and exit with 1 if there are any (warnings don't count).
// dotnet run --project src/Sim -- endless [--runs=10] [--minutes=20]: see EndlessReport.cs.

var opts = args.Skip(1).Select(a => a.TrimStart('-').Split('=', 2)).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "");
if (args.Length == 0 || args[0] is not ("pacing" or "endless"))
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Sim -- pacing [--check] [--out=folder] [--seed=N] [--level=N] [--bot-speed=80%] [--errors=3.5%]");
    Console.Error.WriteLine("       dotnet run --project src/Sim -- endless [--runs=10] [--minutes=20]");
    return 1;
}

var root = FindRoot();
var data = GameData.Load(DataSource.FromDirectory(Path.Combine(root, "data")));
var art = ArtLibrary.FromFolder(Path.Combine(root, "game", "assets", "sprites"));
if (args[0] == "endless") return EndlessReport.Run(data, art, opts);
var outDir = Path.GetFullPath(opts.GetValueOrDefault("out", Path.Combine(root, "pacing")));
ulong seed = ulong.Parse(opts.GetValueOrDefault("seed", "1"));
var levels = opts.TryGetValue("level", out var l) ? new[] { int.Parse(l) } : Enumerable.Range(0, GameData.LevelCount).ToArray();
Directory.CreateDirectory(outDir);
var inv = CultureInfo.InvariantCulture;

// The pacing rules: their numbers, and the tail that also sets how averages are measured.
var rules = PacingRules.Parse(File.ReadAllText(Path.Combine(root, "data", PacingRules.FileName)),
    Path.Combine("data", PacingRules.FileName));

var sets = new[] { data.Main, data.Classic };
var runs = sets.SelectMany(set => levels.SelectMany(level => Difficulty.Cps.Select(cps => (set, level, cps))))
    .AsParallel().AsOrdered()
    .Select(x => (x.set, run: Pacing.Measure(data, art, x.level, x.cps, seed, classic: x.set == data.Classic, tail: rules.TailSeconds)))
    .ToList();
Pacing.Run ClassicOf(Pacing.Run r) => runs.First(x => x.set == data.Classic && x.run.Level == r.Level && x.run.Cps == r.Cps).run;

// The rules, checked on the game's levels (the perfect bot's runs).
var findings = runs.Where(x => x.set == data.Main).SelectMany(x => rules.Check(x.run, x.run.Level + 1)).ToList();

// A bot closer to a real player, reported separately (the rules don't use it).
double Share(string key, double fallback) => opts.TryGetValue(key, out var v)
    ? double.Parse(v.TrimEnd('%'), inv) / (v.EndsWith('%') ? 100 : 1) : fallback;
double botSpeed = Share("bot-speed", Pacing.RealisticSpeed), errorRate = Share("errors", Pacing.RealisticErrors);
var realistic = levels.SelectMany(level => Difficulty.Cps.Select(cps => (level, cps))).AsParallel().AsOrdered()
    .Select(x => Pacing.Realistic(data, art, x.level, x.cps, seed, botSpeed, errorRate)).ToList();

// When each line of each level file happens (as on Hard; the timing is the same at every difficulty).
var lineTimes = new List<(LevelSet Set, int Level, int Line, double Time, string Text)>();
foreach (var set in sets)
foreach (int level in levels)
{
    var text = File.ReadAllLines(Path.Combine(root, "data", set.LevelFile(level)));
    var events = set.BuildLevel(level, new LevelContext(new Rng(seed), data.Tuning, Difficulty.Hard));
    foreach (var g in events.GroupBy(e => e.Line).OrderBy(g => g.Key))
        lineTimes.Add((set, level, g.Key, g.Min(e => e.Time), g.Key > 0 && g.Key <= text.Length ? text[g.Key - 1].Trim() : ""));
}

using (var csv = new StreamWriter(Path.Combine(outDir, "pacing.csv")))
{
    csv.WriteLine("set,level,difficulty,wpm,second,progress,new_chars,demand_wpm,demand_ratio,words_on_screen,visible_words,chars_on_screen,pressure_ratio");
    foreach (var (set, r) in runs)
    foreach (var s in r.Samples)
        csv.WriteLine(string.Join(",", set.Name, r.Level + 1, Difficulty.Name(r.Cps), Speed.ToWpm(r.Cps), s.Second, s.Progress, s.NewChars,
            Speed.ToWpm(s.DemandCps).ToString("0.0", inv), (s.DemandCps / r.Cps).ToString("0.00", inv), s.WordsOnScreen, s.VisibleWords,
            s.CharsOnScreen, (r.Pressure is { } p && s.Second <= p.Length ? p[s.Second - 1] : 0).ToString("0.00", inv)));
}

using (var csv = new StreamWriter(Path.Combine(outDir, "summary.csv")))
{
    csv.WriteLine("set,level,difficulty,wpm,cleared,seconds,last_spawn,average_seconds,total_chars,average_demand_wpm,peak_demand_wpm," +
                  "average_ratio,peak_ratio,classic_average_ratio,classic_peak_ratio,peak_pressure,peak_pressure_at,peak_pressure_lines,ships,missed");
    foreach (var (set, r) in runs)
    {
        var c = ClassicOf(r);
        csv.WriteLine(string.Join(",", set.Name, r.Level + 1, Difficulty.Name(r.Cps), Speed.ToWpm(r.Cps), r.Cleared ? "yes" : "no", r.Seconds,
            PacingRules.Time(Math.Max(0, r.LastSpawnSecond - 1)), r.AverageSeconds, r.TotalChars,
            Speed.ToWpm(r.AverageCps).ToString("0.0", inv), Speed.ToWpm(r.PeakCps).ToString("0.0", inv),
            r.AverageRatio.ToString("0.00", inv), r.PeakRatio.ToString("0.00", inv),
            c.AverageRatio.ToString("0.00", inv), c.PeakRatio.ToString("0.00", inv),
            r.PeakPressure.Value.ToString("0.00", inv), PacingRules.Time(Math.Max(0, r.PeakPressure.Second - 1)),
            "\"" + string.Join(" ", r.PeakPressure.Second > 0 && r.PressureLines is { } pl ? pl[r.PeakPressure.Second - 1] : Array.Empty<int>()) + "\"",
            Field(r.Line, "ships"), Field(r.Line, "missed")));
    }
}

using (var csv = new StreamWriter(Path.Combine(outDir, "lines.csv")))
{
    csv.WriteLine("set,level,line,time,label,text");
    foreach (var x in lineTimes)
        csv.WriteLine(string.Join(",", x.Set.Name, x.Level + 1, x.Line, Clock(x.Time), $"L{x.Line} -> {Clock(x.Time)}",
            "\"" + x.Text.Replace("\"", "\"\"") + "\""));
}

using (var csv = new StreamWriter(Path.Combine(outDir, "violations.csv")))
{
    csv.WriteLine("level,difficulty,rule,time,kind,message");
    foreach (var f in findings)
        csv.WriteLine(string.Join(",", f.Level, f.Difficulty, f.Rule, f.At, f.Warning ? "warning" : "violation",
            "\"" + f.Message.Replace("\"", "\"\"") + "\""));
}

using (var csv = new StreamWriter(Path.Combine(outDir, "realistic.csv")))
{
    csv.WriteLine("level,difficulty,wpm,bot_wpm,error_rate,cleared,seconds,hits,health_left");
    foreach (var o in realistic)
        csv.WriteLine(string.Join(",", o.Level + 1, Difficulty.Name(o.Cps), Speed.ToWpm(o.Cps), Speed.ToWpm(o.BotCps).ToString("0.#", inv),
            o.ErrorRate.ToString("0.###", inv), o.Cleared ? "yes" : "no", o.Seconds, o.Hits, o.Health));
}

if (opts.ContainsKey("check"))
{
    Console.WriteLine($"Pacing rules ({Path.Combine("data", PacingRules.FileName)}) on the game's levels, perfect bot, seed {seed}:");
    foreach (var group in findings.GroupBy(f => (f.Level, f.Difficulty)))
    {
        Console.WriteLine($"\nLevel {group.Key.Level}, {group.Key.Difficulty}:");
        foreach (var f in group.OrderBy(f => f.Second)) Console.WriteLine($"  {(f.Warning ? "warn" : "FAIL")}  {f}");
    }
    int failures = findings.Count(f => !f.Warning), warnings = findings.Count - failures;
    Console.WriteLine();
    Console.WriteLine("Violations per level (Ea No Ha Ex In):");
    foreach (int level in levels.Select(lv => lv + 1).Where(lv => !rules.IsExempt(lv, "all")))
        Console.WriteLine($"  level {level,2}:  " + string.Join("  ", Difficulty.Names.Select(d =>
            $"{findings.Count(f => f.Level == level && f.Difficulty == d && !f.Warning),2}")));
    Console.WriteLine();
    Console.WriteLine($"Realistic bot ({botSpeed * 100:0}% speed, {errorRate * 100:0.#}% mistakes): " +
        string.Join("  ", realistic.Where(o => !rules.IsExempt(o.Level + 1, "all") && !o.Cleared)
            .Select(o => $"L{o.Level + 1} {Difficulty.Name(o.Cps)} lost at {PacingRules.Time(o.Seconds)}")
            .DefaultIfEmpty("cleared every regular level")));
    Console.WriteLine(failures == 0 ? $"\nAll rules pass ({warnings} warnings)." : $"\n{failures} violations, {warnings} warnings.");
    return failures == 0 ? 0 : 1;
}

// The chart page: src/Sim/pacing.html with the runs filled in.
var page = new
{
    generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm", inv),
    seed,
    window = Pacing.Window,
    runs = runs.Select(x => new
    {
        set = x.set.Name, level = x.run.Level + 1, diff = Difficulty.Name(x.run.Cps), wpm = Speed.ToWpm(x.run.Cps), cleared = x.run.Cleared,
        seconds = x.run.Seconds, lastSpawn = Math.Max(0, x.run.LastSpawnSecond - 1), total = x.run.TotalChars,
        avg = Math.Round(Speed.ToWpm(x.run.AverageCps), 1), peak = Math.Round(Speed.ToWpm(x.run.PeakCps), 1),
        d = x.run.Samples.Select(s => (int)Math.Round(Speed.ToWpm(s.DemandCps))).ToArray(),
        w = x.run.Samples.Select(s => s.VisibleWords).ToArray(),
        p = (x.run.Pressure ?? Array.Empty<double>()).Select(v => (int)Math.Round(v * Speed.ToWpm(x.run.Cps))).ToArray(),
        peakPressure = Math.Round(x.run.PeakPressure.Value, 2),
    }),
};
var template = File.ReadAllText(Path.Combine(root, "src", "Sim", "pacing.html"));
File.WriteAllText(Path.Combine(outDir, "pacing.html"),
    template.Replace("\"__DATA__\"", System.Text.Json.JsonSerializer.Serialize(page)));

Console.WriteLine($"Demand ÷ the difficulty's speed: average (start to last spawn + {rules.TailSeconds:0} s) and peak ({Pacing.Window} s window).");
Console.WriteLine($"Press: peak pressure, the speed needed to stop every threat before it hits (÷ the difficulty's speed).");
Console.WriteLine("Classic is the original pacing, converted to seconds on Hard.");
Console.WriteLine($"{"",18}  {"Game levels",-36}  {"Classic",-29}");
Console.WriteLine("Level  Difficulty  Time  Last spawn  Avg   Peak  Press    Time   Avg   Peak  Press");
foreach (var (set, r) in runs.Where(x => x.set == data.Main))
{
    var c = ClassicOf(r);
    Console.WriteLine($"{r.Level + 1,5}  {Difficulty.Name(r.Cps),-10}  {PacingRules.Time(r.Seconds)}  {PacingRules.Time(Math.Max(0, r.LastSpawnSecond - 1)),10}" +
                      $"  {r.AverageRatio,4:0.00}  {r.PeakRatio,5:0.00}  {r.PeakPressure.Value,5:0.00}    {PacingRules.Time(c.Seconds)}  {c.AverageRatio,4:0.00}  {c.PeakRatio,5:0.00}  {c.PeakPressure.Value,5:0.00}" +
                      (r.Cleared ? "" : "   GAME LEVEL NOT CLEARED") + (c.Cleared ? "" : "   CLASSIC NOT CLEARED"));
}
Console.WriteLine($"Rules: {findings.Count(f => !f.Warning)} violations on the game's levels (pacing --check lists them). Realistic bot: " +
    $"{realistic.Count(o => !o.Cleared)} of {realistic.Count} runs lost.");
Console.WriteLine($"Wrote pacing.csv, summary.csv, lines.csv, violations.csv, realistic.csv and pacing.html (charts) in {outDir}");
return 0;

// m:ss.s (tenths), for line times.
static string Clock(double seconds)
{
    int m = (int)(seconds / 60);
    double s = seconds - m * 60;
    return $"{m}:{s.ToString("00.0", CultureInfo.InvariantCulture)}";
}

// "Level 0 cleared: score 292, ships 56, missed 0, ..." -> the number after the word.
static string Field(string line, string name)
{
    int i = line.IndexOf(name + " ", StringComparison.Ordinal);
    return i < 0 ? "" : new string(line[(i + name.Length + 1)..].TakeWhile(char.IsDigit).ToArray());
}

static string FindRoot()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "TypingShip.sln"))) return dir.FullName;
    throw new InvalidOperationException("Run this from inside the repo (can't find TypingShip.sln).");
}
