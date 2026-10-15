using TypingShip.Core.Levels;

namespace TypingShip.Core.Data;

/// <summary>Where data files come from: the data/ folder on disk, or resources embedded in the game.</summary>
public abstract class DataSource
{
    /// <summary>The text of a data file such as "tuning.txt" or "levels/level00.txt", or null if it doesn't exist.</summary>
    public abstract string? Read(string name);

    public static DataSource FromDirectory(string directory) => new DirectorySource(directory);
    public static DataSource FromFunc(Func<string, string?> read) => new FuncSource(read);

    sealed class DirectorySource(string directory) : DataSource
    {
        public override string? Read(string name)
        {
            var path = Path.Combine(directory, name);
            if (!File.Exists(path)) return null;
            // An editor may be saving the file at this very moment (F5 right after saving): try again briefly.
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch (IOException) when (attempt < 10)
                {
                    Thread.Sleep(50);
                }
                catch (IOException e)
                {
                    throw new DataError(name, 0, $"Couldn't read the file ({e.Message}). Try again in a moment.");
                }
            }
        }
    }

    sealed class FuncSource(Func<string, string?> read) : DataSource
    {
        public override string? Read(string name) => read(name);
    }
}

/// <summary>
/// Everything the rules read from data/: the word list, the tuning numbers, the two level sets and the typing test.
/// Load() checks it all (including a trial build of every level) and throws a <see cref="DataError"/> for the
/// first mistake.
/// </summary>
public sealed class GameData
{
    public const int LevelCount = 12;
    public const string TestFile = "typing-test.txt";

    public string Words { get; }
    public Tuning Tuning { get; }
    /// <summary>The game's levels, data/levels/.</summary>
    public LevelSet Main { get; }
    /// <summary>The original 12 levels as they were, data/levels/classic/: a baseline during development, removed before release.</summary>
    public LevelSet Classic { get; }
    /// <summary>typing-test.txt: the typing test's passages.</summary>
    public IReadOnlyList<string> TestPassages { get; }

    GameData(string words, Tuning tuning, LevelSet main, LevelSet classic, IReadOnlyList<string> passages)
    {
        Words = words;
        Tuning = tuning;
        Main = main;
        Classic = classic;
        TestPassages = passages;
    }

    public static GameData Load(DataSource source)
    {
        string Need(string name) => source.Read(name) ?? throw new DataError(name, 0, "The file is missing.");

        var tuning = Tuning.Parse(Need("tuning.txt"), "tuning.txt");
        var data = new GameData(Need("words.txt"), tuning,
            LevelSet.Load("main", "levels", Need), LevelSet.Load("classic", "levels/classic", Need),
            TypingTestRun.ParsePassages(Need(TestFile), TestFile));
        data.Main.Check(tuning);
        data.Classic.Check(tuning);
        return data;
    }
}

/// <summary>
/// Twelve level files and their bosses.txt, in one folder: the game's levels (data/levels/) or the Classic ones
/// (data/levels/classic/).
/// </summary>
public sealed class LevelSet
{
    public string Name { get; }
    /// <summary>The folder inside data/, e.g. "levels/classic".</summary>
    public string Folder { get; }
    public IReadOnlyList<LevelScript> Levels { get; }
    /// <summary>bosses.txt: "[boss N escorts]" (sent in during the fight) and "[boss 1 hangar]" (boss 1's ships).</summary>
    public IReadOnlyDictionary<string, LevelScript> BossScripts { get; }

    LevelSet(string name, string folder, IReadOnlyList<LevelScript> levels, IReadOnlyDictionary<string, LevelScript> boss)
    {
        Name = name;
        Folder = folder;
        Levels = levels;
        BossScripts = boss;
    }

    public string LevelFile(int level) => $"{Folder}/level{level:00}.txt";
    public string BossFile => $"{Folder}/bosses.txt";

    static readonly string[] RequiredBossSections = { "boss 1 escorts", "boss 1 hangar", "boss 2 escorts", "boss 3 escorts" };

    internal static LevelSet Load(string name, string folder, Func<string, string> need)
    {
        var set = new LevelSet(name, folder, Array.Empty<LevelScript>(), new Dictionary<string, LevelScript>());
        var levels = Enumerable.Range(0, GameData.LevelCount).Select(i => LevelScript.Parse(need(set.LevelFile(i)), set.LevelFile(i))).ToList();
        var bossFile = set.BossFile;
        var boss = LevelScript.ParseSections(need(bossFile), bossFile);
        foreach (var section in RequiredBossSections)
            if (!boss.ContainsKey(section)) throw new DataError(bossFile, 0, $"Section [{section}] is missing.");
        foreach (var section in boss.Keys)
            if (!RequiredBossSections.Contains(section))
                throw new DataError(bossFile, 0, $"Unknown section [{section}].{DataError.DidYouMean(section, RequiredBossSections)} " +
                    $"The sections are {string.Join(", ", RequiredBossSections.Select(s => $"[{s}]"))}.");
        return new LevelSet(name, folder, levels, boss);
    }

    /// <summary>Builds every script once at every difficulty, so mistakes only found while building (like "mirror"
    /// with nothing above it) show up at load rather than mid-game.</summary>
    internal void Check(Tuning tuning)
    {
        foreach (double cps in Difficulty.Cps)
        {
            var c = new LevelContext(new Rng(1), tuning, cps);
            foreach (var level in Levels)
            {
                var events = LevelBuilder.Build(level, c);
                if (!events.OfType<SpawnEvent>().Any() && !events.OfType<BossEvent>().Any())
                    throw new DataError(level.File, 0, $"The level has no ships on {Difficulty.Names[c.DifficultyIndex]}.");
            }
            foreach (var script in BossScripts.Values)
            {
                var events = LevelBuilder.Build(script, c);
                if (script.Section == "boss 1 hangar" && events.Any(e => e is not SpawnEvent))
                    throw new DataError(BossFile, events.First(e => e is not SpawnEvent).Line, "The hangar can only have ships.");
            }
        }
    }

    /// <summary>The context a level is built and played in: its pace and density at this difficulty.</summary>
    public LevelContext Context(int level, Rng rng, Tuning tuning, double cps)
    {
        var script = level >= 0 && level < Levels.Count ? Levels[level] : null;
        var c = new LevelContext(rng, tuning, cps, script?.Pace);
        return script?.Density is { } d ? c with { Density = d[c.DifficultyIndex] } : c;
    }

    public List<LevelEvent> BuildLevel(int level, LevelContext c) => LevelBuilder.Build(Levels[level], c);

    /// <summary>Escorts that keep coming while a boss fight is on, starting from the current distance.</summary>
    public List<LevelEvent> BuildBossLoop(int boss, LevelContext c, double distance) =>
        LevelBuilder.Build(BossScripts[$"boss {boss + 1} escorts"], c, distance);

    /// <summary>The six suicide ships boss 1 launches, positioned relative to the boss art's top-left corner.</summary>
    public List<SpawnEvent> BuildHangar(LevelContext c) =>
        LevelBuilder.Build(BossScripts["boss 1 hangar"], c, thin: false).Cast<SpawnEvent>().ToList();
}
