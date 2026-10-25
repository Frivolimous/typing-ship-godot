using Godot;
using FileAccess = Godot.FileAccess;

namespace TypingShip;

// Playtesters' run log: a line per finished level or run (Core RunLog) appended to user://runs.csv, next to the
// save, so they can send it in. Not written when the save isn't (bot runs).
public static class RunLogFile
{
    public const string Path = "user://runs.csv";

    public static void Append(bool cleared)
    {
        if (SaveFile.Path == null) return;
        bool fresh = !FileAccess.FileExists(Path);
        using var f = FileAccess.Open(Path, fresh ? FileAccess.ModeFlags.Write : FileAccess.ModeFlags.ReadWrite);
        if (f == null)
        {
            GD.PushWarning($"Can't write {Path}: {FileAccess.GetOpenError()}");
            return;
        }
        f.SeekEnd();
        if (fresh) f.StoreLine(RunLog.Header);
        f.StoreLine(RunLog.Line(G.Model, cleared, DateTime.UtcNow, BuildInfo.Full));
    }
}
