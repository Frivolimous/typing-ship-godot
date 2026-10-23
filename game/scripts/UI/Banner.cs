using Godot;

namespace TypingShip;

/// <summary>
/// A banner across the middle of the playfield: "LEVEL 4" as a level starts, a warning as a boss arrives, "MISSION
/// COMPLETE" as a level is cleared. Lines sweep out from the centre, the title settles in, it holds, then fades.
/// </summary>
public partial class Banner : Control
{
    const float In = 0.35f, Out = 0.4f, Y = 210;
    /// <summary>Seconds a banner takes to come in.</summary>
    public const float FadeIn = In;
    static readonly Font Spaced = new FontVariation { BaseFont = Fonts.HudBold, SpacingGlyph = 4 };

    readonly Label title, subtitle;
    readonly Color tint;
    readonly float hold;
    float time;

    /// <summary>Shows a banner after <paramref name="delay"/> seconds.</summary>
    public Banner(string titleText, string subtitleText, Color tint, float hold = 1.6f, float delay = 0)
    {
        this.tint = tint;
        this.hold = hold;
        time = -delay;
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(G.StageSize, 80);
        Position = new Vector2(0, Y);
        title = new Label
        {
            Text = titleText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(G.StageSize, 34),
            Position = new Vector2(0, 14),
            PivotOffset = new Vector2(G.StageSize / 2, 17),
            LabelSettings = Fonts.Settings(Spaced, 24, Colors.White, 5, new Color(0, 0, 0, 0.7f)),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        subtitle = new Label
        {
            Text = subtitleText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(G.StageSize, 16),
            Position = new Vector2(0, 50),
            LabelSettings = Fonts.Settings(Fonts.Hud, 10, tint, 3, new Color(0, 0, 0, 0.7f)),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(title);
        AddChild(subtitle);
        Modulate = new Color(1, 1, 1, 0);
        G.UI.AddChild(this);
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        if (time < 0) return;
        if (time > In + hold + Out)
        {
            QueueFree();
            return;
        }
        float appear = Mathf.Clamp(time / In, 0, 1);
        float fade = time > In + hold ? 1 - (time - In - hold) / Out : 1;
        Modulate = new Color(1, 1, 1, appear * fade);
        title.Scale = Vector2.One * (1 + 0.25f * (1 - appear) * (1 - appear));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (time < 0) return;
        float appear = Mathf.Clamp(time / In, 0, 1);
        float half = G.StageSize / 2 * (1 - (1 - appear) * (1 - appear));
        float mid = G.StageSize / 2;
        // A dark band behind the text, fading out at its ends.
        var band = new Rect2(mid - half, 8, half * 2, 64);
        DrawRect(band, new Color(Kit.Shade, 0.55f));
        foreach (float y in new[] { 8f, 72f })
        {
            DrawLine(new Vector2(mid - half, y), new Vector2(mid + half, y), new Color(tint, 0.25f), 4);
            DrawLine(new Vector2(mid - half, y), new Vector2(mid + half, y), new Color(tint, 0.9f), 1.2f);
        }
    }

    /// <summary>The map's name for a level: 1-9 for ordinary levels, B1-B3 for the bosses.</summary>
    public static string LevelName(int i) => (i + 1) % 4 == 0 ? $"BOSS {(i + 1) / 4}" : $"LEVEL {i + 1 - (i + 1) / 4}";
}
