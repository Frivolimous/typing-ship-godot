using Godot;

namespace TypingShip;

// The targeting ring on a boss. Each 3-letter word typed shrinks the ring by 30 px; when it has closed in, a
// 10-letter word appears, and typing that fires a laser at the spot and damages the boss.
public partial class Scanner : GameObject
{
    static readonly Color RingColor = new(0xff5500ff);

    int count, toCount;
    float ring = 300; // the radius last drawn

    public Scanner() { }

    public Scanner(BossShip v)
    {
        WordSize = 3;
        AddWord();
        // A random visible pixel of the boss (not behind boss 0's hangar doors), away from the bottom and left edges.
        Vector2 p;
        do
        {
            var px = Sprites.RandomOpaquePixel(v.View.Texture);
            if (v.Index == 0 && Sprites.Opaque(Sprites.Door[0], px.X, px.Y))
            {
                p = new Vector2(0, 500);
                continue;
            }
            p = v.TopLeft + px * v.SpriteScale;
        } while (p.Y > 475 || p.X < 35);
        Position = p;
        count = 300;
        G.UI.GameView.AddChild(this);
        ResetPhysicsInterpolation();
    }

    public void Remove()
    {
        if (G.Model.Boss != null) G.Model.Boss.Scanner = null;
        RecycleWord();
        GetParent()?.RemoveChild(this);
        QueueFree();
    }

    public void Scan()
    {
        toCount += 30;
        if (count - toCount > 5)
        {
            AddWord();
        }
        else if (WordSize < 10)
        {
            WordSize = 10;
            AddWord();
        }
        else
        {
            BeamEffect.Laser(G.Model.PShip, Position);
            G.Starfield.Explosion(Position.X, Position.Y, 20);
            var boss = G.Model.Boss!;
            Remove();
            boss.Injure();
        }
    }

    public void Update()
    {
        if (count <= 0 || toCount <= 0) return;
        if (count < 5) count = 5;
        count -= 3;
        toCount -= 3;
        ring = count;
        if (count <= 5) count = -1;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawArc(Vector2.Zero, ring, 0, Mathf.Tau, Math.Max(32, (int)ring / 2), RingColor, 3, true);
        DrawCircle(Vector2.Zero, 2.5f, RingColor);
    }
}
