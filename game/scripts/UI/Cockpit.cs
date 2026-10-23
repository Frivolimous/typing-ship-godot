using Godot;

namespace TypingShip;

/// <summary>
/// The cockpit along the bottom of the playfield, drawn in code in the shape of the original's frame: a gunmetal hull
/// with a lit rim, panel seams and rivets, indicator lights, and the inset screens the HUD sits in.
/// </summary>
public partial class Cockpit : Node2D
{
    /// <summary>The top edge, left to right, in stage space (the original frame's outline).</summary>
    static readonly Vector2[] Edge =
    {
        new(0, 425), new(150, 447), new(230, 492), new(320, 492), new(400, 448), new(550, 424),
    };

    /// <summary>The inset screens: typed text, hull, and the pause button. They end above 545, the bottom of what the
    /// window shows (G.ShownSize).</summary>
    public static readonly Rect2 TypedScreen = new(6, 508, 150, 32), HullScreen = new(162, 500, 226, 42),
        ButtonScreen = new(408, 508, 136, 30);

    /// <summary>The score and progress screens on the wings: their tops run parallel to the rim.</summary>
    public static readonly Vector2[] ScoreScreen = SlopedScreen(12, 140, 502), ProgressScreen = SlopedScreen(408, 540, 502);

    /// <summary>Where the score and progress readouts sit (their screens' left edges, at a common height).</summary>
    public static readonly Vector2 ScoreAnchor = new(12, 452), ProgressAnchor = new(408, 452);

    const float ScreenBelowRim = 12;

    static Vector2[] SlopedScreen(float left, float right, float bottom) => new[]
    {
        new Vector2(left, TopAt(left) + ScreenBelowRim), new Vector2(right, TopAt(right) + ScreenBelowRim),
        new Vector2(right, bottom), new Vector2(left, bottom),
    };

    static Vector2[] Corners(Rect2 r) => new[] { r.Position, new Vector2(r.End.X, r.Position.Y), r.End, new Vector2(r.Position.X, r.End.Y) };

    // The palette follows the player's ship, toned down: muted brass lines, red trim, dark bronze, and cool text (Ink)
    // so readouts stand apart from the warm explosions. Kit (the menus) shares it.
    public static readonly Color Rim = new(0.88f, 0.72f, 0.45f), Trim = new(0.78f, 0.2f, 0.15f),
        Steel = new(0.24f, 0.17f, 0.1f), Deep = new(0.07f, 0.045f, 0.03f), Glass = new(0.035f, 0.02f, 0.012f, 0.95f);
    static readonly Color Alarm = new(1, 0.2f, 0.15f);

    float time;

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        const float bottom = G.StageSize;
        // The hull: lighter along the top edge, dark at the bottom.
        var hull = new List<Vector2>(Edge) { new(G.StageSize, bottom), new(0, bottom) };
        var shades = hull.Select(p => Steel.Lerp(Deep, Mathf.Clamp((p.Y - 424) / 126, 0, 1))).ToArray();
        DrawPolygon(hull.ToArray(), shades);

        // A bevel and two seams following the edge.
        DrawPolyline(Offset(Edge, 5), new Color(0.55f, 0.4f, 0.2f, 0.6f), 1.5f, true);
        DrawPolyline(Offset(Edge, 22), new Color(0, 0, 0, 0.45f), 2, true);
        DrawPolyline(Offset(Edge, 21), new Color(Trim, 0.4f), 1, true);
        foreach (float x in new[] { 70f, 480f })
            DrawLine(new Vector2(x, TopAt(x) + 22), new Vector2(x, 450), new Color(0, 0, 0, 0.45f), 1);
        for (float x = 16; x < G.StageSize; x += 36)
        {
            if (x > 150 && x < 400) continue; // the console's slopes
            DrawCircle(new Vector2(x, TopAt(x) + 13), 1.2f, new Color(0.7f, 0.52f, 0.28f, 0.7f));
        }

        // The rim lights up gold, or pulses red while the hull is down to its last point.
        bool critical = G.Model != null && G.Model.Player.Health == 1 && !G.Model.Player.Destroyed;
        var rim = critical ? Alarm.Lerp(Colors.White, 0.15f * (0.5f + 0.5f * Mathf.Sin(time * 8))) : Rim;
        float glow = critical ? 0.35f + 0.25f * Mathf.Sin(time * 8) : 0.14f;
        DrawPolyline(Edge, new Color(rim, glow), 6, true);
        DrawPolyline(Edge, new Color(critical ? rim * 1.6f : rim * 1.15f, 0.95f), 1.6f, true); // a little overbright: it blooms softly
        // A glint running along the rim now and then.
        float run = time * 0.35f % 1.6f;
        if (run < 1)
        {
            var at = Along(Edge, run);
            DrawCircle(at, 6, new Color(rim, 0.18f));
            DrawCircle(at, 2.2f, new Color(Colors.White, 0.7f));
        }

        foreach (var screen in new[] { ScoreScreen, ProgressScreen, Corners(TypedScreen), Corners(HullScreen), Corners(ButtonScreen) })
            Screen(screen);

        // Indicator lights, blinking out of step.
        Light(new Vector2(146, 504), 0.0f, Rim);
        Light(new Vector2(154, 504), 0.4f, Trim);
        Light(new Vector2(396, 504), 0.7f, Trim);
        Light(new Vector2(404, 504), 0.2f, critical ? Alarm : Rim);
    }

    /// <summary>An inset screen: a dark recess, a thin lit border, and a faint sheen along its top edge.</summary>
    void Screen(Vector2[] corners)
    {
        var closed = corners.Append(corners[0]).ToArray();
        DrawPolyline(closed, new Color(0, 0, 0, 0.6f), 4);
        DrawColoredPolygon(corners, Glass);
        DrawPolyline(closed, new Color(Rim, 0.35f), 1, true);
        DrawLine(corners[0] + new Vector2(2, 2), corners[1] + new Vector2(-2, 2), new Color(Rim, 0.12f), 2, true);
    }

    void Light(Vector2 at, float phase, Color c)
    {
        bool on = (time * 0.9f + phase) % 1 < 0.6f;
        DrawCircle(at, 2.2f, new Color(c, on ? 0.95f : 0.2f));
        if (on) DrawCircle(at, 5, new Color(c, 0.2f));
    }

    /// <summary>The top edge's height at x.</summary>
    static float TopAt(float x)
    {
        for (int i = 1; i < Edge.Length; i++)
            if (x <= Edge[i].X)
                return Mathf.Lerp(Edge[i - 1].Y, Edge[i].Y, (x - Edge[i - 1].X) / (Edge[i].X - Edge[i - 1].X));
        return Edge[^1].Y;
    }

    static Vector2[] Offset(Vector2[] line, float down) => line.Select(p => p + new Vector2(0, down)).ToArray();

    /// <summary>A point a fraction of the way along a polyline.</summary>
    static Vector2 Along(Vector2[] line, float u)
    {
        float total = 0;
        for (int i = 1; i < line.Length; i++) total += line[i - 1].DistanceTo(line[i]);
        float d = u * total;
        for (int i = 1; i < line.Length; i++)
        {
            float seg = line[i - 1].DistanceTo(line[i]);
            if (d <= seg) return line[i - 1].Lerp(line[i], d / seg);
            d -= seg;
        }
        return line[^1];
    }
}

/// <summary>
/// The typed text, in its screen at the bottom left: letters in the word font with a blinking caret. As the text
/// nears the bar's length the oldest letters turn red, since each one pushed off the end costs a point.
/// </summary>
public partial class TypedDisplay : Node2D
{
    const int FontSize = 12;
    public static readonly Color Ink = new(0.72f, 0.92f, 1), Danger = new(1, 0.3f, 0.25f);
    string shown = "";
    float time;

    public override void _Process(double delta)
    {
        time += (float)delta;
        var typed = G.Model?.Typed ?? "";
        if (typed != shown) time = 0; // the caret shows solid while typing
        shown = typed;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var font = Fonts.Word;
        float advance = font.GetCharSize('m', FontSize).X;
        int bar = G.Model?.Tuning.TypingBar ?? 20;
        int left = bar - shown.Length; // letters to go before the oldest one falls off
        float x = 0, ascent = font.GetAscent(FontSize);
        for (int i = 0; i < shown.Length; i++)
        {
            // Within 4 letters of the end, the oldest letters redden, the oldest most.
            float danger = (4 - left - i) / 4f;
            var c = Ink.Lerp(Danger, Mathf.Clamp(danger, 0, 1));
            DrawChar(font, new Vector2(x, ascent), shown[i].ToString(), FontSize, c);
            x += advance;
        }
        if ((int)(time * 2.2f) % 2 == 0)
            DrawRect(new Rect2(x + 1, 2, advance * 0.6f, ascent), new Color(Ink, 0.8f));
    }
}
