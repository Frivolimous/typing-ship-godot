using Godot;
using FileAccess = Godot.FileAccess;

namespace TypingShip;

// Where the save (TypingShip.Core.SaveData) lives: JSON in user://, written whenever it changes.
public static class SaveFile
{
    /// <summary>File name in user://, or null to keep everything in memory (bot runs).</summary>
    public static string? Path = "user://save.json";

    public static SaveData Load()
    {
        string? json = Path != null && FileAccess.FileExists(Path) ? FileAccess.GetFileAsString(Path) : null;
        var save = SaveData.FromJson(json, GD.PushWarning);
        if (Path is { } path) save.Store = text => Write(path, text);
        return save;
    }

    static void Write(string path, string text)
    {
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning($"Can't write {path}: {FileAccess.GetOpenError()}");
            return;
        }
        f.StoreString(text);
    }
}
