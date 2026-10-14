namespace TypingShip;

// One step of a ship's script: fly to (x, y), or wait timer ticks; optionally fire when the step starts.
// Targets inside the view get up to ±10 px of jitter, as in the original (re-rolled on clone/mirror/offset).
public class ObjectCommand
{
    public float X, Y;
    public int Timer;
    public bool Fire, Move;

    public ObjectCommand(float x = 0, float y = 0, int timer = 0, bool move = true, bool fire = false)
    {
        X = x > 0 && x < G.ViewWidth ? Math.Clamp(x + G.Rand() * 20 - 10, 0, G.ViewWidth) : x;
        Y = y > 0 && y < G.ViewHeight ? Math.Clamp(y + G.Rand() * 20 - 10, 0, G.ViewHeight) : y;
        Timer = (int)(timer * 400f / G.Model.Wpm);
        Move = move;
        Fire = fire;
    }

    public bool Condition(GameSprite v)
    {
        if (Move)
            return Math.Abs(v.Position.X - X) < 20 && Math.Abs(v.Position.Y - Y) < 20;
        if (Timer <= 0) return true;
        Timer--;
        return false;
    }

    int BaseTimer => (int)(Timer * G.Model.Wpm / 400f);

    public ObjectCommand Clone() => new(X, Y, BaseTimer, Move, Fire);
    public ObjectCommand Mirror() => new(G.ViewWidth - X, Y, BaseTimer, Move, Fire);
    public ObjectCommand Offset(float xOff, float yOff) => new(X + xOff, Y + yOff, BaseTimer, Move, Fire);
}
