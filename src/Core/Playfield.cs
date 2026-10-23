namespace TypingShip.Core;

/// <summary>Sizes of the play area, in the original's units.</summary>
public static class Playfield
{
    /// <summary>The game view, where ships fly (GameUI.VIEW_*). It's 25 px bigger than the stage on each side.</summary>
    public const float ViewWidth = 600, ViewHeight = 550;
    /// <summary>Level scripts place things on a 12 x 12 grid over the game view.</summary>
    public const float GridX = ViewWidth / 12, GridY = ViewHeight / 12;
    public const int TickRate = 24;
    /// <summary>Where the player's ship sits: 12 px higher than the original's (ViewHeight - 100), so its engine flames clear the cockpit.</summary>
    public const float PlayerX = ViewWidth / 2, PlayerY = ViewHeight - 112;
    /// <summary>The part of the game view the player sees: the 550 px stage, which sits at 25,25 in it.</summary>
    public const float VisibleLeft = 25, VisibleTop = 25, VisibleRight = 575, VisibleBottom = 575;
}

/// <summary>Weapon and kill types (ActionControl's constants).</summary>
public static class Weapons
{
    public const int Laser = 0, Missile = 1, AutoMissile = 2, Emp = 3, Suicide = 4, Instant = 5;
}

/// <summary>
/// The five difficulties. The rules use their speed in characters per second (<see cref="Cps"/>); players see it
/// in words per minute (<see cref="Wpm"/>).
/// </summary>
public static class Difficulty
{
    /// <summary>What players see: 25 Easy, 40 Normal, 65 Hard, 90 Extreme, 120 Insane (the original had 75 and 90).</summary>
    public static readonly int[] Wpm = { 25, 40, 65, 90, 120 };
    /// <summary>The same in characters per second, which is what the rules use.</summary>
    public static readonly double[] Cps = Wpm.Select(w => Speed.FromWpm(w)).ToArray();
    public static readonly string[] Names = { "Easy", "Normal", "Hard", "Extreme", "Insane" };

    public static double Easy => Cps[0];
    public static double Normal => Cps[1];
    public static double Hard => Cps[2];
    public static double Extreme => Cps[3];
    public static double Insane => Cps[4];

    /// <summary>1 (easy) to 5 (insane). Dev runs can set any speed; those count as the nearest difficulty below.</summary>
    public static int Of(double cps) => Math.Max(1, Array.FindLastIndex(Cps, c => c <= cps + 1e-9) + 1);

    /// <summary>"Hard", or "47 WPM" for a speed that isn't one of the five.</summary>
    public static string Name(double cps)
    {
        int i = Array.FindIndex(Cps, c => Math.Abs(c - cps) < 1e-9);
        return i >= 0 ? Names[i] : $"{Speed.ToWpm(cps):0.#} WPM";
    }
}

/// <summary>
/// Typing speed. The rules and tuning work in characters per second (CPS); everything players see is in words per
/// minute (WPM), counting a word as 5 characters: CPS = WPM x 5 / 60.
/// </summary>
public static class Speed
{
    public static double FromWpm(double wpm) => wpm * 5 / 60;
    public static double ToWpm(double cps) => cps * 12;

    // CPS values such as 25 WPM (2.0833... CPS) aren't exact in binary, so a time worked out from one that was a
    // whole number of ticks in the original's whole-number WPM arithmetic can come out a hair either side of it.
    // These allow for that, so whole-tick times stay what they were.

    /// <summary>A time in ticks, rounded down to a whole tick.</summary>
    public static int FloorTicks(double ticks) => (int)Math.Floor(ticks + 1e-6);

    /// <summary>A time in ticks, rounded up to a whole tick.</summary>
    public static int CeilingTicks(double ticks) => (int)Math.Ceiling(ticks - 1e-6);

    /// <summary>
    /// A speed that grows with typing speed, perCps x cps, in single precision. It's worked out per WPM, as the
    /// original did, so it rounds exactly as before: perCps x cps directly can differ in the last bit.
    /// </summary>
    public static float Scaled(double perCps, double cps) => (float)(perCps / 12) * (float)ToWpm(cps);
}
