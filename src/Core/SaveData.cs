using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypingShip.Core;

/// <summary>
/// What the original kept in its "SAVE" SharedObject. The game stores it as JSON (user://save.json) through
/// <see cref="Store"/>; with no Store set (bot runs, tests) it lives only in memory.
/// </summary>
public sealed class SaveData
{
    public const int CurrentVersion = 10; // GameModel.VERSION; an older or newer save is wiped, as in the original

    public int Version { get; set; } = CurrentVersion;
    /// <summary>Tier reached (0-3) per achievement: soldier, conqueror, riddler, defender, perfection, explorer.</summary>
    public int[] Achieve { get; set; } = new int[6];
    /// <summary>Achievement counters (only [0], soldier kills, is used).</summary>
    public int[] AchieveC { get; set; } = new int[6];
    /// <summary>Per level: best score, best difficulty cleared (0 = never, 1-5), health medal (0-3), accuracy medal (0-3).</summary>
    public int[][] LevelScore { get; set; } = NewLevelScores();
    /// <summary>Endless mode, per difficulty (0 easy to 4 insane): best score, and the longest run in seconds.</summary>
    public int[] EndlessBest { get; set; } = new int[5];
    public int[] EndlessSeconds { get; set; } = new int[5];
    /// <summary>The typing test's last result in WPM, 0 if never taken. (New: the original threw it away.)</summary>
    public int TestWpm { get; set; }

    /// <summary>Writes the save somewhere (the game sets this); null keeps it in memory.</summary>
    [JsonIgnore] public Action<string>? Store;

    static int[][] NewLevelScores() => Enumerable.Range(0, 12).Select(_ => new int[4]).ToArray();

    [JsonIgnore] public int Highscore => LevelScore.Sum(l => l[0]);

    public bool Unlocked(int level) => level == 0 || LevelScore[level - 1][1] > 0;

    /// <summary>Reads a save; an unreadable one, or one from another version, gives a fresh save.</summary>
    public static SaveData FromJson(string? json, Action<string>? warn = null)
    {
        if (json == null) return new SaveData();
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data is { Version: CurrentVersion } && data.Achieve.Length == 6 && data.AchieveC.Length == 6 &&
                data.LevelScore.Length == 12 && data.LevelScore.All(l => l.Length == 4) &&
                data.EndlessBest is { Length: 5 } && data.EndlessSeconds is { Length: 5 })
                return data;
        }
        catch (JsonException e)
        {
            warn?.Invoke($"Save file unreadable, starting fresh: {e.Message}");
        }
        return new SaveData();
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public void Save() => Store?.Invoke(ToJson());

    /// <summary>Keeps an endless run's score and time if they're bests for the difficulty. True for a new best score.</summary>
    public bool RecordEndless(int difficulty, int score, int seconds)
    {
        bool best = score > EndlessBest[difficulty];
        EndlessBest[difficulty] = Math.Max(EndlessBest[difficulty], score);
        EndlessSeconds[difficulty] = Math.Max(EndlessSeconds[difficulty], seconds);
        Save();
        return best;
    }

    /// <summary>Ctrl+Page Down in the original.</summary>
    public void Clear()
    {
        Achieve = new int[6];
        AchieveC = new int[6];
        LevelScore = NewLevelScores();
        TestWpm = 0;
        Save();
    }

    /// <summary>Ctrl+Page Up in the original: every level cleared on Easy with 100 points.</summary>
    public void UnlockAll()
    {
        LevelScore = Enumerable.Range(0, 12).Select(_ => new[] { 100, 1, 0, 0 }).ToArray();
        Save();
    }

    /// <summary>How many levels have a gold medal: slot 2 = health, 3 = accuracy.</summary>
    public int GoldMedals(int slot) => LevelScore.Count(l => l[slot] == 3);
}

/// <summary>The six achievements, each with three tiers (bronze, silver, gold).</summary>
public static class Achievement
{
    public const int Soldier = 0, Conqueror = 1, Riddler = 2, Defender = 3, Perfectionist = 4, Explorer = 5;
}

/// <summary>The score screen's bonuses and medals (ScoreUI's arithmetic).</summary>
public static class Scoring
{
    public static float DifficultyBonus(double cps) => 1 + (float)(cps * 0.12); // 1 + wpm / 100

    /// <summary>x2 untouched; otherwise up to x1.2 for the health left.</summary>
    public static float HealthBonus(bool injured, int health, int maxHealth) =>
        injured ? 1 + health / (float)maxHealth * 0.2f : 2;

    /// <summary>3 (gold) untouched, 2 at full health after a repair, 1 at half health or more, else 0.</summary>
    public static int HealthMedal(bool injured, int health, int maxHealth) =>
        !injured ? 3 : health >= maxHealth ? 2 : health >= maxHealth / 2f ? 1 : 0;

    /// <summary>x2 for every ship hit, else 1 + half the hit ratio.</summary>
    public static float AccuracyBonus(float hitRatio) => hitRatio < 1 ? 1 + hitRatio / 2 : 2;

    public static int AccuracyMedal(float hitRatio) => hitRatio == 1 ? 3 : hitRatio > 0.75f ? 2 : hitRatio > 0.5f ? 1 : 0;

    /// <summary>One bonus row: the score after it, and the step it counts up by on screen (about 72 steps).</summary>
    public static (int Score, int Step) ApplyBonus(int score, float mult) =>
        ((int)(score * mult), Math.Max(1, (int)(score * (mult - 1) / 72)));
}
