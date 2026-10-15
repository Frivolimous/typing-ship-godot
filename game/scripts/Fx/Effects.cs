using Godot;

namespace TypingShip;

/// <summary>Per-tick animation outside the rules (the Flash ENTER_FRAME listeners that only drew). False = done.</summary>
public interface IViewTicker
{
    bool Tick();
}

// A beam that fades out over ~12 ticks: red from the player, blue from enemies, white for the EMP.
public partial class BeamEffect : Node2D, IViewTicker
{
    readonly Vector2 to;
    readonly Color color, glow;
    readonly float width;

    public BeamEffect() { }

    public BeamEffect(Vec2 from, Vec2 to, BeamKind kind)
    {
        Position = G.V(from);
        this.to = G.V(to) - Position;
        (color, glow, width) = kind switch
        {
            BeamKind.PlayerLaser => (new Color(0xff0000ff), new Color(1, 0.47f, 0.47f, 0.5f), 1f),
            BeamKind.EnemyLaser => (new Color(0x0000ffff), new Color(0.47f, 0.47f, 1, 0.5f), 1f),
            _ => (Colors.White, new Color(1, 0.67f, 0.67f, 0.8f), 2f),
        };
        G.UI.AddToGameViewBack(this);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    public bool Tick()
    {
        var m = Modulate;
        m.A -= 0.08f;
        Modulate = m;
        if (m.A > 0) return true;
        QueueFree();
        return false;
    }

    public override void _Draw()
    {
        DrawLine(Vector2.Zero, to, new Color(glow, glow.A * 0.5f), width + 6, true);
        DrawLine(Vector2.Zero, to, glow, width + 2, true);
        DrawLine(Vector2.Zero, to, color, width, true);
    }
}

// Floating "+5" / "-1" where points were won or lost.
public partial class PopScore : Node2D, IViewTicker
{
    readonly float rate;

    public PopScore() { }

    /// <param name="inGameView">True when the position is in game-view space (a ship), false for HUD space.</param>
    public PopScore(Vector2 pos, int s, bool inGameView = true)
    {
        var label = new Label
        {
            Text = (s > 0 ? "+" : "") + s,
            LabelSettings = Fonts.Settings(Fonts.Game, (int)(14 + s / 20f), s > 0 ? new Color(0x00ff00ff) : new Color(0xff0000ff),
                outline: 5, outlineColor: new Color(0, 0, 0, 0.9f)),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(label);
        rate = 1 - s / 120f;
        Position = pos;
        if (inGameView) G.UI.GameView.AddChild(this);
        else G.UI.AddChild(this);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    public bool Tick()
    {
        var m = Modulate;
        if (m.A <= 0)
        {
            QueueFree();
            return false;
        }
        m.A -= 0.02f * rate;
        Modulate = m;
        Position += new Vector2(0, -0.6f * rate);
        return true;
    }
}

// Stealth ships materialise: red and blue ghost images spiral in before the ship fades up (the rules count the
// fade-in; this draws it).
public partial class StealthEffect : Node2D, IViewTicker
{
    readonly EnemyShipView toSpawn = null!;
    readonly EnemyShip ship = null!;
    readonly Sprite2D red = new(), blue = new();

    public StealthEffect() { }

    public StealthEffect(EnemyShipView v)
    {
        toSpawn = v;
        ship = (EnemyShip)v.Model;
        v.GetParent().AddChild(this);
        Position = v.Position;
        foreach (var (s, tint) in new[] { (red, new Color(2, 0.4f, 0.4f)), (blue, new Color(0.4f, 0.4f, 2)) })
        {
            s.Texture = v.View.Texture;
            s.Scale = v.View.Scale;
            s.Rotation = v.View.Rotation;
            s.SelfModulate = tint;
            AddChild(s);
        }
        Modulate = new Color(1, 1, 1, 0);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    public bool Tick()
    {
        if (ship.Removed || !IsInstanceValid(toSpawn))
        {
            QueueFree();
            return false;
        }
        Rotation = toSpawn.Rotation;
        Position = toSpawn.Position;
        float counter = ship.StealthCounter;
        float a = counter / 100 * Mathf.Tau;
        float r = 20 * (1 - counter / 100);
        red.Position = new Vector2(r * Mathf.Sin(a), r * Mathf.Cos(a));
        blue.Position = -red.Position;
        Modulate = new Color(1, 1, 1, 0.1f + counter / 150);
        if (counter < 100) return true;
        QueueFree();
        return false;
    }
}
