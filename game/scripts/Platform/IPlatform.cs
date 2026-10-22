using System.Globalization;

namespace TypingShip;

// What the game asks of the store it runs on: achievements and leaderboards. G.Platform is SteamPlatform when
// Steam is running (Platform/SteamPlatform.cs), otherwise NoPlatform, so the game plays the same without it and
// nothing else touches Steamworks. Saves reach Steam Cloud without code (Auto-Cloud; see steam/README.md).
public interface IPlatform
{
    /// <summary>"Steam", or null when there's no store (Steam not running, a dev run).</summary>
    string? Name { get; }

    /// <summary>Unlocks the store's achievement for a tier (1-3) of one of the six (SaveData's Achievement).</summary>
    void Unlock(int achievement, int tier);

    /// <summary>Unlocks every tier the save has reached: awards earned while Steam was closed, or in an older
    /// save.</summary>
    void Sync(SaveData save);

    /// <summary>Sends a score to a leaderboard (named in <see cref="Boards"/>), keeping the player's best. Done gets
    /// where the player's best stands, or null if there's no store or it failed.</summary>
    void Submit(string board, int score, int[] details, Action<Rank?> done);
}

/// <summary>A place on a leaderboard: 1 is the top, out of everyone on it.</summary>
public readonly record struct Rank(int Position, int Of);

/// <summary>No store: nothing is sent and every leaderboard answer is null.</summary>
public sealed class NoPlatform : IPlatform
{
    public string? Name => null;
    public void Unlock(int achievement, int tier) { }
    public void Sync(SaveData save) { }
    public void Submit(string board, int score, int[] details, Action<Rank?> done) => done(null);
}

/// <summary>The leaderboards. The store creates each the first time a score is sent to it (best score first).</summary>
public static class Boards
{
    /// <summary>Endless mode on one difficulty (0 Easy to 4 Insane): the run's score.</summary>
    public static string Endless(int difficulty) => "endless_" + Difficulty.Names[difficulty].ToLowerInvariant();

    /// <summary>Endless mode on every difficulty together. Scores already count for more on a harder difficulty
    /// (EndlessRun.ScoreFactor), so they compare fairly.</summary>
    public const string EndlessAll = "endless_all";

    // The daily challenge (not built yet) gets a board per day, "daily_2026-10-22", sent the same way.
}

/// <summary>Sends an ended endless run to its difficulty's board and the global one.</summary>
public static class Leaderboards
{
    /// <summary>The run's score on both boards; done gets the line to show for the ranks, or null for none.</summary>
    public static void SubmitEndless(Game m, Action<string?> done)
    {
        int diff = m.Diff - 1;
        if (G.Platform.Name is not { } store || m.Endless == null || diff is < 0 or > 4)
        {
            done(null);
            return;
        }
        // Details shown with the score: the wave reached, the time in seconds and the difficulty.
        int[] details = { m.Endless.Wave, (int)m.Time, diff };
        Rank? own = null, all = null;
        int answers = 0;
        void Answer()
        {
            if (++answers < 2) return;
            done(own == null && all == null ? $"{store.ToUpperInvariant()}: LEADERBOARD UNAVAILABLE"
                : $"{store.ToUpperInvariant()} RANK  ·  {Difficulty.Names[diff].ToUpperInvariant()} {Show(own)}  ·  ALL {Show(all)}");
        }
        G.Platform.Submit(Boards.Endless(diff), m.Score, details, r => { own = r; Answer(); });
        G.Platform.Submit(Boards.EndlessAll, m.Score, details, r => { all = r; Answer(); });
    }

    static string Show(Rank? r) => r is { } k ? string.Format(CultureInfo.InvariantCulture, "#{0:N0} OF {1:N0}", k.Position, k.Of) : "-";
}
