namespace TypingShip.Core;

/// <summary>Sizes of the play area, in the original's units.</summary>
public static class Playfield
{
    /// <summary>The game view, where ships fly (GameUI.VIEW_*). It's 25 px bigger than the stage on each side.</summary>
    public const float ViewWidth = 600, ViewHeight = 550;
    /// <summary>Level scripts place things on a 12 x 12 grid over the game view.</summary>
    public const float GridX = ViewWidth / 12, GridY = ViewHeight / 12;
    public const int TickRate = 24;
    public const float PlayerX = ViewWidth / 2, PlayerY = ViewHeight - 100;
}

/// <summary>Weapon and kill types (ActionControl's constants).</summary>
public static class Weapons
{
    public const int Laser = 0, Missile = 1, AutoMissile = 2, Emp = 3, Suicide = 4, Instant = 5;
}

/// <summary>The five difficulties, as target words per minute.</summary>
public static class Difficulty
{
    public const int Easy = 25, Normal = 40, Hard = 65, Extreme = 75, Insane = 90;
    public static readonly int[] Wpm = { Easy, Normal, Hard, Extreme, Insane };

    /// <summary>1 (easy) to 5 (insane). Dev runs can set any WPM; those count as the nearest difficulty below.</summary>
    public static int Of(int wpm) => Math.Max(1, Array.FindLastIndex(Wpm, w => w <= wpm) + 1);
}
