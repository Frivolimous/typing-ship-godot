namespace TypingShip.Core;

// Weapons, damage and scoring (the original's ActionControl).
public sealed partial class Game
{
    public void ShootMissile(Sprite o, Sprite t, bool auto = false)
    {
        if (o == Player)
        {
            // One missile per point of the target's health, fanned out and staggered.
            // Each later missile waits n x 100 / wpm ticks (100 / 12 / cps).
            int Stagger(int n) => Speed.CeilingTicks(n * 100 / 12.0 / Cps);
            switch (t.Health)
            {
                case 4:
                    LaunchPlayerMissile(t, -MathF.PI * 5 / 6);
                    LaunchPlayerMissile(t, -MathF.PI / 6, Stagger(1));
                    LaunchPlayerMissile(t, MathF.PI * 3 / 4, Stagger(2));
                    LaunchPlayerMissile(t, MathF.PI / 4, Stagger(3));
                    break;
                case 3:
                    LaunchPlayerMissile(t, -MathF.PI / 2);
                    LaunchPlayerMissile(t, MathF.PI * 3 / 4, Stagger(2));
                    LaunchPlayerMissile(t, MathF.PI / 4, Stagger(3));
                    break;
                case 2:
                    LaunchPlayerMissile(t, -MathF.PI, Stagger(1));
                    LaunchPlayerMissile(t, 0);
                    break;
                case 1:
                    LaunchPlayerMissile(t);
                    break;
            }
            return;
        }

        // Enemies build up to a shot; at low typing speeds most fire commands come to nothing.
        if (auto) MCount += 1;
        else
            MCount += o.Health switch
            {
                2 => MRate * Tuning.FireRateHealth2,
                3 or 4 => MRate * Tuning.FireRateHealth3,
                _ => MRate,
            };

        if (MCount > 1)
        {
            o.Priority = 0;
            o.Value -= 1;
            MCount -= 1;
            var m = new Missile(this);
            m.Setup(o, t);
            Objects.Add(m);
            Events.Spawned(m, Layer.Front);
            ShowTutorial(7, m.P);
        }
    }

    void LaunchPlayerMissile(Sprite target, float n = float.NaN, int delay = 0)
    {
        var m = new Missile(this);
        m.Setup(Player, target, n, delay);
        Objects.Insert(0, m);
        Events.Spawned(m, Layer.Back);
    }

    public void ShootLaser(Sprite o, Sprite t, bool fire = false)
    {
        if (o == Player)
        {
            Events.Beam(o.P, t is Turret turret ? turret.Ship.Position : t.P, BeamKind.PlayerLaser);
            HitTarget(t, Weapons.Laser);
            Events.Sound(GameSound.Laser);
        }
        else if (fire)
        {
            Events.Beam(o.P, t.P, BeamKind.EnemyLaser);
            HitTarget(t, Weapons.Laser);
            Events.Explosion(t.P, 10);
            Events.Sound(GameSound.Laser);
            o.Priority = 0;
        }
        else
        {
            ((EnemyShip)o).Charge!.Add(o, t);
            o.Priority = 2;
            Events.Sound(GameSound.Charge);
        }
    }

    /// <summary>Typing one of boss 3's counter-words: shields the player against one hit of its beam.</summary>
    public void ShootInstant(PlayerShip o, Sprite t)
    {
        Boss!.Count++;
        if (o.ShieldOn) o.SetShieldAlpha(o.ShieldAlpha + 0.2f);
        else
        {
            o.AddShield();
            o.SetShieldAlpha(0.2f);
        }
        HitTarget(t, Weapons.Instant);
    }

    public void ShootEmp(Sprite o, Sprite t, bool fire = false)
    {
        if (fire)
        {
            Events.Beam(o.P, t.P, BeamKind.Emp);
            HitTarget(t, Weapons.Emp);
            Events.Sound(GameSound.Emp);
        }
        else
        {
            Player.Charge.Add(o, t);
        }
    }

    public void ShootSuicide(EnemyShip o, Sprite t)
    {
        o.Commands = new List<Levels.ObjectCommand> { new(Context, t.Position.X, t.Position.Y) };
        o.A *= 2;
        o.Priority = 3;
    }

    public void Heal(int i = 1)
    {
        if (i == 10) Events.Sound(GameSound.Heal);
        var ship = Player;
        ship.Health = Math.Clamp(ship.Health + i, 0, ship.MaxHealth);
        if (ship.Health < ship.MaxHealth)
        {
            if (Hull.Word == null) Hull.AddWord();
            Hull.Priority = ship.Health switch
            {
                <= 1 => 3,
                <= 2 => 2,
                <= 3 => 1,
                _ => 0,
            };
        }
        else
        {
            Hull.RecycleWord();
        }
    }

    public void DamagePlayer(int i = 1)
    {
        if (Delay > 0 || Invincible) return;
        Heal(-i);
        Player.Injured = true;
    }

    /// <summary>Score points, shown as a floating number at a point in the game view.</summary>
    public void AddScore(int amount, Vec2 at)
    {
        Score += amount;
        Events.ScorePopup(at, amount, true);
    }

    public void HitTarget(Sprite t, int s)
    {
        if (t == Player)
        {
            DamagePlayer();
            ShowTutorial(8, new Vec2(300, 528), PointerDir.Down); // the hull screen in the middle of the cockpit
            return;
        }
        if (t.ShieldOn && s == Weapons.Emp)
        {
            if (!t.Removed) ((EnemyShip)t).RemoveShield(); // it may have left during the 5-tick EMP charge
            return;
        }
        // The original exploded a turret at its position inside the ship, i.e. near the game view's corner.
        var p = t is Turret turret ? turret.ViewPosition : t.P;
        if (s == Weapons.Missile && t.Health > 1)
        {
            t.Health--;
            Events.Explosion(p, 20);
            return;
        }

        if (t.Value != 0 && !BossMode) AddScore(t.Value, p);
        if (t.WordSize == 3)
        {
            Events.Explosion(p, 20);
        }
        else if (t.WordSize > 3)
        {
            if (s == Weapons.Missile) CountKill();
            Events.Explosion(p, 50);
        }
        else if (t is Superman)
        {
            Events.Explosion(p, 300);
            AddScore(99, p);
            if (Save.Achieve[Achievement.Explorer] == 2) Award(Achievement.Explorer, 3);
        }

        if (t.Removed) MissedShips--; // it escaped, but the missile was already on its way
        else t.Remove();
    }
}
