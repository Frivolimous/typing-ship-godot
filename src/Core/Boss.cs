using TypingShip.Core.Levels;

namespace TypingShip.Core;

/// <summary>
/// The three bosses (levels 3, 7, 11; B1-B3 on the map; Index 0-2 here). A boss flies in, stops, and a Scanner
/// appears on it; typing the scanner's words zeroes in on a weak spot, and its final long word damages the boss.
/// Three hits kill it. Meanwhile it attacks on a timer:
///   0: opens its hangar and launches six suicide ships.
///   1: drops an invisible laser turret at one of six gun ports.
///   2: charges a big beam. Four counter-words appear around the player; each one typed shields one of the beam's
///      four hits.
/// Not in Game.Objects: Game updates it after them.
/// </summary>
public sealed class Boss : Sprite
{
    public List<ObjectCommand>? Commands = new();
    public Scanner? Scanner;
    public int Index;
    /// <summary>Counter-words typed during boss 3's beam (Game.ShootInstant).</summary>
    public int Count;
    /// <summary>The boss art showing now ("Boss0", "Boss1a", "Boss2", or boss 3's charge frames "Boss2a"-"Boss2c").</summary>
    public string Art;
    /// <summary>The overlay art showing (hangar doors, boss 2's glow, boss 3's beam), or null.</summary>
    public string? Over;
    /// <summary>The art's scale in the game view.</summary>
    public readonly float SpriteScale;
    /// <summary>Where the art is drawn relative to Position (boss 3's art is 12 px off centre).</summary>
    public readonly Vec2 ArtOffset;
    readonly List<Sprite> objects = new();
    int delay = -1;
    readonly int five;
    readonly LevelContext c;

    public static readonly string[] Arts = { "Boss0", "Boss1a", "Boss2" };

    public Boss(Game game, int i, LevelContext c) : base(game)
    {
        this.c = c;
        var t = game.Tuning;
        Health = t.BossHealth;
        five = t.Boss2BeamTime / game.Wpm;
        Index = i;
        WordSize = -1;
        Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 100));
        if (i is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(i), $"Boss int is out of bounds: {i}");
        Art = Arts[i];
        switch (Index)
        {
            case 0: Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 100)); SpriteScale = 0.5f; break;
            case 1: Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 170)); SpriteScale = 0.7f; break;
            default: Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 170)); SpriteScale = 0.5f; break;
        }
        Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 500, t.BossFirstAttack, false, true));

        Init(Playfield.ViewWidth / 2, -50, MathF.PI / 2, 0.1f, 0, true);
        switch (Index)
        {
            case 0: Over = "Door0"; break;
            case 2: ArtOffset = new Vec2(12, 0); break;
        }
    }

    /// <summary>Ticks until the last hit of boss 3's beam (until then a counter-word still blocks one), or -1.</summary>
    public int BeamTicksLeft => Index == 2 && delay > 0 ? delay : -1;

    /// <summary>The scaled size of the boss art (the original's view.width / view.height).</summary>
    public Vec2 ViewSize => Game.Art[Art].Size * SpriteScale;

    /// <summary>Top-left corner of the art in game-view space.</summary>
    public Vec2 TopLeft => Position + ArtOffset - ViewSize / 2;

    public override bool MoreUpdate()
    {
        if (Scanner == null)
        {
            if (VT == 0 && !Game.BossMode && Health > 0)
            {
                Scanner = new Scanner(Game, this);
                Game.BossMode = true;
            }
        }
        else
        {
            Scanner.Update();
        }
        if (Health == 0) return false;

        switch (Index)
        {
            case 0: UpdateHangar(); break;
            case 1:
                if (delay <= 0) delay = 36;
                else
                {
                    delay--;
                    if (delay == 35) Over = "Boss1b";
                    else if (delay == 20) Over = null;
                }
                break;
            case 2: UpdateBeam(); break;
        }

        if (Commands == null) return true;
        if (Commands[0].Condition(this))
        {
            if (Commands[0].Fire) Attack();
            Commands.RemoveAt(0);
            if (Commands.Count == 0)
            {
                var t = Game.Tuning;
                int timer = Index switch
                {
                    0 => t.Boss0Attack,
                    1 => t.Boss1Attack + Health * t.Boss1AttackPerHealth,
                    _ => t.Boss2Attack,
                };
                Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 500, timer, false, true));
            }
            Halt = !Commands[0].Move;
        }
        return true;
    }

    void Attack()
    {
        switch (Index)
        {
            case 0:
                foreach (var e in Game.Data.BuildHangar(c))
                {
                    var ship = Game.Spawn(e, show: false);
                    Game.Objects.Insert(0, ship);
                    objects.Add(ship);
                    ship.Position += TopLeft;
                    Game.Events.Spawned(ship, Layer.Back);
                }
                Over = "Door1";
                delay = 20;
                break;
            case 1:
                var port = (int)(Game.Rng.Rand() * 6) switch
                {
                    0 => new Vec2(96, 115),
                    1 => new Vec2(205, 235),
                    2 => new Vec2(250, 185),
                    3 => new Vec2(390, 185),
                    4 => new Vec2(430, 235),
                    _ => new Vec2(540, 115),
                };
                var p = TopLeft + port * SpriteScale;
                var spawn = new SpawnEvent(c, 0, Health > 1 ? "nln" : "nls", p.X, p.Y,
                [
                    new ObjectCommand(c, p.X, 500, Health > 1 ? 1 : 3, false),
                    new ObjectCommand(c, p.X, 500, 20, false, true),
                ]);
                Game.Objects.Add(Game.Spawn(spawn));
                break;
            case 2:
                Art = "Boss2a";
                Game.Events.Sound(GameSound.BossCharge);
                Count = 0;
                delay = five * 44;
                break;
        }
    }

    /// <summary>Boss 1: the doors open as the ships launch and close once they've all left (or died).</summary>
    void UpdateHangar()
    {
        if (delay > 0)
        {
            if (delay == 10)
            {
                if (objects.Count == 0) Over = "Door1";
            }
            else if (delay == 1)
            {
                Over = objects.Count > 0 ? "Door2" : "Door0";
            }
            delay--;
        }
        else if (objects.Count > 0)
        {
            // A ship has left the hangar once it's down to its last command (it's been sent at the player).
            while (objects.Count > 0 && (objects[0].Removed || ((EnemyShip)objects[0]).Commands.Count == 1))
                objects.RemoveAt(0);
            if (objects.Count == 0) delay = 20;
        }
    }

    /// <summary>Boss 3's beam, counted down from five*44 ticks (five = beam time / wpm).</summary>
    void UpdateBeam()
    {
        if (delay < 0) return;
        var player = Game.Player;
        if (delay == five * 43) MakeCounter(30, 20);
        else if (delay == five * 42) MakeCounter(-75, 20);
        else if (delay == five * 41) MakeCounter(30, -20);
        else if (delay == five * 40) MakeCounter(-75, -20);
        else if (delay == five * 36)
        {
            Art = "Boss2b";
            Game.Events.Sound(GameSound.BossCharge);
        }
        else if (delay == five * 28)
        {
            Art = "Boss2c";
            Game.Events.Sound(GameSound.BossCharge);
        }
        else if (delay == five * 20)
        {
            // The original left a stale shield frame here if the last beam ended with counters unused.
            Over = Count > 0 ? "Boss2L1a" : "Boss2L0";
            Game.Events.Sound(GameSound.BossFire);
        }
        else if (delay < five * 15)
        {
            if (delay % 5 == 0)
            {
                if (Count == 0) Game.Events.BeamHitsPlayer();
                else Over = BeamFrames[delay / 5 % 3 + 1];
            }
            if (delay % (five * 4) == 0)
            {
                if (delay > 0) Game.Events.Sound(GameSound.BossFire);
                if (Count > 0)
                {
                    Count--;
                    player.SetShieldAlpha(player.ShieldAlpha - 0.2f);
                    if (Count == 0)
                    {
                        Over = BeamFrames[0];
                        player.RemoveShield();
                    }
                }
                else
                {
                    Game.HitTarget(player, Weapons.Suicide);
                }
            }
            if (delay == 0)
            {
                player.RemoveShield();
                Over = null;
                Art = Arts[2];
                foreach (var o in objects)
                    if (!o.Removed) o.Remove();
                objects.Clear();
            }
        }
        delay--;
    }

    /// <summary>The beam: plain, then the three frames of it hitting the player's shield.</summary>
    static readonly string[] BeamFrames = { "Boss2L0", "Boss2L1a", "Boss2L1c", "Boss2L1b" };

    /// <summary>A counter-word next to the player. Typing it (Game.ShootInstant) blocks one beam hit.</summary>
    void MakeCounter(float x, float y)
    {
        var counter = new Counter(Game);
        var p = Game.Player.Position;
        counter.Init(p.X + x - 5, p.Y + y, 0, 0, 2);
        counter.WordSize = 0;
        counter.KillBy = Weapons.Instant;
        counter.AddWord(Game.Tuning.CounterWord - Health);
        objects.Insert(0, counter);
        Game.Objects.Add(counter);
        Game.Events.Spawned(counter, Layer.Front);
    }

    public override void Remove()
    {
        Scanner?.Remove();
        Over = null;
        RecycleWord();
        Game.Boss = null;
        Detach();
    }

    public void Injure()
    {
        Game.ShowTutorial(10, P);
        Health--;
        if (Health > 0)
        {
            Scanner = new Scanner(Game, this);
        }
        else
        {
            Game.AddScore(Game.Tuning.BossPoints, P);
            Game.KillBoss();
        }
    }

    /// <summary>A random point on the visible boss art, in game-view space.</summary>
    public Vec2 RandomPoint(Rng rng) => TopLeft + Game.Art[Art].RandomOpaquePixel(rng) * SpriteScale;

    public override string TraceName => "BossShip";
}

/// <summary>
/// The targeting ring on a boss. Each short word typed shrinks the ring; when it has closed in, a long word
/// appears, and typing that fires a laser at the spot and damages the boss.
/// </summary>
public sealed class Scanner : Entity
{
    int count, toCount;
    /// <summary>The ring's radius, for drawing.</summary>
    public float Ring;

    public Scanner(Game game, Boss v) : base(game)
    {
        var t = game.Tuning;
        WordSize = t.ScannerWord;
        AddWord();
        // A random visible pixel of the boss (not behind boss 1's hangar doors), away from the bottom and left edges.
        Vec2 p;
        var art = game.Art[v.Art];
        do
        {
            var px = art.RandomOpaquePixel(game.Rng);
            if (v.Index == 0 && game.Art["Door0"].Opaque(px.X, px.Y))
            {
                p = new Vec2(0, 500);
                continue;
            }
            p = v.TopLeft + px * v.SpriteScale;
        } while (p.Y > 475 || p.X < 35);
        Position = p;
        count = t.ScannerRing;
        Ring = t.ScannerRing;
        game.Events.ScannerAdded(this);
    }

    public bool Removed { get; private set; }

    public void Remove()
    {
        if (Game.Boss != null) Game.Boss.Scanner = null;
        RecycleWord();
        Removed = true;
        Game.Events.Removed(this);
    }

    public void Scan()
    {
        var t = Game.Tuning;
        toCount += t.ScannerStep;
        if (count - toCount > 5)
        {
            AddWord();
        }
        else if (WordSize < t.ScannerFinalWord)
        {
            WordSize = t.ScannerFinalWord;
            AddWord();
        }
        else
        {
            Game.Events.Beam(Game.Player.P, Position, BeamKind.PlayerLaser);
            Game.Events.Explosion(Position, 20);
            var boss = Game.Boss!;
            Remove();
            boss.Injure();
        }
    }

    public void Update()
    {
        if (count <= 0 || toCount <= 0) return;
        if (count < 5) count = 5;
        count -= 3;
        toCount -= 3;
        Ring = count;
        if (count <= 5) count = -1;
    }

    public override string TraceName => "Scanner";
}
