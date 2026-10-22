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

    /// <summary>user:// used to be Godot's default folder (on Windows %APPDATA%\Godot\app_userdata\Millennium Typer);
    /// it's the game's own now (%APPDATA%\Millennium Typer, which Steam Cloud syncs). Brings the save, settings and
    /// profiles over once, never over newer files.</summary>
    public static void MoveFromGodotFolder()
    {
        var old = System.IO.Path.Combine(OS.GetDataDir(), "Godot", "app_userdata", "Millennium Typer");
        var now = ProjectSettings.GlobalizePath("user://");
        if (!Directory.Exists(old) || System.IO.Path.GetFullPath(old) == System.IO.Path.GetFullPath(now)) return;
        try
        {
            foreach (var file in Directory.GetFiles(old, "*.json"))
            {
                var to = System.IO.Path.Combine(now, System.IO.Path.GetFileName(file));
                if (!File.Exists(to)) File.Copy(file, to);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Can't bring the save over from {old}: {e.Message}");
        }
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
