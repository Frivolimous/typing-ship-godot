using TypingShip.Core.Data;

namespace TypingShip.Core.Tests;

static class TestData
{
    public static string RepoRoot { get; } = Find();
    public static string DataDir => Path.Combine(RepoRoot, "data");
    public static string SpritesDir => Path.Combine(RepoRoot, "game", "assets", "sprites");

    static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "TypingShip.sln")))
                return dir.FullName;
        throw new InvalidOperationException("Can't find the repo root (TypingShip.sln).");
    }

    static readonly Lazy<GameData> data = new(() => GameData.Load(DataSource.FromDirectory(DataDir)));
    static readonly Lazy<ArtLibrary> art = new(() => ArtLibrary.FromFolder(SpritesDir));

    /// <summary>The real data files.</summary>
    public static GameData Data => data.Value;
    public static ArtLibrary Art => art.Value;
    public static Tuning Tuning => Data.Tuning;

    /// <summary>A game on the real data, set up but not started.</summary>
    public static Game NewGame(ulong seed = 1, int wpm = 40, IGameEvents? events = null)
    {
        var g = new Game(Data, new Rng(seed), Art, events: events) { Cps = Speed.FromWpm(wpm) };
        g.TutOff();
        return g;
    }

    /// <summary>The real data with some files replaced (by name, e.g. "levels/level00.txt").</summary>
    public static GameData With(params (string Name, string Text)[] files) =>
        GameData.Load(DataSource.FromFunc(name =>
            files.FirstOrDefault(f => f.Name == name).Text ?? File.ReadAllText(Path.Combine(DataDir, name))));

    /// <summary>A game whose level 0 is the given script.</summary>
    public static Game WithLevel0(string script, ulong seed = 1, int wpm = 40, IGameEvents? events = null)
    {
        var g = new Game(With(("levels/level00.txt", script)), new Rng(seed), Art, events: events) { Cps = Speed.FromWpm(wpm) };
        g.TutOff();
        return g;
    }
}
