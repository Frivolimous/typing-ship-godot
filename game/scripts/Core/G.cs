using TypingShip.Core;

namespace TypingShip;

// Global access to the game's parts, in the role the Flash version's Facade played. The rules are G.Model, a
// plain-C# TypingShip.Core.Game; everything here in the Godot project only draws it, plays its sounds and feeds it
// input. Gameplay runs in a fixed 24 Hz tick (the original frame rate); physics interpolation smooths the display.
public static class G
{
    public const float ViewWidth = Playfield.ViewWidth, ViewHeight = Playfield.ViewHeight; // game-view space
    public const float UiWidth = 550, UiHeight = 500;
    /// <summary>
    /// The stage: the original's 550 px square, which the rules and the layout use. The window shows the middle
    /// <see cref="ShownSize"/> of it (project.godot's viewport), so a stage pixel is a whole number of screen pixels
    /// at 1080p (2) and 4K (4); the 5 px lost at each edge are off-screen.
    /// </summary>
    public const float StageSize = 550;
    public const float ShownSize = 540;
    public const int TickRate = Playfield.TickRate;

    /// <summary>The rules: the level, ships, score, typing.</summary>
    public static Game Model = null!;
    public static GameControl Ctrl = null!;
    public static Presenter World = null!;
    public static SoundControl Sound = null!;
    public static GameUI UI = null!;
    public static Starfield Starfield = null!;
    public static Shell Shell = null!;
    /// <summary>Screen shake.</summary>
    public static ShakeCamera Camera = null!;
    /// <summary>Flashes over the playfield (hits, the boss's last blast).</summary>
    public static ScreenFlash Flash = null!;

    /// <summary>The store (Steam) for achievements and leaderboards; NoPlatform without one.</summary>
    public static IPlatform Platform = new NoPlatform();

    /// <summary>False in a release export: the dev options and dev keys are off (docs/commands.md).</summary>
    public static readonly bool Dev = Godot.OS.IsDebugBuild();

    /// <summary>Random numbers for the presentation (stars, particles, flicker).</summary>
    public static Rng Rng = new();

    /// <summary>Math.random(): [0, 1).</summary>
    public static float Rand() => Rng.Rand();

    public static Godot.Vector2 V(Vec2 v) => new(v.X, v.Y);
}
