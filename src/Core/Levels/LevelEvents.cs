using TypingShip.Core.Data;

namespace TypingShip.Core.Levels;

/// <summary>What building a level needs: the random numbers, the tuning, and the difficulty (its speed in CPS).</summary>
public sealed record LevelContext(Rng Rng, Tuning Tuning, double Cps)
{
    /// <summary>0 (easy) to 4 (insane).</summary>
    public int DifficultyIndex => Difficulty.Of(Cps) - 1;
    /// <summary>The speed ships move and halts count down at: Cps, flattened above Hard (Tuning.Pace).</summary>
    public double Pace => Tuning.Pace(Cps);
}

/// <summary>Something in a level that happens at Time: seconds from the start of the level, the same at every difficulty.</summary>
public abstract class LevelEvent
{
    public double Time;
    /// <summary>Where it came from (for the editing overlay and "start from line N"), or 0.</summary>
    public string File = "";
    public int Line;
    /// <summary>The easiest difficulty it's on (0 Easy to 4 Insane): its line's tag, or the tag of the ship it copies.</summary>
    public int Easiest;
}

/// <summary>The original's LevelEvent: Loop (re-run the level from event Index), Wait, or Jump (skip ahead Index seconds).</summary>
public sealed class MarkerEvent : LevelEvent
{
    public enum Kind { Loop, Wait, Jump }

    public Kind Type;
    public double Index;

    public MarkerEvent(double time, Kind type, double index)
    {
        Time = time;
        Type = type;
        Index = index;
    }
}

/// <summary>Brings in a boss (0, 1 or 2: B1, B2, B3 on the map).</summary>
public sealed class BossEvent : LevelEvent
{
    public int Type;

    public BossEvent(double time, int type)
    {
        Time = time;
        Type = type;
    }
}

/// <summary>
/// An enemy ship joining. Ship codes are three letters: size, weapon, defence.
///   size:    [s]mall, [m]edium, [l]arge, x = stealth, n = invisible laser
///   weapon:  [m]issile, [l]aser, [s]uicide (small = landmine, medium = suicide ship)
///   defence: [s]hield, [t]urret, [b]oth, [n]one
/// The numbers each letter gives are in data/tuning.txt.
/// </summary>
public sealed class SpawnEvent : LevelEvent
{
    public string Type;
    /// <summary>The first two letters, which pick the ship's art and health.</summary>
    public string Hull => Type[..2];
    public float X, Y;
    public ObjectCommand[] Commands;
    public float A;
    public int Fires, KillBy = Weapons.Missile, Value, WordSize, Health;
    public bool Shielded, HasTurret;

    public SpawnEvent(LevelContext c, double time, string t, float x, float y, ObjectCommand[]? commands = null)
    {
        var tune = c.Tuning;
        Type = t;
        Time = time;
        X = x;
        Y = y;
        Commands = commands ?? Array.Empty<ObjectCommand>();
        if (t.Length != 3 || !tune.HullHealth.TryGetValue(t[..2], out Health))
            throw new ArgumentException($"\"{t}\" is not a ship code: the first two letters must be one of the hulls in tuning.txt " +
                $"({string.Join(", ", tune.HullHealth.Keys)}).");
        if (t == "nl" + t[2]) KillBy = Weapons.Laser;

        A = tune.BaseSpeed;
        Value = tune.BasePoints;
        var size = tune.Sizes[t[0]];
        WordSize = size.Letters;
        Value += size.Points;
        A += size.Speed;

        switch (t[1])
        {
            case 'm': Fires = Weapons.Missile; break;
            case 'l':
                Fires = Weapons.Laser;
                WordSize += tune.LaserExtraLetters;
                Value += tune.LaserPoints;
                A += tune.LaserSpeed;
                break;
            case 's':
                Fires = Weapons.Suicide;
                WordSize = tune.SuicideLetters;
                KillBy = Weapons.Laser;
                Value += tune.SuicidePoints;
                A += t[0] == 's' ? tune.MineSpeed : tune.SuicideSpeed;
                break;
        }
        if (t[0] == 'x') Fires = Weapons.AutoMissile;

        if (!tune.DefencePoints.TryGetValue(t[2], out int extra))
            throw new ArgumentException($"\"{t}\": the 3rd letter (defence) must be one of {string.Join(", ", tune.DefencePoints.Keys)}.");
        Value += extra;
        switch (t[2])
        {
            case 's': Shielded = true; break;
            case 't': HasTurret = true; break;
            case 'b': Shielded = true; HasTurret = true; break;
        }
        A *= (float)(c.Pace * tune.SpeedPerCps);
    }
}

/// <summary>
/// One step of a ship's script: fly to (x, y), or wait timer ticks; optionally fire when the step starts.
/// Halts are given in "halt units" (the original's), timer x halt scale / cps ticks; a halt written in seconds as
/// played on Hard is <see cref="UnitsFromSeconds"/> units, so it lasts that long on Hard and scales with cps elsewhere.
/// Targets inside the view get up to ±jitter px, as in the original (re-rolled on clone/mirror/offset).
/// </summary>
public sealed class ObjectCommand
{
    public float X, Y;
    public int Timer;
    public bool Fire, Move;
    readonly LevelContext c;

    public ObjectCommand(LevelContext c, float x = 0, float y = 0, double timer = 0, bool move = true, bool fire = false)
    {
        this.c = c;
        float j = c.Tuning.Jitter;
        X = x > 0 && x < Playfield.ViewWidth ? Math.Clamp(x + c.Rng.Rand() * (j * 2) - j, 0, Playfield.ViewWidth) : x;
        Y = y > 0 && y < Playfield.ViewHeight ? Math.Clamp(y + c.Rng.Rand() * (j * 2) - j, 0, Playfield.ViewHeight) : y;
        Timer = Speed.FloorTicks(timer * c.Tuning.HaltScale / c.Pace);
        Move = move;
        Fire = fire;
    }

    /// <summary>Is this step done? A halt counts its timer down.</summary>
    public bool Condition(Sprite v)
    {
        if (Move)
        {
            float d = c.Tuning.ArriveDistance;
            return Math.Abs(v.Position.X - X) < d && Math.Abs(v.Position.Y - Y) < d;
        }
        if (Timer <= 0) return true;
        Timer--;
        return false;
    }

    // Back to halt units, rounding down (so a copy can wait a tick less, as in the original).
    int BaseTimer => Speed.FloorTicks(Timer * c.Pace / c.Tuning.HaltScale);

    public ObjectCommand Clone() => new(c, X, Y, BaseTimer, Move, Fire);
    public ObjectCommand Mirror() => new(c, Playfield.ViewWidth - X, Y, BaseTimer, Move, Fire);
    public ObjectCommand Offset(float xOff, float yOff) => new(c, X + xOff, Y + yOff, BaseTimer, Move, Fire);

    /// <summary>Halt units for a halt of this many seconds on Hard.</summary>
    public static double UnitsFromSeconds(double seconds, Tuning t) => seconds * Playfield.TickRate * Difficulty.Hard / t.HaltScale;
}
