using Godot;

namespace TypingShip;

// Fonts. Everything players see uses bundled OFL fonts (assets/fonts, licences beside them): Orbitron for the HUD and
// the screens, JetBrains Mono for words and typed text. Game and GameBold read Arial from the system, and only the dev
// level editor uses them.
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

    public static LabelSettings Settings(Font font, int size, Color color, int outline = 0, Color? outlineColor = null) => new()
    {
        Font = font,
        FontSize = size,
        FontColor = color,
        OutlineSize = outline,
        OutlineColor = outlineColor ?? Colors.Black,
    };
}
