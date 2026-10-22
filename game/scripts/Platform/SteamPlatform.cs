using System.Runtime.InteropServices;
using Godot;
using Steamworks;

namespace TypingShip;

// Steam, through Steamworks.NET: the achievements and the leaderboards. Start() gives NoPlatform instead when Steam
// can't start (not running, no steam_api library, the game not launched from Steam), and the game plays on without
// it. The node runs Steam's callbacks every frame and shuts Steam down when the game quits.
public partial class SteamPlatform : Node, IPlatform
{
    /// <summary>Millennium Typer's Steam App ID (the Steamworks partner site). Steam passes it to the game it
    /// launches; a dev run from a checkout (--steam) needs it here.</summary>
    public const uint AppId = 5450910;

    /// <summary>The achievements' API names on the partner site: one per tier, e.g. SOLDIER_BRONZE.</summary>
    public static string ApiName(int achievement, int tier) =>
        $"{ApiNames[achievement]}_{Strings.Tiers[tier]}";

    static readonly string[] ApiNames = { "SOLDIER", "CONQUEROR", "RIDDLER", "DEFENDER", "PERFECTIONIST", "EXPLORER" };

    string? IPlatform.Name => "Steam"; // explicit: Node has a Name of its own

    /// <summary>Starts Steam if it can. checkout: a dev run, which Steam didn't launch, so it's told the App ID.</summary>
    public static IPlatform Start(Node owner, bool checkout)
    {
        try
        {
            FindSteamLibrary();
            if (checkout)
            {
                System.Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());
                System.Environment.SetEnvironmentVariable("SteamGameId", AppId.ToString());
            }
            if (SteamAPI.InitEx(out string error) != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                GD.Print($"Steam: off ({error})");
                return new NoPlatform();
            }
        }
        catch (Exception e) // no steam_api library, or the wrong one: play without Steam
        {
            GD.Print($"Steam: off ({e.GetType().Name}: {e.Message})");
            return new NoPlatform();
        }
        GD.Print($"Steam: on, app {SteamUtils.GetAppID()}");
        var steam = new SteamPlatform { Name = "Steam" };
        owner.AddChild(steam);
        return steam;
    }

    /// <summary>Godot loads the game's assemblies in a context of its own, which doesn't look for native libraries
    /// next to them, so steam_api (copied there by TypingShip.csproj) is loaded from that folder by hand: in a
    /// checkout .godot/mono/temp/bin/Debug, in an export data_TypingShip_*.</summary>
    static void FindSteamLibrary()
    {
        var folder = AppContext.BaseDirectory; // the assemblies' folder (Assembly.Location is empty under Godot)
        if (string.IsNullOrEmpty(folder)) return;
        NativeLibrary.SetDllImportResolver(typeof(SteamAPI).Assembly, (name, assembly, paths) =>
        {
            foreach (var file in new[] { name + ".dll", "lib" + name + ".so", name })
                if (NativeLibrary.TryLoad(System.IO.Path.Combine(folder, file), out var handle)) return handle;
            return IntPtr.Zero; // the usual search
        });
    }

    Callback<UserStatsReceived_t>? statsReceived;
    /// <summary>Steam has sent the player's achievements, so they can be set.</summary>
    bool statsReady;
    /// <summary>Achievements to unlock once Steam has the player's stats.</summary>
    readonly HashSet<string> pending = new();
    readonly HashSet<string> missing = new();
    readonly Dictionary<string, SteamLeaderboard_t> boards = new();
    /// <summary>Calls waiting for Steam's answer (kept here so they aren't collected first).</summary>
    readonly List<IDisposable> calls = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // answers still arrive while the game is paused
        statsReceived = Callback<UserStatsReceived_t>.Create(OnStatsReceived);
        SteamUserStats.RequestCurrentStats();
    }

    public override void _Process(double delta) => SteamAPI.RunCallbacks();

    public override void _ExitTree()
    {
        statsReceived?.Dispose();
        SteamAPI.Shutdown();
    }

    void OnStatsReceived(UserStatsReceived_t r)
    {
        if (r.m_nGameID != SteamUtils.GetAppID().m_AppId) return;
        if (r.m_eResult != EResult.k_EResultOK)
        {
            GD.PushWarning($"Steam: no achievements ({r.m_eResult})");
            return;
        }
        statsReady = true;
        UnlockPending();
    }

    public void Unlock(int achievement, int tier)
    {
        pending.Add(ApiName(achievement, tier));
        UnlockPending();
    }

    public void Sync(SaveData save)
    {
        for (int a = 0; a < save.Achieve.Length; a++)
            for (int tier = 1; tier <= save.Achieve[a]; tier++)
                pending.Add(ApiName(a, tier));
        UnlockPending();
    }

    void UnlockPending()
    {
        if (!statsReady || pending.Count == 0) return;
        bool changed = false;
        foreach (var name in pending)
        {
            if (!SteamUserStats.GetAchievement(name, out bool done))
            {
                if (missing.Add(name)) GD.PushWarning($"Steam: no achievement {name} on the partner site");
            }
            else if (!done) changed |= SteamUserStats.SetAchievement(name);
        }
        pending.Clear();
        if (changed) SteamUserStats.StoreStats();
    }

    public void Submit(string board, int score, int[] details, Action<Rank?> done) => Find(board, handle =>
    {
        if (handle is not { } h)
        {
            done(null);
            return;
        }
        var call = SteamUserStats.UploadLeaderboardScore(h, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
            score, details, details.Length);
        Call<LeaderboardScoreUploaded_t>(call, (r, failed) =>
        {
            // With the best kept, the rank is the player's best score's, even when this one was lower.
            if (failed || r.m_bSuccess == 0 || r.m_nGlobalRankNew <= 0) done(null);
            else done(new Rank(r.m_nGlobalRankNew, Math.Max(r.m_nGlobalRankNew, SteamUserStats.GetLeaderboardEntryCount(h))));
        });
    });

    /// <summary>A leaderboard's handle, made on Steam the first time (the best score first, shown as a number).</summary>
    void Find(string board, Action<SteamLeaderboard_t?> found)
    {
        if (boards.TryGetValue(board, out var known))
        {
            found(known);
            return;
        }
        var call = SteamUserStats.FindOrCreateLeaderboard(board, ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        Call<LeaderboardFindResult_t>(call, (r, failed) =>
        {
            if (failed || r.m_bLeaderboardFound == 0)
            {
                GD.PushWarning($"Steam: can't open the leaderboard {board}");
                found(null);
                return;
            }
            boards[board] = r.m_hSteamLeaderboard;
            found(r.m_hSteamLeaderboard);
        });
    }

    /// <summary>Waits for a Steam call's answer; failed when the call couldn't be made or reach Steam.</summary>
    void Call<T>(SteamAPICall_t call, Action<T, bool> answer) where T : struct
    {
        if (call == SteamAPICall_t.Invalid)
        {
            answer(default, true);
            return;
        }
        CallResult<T>? result = null;
        result = CallResult<T>.Create((r, ioFailure) =>
        {
            calls.Remove(result!);
            Callable.From(result!.Dispose).CallDeferred(); // not from inside its own callback
            answer(r, ioFailure);
        });
        result.Set(call);
        calls.Add(result);
    }
}
