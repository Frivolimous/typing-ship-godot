using Godot;

namespace TypingShip;

// A moving thing in the game view: steers towards a heading n with acceleration a.
// All logic runs in the 24 Hz tick, in the original's pixel units per tick.
public partial class GameSprite : GameObject
{
    public float VX, VY, VT, N, A;
    public int Value, KillBy, Fires, Frame, Health = 1;
    public float TurnRate;
    public bool ShieldOn, Halt;
    public Turret? Turret;
    public readonly Sprite2D View = new();
    public Texture2D[]? ViewSource;
    public float SpriteScale = 0.2f;

    /// <summary>Set once the sprite has left play. Others may still hold a reference (a missile homing on it).</summary>
    public bool Removed;
    Vector2 lastPos;

    /// <summary>Position, or where it was when it was removed.</summary>
    public Vector2 P => Removed ? lastPos : Position;

    public void Init(float x, float y, float n, float a, int p, bool isBoss = false)
    {
        if (isBoss)
        {
            VT = 0.01f;
            View.Texture = ViewSource?[0];
            View.Scale = new Vector2(SpriteScale, SpriteScale);
            AddChild(View);
            N = Mathf.Pi / 2;
            Rotation = 0;
        }
        else
        {
            SetFrame(0);
            View.Scale = new Vector2(SpriteScale, SpriteScale);
            View.Rotation = Mathf.Pi / 2; // art points up; heading 0 points right
            AddChild(View);
            N = n;
            Rotation = n;
        }

        Position = new Vector2(x, y);
        A = a;
        TurnRate = a / 7;
        AddWord();
        Priority = p;
        G.UI.GameView.AddChild(this);
        ResetPhysicsInterpolation();
    }

    public virtual bool Update()
    {
        if (!MoreUpdate()) return false;
        VT += (Halt ? 0 : A) - VT * 0.1f;
        VX = VT * Mathf.Cos(N);
        VY = VT * Mathf.Sin(N);
        if (Math.Abs(VT) < Math.Abs(A))
            VX = VY = VT = 0;
        else
            Position += new Vector2(VX, VY);
        return true;
    }

    public virtual bool MoreUpdate() => true;

    public void SetFrame(int i) => View.Texture = ViewSource?[Frame = i];

    /// <summary>SpriteSheets.update: step an animated sprite to its next frame.</summary>
    public void AdvanceFrame()
    {
        if (ViewSource == null || ViewSource.Length <= 1) return;
        Frame++;
        if (Frame >= ViewSource.Length) Frame = 0;
        View.Texture = ViewSource[Frame];
    }

    public virtual void Remove()
    {
        if (TextObject != null) new Overflow(TextObject.Text);
        if (!G.Model.GObjects.Remove(this) && this is Turret && GetParent() is EnemyShip ship) ship.Turret = null;
        Detach();
    }

    protected void Detach()
    {
        lastPos = Position;
        Removed = true;
        GetParent()?.RemoveChild(this);
        QueueFree();
    }

    public void HomeTarget(Vector2 target)
    {
        float tDiff = Mathf.Atan2(target.Y - Position.Y, target.X - Position.X) - N;
        while (tDiff < -Mathf.Pi) tDiff += 2 * Mathf.Pi;
        while (tDiff > Mathf.Pi) tDiff -= 2 * Mathf.Pi;

        if (tDiff > 0) N += Math.Min(tDiff, TurnRate);
        else if (tDiff < 0) N += Math.Max(tDiff, -TurnRate);
        else return;

        Rotation = N;
        if (TextObject != null) TextObject.Rotation = -Rotation;
    }
}
