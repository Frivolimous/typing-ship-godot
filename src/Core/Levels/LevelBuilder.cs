using TypingShip.Core.Data;

namespace TypingShip.Core.Levels;

/// <summary>
/// Turns a level script into its list of events for one play: positions on the 12 x 12 grid become pixels, ships
/// get their flight plans, mirror/repeat/insert copy and shuffle events, and each line's time (seconds, the same at
/// every difficulty) is worked out from its @ time or + gap. Random numbers are drawn in the order they're written,
/// as the original's LevelData helpers drew them.
/// </summary>
public sealed class LevelBuilder
{
    readonly LevelContext c;
    readonly List<LevelEvent> array = new();
    readonly double start;
    double time;
    string file = "";
    int line;

    LevelBuilder(LevelContext c, double startTime)
    {
        this.c = c;
        start = time = startTime;
    }

    /// <summary>
    /// Builds a script's events. A level starts at 0; a boss's escorts start where the level is (startTime), and
    /// their @ times count from there.
    /// </summary>
    public static List<LevelEvent> Build(LevelScript script, LevelContext c, double startTime = 0)
    {
        var b = new LevelBuilder(c, startTime) { file = script.File };
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

    /// <summary>Moves the clock to the line's time.</summary>
    void When(Timing t)
    {
        switch (t.Type)
        {
            case Timing.Kind.At: time = start + t.At; break;
            case Timing.Kind.After: time += E(t.Gap!); break;
        }
    }

    void Run(Statement s)
    {
        // The time comes first, then the line's numbers in the order they're written (as the original evaluated
        // its arguments, so random numbers are drawn in the same order).
        if (s.Moment is { } when) When(when);
        switch (s)
        {
            case Statement.Missile m:
            {
                double x = E(m.From.X), y = E(m.From.Y);
                var to = new double[6];
                Array.Fill(to, -1);
                for (int i = 0; i < m.To.Length; i++)
                {
                    to[i * 2] = E(m.To[i].X);
                    to[i * 2 + 1] = E(m.To[i].Y);
                }
                missile(m.Type, x, y, to[0], to[1], to[2], to[3], to[4], to[5]);
                break;
            }
            case Statement.Laser l:
            {
                double x = E(l.At.X), y = E(l.At.Y), speed = E(l.Speed);
                laser(l.Type, x, y, (int)speed);
                break;
            }
            case Statement.Suicide su:
            {
                double x = E(su.From.X), y = E(su.From.Y), xTo = E(su.To.X), yTo = E(su.To.Y);
                suicide(su.Type, x, y, xTo, yTo);
                break;
            }
            case Statement.Stealth st:
            {
                double x = E(st.From.X), y = E(st.From.Y), xTo = E(st.To.X), yTo = E(st.To.Y);
                stealth(x, y, xTo, yTo, st.Shield);
                break;
            }
            case Statement.Ship sh:
            {
                double x = E(sh.From.X), y = E(sh.From.Y);
                var commands = sh.Steps.Select(step => step.Type switch
                {
                    Step.Kind.Move => move(E(step.To!.X), E(step.To.Y), step.Fire),
                    Step.Kind.Halt => halt(ObjectCommand.UnitsFromSeconds(E(step.Halt!), c.Tuning), step.Fire),
                    _ => fire(),
                }).ToArray();
                ship(sh.Code, x, y, commands);
                break;
            }
            case Statement.Boss bo: Add(new BossEvent(time, bo.Number - 1)); break;
            case Statement.Mirror mi: mirror(mi.Back); break;
            case Statement.Copy cl: clone(cl.Back); break;
            case Statement.Offset o:
            {
                double x = E(o.By.X), y = E(o.By.Y);
                offset(x, y, o.Back);
                break;
            }
            case Statement.Repeat re: repeat(E(re.Gap), (int)E(re.Times), re.Count, re.Mirrored); break;
            case Statement.Insert ins: insert(ins.At); break;
            case Statement.Wait: Add(new MarkerEvent(time, MarkerEvent.Kind.Wait, 0)); break;
            case Statement.Jump j: Add(new MarkerEvent(time, MarkerEvent.Kind.Jump, E(j.Seconds))); break;
            case Statement.Loop lo: Add(new MarkerEvent(time, MarkerEvent.Kind.Loop, lo.From < 0 ? array.Count + lo.From : lo.From)); break;
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

    void laser(string type, double x, double y, int speed = 0)
    {
        ObjectCommand[] commands;
        if (x == 0) commands = [move(2, y), halt(10 - speed), halt(20 - speed, true), move(-2, y)];
        else if (x == 12) commands = [move(10, y), halt(10 - speed), halt(20 - speed, true), move(14, y)];
        else if (y == 0) commands = [move(x, 2), halt(10 - speed), halt(20 - speed, true), move(x, -2)];
        else if (y == 12) commands = [move(x, 10), halt(10 - speed), halt(20 - speed, true), move(x, 14)];
        else throw new ArgumentException($"A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at {x},{y}.");
        ship($"{type[0]}l{type[1]}", x, y, commands);
    }

    void suicide(string type, double x, double y, double xTo, double yTo) =>
        ship($"{type[0]}s{type[1]}", x, y, [move(xTo, yTo), fire()]);

    void stealth(double x, double y, double xTo, double yTo, bool shield = false) =>
        ship(shield ? "xms" : "xmn", x, y,
            [halt(8), halt(10, true), halt(10, true), halt(10, true), halt(10, true), halt(5, true), move(xTo, yTo)]);

    void missile(string type, double x, double y, double xTo1, double yTo1,
        double xTo2 = -1, double yTo2 = -1, double xTo3 = -1, double yTo3 = -1) =>
        ship($"{type[0]}m{type[1]}", x, y,
        [
            move(xTo1, yTo1),
            move(xTo2, yTo2, xTo3 == -1),
            xTo3 != -1 ? move(xTo3, yTo3, true) : halt(1),
        ]);

    /// <summary>Move the last event back to position j, at that event's time.</summary>
    void insert(int j)
    {
        int at = j < 0 ? array.Count + j : j;
        if (at < 0 || at >= array.Count)
            throw new ArgumentException($"insert {j}: there are only {array.Count} events above this line.");
        var last = array[^1];
        last.Time = array[at].Time; // before removing it: insert -1 leaves it where it is
        array.RemoveAt(array.Count - 1);
        array.Insert(at, last);
    }

    void ship(string type, double x, double y, ObjectCommand[]? commands = null) =>
        Add(new SpawnEvent(c, time, type, (float)(x * Playfield.GridX), (float)(y * Playfield.GridY), commands));

    SpawnEvent At(int j, string what)
    {
        int at = j < 0 ? array.Count + j : j;
        if (at < 0 || at >= array.Count)
            throw new ArgumentException($"{what} copies the ship {-j} back, but there {(array.Count == 1 ? "is only 1 event" : $"are only {array.Count} events")} above this line.");
        return array[at] as SpawnEvent
            ?? throw new ArgumentException($"{what} copies the ship {-j} back, but that's a {(array[at] is BossEvent ? "boss" : "wait/jump/loop")}, not a ship.");
    }

    void clone(int j = -1)
    {
        var src = At(j, "clone");
        Add(new SpawnEvent(c, time, src.Type, src.X, src.Y, src.Commands.Select(cmd => cmd.Clone()).ToArray()));
    }

    void mirror(int j = -1)
    {
        var src = At(j, "mirror");
        Add(new SpawnEvent(c, time, src.Type, Playfield.ViewWidth - src.X, src.Y, src.Commands.Select(cmd => cmd.Mirror()).ToArray()));
    }

    void offset(double xOff = 0, double yOff = 0, int j = -1)
    {
        var src = At(j, "offset");
        float x = (float)(xOff * Playfield.GridX), y = (float)(yOff * Playfield.GridY);
        Add(new SpawnEvent(c, time, src.Type, src.X + x, src.Y + y, src.Commands.Select(cmd => cmd.Offset(x, y)).ToArray()));
    }

    /// <summary>
    /// Repeat the last repeatC spawns repeatN times (mirrored if reverse): each group starts gap seconds after the
    /// one before, and keeps the spacing its ships had.
    /// </summary>
    void repeat(double gap, int repeatN = 1, int repeatC = 1, bool reverse = false)
    {
        if (repeatC > array.Count)
            throw new ArgumentException($"repeat copies the last {repeatC} ship(s), but there {(array.Count == 1 ? "is only 1 event" : $"are only {array.Count} events")} above this line.");
        for (int k = 0; k < repeatN * repeatC; k++)
        {
            time += k % repeatC == 0 ? gap : array[^repeatC].Time - array[^(repeatC + 1)].Time;
            if (reverse) mirror(-repeatC);
            else clone(-repeatC);
        }
    }

    ObjectCommand move(double x, double y, bool fire = false) =>
        new(c, (float)(x * Playfield.GridX), (float)(y * Playfield.GridY), 0, true, fire);

    ObjectCommand halt(double units, bool fire = false) =>
        new(c, Playfield.ViewWidth / 2, Playfield.ViewHeight - 100, units, false, fire);

    ObjectCommand fire() => new(c, 0, 0, 1, false, true);
}
