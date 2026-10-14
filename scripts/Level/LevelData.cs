namespace TypingShip;

// Level scripts. The helpers keep the original lowercase names so the scripts read exactly as they did in AS3.
// Positions are on a 12x12 grid over the game view; distances are in "distance units" (the ship travels
// wpm/3600 of them per second), and steps of 0.1 or more shrink at higher typing speeds.
public static class LevelData
{
    static double dist;
    static int level;
    static List<ILevelEvent> array = new();
    static double mult;

    const float GridX = G.ViewWidth / 12, GridY = G.ViewHeight / 12;

    public static List<ILevelEvent> LoadData(int lvl)
    {
        mult = Math.Max(1 - G.Model.Wpm / 300.0, 0.5);
        array = new List<ILevelEvent>();
        dist = 0;
        level = lvl;
        switch (level)
        {
            case 0:
                missile("sn", 0.05, 5, 0, 5, 6, 4, 12);
                mirror();

                missile("sn", 0.15, 0, 3, 7, 3, 12, 3);
                missile("sn", 0.01, 12, 6, 5, 6, 0, 6);
                repeat(0.01, 3, 2);

                laser("sn", 0.1, 6, 0);

                missile("sn", 0.1, 3, 0, 7, 5, 4, 5, 7, 0);
                repeat(0.01, 5);
                laser("sn", 0, 12, 5);

                missile("sn", 0.13, 2, 0, 2, 4, 12, 9);
                repeat(0.01, 7, 1, true);
                laser("sn", 0, 0, 6);
                insert(-4);
                laser("sn", 0, 12, 6);
                insert(-4);

                laser("sn", 0.15, 0, 3);
                laser("sn", 0.005, 12, 3);
                laser("sn", 0.005, 0, 6);
                laser("sn", 0.005, 12, 6);

                missile("sn", 0.13, 3, 0, 4, 4, 7, 0);
                missile("sn", 0.01, 12, 5, 7, 6, 0, 5);
                repeat(0.02, 4, 2);
                laser("sn", 0.04, 0, 2);
                laser("sn", 0.01, 12, 2);
                laser("sn", 0.02, 0, 4);
                laser("sn", 0.01, 12, 4);
                missile("sn", 0.07, 9, 0, 8, 4, 5, 0);
                missile("sn", 0.01, 0, 5, 5, 6, 12, 5);
                repeat(0.02, 4, 2);
                break;

            default: throw new ArgumentOutOfRangeException(nameof(lvl), $"Level {lvl} isn't ported yet");
        }
        return array;
    }

    static void setDist(double d)
    {
        if (d >= 0.1) d *= mult;
        dist += d;
    }

    static void loopBack(int j = 0)
    {
        if (j < 0) j = array.Count + j;
        array.Add(new LevelEvent(dist, LevelEvent.Kind.Loop, j));
    }

    static LevelEvent wait(double d = 0)
    {
        setDist(d);
        var m = new LevelEvent(dist, LevelEvent.Kind.Wait, d);
        array.Add(m);
        return m;
    }

    static void jump(double j = 0) => array.Add(new LevelEvent(dist, LevelEvent.Kind.Jump, j));

    static void laser(string type, double d, float x, float y, int speed = 0)
    {
        ObjectCommand[] commands;
        if (x == 0) commands = new[] { move(2, y), halt(10 - speed), halt(20 - speed, true), move(-2, y) };
        else if (x == 12) commands = new[] { move(10, y), halt(10 - speed), halt(20 - speed, true), move(14, y) };
        else if (y == 0) commands = new[] { move(x, 2), halt(10 - speed), halt(20 - speed, true), move(x, -2) };
        else if (y == 12) commands = new[] { move(x, 10), halt(10 - speed), halt(20 - speed, true), move(x, 14) };
        else throw new ArgumentException($"Invalid spawn location: {x} {y}");
        ship($"{type[0]}l{type[1]}", d, x, y, commands);
    }

    static void suicide(string type, double d, float x, float y, float xTo, float yTo) =>
        ship($"{type[0]}s{type[1]}", d, x, y, new[] { move(xTo, yTo), fire() });

    static void stealth(double d, float x, float y, float xTo, float yTo, bool shield = false) =>
        ship(shield ? "xms" : "xmn", d, x, y, new[]
        {
            halt(8), halt(10, true), halt(10, true), halt(10, true), halt(10, true), halt(5, true), move(xTo, yTo),
        });

    static void missile(string type, double d, float x, float y, float xTo1, float yTo1,
        float xTo2 = -1, float yTo2 = -1, float xTo3 = -1, float yTo3 = -1) =>
        ship($"{type[0]}m{type[1]}", d, x, y, new[]
        {
            move(xTo1, yTo1),
            move(xTo2, yTo2, xTo3 == -1),
            xTo3 != -1 ? move(xTo3, yTo3, true) : halt(1),
        });

    /// <summary>Move the last event back to position j, at that event's distance.</summary>
    static void insert(int j)
    {
        if (j < 0) j = array.Count + j;
        var last = array[^1];
        array.RemoveAt(array.Count - 1);
        last.Distance = array[j].Distance;
        array.Insert(j, last);
    }

    static void ship(string type, double d, float x, float y, ObjectCommand[]? commands = null)
    {
        setDist(d);
        array.Add(new SpawnEvent(dist, type, x * GridX, y * GridY, commands));
    }

    static SpawnEvent At(int j) => (SpawnEvent)array[j < 0 ? array.Count + j : j];

    static void clone(double d = 0, int j = -1)
    {
        var src = At(j);
        setDist(d);
        array.Add(new SpawnEvent(dist, src.Type, src.X, src.Y, src.Commands.Select(c => c.Clone()).ToArray()));
    }

    static void mirror(double d = 0, int j = -1)
    {
        var src = At(j);
        setDist(d);
        array.Add(new SpawnEvent(dist, src.Type, G.ViewWidth - src.X, src.Y, src.Commands.Select(c => c.Mirror()).ToArray()));
    }

    static void offset(double d = 0, float xOff = 0, float yOff = 0, int j = -1)
    {
        var src = At(j);
        setDist(d);
        xOff *= GridX;
        yOff *= GridY;
        array.Add(new SpawnEvent(dist, src.Type, src.X + xOff, src.Y + yOff,
            src.Commands.Select(c => c.Offset(xOff, yOff)).ToArray()));
    }

    /// <summary>Repeat the last repeatC spawns repeatN times (mirrored if reverse), keeping their spacing.</summary>
    static void repeat(double d, int repeatN = 1, int repeatC = 1, bool reverse = false)
    {
        for (int k = 0; k < repeatN * repeatC; k++)
        {
            double step = k % repeatC == 0
                ? d
                : array[^repeatC].Distance - array[^(repeatC + 1)].Distance;
            if (reverse) mirror(step, -repeatC);
            else clone(step, -repeatC);
        }
    }

    static ObjectCommand move(float x, float y, bool fire = false) => new(x * GridX, y * GridY, 0, true, fire);

    static ObjectCommand halt(int timer, bool fire = false) => new(G.ViewWidth / 2, G.ViewHeight - 100, timer, false, fire);

    static ObjectCommand fire() => new(0, 0, 1, false, true);
}
