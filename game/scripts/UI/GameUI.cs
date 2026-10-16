using Godot;

namespace TypingShip;

// In-game HUD over the 550x550 playfield: the game view, and the cockpit along the bottom with its screens (score,
// typed text, hull, progress).
public partial class GameUI : Control
{
    public readonly Node2D GameView = new() { Position = new Vector2(-25, -25) };
    public readonly Node2D BossView = new() { Position = new Vector2(-25, -25) };
    readonly Node2D effectsBack = new();
    readonly Cockpit cockpit = new() { Name = "Cockpit" };
    readonly ScoreReadout score = new() { Name = "Score" };
    readonly ProgressReadout progress = new() { Name = "Progress" };
    readonly TypedDisplay typed = new() { Name = "Typed" };
    public PopBox? TutBox;

    /// <summary>Where the typing bar is (for its "-1" pop-ups).</summary>
    public Vector2 TypedPosition => typed.Position;

    /// <summary>The hull bar and its repair word.</summary>
    public readonly HullBar Hull = new() { Name = "Hull" };

    public GameUI()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(G.StageSize, G.StageSize);
    }

    public void Init()
    {
        score.Position = Cockpit.ScoreAnchor;
        progress.Position = Cockpit.ProgressAnchor;
        typed.Position = Cockpit.TypedScreen.Position + new Vector2(7, 9);
        Hull.Position = Cockpit.HullScreen.Position;

        GameView.AddChild(effectsBack);
        AddChild(BossView);
        AddChild(GameView);
        AddChild(cockpit);
        AddChild(Hull);
        AddChild(score);
        AddChild(progress);
        AddChild(typed);
    }

    /// <summary>A new level: the score shows its value at once rather than counting to it.</summary>
    public void SnapScore() => score.Snap();

    /// <summary>Particle layer sits at the very back of the game view.</summary>
    public void SetParticleLayer(Node2D layer)
    {
        GameView.AddChild(layer);
        GameView.MoveChild(layer, 0);
    }

    /// <summary>Beams and player missiles go behind the ships (the original's addChildAt(…, 0)).</summary>
    public void AddToGameViewBack(Node2D node)
    {
        if (node.GetParent() != GameView) GameView.AddChild(node);
        GameView.MoveChild(node, effectsBack.GetIndex() + 1);
    }

    /// <summary>The hull follows the rules every frame (the typed text and score do so themselves).</summary>
    public override void _Process(double delta) => Hull.Sync();

    public void OnTick() =>
        progress.Set(G.Model.MaxTime > 0 ? Math.Min(1, (float)(G.Model.Time / G.Model.MaxTime)) : 0);
}

/// <summary>The score screen: the score counts up to its value, and pulses green as points come in (red as they go).</summary>
public partial class ScoreReadout : Node2D
{
    static readonly Color Gain = new(0.45f, 1, 0.65f), Loss = new(1, 0.35f, 0.3f);
    readonly Label caption = new()
    {
        Text = "SCORE",
        Position = new Vector2(8, 8),
        LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(Cockpit.Rim, 0.75f)),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    readonly Label number = new()
    {
        Position = new Vector2(8, 18),
        Size = new Vector2(112, 30),
        LabelSettings = Fonts.Settings(Fonts.HudBold, 20, Colors.White, 3, new Color(0, 0, 0, 0.6f)),
        PivotOffset = new Vector2(0, 15),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    float shown, pulse;
    Color flash = Gain;

    public ScoreReadout()
    {
        AddChild(caption);
        AddChild(number);
    }

    public void Snap() => shown = G.Model.Score;

    public override void _Process(double delta)
    {
        float d = (float)delta, target = G.Model.Score;
        if (target != Mathf.Round(shown))
        {
            if (target > shown) flash = Gain;
            else if (target < shown) flash = Loss;
            pulse = 1;
        }
        // Count towards the score: fast for big jumps, never slower than 30 a second.
        float step = Mathf.Max(30, Mathf.Abs(target - shown) * 8) * d;
        shown = Mathf.Abs(target - shown) <= step ? target : shown + Mathf.Sign(target - shown) * step;
        pulse = Mathf.Max(0, pulse - d * 3);

        number.Text = ((int)Mathf.Round(shown)).ToString();
        number.Scale = Vector2.One * (1 + 0.15f * pulse);
        number.Modulate = Colors.White.Lerp(flash, pulse);
    }
}

/// <summary>The progress screen: how far through the level, as a percentage and a bar.</summary>
public partial class ProgressReadout : Node2D
{
    readonly Label caption = new()
    {
        Text = "MISSION",
        Position = new Vector2(10, 22),
        LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(Cockpit.Rim, 0.75f)),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    readonly Label percent = new()
    {
        Position = new Vector2(8, 15),
        Size = new Vector2(116, 24),
        HorizontalAlignment = HorizontalAlignment.Right,
        LabelSettings = Fonts.Settings(Fonts.HudBold, 15, Colors.White, 3, new Color(0, 0, 0, 0.6f)),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    float fraction;

    public ProgressReadout()
    {
        AddChild(caption);
        AddChild(percent);
    }

    public void Set(float f)
    {
        fraction = f;
        percent.Text = $"{(int)(f * 100)}%";
        QueueRedraw();
    }

    public override void _Draw()
    {
        var bar = new Rect2(8, 40, 116, 4);
        DrawRect(bar, new Color(Cockpit.Rim, 0.15f));
        DrawRect(new Rect2(bar.Position, new Vector2(bar.Size.X * fraction, bar.Size.Y)), new Color(Cockpit.Rim, 0.85f));
    }
}

// The hull bar under the cockpit. While damaged it carries a long word: type it to repair one point.
public partial class HullBar : Node2D
{
    const float FlickerTime = 0.6f;
    const float Width = 226, PipTop = 28, PipHeight = 9, Gap = 5, Margin = 10;
    static readonly Color Full = new(0.4f, 1, 0.7f), Worn = new(1, 0.75f, 0.2f), Critical = new(1, 0.25f, 0.2f);
    static readonly Color Hurt = new(1, 0.35f, 0.35f);

    // The repair word, shown above the pips while the hull is damaged.
    readonly WordLabel word = new() { Position = new Vector2(Width / 2, 6) };
    readonly Label caption = new()
    {
        Text = "HULL",
        Position = new Vector2(Margin, 7),
        LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(Cockpit.Rim, 0.75f)),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    int health = -1, lost = -1;
    float flicker, time;

    public HullBar()
    {
        AddChild(caption);
        AddChild(word);
    }

    public void Sync()
    {
        var player = G.Model.Player;
        if (player.Health != health)
        {
            if (health > 0 && player.Health < health) Hit(player.Health);
            health = player.Health;
        }
        word.Set(G.Model.Hull.Word, G.Model.Hull.Priority);
        caption.Visible = G.Model.Hull.Word == null; // the repair word takes its place
    }

    /// <summary>The player took damage: shake, red at the screen edges, and the pips flicker as the lost one burns off.</summary>
    void Hit(int now)
    {
        G.Camera.Shake(0.45f);
        G.Flash.Flash(new Color(1, 0.1f, 0.05f, 0.75f), 1, 0.45f, edgesOnly: true);
        lost = now;
        flicker = FlickerTime;
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        if (flicker > 0) flicker -= (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (G.Model == null) return;
        int max = G.Model.Player.MaxHealth;
        float pip = (Width - 2 * Margin - Gap * (max - 1)) / max;
        var colour = health <= 1 ? Critical : health <= 2 ? Worn : Full;
        if (health == 1) colour = colour.Lerp(Colors.White, 0.25f * (0.5f + 0.5f * Mathf.Sin(time * 8)));
        float t = 1 - Mathf.Max(flicker, 0) / FlickerTime;
        bool blinkOff = flicker > 0 && (int)(t * 6) % 2 == 1; // three blinks
        for (int i = 0; i < max; i++)
        {
            float x = Margin + i * (pip + Gap);
            // Slanted pips, like the rest of the cockpit.
            var pts = new[]
            {
                new Vector2(x + 3, PipTop), new Vector2(x + pip, PipTop),
                new Vector2(x + pip - 3, PipTop + PipHeight), new Vector2(x, PipTop + PipHeight),
            };
            if (i < health)
            {
                var c = blinkOff ? Hurt : colour;
                DrawColoredPolygon(pts, new Color(c, 0.25f));
                DrawColoredPolygon(pts.Select(p => p.Lerp(new Vector2(x + pip / 2, PipTop + PipHeight / 2), 0.25f)).ToArray(), c);
            }
            else if (flicker > 0 && i == lost)
            {
                DrawColoredPolygon(pts, new Color(1, 1, 1, 1 - t)); // the lost pip burns off white
            }
            DrawPolyline(pts.Append(pts[0]).ToArray(), new Color(Cockpit.Rim, i < health ? 0.5f : 0.2f), 1, true);
        }
    }

    public void ShowWord(bool b) => word.ShowWord(b);

    public void PopWord() => word.Pop();
}
