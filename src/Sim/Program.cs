using System.Globalization;
using TypingShip.Core;
using TypingShip.Core.Data;

// dotnet run --project src/Sim -- pacing [--out=folder] [--seed=N] [--level=N]
//   Plays every level at every difficulty with the perfect bot and writes:
//     <folder>/pacing.csv   one row per level, difficulty and second (for charts and spreadsheets)
//     <folder>/summary.csv  one row per level and difficulty
//     <folder>/pacing.html  charts of the same (open in a browser)
//   Default folder: pacing/ (git-ignored).

var opts = args.Skip(1).Select(a => a.TrimStart('-').Split('=', 2)).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "");
if (args.Length == 0 || args[0] != "pacing")
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Sim -- pacing [--out=folder] [--seed=N] [--level=N]");
    return 1;
}

var root = FindRoot();
var data = GameData.Load(DataSource.FromDirectory(Path.Combine(root, "data")));
var art = ArtLibrary.FromFolder(Path.Combine(root, "game", "assets", "sprites"));
var outDir = Path.GetFullPath(opts.GetValueOrDefault("out", Path.Combine(root, "pacing")));
ulong seed = ulong.Parse(opts.GetValueOrDefault("seed", "1"));
var levels = opts.TryGetValue("level", out var l) ? new[] { int.Parse(l) } : Enumerable.Range(0, 12).ToArray();
Directory.CreateDirectory(outDir);

var inv = CultureInfo.InvariantCulture;
var runs = levels.SelectMany(level => Difficulty.Wpm.Select(wpm => (level, wpm)))
    .AsParallel().AsOrdered()
    .Select(x => Pacing.Measure(data, art, x.level, x.wpm, seed))
    .ToList();

using (var csv = new StreamWriter(Path.Combine(outDir, "pacing.csv")))
{
    csv.WriteLine("level,difficulty,wpm,second,progress,new_chars,demand_wpm,words_on_screen,chars_on_screen");
    foreach (var r in runs)
    foreach (var s in r.Samples)
        csv.WriteLine(string.Join(",", r.Level + 1, Name(r.Wpm), r.Wpm, s.Second, s.Progress, s.NewChars,
            s.DemandWpm.ToString("0.0", inv), s.WordsOnScreen, s.CharsOnScreen));
}

using (var csv = new StreamWriter(Path.Combine(outDir, "summary.csv")))
{
    csv.WriteLine("level,difficulty,wpm,cleared,seconds,total_chars,average_demand_wpm,peak_demand_wpm,ships,missed");
    foreach (var r in runs)
        csv.WriteLine(string.Join(",", r.Level + 1, Name(r.Wpm), r.Wpm, r.Cleared ? "yes" : "no", r.Seconds, r.TotalChars,
            r.AverageWpm.ToString("0.0", inv), r.PeakWpm.ToString("0.0", inv), Field(r.Line, "ships"), Field(r.Line, "missed")));
}

// The chart page: src/Sim/pacing.html with the runs filled in.
var page = new
{
    generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm", inv),
    seed,
    window = Pacing.Window,
    runs = runs.Select(r => new
    {
        level = r.Level + 1, diff = Name(r.Wpm), wpm = r.Wpm, cleared = r.Cleared, seconds = r.Seconds,
        total = r.TotalChars, avg = Math.Round(r.AverageWpm, 1), peak = Math.Round(r.PeakWpm, 1),
        d = r.Samples.Select(s => (int)Math.Round(s.DemandWpm)).ToArray(),
    }),
};
var template = File.ReadAllText(Path.Combine(root, "src", "Sim", "pacing.html"));
File.WriteAllText(Path.Combine(outDir, "pacing.html"),
    template.Replace("\"__DATA__\"", System.Text.Json.JsonSerializer.Serialize(page)));

Console.WriteLine($"Level  Difficulty  Time    Avg WPM  Peak WPM  (demand; {Pacing.Window} s window)");
foreach (var r in runs)
    Console.WriteLine($"{r.Level + 1,5}  {Name(r.Wpm),-10}  {r.Seconds / 60}:{r.Seconds % 60:00}  {r.AverageWpm,7:0}  {r.PeakWpm,8:0}" +
                      (r.Cleared ? "" : "   NOT CLEARED"));
Console.WriteLine($"Wrote pacing.csv, summary.csv and pacing.html (charts) in {outDir}");
return 0;

static string Name(int wpm) => wpm switch
{
    Difficulty.Easy => "Easy", Difficulty.Normal => "Normal", Difficulty.Hard => "Hard",
    Difficulty.Extreme => "Extreme", Difficulty.Insane => "Insane", _ => wpm.ToString(),
};

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
