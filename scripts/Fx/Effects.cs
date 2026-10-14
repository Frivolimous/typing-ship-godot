using Godot;

namespace TypingShip;

/// <summary>Per-tick behaviour outside the main object list (the Flash ENTER_FRAME listeners). False = done.</summary>
public interface ITicker
{
    bool Tick();
}

// A beam that fades out over ~12 ticks: red from the player, blue from enemies, white for the EMP.
public partial class BeamEffect : Node2D, ITicker
{
    readonly Vector2 to;
    readonly Color color, glow;
    readonly float width;

    public BeamEffect() { }

    BeamEffect(Vector2 from, Vector2 to, Color color, Color glow, float width)
    {
        Position = from;
        this.to = to - from;
        this.color = color;
        this.glow = glow;
        this.width = width;
        G.UI.AddToGameViewBack(this);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    public static void Laser(GameSprite origin, Vector2 target)
    {
        bool player = origin == G.Model.PShip;
        new BeamEffect(origin.P, target,
            player ? new Color(0xff0000ff) : new Color(0x0000ffff),
            player ? new Color(1, 0.47f, 0.47f, 0.5f) : new Color(0.47f, 0.47f, 1, 0.5f), 1);
    }

    public static void Emp(GameSprite origin, Vector2 target) =>
        new BeamEffect(origin.P, target, Colors.White, new Color(1, 0.67f, 0.67f, 0.8f), 2);

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

// Floating "+5" / "-1" that also applies the score change.
public partial class PopScore : Node2D, ITicker
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
        G.Model.Score += s;
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

// A word whose ship left the screen stays typeable for 30 ticks, so a half-typed word isn't wasted.
public class Overflow
{
    public readonly string Word;
    int delay = 30;

    public Overflow(string s)
    {
        Word = s;
        G.Model.GOverflow.Add(this);
    }

    public bool Update()
    {
        if (delay > 0)
        {
            delay--;
            return true;
        }
        Remove();
        return false;
    }

    public void Remove()
    {
        G.Words.Push(Word);
        G.Model.GOverflow.Remove(this);
    }
}

// Stealth ships materialise: red and blue ghost images spiral in before the ship fades up.
public partial class StealthEffect : Node2D, ITicker
{
    readonly EnemyShip toSpawn = null!;
    readonly Sprite2D red = new(), blue = new();
    readonly float countUp;
    float counter;

    public StealthEffect() { }

    public StealthEffect(EnemyShip v)
    {
        countUp = 1 + G.Model.Wpm / 30f;
        toSpawn = v;
        v.GetParent().AddChild(this);
        v.Modulate = new Color(1, 1, 1, 0);
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
        if (toSpawn.Removed)
        {
            QueueFree();
            return false;
        }
        Rotation = toSpawn.Rotation;
        Position = toSpawn.Position;
        if (!G.Model.Running) return true;
        counter += countUp;
        float a = counter / 100 * Mathf.Tau;
        float r = 20 * (1 - counter / 100);
        red.Position = new Vector2(r * Mathf.Sin(a), r * Mathf.Cos(a));
        blue.Position = -red.Position;
        Modulate = new Color(1, 1, 1, 0.1f + counter / 150);
        toSpawn.Modulate = new Color(1, 1, 1, Math.Max((counter - 70) / 30, 0));
        if (counter < 100) return true;
        toSpawn.Modulate = Colors.White;
        QueueFree();
        return false;
    }
}
