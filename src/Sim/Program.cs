using System.Globalization;
using TypingShip.Core;
using TypingShip.Core.Data;

// dotnet run --project src/Sim -- pacing [--check] [--out=folder] [--seed=N] [--level=N] [--bot-speed=80%] [--errors=3.5%]
//   Plays every level of both level sets (the game's, data/levels, and Classic, data/levels/classic) at every
//   difficulty with the perfect bot and writes:
//     <folder>/pacing.csv   one row per set, level, difficulty and second (for charts and spreadsheets)
//     <folder>/summary.csv  one row per set, level and difficulty, with demand as a share of the difficulty's speed
//     <folder>/pacing.html  charts of the same (open in a browser)
//     <folder>/violations.csv  the pacing rules (data/pacing-rules.txt, docs/pacing-rules.md) each game level breaks
//     <folder>/realistic.csv   how a bot at 80% of the speed, with typing mistakes, fares on each game level
//   Default folder: pacing/ (git-ignored).
//   --check: print every rule a game level breaks, and exit with 1 if there are any (warnings don't count).

var opts = args.Skip(1).Select(a => a.TrimStart('-').Split('=', 2)).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "");
if (args.Length == 0 || args[0] != "pacing")
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Sim -- pacing [--check] [--out=folder] [--seed=N] [--level=N] [--bot-speed=80%] [--errors=3.5%]");
    return 1;
}

var root = FindRoot();
var data = GameData.Load(DataSource.FromDirectory(Path.Combine(root, "data")));
var art = ArtLibrary.FromFolder(Path.Combine(root, "game", "assets", "sprites"));
var outDir = Path.GetFullPath(opts.GetValueOrDefault("out", Path.Combine(root, "pacing")));
ulong seed = ulong.Parse(opts.GetValueOrDefault("seed", "1"));
var levels = opts.TryGetValue("level", out var l) ? new[] { int.Parse(l) } : Enumerable.Range(0, GameData.LevelCount).ToArray();
Directory.CreateDirectory(outDir);

var inv = CultureInfo.InvariantCulture;
var sets = new[] { data.Main, data.Classic };
var runs = sets.SelectMany(set => levels.SelectMany(level => Difficulty.Cps.Select(cps => (set, level, cps))))
    .AsParallel().AsOrdered()
    .Select(x => (x.set, run: Pacing.Measure(data, art, x.level, x.cps, seed, classic: x.set == data.Classic)))
    .ToList();

// The pacing rules, checked on the game's levels (the perfect bot's runs).
var rules = PacingRules.Parse(File.ReadAllText(Path.Combine(root, "data", PacingRules.FileName)),
    Path.Combine("data", PacingRules.FileName));
rules.TailSeconds = data.Tuning.DurationTail;
var findings = runs.Where(x => x.set == data.Main).SelectMany(x => rules.Check(x.run, x.run.Level + 1)).ToList();

// A bot closer to a real player, reported separately (the rules don't use it).
double Share(string key, double fallback) => opts.TryGetValue(key, out var v)
    ? double.Parse(v.TrimEnd('%'), inv) / (v.EndsWith('%') ? 100 : 1) : fallback;
double botSpeed = Share("bot-speed", 0.8), errorRate = Share("errors", 0.035);
var realistic = levels.SelectMany(level => Difficulty.Cps.Select(cps => (level, cps))).AsParallel().AsOrdered()
    .Select(x => Pacing.Realistic(data, art, x.level, x.cps, seed, botSpeed, errorRate)).ToList();

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
    foreach (int level in levels.Select(l => l + 1).Where(l => !rules.IsExempt(l, "all")))
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

// The Classic run for the same level and difficulty, to compare with.
Pacing.Run ClassicOf(Pacing.Run r) => runs.First(x => x.set == data.Classic && x.run.Level == r.Level && x.run.Cps == r.Cps).run;

// The density that should bring a game level's average demand to the Classic share at this difficulty. Thinning
// out waves cuts demand roughly in proportion, so this is a starting point to try, not an exact answer.
double? SuggestDensity(LevelSet set, Pacing.Run r)
{
    if (set != data.Main) return null;
    double now = set.Levels[r.Level].Density?[Difficulty.Of(r.Cps) - 1] ?? 1;
    return Math.Clamp(now * ClassicOf(r).AverageRatio / r.AverageRatio, 0.01, 3); // above 100% adds copies of waves
}

using (var csv = new StreamWriter(Path.Combine(outDir, "pacing.csv")))
{
    csv.WriteLine("set,level,difficulty,wpm,second,progress,new_chars,demand_wpm,demand_ratio,words_on_screen,chars_on_screen");
    foreach (var (set, r) in runs)
    foreach (var s in r.Samples)
        csv.WriteLine(string.Join(",", set.Name, r.Level + 1, Difficulty.Name(r.Cps), Speed.ToWpm(r.Cps), s.Second, s.Progress, s.NewChars,
            Speed.ToWpm(s.DemandCps).ToString("0.0", inv), (s.DemandCps / r.Cps).ToString("0.00", inv), s.WordsOnScreen, s.CharsOnScreen));
}

using (var csv = new StreamWriter(Path.Combine(outDir, "summary.csv")))
{
    csv.WriteLine("set,level,difficulty,wpm,cleared,seconds,total_chars,average_demand_wpm,peak_demand_wpm,average_ratio,peak_ratio," +
                  "classic_average_ratio,classic_peak_ratio,suggested_density,ships,missed");
    foreach (var (set, r) in runs)
    {
        var c = ClassicOf(r);
        csv.WriteLine(string.Join(",", set.Name, r.Level + 1, Difficulty.Name(r.Cps), Speed.ToWpm(r.Cps), r.Cleared ? "yes" : "no", r.Seconds,
            r.TotalChars, Speed.ToWpm(r.AverageCps).ToString("0.0", inv), Speed.ToWpm(r.PeakCps).ToString("0.0", inv),
            r.AverageRatio.ToString("0.00", inv), r.PeakRatio.ToString("0.00", inv),
            c.AverageRatio.ToString("0.00", inv), c.PeakRatio.ToString("0.00", inv),
            SuggestDensity(set, r) is { } d ? $"{d * 100:0}%" : "", Field(r.Line, "ships"), Field(r.Line, "missed")));
    }
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
        seconds = x.run.Seconds, total = x.run.TotalChars, avg = Math.Round(Speed.ToWpm(x.run.AverageCps), 1),
        peak = Math.Round(Speed.ToWpm(x.run.PeakCps), 1), density = SuggestDensity(x.set, x.run) is { } d ? Math.Round(d * 100) : (double?)null,
        d = x.run.Samples.Select(s => (int)Math.Round(Speed.ToWpm(s.DemandCps))).ToArray(),
    }),
};
var template = File.ReadAllText(Path.Combine(root, "src", "Sim", "pacing.html"));
File.WriteAllText(Path.Combine(outDir, "pacing.html"),
    template.Replace("\"__DATA__\"", System.Text.Json.JsonSerializer.Serialize(page)));

Console.WriteLine($"Demand ÷ the difficulty's speed: average and peak ({Pacing.Window} s window). Classic is the original pacing.");
Console.WriteLine($"{"",18}  {"Game levels",-24}  {"Classic",-24}");
Console.WriteLine("Level  Difficulty  Time   Avg   Peak  Density?  Time   Avg   Peak");
foreach (var (set, r) in runs.Where(x => x.set == data.Main))
{
    var c = ClassicOf(r);
    var d = SuggestDensity(set, r) is { } v && Math.Abs(v - (set.Levels[r.Level].Density?[Difficulty.Of(r.Cps) - 1] ?? 1)) > 0.005 ? $"{v * 100:0}%" : "";
    Console.WriteLine($"{r.Level + 1,5}  {Difficulty.Name(r.Cps),-10}  {Time(r.Seconds)}  {r.AverageRatio,4:0.00}  {r.PeakRatio,5:0.00}  {d,8}" +
                      $"  {Time(c.Seconds)}  {c.AverageRatio,4:0.00}  {c.PeakRatio,5:0.00}" +
                      (r.Cleared ? "" : "   GAME LEVEL NOT CLEARED") + (c.Cleared ? "" : "   CLASSIC NOT CLEARED"));
}
foreach (var (cps, i) in Difficulty.Cps.Select((cps, i) => (cps, i)))
{
    double Mean(LevelSet set, Func<Pacing.Run, double> f) => runs.Where(x => x.set == set && x.run.Cps == cps).Average(x => f(x.run));
    Console.WriteLine($"  all  {Difficulty.Names[i],-10}  {Time((int)Mean(data.Main, r => r.Seconds))}  {Mean(data.Main, r => r.AverageRatio),4:0.00}  " +
                      $"{Mean(data.Main, r => r.PeakRatio),5:0.00}  {"",8}  {Time((int)Mean(data.Classic, r => r.Seconds))}  " +
                      $"{Mean(data.Classic, r => r.AverageRatio),4:0.00}  {Mean(data.Classic, r => r.PeakRatio),5:0.00}");
}
Console.WriteLine("Density?: a density for this difficulty that should bring the level near the Classic average share.");
Console.WriteLine($"Rules: {findings.Count(f => !f.Warning)} violations on the game's levels (pacing --check lists them). Realistic bot: " +
    $"{realistic.Count(o => !o.Cleared)} of {realistic.Count} runs lost.");
Console.WriteLine($"Wrote pacing.csv, summary.csv, violations.csv, realistic.csv and pacing.html (charts) in {outDir}");
return 0;

static string Time(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

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
