using Godot;

namespace TypingShip;

// Dev aid (--autotype): types like a player at the selected WPM, so levels can be played through unattended.
// Plays earliest-deadline-first: it estimates how many ticks each threat has before it hurts (a missile or suicide
// ship reaching the player, a laser finishing its charge, boss 2's beam), types the most pressing one it can still
// finish in time, repairs the hull when there's slack, and otherwise clears the nearest ship (turret first) or
// works on the boss scanner.
public partial class DevAutoTyper : Node
{
    public int BotWpm; // 0 = type at the game's WPM
    float charge;
    string? word;
    GameObject? owner;
    int typed;

    const float Never = 100000;

    float Rate => (BotWpm > 0 ? BotWpm : G.Model.Wpm) * 5f / 60f / G.TickRate; // letters per tick

    public override void _PhysicsProcess(double delta)
    {
        var m = G.Model;
        if (!m.Running || !m.CanType) return;
        charge += Rate;
        while (charge >= 1)
        {
            charge -= 1;
            if (word == null || owner is GameSprite { Removed: true } || owner is Scanner { TextObject: null } ||
                owner?.TextObject?.Text != word || typed >= word.Length || Hopeless(owner, word.Length - typed))
                Pick();
            if (word == null) return;
            GetParent().GetNode<KeyboardControl>("Keyboard").TypeLetter(word[typed++].ToString());
            // Leftover letters plus a word can spell "pause" (the original's pause command); a player would just resume.
            if (!m.Running && !G.Ctrl.LevelEnded) G.Ctrl.PauseGame(false);
        }
    }

    /// <summary>A threat that will hurt before its word can be finished; better to spend the letters elsewhere.</summary>
    bool Hopeless(GameObject o, int letters) => Deadline(o) < letters / Rate;

    void Pick()
    {
        var m = G.Model;
        GameObject? best = null, nearest = null;
        float bestDeadline = Never, nearestDist = float.MaxValue;
        foreach (var o in m.GObjects)
        {
            var target = o.Turret is { TextObject: not null } t ? t : (GameObject)o; // a turret shoots down missiles
            if (target.TextObject == null || o.Modulate.A < 0.5f) continue;
            if (o.Position.X < 25 || o.Position.X > 575 || o.Position.Y < 25) continue; // still off screen
            float need = target.TextObject.Text.Length / Rate;
            float deadline = Deadline(o);
            if (deadline < Never && deadline >= need && deadline < bestDeadline)
            {
                bestDeadline = deadline;
                best = target;
            }
            float dist = o.Position.DistanceTo(m.PShip.Position);
            if (dist < nearestDist && deadline >= need)
            {
                nearestDist = dist;
                nearest = target;
            }
        }

        // Repair when the next threat leaves time to.
        if (m.Hull.TextObject is { } heal && bestDeadline > heal.Text.Length / Rate * 1.5f)
            best = m.Hull;
        else if (best == null)
            best = m.Boss?.Scanner is { TextObject: not null } scanner && nearestDist > 200 ? scanner : nearest;

        owner = best;
        word = best?.TextObject?.Text;
        typed = 0;
    }

    /// <summary>Roughly how many ticks until this object hurts the player (Never if it won't directly).</summary>
    float Deadline(GameObject o)
    {
        var m = G.Model;
        var player = m.PShip.Position;
        switch (o)
        {
            case Turret t:
                return Deadline((GameObject)t.GetParent());
            case Missile mi when mi.WordSize == 3:
                return mi.Position.DistanceTo(player) / Math.Max(mi.A * 10, 0.1f);
            case EnemyShip { Fires: ActionControl.Suicide } s:
            {
                float speed = Math.Max(s.A * 10, 0.1f);
                if (s.Priority >= 3) return s.Position.DistanceTo(player) / speed;
                var stage = new Vector2(s.Commands[0].X, s.Commands[0].Y);
                return s.Position.DistanceTo(stage) / speed + stage.DistanceTo(player) / (speed * 2);
            }
            case EnemyShip { Fires: ActionControl.Laser, Charge: { } c } s:
            {
                if (c.Remaining >= 0) return c.Remaining;
                // Not charging yet: time to fly in and sit out the halts before the firing step, plus the charge.
                float ticks = 6000f / m.Wpm, speed = Math.Max(s.A * 10, 0.1f);
                var pos = s.Position;
                foreach (var cmd in s.Commands)
                {
                    if (cmd.Fire) return ticks;
                    if (cmd.Move)
                    {
                        var to = new Vector2(cmd.X, cmd.Y);
                        ticks += pos.DistanceTo(to) / speed;
                        pos = to;
                    }
                    else ticks += cmd.Timer;
                }
                return Never; // already fired
            }
            case GameSprite { KillBy: ActionControl.Instant }:
                return m.Boss?.BeamTicksLeft is > 0 and var left ? left : Never;
            default:
                return Never;
        }
    }
}
