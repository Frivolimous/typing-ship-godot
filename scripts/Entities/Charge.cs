using Godot;

namespace TypingShip;

// A weapon charging up on its owner: a growing glow; fires when full.
public partial class Charge : Node2D, ITicker
{
    int count;
    readonly int maxCount;
    readonly int type;
    GameSprite origin = null!, target = null!;
    bool active;
    Color color;

    public Charge(int x, int y, int time, int type = ActionControl.Laser)
    {
        Position = new Vector2(x, y);
        maxCount = time;
        this.type = type;
    }

    public Charge() { }

    public void Add(GameSprite origin, GameSprite target)
    {
        this.origin = origin;
        this.target = target;
        count = 0;
        GetParent()?.RemoveChild(this);
        origin.AddChild(this);
        origin.MoveChild(this, 0);
        if (!active)
        {
            active = true;
            G.Ctrl.AddTicker(this);
        }
    }

    public void Cancel()
    {
        active = false;
        GetParent()?.RemoveChild(this);
    }

    public bool Tick()
    {
        if (!active) return false;
        if (!G.Model.Running) return true;
        count++;
        if (count >= maxCount)
        {
            active = false;
            GetParent()?.RemoveChild(this);
            if (type == ActionControl.Laser) G.Action.ShootLaser(origin, target, true);
            else if (type == ActionControl.Emp) G.Action.ShootEmp(origin, target, true);
            return false;
        }
        if (count == maxCount / 2)
        {
            origin.Priority = 3;
            G.Sound.Sound(SoundControl.Charge);
        }
        // 0x0000ff plus a random touch of green, minus a random touch of blue.
        color = new Color(0, Mathf.Floor(G.Rand() * 0x40) / 255f, (255 - Mathf.Floor(G.Rand() * 0x40)) / 255f);
        QueueRedraw();
        return true;
    }

    public override void _Draw()
    {
        float r = 10f * count / maxCount;
        DrawCircle(Vector2.Zero, r * 1.8f, new Color(color, 0.25f));
        DrawCircle(Vector2.Zero, r, color);
    }
}
