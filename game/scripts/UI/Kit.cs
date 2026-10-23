using Godot;

namespace TypingShip;

/// <summary>
/// The look shared by every screen outside a level, matching the cockpit (and the player's ship): dark bronze chamfered
/// panels with a lit gold rim and red trim, Orbitron for titles and labels, and the difficulty colours.
/// </summary>
public static class Kit
{
    public static readonly Color Rim = Cockpit.Rim, Trim = Cockpit.Trim, Fill = new(0.035f, 0.02f, 0.012f, 0.92f),
        Ink = new(0.88f, 0.94f, 1), Good = new(0.45f, 1, 0.65f), Bad = new(1, 0.35f, 0.3f);

    /// <summary>The ship's bright gold, kept for the game's name and START GAME.</summary>
    public static readonly Color Gold = new(1, 0.8f, 0.3f);

    /// <summary>Locked or unavailable things (outlines, text), and the tint for art not earned yet.</summary>
    public static readonly Color Locked = new(0.55f, 0.48f, 0.4f), Unearned = new(0.36f, 0.31f, 0.26f);

    /// <summary>What dims the screen behind a box (add an alpha).</summary>
    public static readonly Color Shade = new(0.03f, 0.015f, 0.005f);

    /// <summary>Easy, Normal, Hard, Extreme, Insane.</summary>
    public static readonly Color[] DiffColours =
        { new(0.45f, 0.72f, 1), new(0.45f, 1, 0.6f), new(1, 0.9f, 0.35f), new(1, 0.62f, 0.25f), new(1, 0.35f, 0.3f) };

    /// <summary>Bronze, silver and gold (index 1-3; 0 is unused).</summary>
    public static readonly Color[] TierColours = { Colors.White, new(0.9f, 0.58f, 0.3f), new(0.82f, 0.88f, 0.98f), new(1, 0.82f, 0.3f) };

    static readonly Dictionary<(Font, int), Font> spaced = new();

    /// <summary>A font with extra space between letters, for titles.</summary>
    public static Font Spaced(Font font, int spacing)
    {
        if (!spaced.TryGetValue((font, spacing), out var f))
            spaced[(font, spacing)] = f = new FontVariation { BaseFont = font, SpacingGlyph = spacing };
        return f;
    }

    /// <summary>A dark brass edge for title-style text that shouldn't look like a warning (scores, a win).</summary>
    public static readonly Color BrassEdge = new(0.32f, 0.22f, 0.1f, 0.85f);

    /// <summary>Title text: spaced Orbitron with a thick edge (faint red by default, or <paramref name="edge"/>).</summary>
    public static LabelSettings Title(int size, Color? colour = null, Color? edge = null) =>
        Fonts.Settings(Spaced(Fonts.HudBold, Math.Max(2, size / 7)), size, colour ?? Colors.White, Math.Max(4, size / 5),
            edge ?? new Color(Trim, 0.3f));

    public static LabelSettings Caption(int size = 8, Color? colour = null) => Fonts.Settings(Fonts.Hud, size, colour ?? new Color(Rim, 0.85f));

    public static LabelSettings Body(int size, Color? colour = null) =>
        Fonts.Settings(Fonts.HudBold, size, colour ?? Ink, 3, new Color(0, 0, 0, 0.6f));

    /// <summary>A label added to a control; with a width it can be aligned (and wraps if <paramref name="wrap"/>).</summary>
    public static Label Label(Control parent, string text, LabelSettings settings, float x, float y, float width = 0,
        HorizontalAlignment align = HorizontalAlignment.Left, bool wrap = false)
    {
        var l = new Label { Position = new Vector2(x, y), LabelSettings = settings, MouseFilter = Control.MouseFilterEnum.Ignore };
        if (width > 0)
        {
            if (wrap) l.AutowrapMode = TextServer.AutowrapMode.WordSmart; // before the text, or it widens the label
            l.Size = new Vector2(width, 0);
            l.HorizontalAlignment = align;
        }
        l.Text = text;
        parent.AddChild(l);
        return l;
    }

    /// <summary>How tall some text will be once wrapped to a width (lines plus the labels' line spacing).</summary>
    public static float TextHeight(string text, LabelSettings settings, float width)
    {
        var font = settings.Font;
        float line = font.GetHeight(settings.FontSize);
        int lines = Mathf.Max(1, Mathf.RoundToInt(font.GetMultilineStringSize(text, HorizontalAlignment.Left, width, settings.FontSize, -1,
            TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Mandatory).Y / line));
        return lines * (line + settings.LineSpacing);
    }

    /// <summary>A panel's outline: a rectangle with its top-left and bottom-right corners cut off.</summary>
    public static Vector2[] Chamfer(Rect2 r, float cut = 10) => new[]
    {
        new Vector2(r.Position.X + cut, r.Position.Y), new Vector2(r.End.X, r.Position.Y), new Vector2(r.End.X, r.End.Y - cut),
        new Vector2(r.End.X - cut, r.End.Y), new Vector2(r.Position.X, r.End.Y), new Vector2(r.Position.X, r.Position.Y + cut),
    };

    /// <summary>
    /// Draws a panel: a shadow, the dark fill, a lit border, a faint sheen along the top, and optionally a stripe of
    /// colour down the left edge.
    /// </summary>
    public static void Panel(CanvasItem ci, Rect2 r, Color? accent = null, float cut = 10, float glow = 0.5f)
    {
        var pts = Chamfer(r, cut);
        var closed = pts.Append(pts[0]).ToArray();
        ci.DrawPolyline(closed, new Color(0, 0, 0, 0.55f), 5);
        ci.DrawColoredPolygon(pts, Fill);
        ci.DrawPolyline(closed, new Color(Rim, glow), 1.5f, true);
        ci.DrawLine(pts[0] + new Vector2(2, 3), pts[1] + new Vector2(-3, 3), new Color(Rim, 0.12f), 2);
        if (accent is { } a)
            ci.DrawColoredPolygon(new[]
            {
                new Vector2(r.Position.X, r.Position.Y + cut), new Vector2(r.Position.X + 4, r.Position.Y + cut - 4),
                new Vector2(r.Position.X + 4, r.End.Y), new Vector2(r.Position.X, r.End.Y),
            }, a);
    }

    /// <summary>A thin rule with a glow, e.g. under a title.</summary>
    public static void Rule(CanvasItem ci, Vector2 from, Vector2 to, Color? colour = null, float alpha = 0.8f)
    {
        var c = colour ?? Rim;
        ci.DrawLine(from, to, new Color(c, alpha * 0.25f), 4);
        ci.DrawLine(from, to, new Color(c, alpha), 1.2f);
    }

    /// <summary>
    /// A slanted chip behind a button's text: normal, and lit under the mouse. A bold one has a thick rim in the full colour
    /// and no glow.
    /// </summary>
    public static StyleBoxFlat ButtonBox(bool lit, Color? colour = null, bool bold = false)
    {
        var c = colour ?? Rim;
        int w = bold ? 2 : 1;
        return new StyleBoxFlat
        {
            BgColor = lit ? new Color(c.Lerp(Fill, 0.7f), 0.95f) : Fill,
            BorderColor = bold ? c : new Color(c, lit ? 1 : 0.55f),
            BorderWidthLeft = w, BorderWidthTop = w, BorderWidthRight = w, BorderWidthBottom = w,
            Skew = new Vector2(0.25f, 0),
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 6, ContentMarginBottom = 5,
            AntiAliasing = true,
            ShadowColor = new Color(c, lit && !bold ? 0.35f : 0),
            ShadowSize = lit && !bold ? 6 : 0,
        };
    }
}
