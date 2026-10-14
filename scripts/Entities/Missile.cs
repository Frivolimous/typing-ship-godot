using Godot;

namespace TypingShip;

public partial class Missile : GameSprite
{
    GameSprite target = null!;
    int delay;

    /// <param name="n">Launch heading; NaN means "the origin's rotation".</param>
    public void Setup(GameSprite origin, GameSprite target, float n = float.NaN, int delay = 0)
    {
        this.target = target;
        float speed, heading;
        if (origin == G.Model.PShip)
        {
            ViewSource = Sprites.PMissile;
            speed = 0.1f * G.Model.Wpm;
            WordSize = -1;
            heading = float.IsNaN(n) ? origin.Rotation : n;
        }
        else
        {
            KillBy = ActionControl.Laser;
            Value = G.Model.BossMode ? 0 : 1;
            ViewSource = Sprites.EMissile;
            speed = 0.008f * G.Model.Wpm;
            WordSize = 3;
            heading = origin.Rotation;
        }
        SpriteScale = 0.1f;
        Init(origin.Position.X, origin.Position.Y, heading, speed, 3);
        TurnRate = speed / 20;
        this.delay = delay;
    }

    public override bool MoreUpdate()
    {
        if (delay > 0)
        {
            delay--;
            Halt = delay > 0;
            return true;
        }
        TurnRate += A / 300;
        target.Turret?.TestTarget(this);
        if (Removed) return false; // shot down by the turret

        var t = target.P;
        HomeTarget(t);
        var d = Position - t;
        if ((Math.Abs(d.X) <= Math.Abs(VX * 1.5f) && Math.Abs(d.Y) <= Math.Abs(VY * 1.5f)) ||
            (Math.Abs(d.X) <= 30 && Math.Abs(d.Y) <= 30))
        {
            G.Action.HitTarget(target, ActionControl.Missile);
            if (target == G.Model.PShip) G.Starfield.Explosion(Position.X, Position.Y, 10);
            Remove();
            return false;
        }
        return true;
    }
}
