using Godot;

namespace TypingShip;

// Placeholder fonts read from the system. The originals (Consolas, Arial, ...) can't ship in a commercial build,
// so these get replaced by bundled OFL fonts before release.
public static class Fonts
{
    public static readonly Font Word = new SystemFont { FontNames = new[] { "Consolas", "Courier New", "monospace" } };
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
