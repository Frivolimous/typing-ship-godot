using TypingShip.Core.Levels;

namespace TypingShip.Core;

/// <summary>Anything that can carry a typeable word (the original's GameObject).</summary>
public class Entity
{
    protected readonly Game Game;
    /// <summary>The word on it (TextObject.text), "" for an empty one, or null when it has none.</summary>
    public string? Word;
    public int WordSize = -1;
    public Vec2 Position;
    public float Rotation;
    int priority;

    public Entity(Game game) => Game = game;

    public void AddWord(int size = 0)
    {
        RecycleWord();
        if (WordSize < 0) return;
        int s = size == 0 ? WordSize : size;
        Word = s > 0 ? Game.Words.Shift(s) : "";
    }

    public void RecycleWord()
    {
        if (Word == null) return;
        Game.Words.Push(Word);
        Word = null;
    }

    /// <summary>How urgent its word is, 0-3 (white, yellow, orange, red).</summary>
    public int Priority
    {
        get => priority;
        set => priority = value;
    }

    /// <summary>The original's class name, used in --trace output.</summary>
    public virtual string TraceName => GetType().Name;
}

/// <summary>Where a new sprite is drawn, front to back (the original's display-list juggling).</summary>
public enum Layer
{
    /// <summary>On top of the ships drawn so far.</summary>
    Front,
    /// <summary>Behind the ships, over the explosion particles (player missiles, boss 1's launched ships).</summary>
    Back,
    /// <summary>Behind everything, even the particles (superman).</summary>
    Bottom,
    /// <summary>The boss layer, behind the game view.</summary>
    Boss,
}

/// <summary>
/// A moving thing in the game view (the original's GameSprite): steers towards heading N with acceleration A.
/// Units are the original's: pixels and radians per tick.
/// </summary>
public class Sprite : Entity
{
    public float VX, VY, VT, N, A;
    public int Value, KillBy, Fires, Health = 1;
    public float TurnRate;
    public bool ShieldOn, Halt;
    public Turret? Turret;
    /// <summary>0-1; only stealth ships fading in are less than 1.</summary>
    public float Alpha = 1;

    /// <summary>Set once the sprite has left play. Others may still hold a reference (a missile homing on it).</summary>
    public bool Removed;
    Vec2 lastPos;

    public Sprite(Game game) : base(game) { }

    /// <summary>Position, or where it was when it was removed.</summary>
    public Vec2 P => Removed ? lastPos : Position;

    public void Init(float x, float y, float n, float a, int p, bool isBoss = false)
    {
        if (isBoss)
        {
            VT = 0.01f;
            N = MathF.PI / 2;
            Rotation = 0;
        }
        else
        {
            N = n;
            Rotation = n;
        }
        Position = new Vec2(x, y);
        A = a;
        TurnRate = a / 7;
        AddWord();
        Priority = p;
    }

    /// <summary>One tick. False only when it removed itself (the next object then slides into its slot).</summary>
    public virtual bool Update()
    {
        if (!MoreUpdate()) return false;
        VT += (Halt ? 0 : A) - VT * 0.1f;
        VX = VT * MathF.Cos(N);
        VY = VT * MathF.Sin(N);
        if (Math.Abs(VT) < Math.Abs(A))
            VX = VY = VT = 0;
        else
            Position += new Vec2(VX, VY);
        return true;
    }

    public virtual bool MoreUpdate() => true;

    public virtual void Remove()
    {
        if (Word != null) Game.AddOverflow(Word);
        if (!Game.Objects.Remove(this) && this is Turret t) t.Ship.Turret = null;
        Detach();
    }

    protected void Detach()
    {
        lastPos = Position;
        Removed = true;
        Game.Events.Removed(this);
    }

    public void HomeTarget(Vec2 target)
    {
        float tDiff = MathF.Atan2(target.Y - Position.Y, target.X - Position.X) - N;
        while (tDiff < -MathF.PI) tDiff += 2 * MathF.PI;
        while (tDiff > MathF.PI) tDiff -= 2 * MathF.PI;

        if (tDiff > 0) N += Math.Min(tDiff, TurnRate);
        else if (tDiff < 0) N += Math.Max(tDiff, -TurnRate);
        else return;

        Rotation = N;
    }

    public override string TraceName => "GameSprite";
}

/// <summary>Something ticked every tick, paused or not, until it says it's done (the Flash ENTER_FRAME listeners).</summary>
public interface ITicker
{
    /// <summary>False when it's finished.</summary>
    bool Tick();
}

public sealed class EnemyShip : Sprite
{
    public List<ObjectCommand> Commands = new();
    public Charge? Charge;
    /// <summary>The first two letters of its ship code, which pick its art.</summary>
    public string Hull = "";
    /// <summary>Where it came from in the level files (for the editing overlay).</summary>
    public string File = "";
    public int Line;
    /// <summary>When the ship joined: its event's time in the level (seconds, as on Hard), for the editing overlay.</summary>
    public double SpawnTime;
    /// <summary>Stealth ships fade in; this counts to 100 (StealthEffect), then they're solid.</summary>
    public float StealthCounter = -1;

    public EnemyShip(Game game) : base(game) { }

    public void Setup(SpawnEvent e)
    {
        Commands = e.Commands.ToList();
        WordSize = e.WordSize;
        Fires = e.Fires;
        KillBy = e.KillBy;
        Value = e.Value;
        Hull = e.Hull;
        Health = e.Health;
        File = e.File;
        Line = e.Line;
        SpawnTime = e.Time;

        if (Fires == Weapons.Laser) Charge = new Charge(Game, Speed.FloorTicks(Game.Tuning.LaserCharge / Game.Cps), Weapons.Laser);

        Init(e.X, e.Y, MathF.Atan2(Commands[0].Y - e.Y, Commands[0].X - e.X), e.A, 1);

        if (e.HasTurret) Turret = new Turret(Game, this);

        if (!Commands[0].Move) Halt = true;
        if (e.Shielded) AddShield();
        if (Hull == "xm")
        {
            Alpha = 0;
            StealthCounter = 0;
            Game.AddTicker(new StealthFade(this, 1 + (float)(Game.Cps / Game.Tuning.StealthFadeCps)));
        }
    }

    public override bool MoreUpdate()
    {
        HomeTarget(new Vec2(Commands[0].X, Commands[0].Y));
        Turret?.Update();

        if (!Commands[0].Condition(this)) return true;

        Commands.RemoveAt(0);
        if (Commands.Count > 0)
        {
            if (Commands[0].Fire)
            {
                var player = Game.Player;
                switch (Fires)
                {
                    case Weapons.Missile: Game.ShootMissile(this, player); break;
                    case Weapons.AutoMissile: Game.ShootMissile(this, player, true); break;
                    case Weapons.Laser: Game.ShootLaser(this, player); break;
                    case Weapons.Suicide: Game.ShootSuicide(this, player); break;
                }
            }
            Halt = !Commands[0].Move;
            return true;
        }

        if (Fires == Weapons.Suicide)
        {
            Game.HitTarget(Game.Player, Weapons.Suicide);
            Game.Events.Explosion(Position, 10);
        }
        Remove();
        Game.MissedShips++;
        return false;
    }

    public override void Remove()
    {
        if (Word != null) Game.AddOverflow(Word);
        Game.Objects.Remove(this);
        Charge?.Cancel();
        Detach();
    }

    public void AddShield()
    {
        ShieldOn = true;
        RecycleWord();
        Turret?.RecycleWord();
        AddWord(3);
    }

    public void RemoveShield()
    {
        if (!ShieldOn) return;
        ShieldOn = false;
        RecycleWord();
        AddWord();
        Turret?.AddWord();
    }

    /// <summary>StealthEffect: the ship fades in once the ghost images have spiralled in.</summary>
    sealed class StealthFade(EnemyShip ship, float countUp) : ITicker
    {
        public bool Tick()
        {
            if (ship.Removed) return false;
            if (!ship.Game.Running) return true;
            ship.StealthCounter += countUp;
            ship.Alpha = Math.Max((ship.StealthCounter - 70) / 30, 0);
            if (ship.StealthCounter < 100) return true;
            ship.Alpha = 1;
            return false;
        }
    }

    public override string TraceName => "EnemyShip";
}

/// <summary>Point-defence gun on an enemy ship: shoots down player missiles that come within range of the ship.</summary>
public sealed class Turret : Sprite
{
    public readonly EnemyShip Ship;

    public Turret(Game game, EnemyShip ship) : base(game)
    {
        Ship = ship;
        Position = new Vec2(-20, 0); // relative to the ship
        A = 0;
        TurnRate = 1;
        WordSize = 4;
        KillBy = Weapons.Laser;
        AddWord();
        Priority = 1;
    }

    /// <summary>Where the turret is in game-view space.</summary>
    public Vec2 ViewPosition => Ship.Position + Position.Rotated(Ship.Rotation);

    public override bool Update() => true;

    public void TestTarget(Missile t)
    {
        var ship = Ship;
        var pos = ship.Position + Position;
        Rotation = MathF.Atan2(t.Position.Y - pos.Y, t.Position.X - pos.X) - ship.Rotation;
        if (ship.Position.DistanceTo(t.Position) < Game.Tuning.TurretRange)
        {
            Game.ShootLaser(ship, t, true);
            ship.AddWord();
        }
    }

    public override string TraceName => "Turret";
}

public sealed class PlayerShip : Sprite
{
    public bool Injured;
    /// <summary>The shield bubble's opacity (boss 3's counter-words fill it, its beam drains it).</summary>
    public float ShieldAlpha = 1;
    /// <summary>The EMP that knocks out enemy shields.</summary>
    public readonly Charge Charge;
    /// <summary>Blown up: hidden until the next level starts.</summary>
    public bool Destroyed;

    public PlayerShip(Game game) : base(game)
    {
        Charge = new Charge(game, game.Tuning.EmpCharge, Weapons.Emp);
        Init(Playfield.PlayerX, Playfield.PlayerY, -MathF.PI / 2, 0, 0);
    }

    public int MaxHealth => Game.Tuning.PlayerHealth;

    public void AddShield()
    {
        ShieldAlpha = 1;
        ShieldOn = true;
    }

    public void RemoveShield()
    {
        if (!ShieldOn) return;
        ShieldOn = false;
    }

    public void SetShieldAlpha(float a) => ShieldAlpha = Math.Clamp(a, 0, 1); // Flash clamped alpha too

    public override string TraceName => "PlayerShip";
}

public sealed class Missile : Sprite
{
    Sprite target = null!;
    int delay;
    /// <summary>Fired by the player (no word), or by an enemy (a 3-letter word to shoot it down).</summary>
    public bool FromPlayer;

    public Missile(Game game) : base(game) { }

    public Sprite Target => target;

    /// <param name="n">Launch heading; NaN means "the origin's rotation".</param>
    public void Setup(Sprite origin, Sprite target, float n = float.NaN, int delay = 0)
    {
        this.target = target;
        float speed, heading;
        var t = Game.Tuning;
        if (origin == Game.Player)
        {
            FromPlayer = true;
            speed = Speed.Scaled(t.PlayerMissileSpeed, Game.Cps);
            WordSize = -1;
            heading = float.IsNaN(n) ? origin.Rotation : n;
        }
        else
        {
            KillBy = Weapons.Laser;
            Value = Game.BossMode ? 0 : t.EnemyMissilePoints;
            speed = Speed.Scaled(t.EnemyMissileSpeed, Game.Cps);
            WordSize = 3;
            heading = origin.Rotation;
        }
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
        if (!target.Removed) target.Turret?.TestTarget(this); // a turret leaves with its ship
        if (Removed) return false; // shot down by the turret

        var t = target.P;
        HomeTarget(t);
        var d = Position - t;
        float range = Game.Tuning.MissileHitRange;
        if ((Math.Abs(d.X) <= Math.Abs(VX * 1.5f) && Math.Abs(d.Y) <= Math.Abs(VY * 1.5f)) ||
            (Math.Abs(d.X) <= range && Math.Abs(d.Y) <= range))
        {
            Game.HitTarget(target, Weapons.Missile);
            if (target == Game.Player) Game.Events.Explosion(Position, 10);
            Remove();
            return false;
        }
        return true;
    }

    public override string TraceName => "Missile";
}

/// <summary>The "superman" cheat: a little hero flies up the screen. "kamikaze" blows it up for 99 points.</summary>
public sealed class Superman : Sprite
{
    public Superman(Game game) : base(game) { }

    // The original never removed it, so typing "superman" stopped the level from ever ending.
    public override bool MoreUpdate()
    {
        if (Position.Y > -100) return true;
        Remove();
        return false;
    }

    public override string TraceName => "Superman";
}

/// <summary>One of boss 3's counter-words next to the player: typing it blocks one hit of the beam.</summary>
public sealed class Counter : Sprite
{
    public Counter(Game game) : base(game) { }
}

/// <summary>A word whose ship left the screen stays typeable for a while, so a half-typed word isn't wasted.</summary>
public sealed class Overflow
{
    public readonly string Word;
    readonly Game game;
    int delay;

    public Overflow(Game game, string word)
    {
        this.game = game;
        Word = word;
        delay = game.Tuning.OverflowTicks;
    }

    public bool Update()
    {
        if (delay > 0)
        {
            delay--;
            return true;
        }
        Remove();
        return false;
    }

    public void Remove()
    {
        game.Words.Push(Word);
        game.Overflows.Remove(this);
    }
}

/// <summary>A weapon charging up on its owner (an enemy laser, or the player's EMP); fires when full.</summary>
public sealed class Charge : ITicker
{
    readonly Game game;
    int count;
    readonly int maxCount;
    readonly int type;
    public Sprite Origin { get; private set; } = null!;
    public Sprite Target { get; private set; } = null!;
    public bool Active { get; private set; }

    public Charge(Game game, int time, int type)
    {
        this.game = game;
        maxCount = time;
        this.type = type;
    }

    /// <summary>0-1, how charged it is.</summary>
    public float Fill => (float)count / maxCount;

    /// <summary>Ticks until it fires, or -1 when it isn't charging.</summary>
    public int Remaining => Active ? maxCount - count : -1;

    public void Add(Sprite origin, Sprite target)
    {
        Origin = origin;
        Target = target;
        count = 0;
        if (!Active)
        {
            Active = true;
            game.AddTicker(this);
        }
    }

    public void Cancel() => Active = false;

    public bool Tick()
    {
        if (!Active) return false;
        if (!game.Running) return true;
        count++;
        if (count >= maxCount)
        {
            Active = false;
            if (type == Weapons.Laser) game.ShootLaser(Origin, Target, true);
            else if (type == Weapons.Emp) game.ShootEmp(Origin, Target, true);
            return false;
        }
        if (count == maxCount / 2)
        {
            Origin.Priority = 3;
            game.Events.Sound(GameSound.Charge);
        }
        game.Events.ChargeTicked(this);
        return true;
    }
}

/// <summary>The hull bar's repair word (ShipSystem): while damaged, typing it repairs one point.</summary>
public sealed class Hull : Entity
{
    public Hull(Game game) : base(game)
    {
        var t = game.Tuning;
        WordSize = t.HealWordShortest + (int)Math.Floor(game.Rng.Rand() * (t.HealWordLongest - t.HealWordShortest + 1));
    }

    public override string TraceName => "ShipSystem";
}
