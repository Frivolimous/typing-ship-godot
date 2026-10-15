using Godot;

namespace TypingShip;

// Blue energy bubble; flashes white and fades when knocked out.
public partial class ShieldView : Node2D, IViewTicker
{
    Vector2 radius = new(10, 10);
    bool breaking, fading;

    public void SetSize(float w, float h, float x = 0, float y = 0)
    {
        radius = new Vector2(w / 2, h / 2);
        Position = new Vector2(x, y);
        QueueRedraw();
    }

    public void Add(Node2D target)
    {
        Modulate = Colors.White;
        breaking = false;
        fading = false;
        GetParent()?.RemoveChild(this);
        target.AddChild(this);
        QueueRedraw();
    }

    public float Alpha
    {
        get => Modulate.A;
        set => Modulate = new Color(1, 1, 1, Math.Clamp(value, 0, 1)); // Flash clamped alpha too
    }

    public void Remove()
    {
        breaking = true;
        QueueRedraw();
        if (!fading)
        {
            fading = true;
            G.Ctrl.AddTicker(this);
        }
    }

    public bool Tick()
    {
        if (!fading || !IsInstanceValid(this)) return false; // freed along with its ship
        var m = Modulate;
        m.A -= 0.1f;
        Modulate = m;
        if (m.A > 0) return true;
        fading = false;
        GetParent()?.RemoveChild(this);
        return false;
    }

    public override void _Draw()
    {
        // Ellipse = unit circle scaled; the original drew a radius-10 circle stretched to width x height.
        DrawSetTransform(Vector2.Zero, 0, radius / 10);
        if (breaking)
        {
            DrawCircle(Vector2.Zero, 13, new Color(1, 0.67f, 0.67f, 0.35f));
            DrawCircle(Vector2.Zero, 10, new Color(1, 1, 1, 0.8f));
        }
        else
        {
            DrawCircle(Vector2.Zero, 10, new Color(0, 0.67f, 1, 0.35f));
            DrawArc(Vector2.Zero, 9.5f, 0, Mathf.Tau, 48, new Color(0.3f, 0.8f, 1, 0.9f), 1.2f, true);
        }
    }
}
