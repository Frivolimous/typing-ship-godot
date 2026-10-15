using TypingShip.Core.Data;

namespace TypingShip.Core.Levels;

/// <summary>
/// Turns a level script into its list of events for one play: positions on the 12 x 12 grid become pixels, ships
/// get their flight plans, mirror/repeat/insert copy and shuffle events, and steps of 0.1 or more shrink at higher
/// typing speeds. This is the original LevelData's helpers, line for line; random numbers are drawn in the same
/// order, so a seed gives the same level it always did.
/// </summary>
public sealed class LevelBuilder
{
    readonly LevelContext c;
    readonly List<LevelEvent> array = new();
    readonly double mult;
    double dist;
    string file = "";
    int line;

    LevelBuilder(LevelContext c, double startDistance)
    {
        this.c = c;
        var t = c.Tuning;
        mult = Math.Max(1 - c.ClockCps / t.ShrinkCps, t.ShrinkLimit);
        dist = startDistance;
    }

    /// <summary>Builds a script's events, starting at the given distance (bossLoop starts where the ship is).</summary>
    public static List<LevelEvent> Build(LevelScript script, LevelContext c, double startDistance = 0)
    {
        var b = new LevelBuilder(c, startDistance) { file = script.File };
        foreach (var s in script.Statements)
        {
            b.line = s.Line;
            try
            {
                b.Run(s);
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or IndexOutOfRangeException)
            {
                throw new DataError(script.File, s.Line, e.Message);
            }
        }
        return b.array;
    }

    double E(Num n) => n.Eval(c.Rng, c.DifficultyIndex);

    void Run(Statement s)
    {
        var r = c.Rng;
        switch (s)
        {
            // Numbers are rolled in the order they're written, as the original evaluated its arguments.
            case Statement.Missile m:
            {
                double d = E(m.Dist), x = E(m.From.X), y = E(m.From.Y);
                var to = new double[6];
                Array.Fill(to, -1);
                for (int i = 0; i < m.To.Length; i++)
                {
                    to[i * 2] = E(m.To[i].X);
                    to[i * 2 + 1] = E(m.To[i].Y);
                }
                missile(m.Type, d, x, y, to[0], to[1], to[2], to[3], to[4], to[5]);
                break;
            }
            case Statement.Laser l:
            {
                double d = E(l.Dist), x = E(l.At.X), y = E(l.At.Y), speed = E(l.Speed);
                laser(l.Type, d, x, y, (int)speed);
                break;
            }
            case Statement.Suicide su:
            {
                double d = E(su.Dist), x = E(su.From.X), y = E(su.From.Y), xTo = E(su.To.X), yTo = E(su.To.Y);
                suicide(su.Type, d, x, y, xTo, yTo);
                break;
            }
            case Statement.Stealth st:
            {
                double d = E(st.Dist), x = E(st.From.X), y = E(st.From.Y), xTo = E(st.To.X), yTo = E(st.To.Y);
                stealth(d, x, y, xTo, yTo, st.Shield);
                break;
            }
            case Statement.Ship sh:
            {
                double d = E(sh.Dist), x = E(sh.From.X), y = E(sh.From.Y);
                var commands = sh.Steps.Select(step => step.Type switch
                {
                    Step.Kind.Move => move(E(step.To!.X), E(step.To.Y), step.Fire),
                    Step.Kind.Halt => halt(step.Halt, step.Fire),
                    _ => fire(),
                }).ToArray();
                ship(sh.Code, d, x, y, commands);
                break;
            }
            case Statement.Boss bo: boss(E(bo.Dist), bo.Number - 1); break;
            case Statement.Mirror mi: mirror(E(mi.Dist), mi.Back); break;
            case Statement.Copy cl: clone(E(cl.Dist), cl.Back); break;
            case Statement.Offset o:
            {
                double d = E(o.Dist), x = E(o.By.X), y = E(o.By.Y);
                offset(d, x, y, o.Back);
                break;
            }
            case Statement.Repeat re: repeat(E(re.Dist), (int)E(re.Times), re.Count, re.Mirrored); break;
            case Statement.Insert ins: insert(ins.At); break;
            case Statement.Wait w: wait(E(w.Dist)); break;
            case Statement.Jump j: jump(E(j.Dist)); break;
            case Statement.Loop lo: loopBack(lo.From); break;
            default: throw new InvalidOperationException($"Unknown statement {s}");
        }
    }

    T Add<T>(T e) where T : LevelEvent
    {
        e.File = file;
        e.Line = line;
        array.Add(e);
        return e;
    }

    // ---- The original LevelData helpers. Lowercase names kept so they compare easily with the AS3. ----

    void setDist(double d)
    {
        if (d >= c.Tuning.ShrinkFrom) d *= mult;
        dist += d;
    }

    void loopBack(int j = 0)
    {
        if (j < 0) j = array.Count + j;
        Add(new MarkerEvent(dist, MarkerEvent.Kind.Loop, j));
    }

    void wait(double d = 0)
    {
        setDist(d);
        Add(new MarkerEvent(dist, MarkerEvent.Kind.Wait, d));
    }

    void jump(double j = 0) => Add(new MarkerEvent(dist, MarkerEvent.Kind.Jump, j));

    void laser(string type, double d, double x, double y, int speed = 0)
    {
        ObjectCommand[] commands;
        if (x == 0) commands = [move(2, y), halt(10 - speed), halt(20 - speed, true), move(-2, y)];
        else if (x == 12) commands = [move(10, y), halt(10 - speed), halt(20 - speed, true), move(14, y)];
        else if (y == 0) commands = [move(x, 2), halt(10 - speed), halt(20 - speed, true), move(x, -2)];
        else if (y == 12) commands = [move(x, 10), halt(10 - speed), halt(20 - speed, true), move(x, 14)];
        else throw new ArgumentException($"A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at {x},{y}.");
        ship($"{type[0]}l{type[1]}", d, x, y, commands);
    }

    void suicide(string type, double d, double x, double y, double xTo, double yTo) =>
        ship($"{type[0]}s{type[1]}", d, x, y, [move(xTo, yTo), fire()]);

    void stealth(double d, double x, double y, double xTo, double yTo, bool shield = false) =>
        ship(shield ? "xms" : "xmn", d, x, y,
            [halt(8), halt(10, true), halt(10, true), halt(10, true), halt(10, true), halt(5, true), move(xTo, yTo)]);

    void missile(string type, double d, double x, double y, double xTo1, double yTo1,
        double xTo2 = -1, double yTo2 = -1, double xTo3 = -1, double yTo3 = -1) =>
        ship($"{type[0]}m{type[1]}", d, x, y,
        [
            move(xTo1, yTo1),
            move(xTo2, yTo2, xTo3 == -1),
            xTo3 != -1 ? move(xTo3, yTo3, true) : halt(1),
        ]);

    /// <summary>Move the last event back to position j, at that event's distance.</summary>
    void insert(int j)
    {
        int at = j < 0 ? array.Count + j : j;
        if (at < 0 || at >= array.Count)
            throw new ArgumentException($"insert {j}: there are only {array.Count} events above this line.");
        var last = array[^1];
        last.Distance = array[at].Distance; // before removing it: insert -1 leaves it where it is
        array.RemoveAt(array.Count - 1);
        array.Insert(at, last);
    }

    void ship(string type, double d, double x, double y, ObjectCommand[]? commands = null)
    {
        setDist(d);
        Add(new SpawnEvent(c, dist, type, (float)(x * Playfield.GridX), (float)(y * Playfield.GridY), commands));
    }

    void boss(double d, int type)
    {
        setDist(d);
        Add(new BossEvent(dist, type));
    }

    SpawnEvent At(int j, string what)
    {
        int at = j < 0 ? array.Count + j : j;
        if (at < 0 || at >= array.Count)
            throw new ArgumentException($"{what} copies the ship {-j} back, but there {(array.Count == 1 ? "is only 1 event" : $"are only {array.Count} events")} above this line.");
        return array[at] as SpawnEvent
            ?? throw new ArgumentException($"{what} copies the ship {-j} back, but that's a {(array[at] is BossEvent ? "boss" : "wait/jump/loop")}, not a ship.");
    }

    void clone(double d = 0, int j = -1)
    {
        var src = At(j, "clone");
        setDist(d);
        Add(new SpawnEvent(c, dist, src.Type, src.X, src.Y, src.Commands.Select(cmd => cmd.Clone()).ToArray()));
    }

    void mirror(double d = 0, int j = -1)
    {
        var src = At(j, "mirror");
        setDist(d);
        Add(new SpawnEvent(c, dist, src.Type, Playfield.ViewWidth - src.X, src.Y, src.Commands.Select(cmd => cmd.Mirror()).ToArray()));
    }

    void offset(double d = 0, double xOff = 0, double yOff = 0, int j = -1)
    {
        var src = At(j, "offset");
        setDist(d);
        float x = (float)(xOff * Playfield.GridX), y = (float)(yOff * Playfield.GridY);
        Add(new SpawnEvent(c, dist, src.Type, src.X + x, src.Y + y, src.Commands.Select(cmd => cmd.Offset(x, y)).ToArray()));
    }

    /// <summary>Repeat the last repeatC spawns repeatN times (mirrored if reverse), keeping their spacing.</summary>
    void repeat(double d, int repeatN = 1, int repeatC = 1, bool reverse = false)
    {
        if (repeatC > array.Count)
            throw new ArgumentException($"repeat copies the last {repeatC} ship(s), but there {(array.Count == 1 ? "is only 1 event" : $"are only {array.Count} events")} above this line.");
        for (int k = 0; k < repeatN * repeatC; k++)
        {
            double step = k % repeatC == 0
                ? d
                : array[^repeatC].Distance - array[^(repeatC + 1)].Distance;
            if (reverse) mirror(step, -repeatC);
            else clone(step, -repeatC);
        }
    }

    ObjectCommand move(double x, double y, bool fire = false) =>
        new(c, (float)(x * Playfield.GridX), (float)(y * Playfield.GridY), 0, true, fire);

    ObjectCommand halt(int timer, bool fire = false) =>
        new(c, Playfield.ViewWidth / 2, Playfield.ViewHeight - 100, timer, false, fire);

    ObjectCommand fire() => new(c, 0, 0, 1, false, true);
}
