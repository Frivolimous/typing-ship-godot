using Godot;

namespace TypingShip;

// Global access to the game's singletons, in the role the Flash version's Facade played.
// Gameplay runs in a fixed 24 Hz tick (the original frame rate); physics interpolation smooths the display.
public static class G
{
    public const float ViewWidth = 600, ViewHeight = 550; // game-view space (GameUI.VIEW_*)
    public const float UiWidth = 550, UiHeight = 500;
    public const float StageSize = 550;
    public const int TickRate = 24;

    public const int Easy = 25, Normal = 40, Hard = 65, Extreme = 75, Insane = 90;

    public static GameModel Model = null!;
    public static GameControl Ctrl = null!;
    public static ActionControl Action = null!;
    public static SoundControl Sound = null!;
    public static GameUI UI = null!;
    public static WordList Words = null!;
    public static Starfield Starfield = null!;

    static readonly RandomNumberGenerator rng = new();

    /// <summary>Math.random(): [0, 1).</summary>
    public static float Rand()
    {
        float r = rng.Randf();
        return r >= 1f ? 0.99999994f : r;
    }

    public static void Seed(ulong seed) => rng.Seed = seed;
}
