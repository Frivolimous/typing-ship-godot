using Godot;

namespace TypingShip;

// "New Achievement!": a slanted panel that slides in from the right, just above the cockpit, with a stripe in the
// tier's colour; it stays a few seconds and slides back out. On the score screen it sits under "FINAL SCORE:".
public partial class PopAchieve : Control
{
    const float W = 236, H = 46, Slide = 0.35f, Hold = 2.6f;
    static readonly Color[] TierColours = { Colors.White, new(0.9f, 0.58f, 0.3f), new(0.82f, 0.88f, 0.98f), new(1, 0.82f, 0.3f) };
    readonly Color tint;
    readonly Vector2 shown, hidden;
    float time;

    public PopAchieve(int achieve, int tier)
    {
        tint = TierColours[Math.Clamp(tier, 0, 3)];
        shown = G.Shell.ShowingScore ? new Vector2(30, 365) : new Vector2(G.StageSize - W - 10, 372);
        hidden = G.Shell.ShowingScore ? shown : new Vector2(G.StageSize + 10, shown.Y);
        Position = hidden;
        Size = new Vector2(W, H);
        MouseFilter = MouseFilterEnum.Ignore;
        if (G.Shell.ShowingScore) Modulate = new Color(1, 1, 1, 0);

        // The trophy, small, in a clipped window at the left (the art has empty margins).
        var clip = new Control { Position = new Vector2(14, 5), Size = new Vector2(44, H - 10), ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        clip.AddChild(new TextureRect
        {
            Texture = Sprites.Achieve[tier],
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(64, 40),
            Position = new Vector2(-10, -2),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddChild(clip);
        AddChild(new Label
        {
            Text = Strings.NewAchieve.ToUpperInvariant(),
            Position = new Vector2(64, 7),
            LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(tint, 0.9f)),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddChild(new Label
        {
            Text = $"{Strings.Tiers[tier]} {Strings.AchieveNames[achieve]}",
            Position = new Vector2(64, 20),
            LabelSettings = Fonts.Settings(Fonts.HudBold, 12, Colors.White, 3, new Color(0, 0, 0, 0.6f)),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        G.Shell.AddPopup(this);
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        if (time > Slide * 2 + Hold)
        {
            QueueFree();
            return;
        }
        // Ease in, hold, ease out (a fade instead of the slide on the score screen).
        float u = time < Slide ? time / Slide : time < Slide + Hold ? 1 : 1 - (time - Slide - Hold) / Slide;
        float eased = 1 - (1 - u) * (1 - u);
        Position = hidden.Lerp(shown, eased);
        if (G.Shell.ShowingScore) Modulate = new Color(1, 1, 1, eased);
    }

    public override void _Draw()
    {
        // Slanted ends, like the hull pips.
        var pts = new[] { new Vector2(8, 0), new Vector2(W, 0), new Vector2(W - 8, H), new Vector2(0, H) };
        DrawColoredPolygon(pts, new Color(0.015f, 0.03f, 0.06f, 0.92f));
        DrawPolyline(pts.Append(pts[0]).ToArray(), new Color(Cockpit.Rim, 0.45f), 1.2f, true);
        DrawColoredPolygon(new[] { new Vector2(8, 0), new Vector2(13, 0), new Vector2(5, H), new Vector2(0, H) }, tint);
    }
}

// A tutorial pop-up in the game view: a cockpit-style briefing panel, sized to its text. It pauses the game (without
// the pause screen); Space continues, Escape continues and turns the tutorial off. An optional pointer runs from the
// panel to the thing it's about.
public partial class PopBox : Node2D
{
    const float Left = 165, Top = 150, Width = 220, Pad = 10;
    const int TextSize = 11;
    readonly Rect2 Box;
    readonly Vector2[] pointer = Array.Empty<Vector2>();
    float time;

    /// <summary>Shown when the rules pause for a tutorial (G.Model.ShowTutorial).</summary>
    public PopBox(int tut, Vector2? o = null, PointerDir dir = PointerDir.Auto)
    {
        G.UI.TutBox?.Remove();
        G.UI.TutBox = this;
        var text = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart, // first: setting Text before it would widen the label
            Text = Strings.Tutorial[tut],
            Position = new Vector2(Left + Pad, Top + Pad + 14),
            Size = new Vector2(Width - 2 * Pad, 0),
            LabelSettings = Fonts.Settings(Fonts.Word, TextSize, new Color(0.9f, 0.96f, 1)),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        // The panel grows to the text as the label will wrap it (lines plus the label's line spacing), with room for the
        // key hints under it.
        float lineHeight = Fonts.Word.GetHeight(TextSize);
        int lines = Mathf.RoundToInt(Fonts.Word.GetMultilineStringSize(Strings.Tutorial[tut], HorizontalAlignment.Left, Width - 2 * Pad,
            TextSize, -1, TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Mandatory).Y / lineHeight);
        float textHeight = lines * (lineHeight + text.LabelSettings.LineSpacing);
        const float Hints = 30;
        Box = new Rect2(Left, Top, Width, Pad + 14 + textHeight + 8 + Hints);
        AddChild(new Label
        {
            Text = "BRIEFING",
            Position = Box.Position + new Vector2(Pad, 6),
            LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(Cockpit.Rim, 0.85f)),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        AddChild(text);
        AddChild(new Label
        {
            Text = Strings.TutorialKeys.ToUpperInvariant(),
            Position = new Vector2(Box.Position.X + Pad, Box.End.Y - Hints + 4),
            LabelSettings = Fonts.Settings(Fonts.Hud, 7, new Color(Cockpit.Rim, 0.8f)),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        G.UI.GameView.AddChild(this);
        ResetPhysicsInterpolation();

        if (o is not { } p) return;
        var c = Box.GetCenter();
        if (dir == PointerDir.Auto)
            dir = p.Y < Box.Position.Y ? PointerDir.Up : p.Y > Box.End.Y ? PointerDir.Down
                : p.X < Box.Position.X ? PointerDir.Left : p.X > Box.End.X ? PointerDir.Right : PointerDir.Auto;
        pointer = dir switch
        {
            PointerDir.Up => new[] { new Vector2(c.X, Box.Position.Y), new Vector2(c.X, p.Y), p },
            PointerDir.Down => new[] { new Vector2(c.X, Box.End.Y), new Vector2(c.X, p.Y), p },
            PointerDir.Left => new[] { new Vector2(Box.Position.X, c.Y), new Vector2(p.X, c.Y), p },
            PointerDir.Right => new[] { new Vector2(Box.End.X, c.Y), new Vector2(p.X, c.Y), p },
            _ => pointer,
        };
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        if (pointer.Length > 0) QueueRedraw();
    }

    public override void _Draw()
    {
        var rim = Cockpit.Rim;
        if (pointer.Length > 0)
        {
            DrawPolyline(pointer, new Color(rim, 0.25f), 5, true);
            DrawPolyline(pointer, new Color(rim, 0.9f), 1.5f, true);
            // The end pulses, so the eye finds what it's pointing at.
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 6);
            DrawArc(pointer[^1], 5 + 4 * pulse, 0, Mathf.Tau, 24, new Color(rim, 0.8f - 0.5f * pulse), 1.5f, true);
            DrawCircle(pointer[^1], 2.5f, rim);
        }
        DrawRect(Box.Grow(3), new Color(0, 0, 0, 0.5f));
        DrawRect(Box, new Color(0.015f, 0.03f, 0.06f, 0.92f));
        DrawRect(Box, new Color(rim, 0.5f), false, 1.5f);
        DrawLine(Box.Position + new Vector2(3, 3), new Vector2(Box.End.X - 3, Box.Position.Y + 3), new Color(rim, 0.15f), 2);
    }

    public void Remove()
    {
        if (G.UI.TutBox != this) return;
        G.UI.TutBox = null;
        GetParent()?.RemoveChild(this);
        QueueFree();
        if (G.Shell.InGame) G.Model.CloseTutorial();
    }
}
