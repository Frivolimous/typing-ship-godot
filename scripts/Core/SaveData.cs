using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using FileAccess = Godot.FileAccess;

namespace TypingShip;

// What the original kept in its "SAVE" SharedObject, as JSON in user://. Saved whenever it changes.
public class SaveData
{
    public const int CurrentVersion = 10; // GameModel.VERSION; an older or newer save is wiped, as in the original

    public int Version { get; set; } = CurrentVersion;
    /// <summary>Tier reached (0-3) per achievement: soldier, conqueror, riddler, defender, perfection, explorer.</summary>
    public int[] Achieve { get; set; } = new int[6];
    /// <summary>Achievement counters (only [0], soldier kills, is used).</summary>
    public int[] AchieveC { get; set; } = new int[6];
    /// <summary>Per level: best score, best difficulty cleared (0 = never, 1-5), health medal (0-3), accuracy medal (0-3).</summary>
    public int[][] LevelScore { get; set; } = NewLevelScores();
    /// <summary>The typing test's last result in WPM, 0 if never taken. (New: the original threw it away.)</summary>
    public int TestWpm { get; set; }

    static int[][] NewLevelScores() => Enumerable.Range(0, 12).Select(_ => new int[4]).ToArray();

    /// <summary>File name in user://, or null to keep everything in memory (bot runs).</summary>
    public static string? Path = "user://save.json";

    [JsonIgnore] public int Highscore => LevelScore.Sum(l => l[0]);

    public bool Unlocked(int level) => level == 0 || LevelScore[level - 1][1] > 0;

    public static SaveData Load()
    {
        if (Path == null || !FileAccess.FileExists(Path)) return new SaveData();
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(FileAccess.GetFileAsString(Path));
            if (data is { Version: CurrentVersion } && data.Achieve.Length == 6 && data.AchieveC.Length == 6 &&
                data.LevelScore.Length == 12 && data.LevelScore.All(l => l.Length == 4))
                return data;
        }
        catch (JsonException e)
        {
            GD.PushWarning($"Save file unreadable, starting fresh: {e.Message}");
        }
        return new SaveData();
    }

    public void Save()
    {
        if (Path == null) return;
        using var f = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning($"Can't write {Path}: {FileAccess.GetOpenError()}");
            return;
        }
        f.StoreString(JsonSerializer.Serialize(this));
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
}
