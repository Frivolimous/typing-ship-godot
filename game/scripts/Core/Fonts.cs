using Godot;

namespace TypingShip;

// Fonts. The in-game HUD and the ships' words use bundled OFL fonts (assets/fonts, licences beside them): Orbitron
// for the HUD, JetBrains Mono for words. The rest are placeholders read from the system; the originals (Arial, ...)
// can't ship in a commercial build, so they get replaced by bundled fonts before release.
public static class Fonts
{
    static Font Bundled(string file) => GD.Load<FontFile>($"res://assets/fonts/{file}.woff2");

    /// <summary>The words on ships, missiles, the scanner and the hull, and the typed text.</summary>
    public static readonly Font Word = Bundled("JetBrainsMono-Bold");
    /// <summary>The in-game HUD: captions, numbers, score pop-ups.</summary>
    public static readonly Font Hud = Bundled("Orbitron-Medium"), HudBold = Bundled("Orbitron-Bold"),
        HudBlack = Bundled("Orbitron-Black");
    public static readonly Font Game = new SystemFont { FontNames = new[] { "Arial", "sans-serif" } };
    public static readonly Font GameBold = new SystemFont { FontNames = new[] { "Arial", "sans-serif" }, FontWeight = 700 };
    // The menus' SPACE_FONT ("Quixley LET") and TITLE_FONT ("Rocket Script"), used if installed.
    public static readonly Font Space = new SystemFont { FontNames = new[] { "Quixley LET", "Arial", "sans-serif" }, FontWeight = 700 };
    public static readonly Font Title = new SystemFont { FontNames = new[] { "Rocket Script", "Arial", "sans-serif" }, FontWeight = 700 };

    public static LabelSettings Settings(Font font, int size, Color color, int outline = 0, Color? outlineColor = null) => new()
    {
        Font = font,
        FontSize = size,
        FontColor = color,
        OutlineSize = outline,
        OutlineColor = outlineColor ?? Colors.Black,
    };
}

// Facade's palette.
public static class Palette
{
    public static readonly Color Gold = new(0xffcc33ff), GoldOver = new(0xffee55ff), Blue = new(0x1b4e92ff),
        BlueOver = new(0x55ff99ff), MenuBlue = new(0x000022ff), BorderBlue = new(0xccccffff), Red = new(0xff5555ff),
        Green = new(0x55ff55ff), Bronze = new(0x774422ff), Silver = new(0xccccddff);
}
