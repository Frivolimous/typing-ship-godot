namespace TypingShip.Core;

/// <summary>
/// Dev aid (--autotype, the simulator): types like a player at a set WPM and never mistypes, so levels can be
/// played through unattended. It plays earliest-deadline-first: it estimates how many ticks each threat has before
/// it hurts (a missile or suicide ship reaching the player, a laser finishing its charge, boss 3's beam), types the
/// most pressing one it can still finish in time, repairs the hull when there's slack, and otherwise clears the
/// nearest ship (turret first) or works on the boss scanner. A yardstick for "can a typist at this speed survive
/// this", not a model of a human.
/// </summary>
public sealed class AutoTyper
{
    readonly Game game;
    /// <summary>Characters per second; 0 = type at the game's speed.</summary>
    public double BotCps;
    float charge;
    string? word;
    Entity? owner;
    int typed;

    const float Never = 100000;

    public AutoTyper(Game game, double botCps = 0)
    {
        this.game = game;
        BotCps = botCps;
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
            if (word == null) return;
            m.TypeLetter(word[typed++]);
            // Leftover letters plus a word can spell "pause" (the original's pause command); a player would just resume.
            if (!m.Running && !m.LevelEnded) m.Resume();
        }
    }

    /// <summary>A threat that will hurt before its word can be finished; better to spend the letters elsewhere.</summary>
    bool Hopeless(Entity o, int letters) => Deadline(o) < letters / Rate;

    void Pick()
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

        // Repair when the next threat leaves time to.
        if (m.Hull.Word is { } heal && bestDeadline > heal.Length / Rate * 1.5f)
            best = m.Hull;
        else if (best == null)
            best = m.Boss?.Scanner is { Word: not null } scanner && nearestDist > 200 ? scanner : nearest;

        owner = best;
        word = best?.Word;
        typed = 0;
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
                float ticks = (float)(m.Tuning.LaserCharge / m.Cps), speed = Math.Max(s.A * 10, 0.1f);
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
