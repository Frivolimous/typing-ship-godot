namespace TypingShip.Core;

/// <summary>
/// Dev aid (--autotype, the simulator): types like a player at a set WPM and never mistypes, so levels can be
/// played through unattended. It plays earliest-deadline-first: it estimates how many ticks each threat has before
/// it hurts (a missile or suicide ship reaching the player, a laser finishing its charge, boss 3's beam), types the
/// most pressing one it can still finish in time, otherwise clears the nearest ship (turret first) or works on the
/// boss scanner, and repairs the hull only when there's nothing else to type. A yardstick for "can a typist at this speed survive
/// this", not a model of a human.
/// </summary>
public sealed class AutoTyper
{
    readonly Game game;
    /// <summary>Characters per second; 0 = type at the game's speed.</summary>
    public double BotCps;
    /// <summary>
    /// Share of keystrokes that hit a wrong letter (0 = never). A mistake costs the next keystroke too: the bot
    /// presses Backspace, like a player correcting it. The mistakes come from the bot's own random numbers, so the
    /// game's are untouched.
    /// </summary>
    public double ErrorRate;
    readonly Rng errors;
    bool mistyped;
    float charge;
    string? word;
    Entity? owner;
    int typed;

    const float Never = 100000;

    public AutoTyper(Game game, double botCps = 0, double errorRate = 0, ulong errorSeed = 1)
    {
        this.game = game;
        BotCps = botCps;
        ErrorRate = errorRate;
        errors = new Rng(errorSeed);
    }

    float Rate => (float)(BotCps > 0 ? BotCps : game.Cps) / Playfield.TickRate; // letters per tick

    /// <summary>Call once per tick, after the game's Step.</summary>
    public void Step()
    {
        var m = game;
        if (!m.Running || !m.CanType) return;
        charge += Rate;
        while (charge >= 1)
        {
            charge -= 1;
            if (word == null || owner is Sprite { Removed: true } || owner is Scanner { Word: null } ||
                owner?.Word != word || typed >= word.Length || Hopeless(owner, word.Length - typed))
                Pick();
            else if (owner == m.Hull && Choose(includeHull: false) is { } other)
                Take(other); // something to type turned up: the repair can wait
            if (word == null) return;
            if (mistyped)
            {
                m.Backspace();
                mistyped = false;
                continue;
            }
            if (ErrorRate > 0 && errors.Randf() < ErrorRate)
            {
                char wrong;
                do wrong = (char)('a' + (int)(errors.Randf() * 26) % 26);
                while (wrong == word[typed]);
                m.TypeLetter(wrong);
                mistyped = true;
                continue;
            }
            m.TypeLetter(word[typed++]);
            // Leftover letters plus a word can spell "pause" (the original's pause command); a player would just resume.
            if (!m.Running && !m.LevelEnded) m.Resume();
        }
    }

    /// <summary>A threat that will hurt before its word can be finished; better to spend the letters elsewhere.</summary>
    bool Hopeless(Entity o, int letters) => Deadline(o) < letters / Rate;

    void Pick() => Take(Choose(includeHull: true));

    void Take(Entity? target)
    {
        owner = target;
        word = target?.Word;
        typed = 0;
    }

    /// <summary>What to type next (see the class summary for the order), or null for nothing.</summary>
    Entity? Choose(bool includeHull)
    {
        var m = game;
        Entity? best = null, nearest = null;
        float bestDeadline = Never, nearestDist = float.MaxValue;
        foreach (var o in m.Objects)
        {
            var target = o.Turret is { Word: not null } t ? t : (Entity)o; // a turret shoots down missiles
            if (target.Word == null || o.Alpha < 0.5f) continue;
            if (o.Position.X < 25 || o.Position.X > 575 || o.Position.Y < 25) continue; // still off screen
            float need = target.Word.Length / Rate;
            float deadline = Deadline(o);
            if (deadline < Never && deadline >= need && deadline < bestDeadline)
            {
                bestDeadline = deadline;
                best = target;
            }
            float dist = o.Position.DistanceTo(m.Player.Position);
            if (dist < nearestDist && deadline >= need)
            {
                nearestDist = dist;
                nearest = target;
            }
        }

        if (best == null)
            best = m.Boss?.Scanner is { Word: not null } scanner && nearestDist > 200 ? scanner : nearest;
        // The hull's repair word comes last of all: only when there's nothing else to type.
        if (includeHull) best ??= m.Hull.Word != null ? m.Hull : null;
        return best;
    }

    /// <summary>Roughly how many ticks until this object hurts the player (Never if it won't directly).</summary>
    float Deadline(Entity o)
    {
        var m = game;
        var player = m.Player.Position;
        switch (o)
        {
            case Turret t:
                return Deadline(t.Ship);
            case Missile mi when mi.WordSize == 3:
                return mi.Position.DistanceTo(player) / Math.Max(mi.A * 10, 0.1f);
            case EnemyShip { Fires: Weapons.Suicide } s:
            {
                float speed = Math.Max(s.A * 10, 0.1f);
                if (s.Priority >= 3) return s.Position.DistanceTo(player) / speed;
                var stage = new Vec2(s.Commands[0].X, s.Commands[0].Y);
                return s.Position.DistanceTo(stage) / speed + stage.DistanceTo(player) / (speed * 2);
            }
            case EnemyShip { Fires: Weapons.Laser, Charge: { } c } s:
            {
                if (c.Remaining >= 0) return c.Remaining;
                // Not charging yet: time to fly in and sit out the halts before the firing step, plus the charge.
                float ticks = (float)(m.Tuning.LaserCharge / m.Pace), speed = Math.Max(s.A * 10, 0.1f);
                var pos = s.Position;
                foreach (var cmd in s.Commands)
                {
                    if (cmd.Fire) return ticks;
                    if (cmd.Move)
                    {
                        var to = new Vec2(cmd.X, cmd.Y);
                        ticks += pos.DistanceTo(to) / speed;
                        pos = to;
                    }
                    else ticks += cmd.Timer;
                }
                return Never; // already fired
            }
            case Sprite { KillBy: Weapons.Instant }:
                return m.Boss?.BeamTicksLeft is > 0 and var left ? left : Never;
            default:
                return Never;
        }
    }
}
