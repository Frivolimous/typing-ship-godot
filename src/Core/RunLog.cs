using System.Globalization;

namespace TypingShip.Core;

/// <summary>
/// A line per finished level or run, for playtesters to send in: what was played, how it went and on which build.
/// The game appends it to user://runs.csv (game/scripts/Core/RunLogFile.cs).
/// </summary>
public static class RunLog
{
    public const string Header = "utc,build,mode,level,difficulty,wpm,result,score,seconds,wave,health,ships,missed,accuracy";

    /// <summary>The line for a level or run that just ended. The score is before the score screen's bonuses.</summary>
    public static string Line(Game g, bool cleared, DateTime utc, string build)
    {
        var inv = CultureInfo.InvariantCulture;
        string mode = g.Endless == null ? "level" : g.Endless.Day != null ? "daily" : "endless";
        string level = g.Endless?.Day is { } day ? Daily.Name(day) : g.Endless != null ? "" : MapName(g.Level);
        return string.Join(",",
            utc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", inv), build, mode, level, Difficulty.Name(g.Cps),
            Speed.ToWpm(g.Cps).ToString("0", inv), cleared ? "cleared" : "lost", g.Score, ((int)g.Time).ToString(inv),
            g.Endless?.Wave.ToString(inv) ?? "", Math.Max(0, g.Player.Health), g.TotalShips, g.MissedShips,
            g.TotalShips > 0 ? g.HitRatio().ToString("0.00", inv) : "");
    }

    /// <summary>A level as the map names it: 1-9, and B1-B3 for the bosses.</summary>
    public static string MapName(int level) => (level + 1) % 4 == 0 ? $"B{(level + 1) / 4}" : (level + 1 - (level + 1) / 4).ToString(CultureInfo.InvariantCulture);
}
