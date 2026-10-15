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
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }

    sealed class FuncSource(Func<string, string?> read) : DataSource
    {
        public override string? Read(string name) => read(name);
    }
}

/// <summary>
/// Everything the rules read from data/: the word list, the tuning numbers and the level scripts. Load() checks it
/// all (including a trial build of every level) and throws a <see cref="DataError"/> for the first mistake.
/// </summary>
public sealed class GameData
{
    public const int LevelCount = 12;
    public const string BossFile = "levels/bosses.txt";

    public string Words { get; }
    public Tuning Tuning { get; }
    public IReadOnlyList<LevelScript> Levels { get; }
    /// <summary>bosses.txt: "[boss N escorts]" (sent in during the fight) and "[boss 1 hangar]" (boss 1's ships).</summary>
    public IReadOnlyDictionary<string, LevelScript> BossScripts { get; }

    GameData(string words, Tuning tuning, IReadOnlyList<LevelScript> levels, IReadOnlyDictionary<string, LevelScript> boss)
    {
        Words = words;
        Tuning = tuning;
        Levels = levels;
        BossScripts = boss;
    }

    public static string LevelFile(int level) => $"levels/level{level:00}.txt";

    public static GameData Load(DataSource source)
    {
        string Need(string name) => source.Read(name) ?? throw new DataError(name, 0, "The file is missing.");

        var tuning = Tuning.Parse(Need("tuning.txt"), "tuning.txt");
        var levels = Enumerable.Range(0, LevelCount).Select(i => LevelScript.Parse(Need(LevelFile(i)), LevelFile(i))).ToList();
        var boss = LevelScript.ParseSections(Need(BossFile), BossFile);
        foreach (var name in RequiredBossSections)
            if (!boss.ContainsKey(name)) throw new DataError(BossFile, 0, $"Section [{name}] is missing.");
        foreach (var name in boss.Keys)
            if (!RequiredBossSections.Contains(name))
                throw new DataError(BossFile, 0, $"Unknown section [{name}].{DataError.DidYouMean(name, RequiredBossSections)} " +
                    $"The sections are {string.Join(", ", RequiredBossSections.Select(s => $"[{s}]"))}.");
        var data = new GameData(Need("words.txt"), tuning, levels, boss);
        data.Check();
        return data;
    }

    static readonly string[] RequiredBossSections = { "boss 1 escorts", "boss 1 hangar", "boss 2 escorts", "boss 3 escorts" };

    /// <summary>Builds every script once, so mistakes only found while building (like "mirror" with nothing above
    /// it) show up at load rather than mid-game.</summary>
    void Check()
    {
        foreach (int wpm in Difficulty.Wpm)
        {
            var c = new LevelContext(new Rng(1), Tuning, wpm);
            foreach (var level in Levels)
            {
                var events = LevelBuilder.Build(level, c);
                if (events.Count == 0) throw new DataError(level.File, 0, "The level is empty.");
            }
            foreach (var script in BossScripts.Values)
            {
                var events = LevelBuilder.Build(script, c);
                if (script.Section == "boss 1 hangar" && events.Any(e => e is not SpawnEvent))
                    throw new DataError(BossFile, events.First(e => e is not SpawnEvent).Line, "The hangar can only have ships.");
            }
        }
    }

    public List<LevelEvent> BuildLevel(int level, LevelContext c) => LevelBuilder.Build(Levels[level], c);

    /// <summary>Escorts that keep coming while a boss fight is on, starting from the current distance.</summary>
    public List<LevelEvent> BuildBossLoop(int boss, LevelContext c, double distance) =>
        LevelBuilder.Build(BossScripts[$"boss {boss + 1} escorts"], c, distance);

    /// <summary>The six suicide ships boss 1 launches, positioned relative to the boss art's top-left corner.</summary>
    public List<SpawnEvent> BuildHangar(LevelContext c) =>
        LevelBuilder.Build(BossScripts["boss 1 hangar"], c).Cast<SpawnEvent>().ToList();
}
