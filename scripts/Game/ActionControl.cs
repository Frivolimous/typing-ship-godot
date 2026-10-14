using Godot;

namespace TypingShip;

// Weapons, damage and scoring.
public class ActionControl
{
    public const int Laser = 0, Missile = 1, AutoMissile = 2, Emp = 3, Suicide = 4, Instant = 5;

    public float MCount, MRate = 0.4f;

    GameModel M => G.Model;

    public void ShootMissile(GameSprite o, GameSprite t, bool auto = false)
    {
        if (o == M.PShip)
        {
            // One missile per point of the target's health, fanned out and staggered.
            int w = M.Wpm;
            switch (t.Health)
            {
                case 4:
                    LaunchPlayerMissile(t, -Mathf.Pi * 5 / 6);
                    LaunchPlayerMissile(t, -Mathf.Pi / 6, (int)Math.Ceiling(100.0 / w));
                    LaunchPlayerMissile(t, Mathf.Pi * 3 / 4, (int)Math.Ceiling(200.0 / w));
                    LaunchPlayerMissile(t, Mathf.Pi / 4, (int)Math.Ceiling(300.0 / w));
                    break;
                case 3:
                    LaunchPlayerMissile(t, -Mathf.Pi / 2);
                    LaunchPlayerMissile(t, Mathf.Pi * 3 / 4, (int)Math.Ceiling(200.0 / w));
                    LaunchPlayerMissile(t, Mathf.Pi / 4, (int)Math.Ceiling(300.0 / w));
                    break;
                case 2:
                    LaunchPlayerMissile(t, -Mathf.Pi, (int)Math.Ceiling(100.0 / w));
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
                2 => MRate * 1.5f,
                3 or 4 => MRate * 2,
                _ => MRate,
            };

        if (MCount > 1)
        {
            o.Priority = 0;
            o.Value -= 1;
            MCount -= 1;
            var m = new Missile();
            m.Setup(o, t);
            M.GObjects.Add(m);
            if (!M.TutFlags[7])
            {
                new PopBox(7, m.P);
                M.TutFlags[7] = true;
            }
        }
    }

    void LaunchPlayerMissile(GameSprite target, float n = float.NaN, int delay = 0)
    {
        var m = new Missile();
        m.Setup(M.PShip, target, n, delay);
        M.GObjects.Insert(0, m);
        G.UI.AddToGameViewBack(m);
    }

    public void ShootLaser(GameSprite o, GameSprite t, bool fire = false)
    {
        if (o == M.PShip)
        {
            BeamEffect.Laser(o, t is Turret ? ((Node2D)t.GetParent()).Position : t.P);
            HitTarget(t, Laser);
            G.Sound.Sound(SoundControl.Laser);
        }
        else if (fire)
        {
            BeamEffect.Laser(o, t.P);
            HitTarget(t, Laser);
            G.Starfield.Explosion(t.P.X, t.P.Y, 10);
            G.Sound.Sound(SoundControl.Laser);
            o.Priority = 0;
        }
        else
        {
            ((EnemyShip)o).Charge!.Add(o, t);
            o.Priority = 2;
            G.Sound.Sound(SoundControl.Charge);
        }
    }

    /// <summary>Typing one of boss 2's counter-words: shields the player against one hit of its beam.</summary>
    public void ShootInstant(PlayerShip o, GameSprite t)
    {
        M.Boss!.Count++;
        if (o.ShieldOn) o.ShieldV.Alpha += 0.2f;
        else
        {
            o.AddShield();
            o.ShieldV.Alpha = 0.2f;
        }
        HitTarget(t, Instant);
    }

    public void ShootEmp(GameSprite o, GameSprite t, bool fire = false)
    {
        if (fire)
        {
            BeamEffect.Emp(o, t.P);
            HitTarget(t, Emp);
            G.Sound.Sound(SoundControl.EmpSound);
        }
        else
        {
            M.PShip.Charge.Add(o, t);
        }
    }

    public void ShootSuicide(EnemyShip o, GameSprite t)
    {
        o.Commands = new List<ObjectCommand> { new(t.Position.X, t.Position.Y) };
        o.A *= 2;
        o.Priority = 3;
    }

    public void Heal(int i = 1)
    {
        if (i == 10) G.Sound.Sound(SoundControl.Heal);
        var ship = M.PShip;
        ship.Health = Math.Clamp(ship.Health + i, 0, PlayerShip.MaxHealth);
        M.Hull.DrawBar(ship.Health);
        if (ship.Health < PlayerShip.MaxHealth)
        {
            if (M.Hull.TextObject == null) M.Hull.AddWord();
            M.Hull.Priority = ship.Health switch
            {
                <= 1 => 3,
                <= 2 => 2,
                <= 3 => 1,
                _ => 0,
            };
        }
        else
        {
            M.Hull.RecycleWord();
        }
    }

    public void DamagePlayer(int i = 1)
    {
        if (G.Ctrl.Delay > 0 || M.Invincible) return;
        Heal(-i);
        M.PShip.Injured = true;
    }

    public void HitTarget(GameSprite t, int s)
    {
        if (t == M.PShip)
        {
            DamagePlayer();
            if (!M.TutFlags[8])
            {
                new PopBox(8, new Vector2(420, 530), PopBox.Dir.Right);
                M.TutFlags[8] = true;
                M.Hull.ShowWord(true);
            }
            return;
        }
        if (t.ShieldOn && s == Emp)
        {
            if (!t.Removed) ((EnemyShip)t).RemoveShield(); // it may have left during the 5-tick EMP charge
            return;
        }
        // The original exploded a turret at its position inside the ship, i.e. near the game view's corner.
        var p = t is Turret turret ? turret.ViewPosition : t.P;
        if (s == Missile && t.Health > 1)
        {
            t.Health--;
            G.Starfield.Explosion(p.X, p.Y, 20);
            return;
        }

        if (t.Value != 0 && !M.BossMode) new PopScore(p, t.Value);
        if (t.WordSize == 3)
        {
            G.Starfield.Explosion(p.X, p.Y, 20);
        }
        else if (t.WordSize > 3)
        {
            if (s == Missile) CountKill();
            G.Starfield.Explosion(p.X, p.Y);
        }
        else if (t.ViewSource?[0] == Sprites.Superman)
        {
            G.Starfield.Explosion(p.X, p.Y, 300);
            new PopScore(p, 99);
            if (M.Achieve[Strings.Explorer] == 2) Achievements.Award(Strings.Explorer, 3);
        }

        if (t.Removed) M.MissedShips--; // it escaped, but the missile was already on its way
        else t.Remove();
    }

    /// <summary>Soldier: ships destroyed by missile; one tier at a time at 10, 1,000 and 20,000.</summary>
    void CountKill()
    {
        int kills = ++M.AchieveC[Strings.Soldier];
        int tier = M.Achieve[Strings.Soldier];
        if (tier < 3 && kills >= new[] { 10, 1000, 20000 }[tier]) Achievements.Award(Strings.Soldier, tier + 1);
    }
}
