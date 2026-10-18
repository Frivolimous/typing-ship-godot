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
    /// <summary>The boss art, by sprite name. The view animates it from the state below.</summary>
    public readonly string Art;
    /// <summary>Boss 1: the hangar doors are open.</summary>
    public bool DoorsOpen;
    /// <summary>Boss 2: its guns are flaring.</summary>
    public bool Glowing;
    /// <summary>Boss 3: how far its cannon has charged (0 idle, 1-3).</summary>
    public int ChargeStage;
    /// <summary>Boss 3: the beam showing (-1 none, 0 the plain beam, 1-3 the frames of it hitting the player's shield).</summary>
    public int BeamFrame = -1;
    /// <summary>The art's scale in the game view.</summary>
    public readonly float SpriteScale;
    readonly List<Sprite> objects = new();
    int delay = -1;
    readonly int five;
    readonly LevelContext c;

    public static readonly string[] Arts = { "ai/boss0", "ai/boss1", "ai/boss2" };
    /// <summary>Boss 1's hangar openings (behind the doors), on its art.</summary>
    public const string HangarArt = "ai/boss0_hangar";
    /// <summary>Boss 2's six gun muzzles, in art pixels: the turrets it drops start there.</summary>
    static readonly Vec2[] GunPorts =
        { new(143, 354), new(265, 375), new(390, 481), new(578, 481), new(703, 375), new(825, 353) };
    /// <summary>Boss 3's cannon muzzle, in art pixels.</summary>
    static readonly Vec2 MuzzleArt = new(469, 342);

    public Boss(Game game, int i, LevelContext c) : base(game)
    {
        this.c = c;
        var t = game.Tuning;
        Health = t.BossHealth;
        five = Speed.FloorTicks(t.Boss2BeamTime / game.Cps);
        Index = i;
        WordSize = -1;
        if (i is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(i), $"Boss int is out of bounds: {i}");
        Art = Arts[i];
        SpriteScale = Index switch { 0 => 0.53f, 1 => 0.48f, _ => 0.42f };
        // Where it stops: the original's spot, or lower if the art would poke out of the top of the screen. The
        // margin covers the aim jitter and how far short of the point a ship can stop.
        float original = Index == 0 ? 100 : 170;
        float stop = Math.Max(original, Playfield.VisibleTop + ViewSize.Y / 2 + t.Jitter + t.ArriveDistance);
        Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, Math.Min(100, stop)));
        Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, stop));
        Commands.Add(new ObjectCommand(c, Playfield.ViewWidth / 2, 500, t.BossFirstAttack, false, true));

        Init(Playfield.ViewWidth / 2, -50, MathF.PI / 2, 0.1f, 0, true);
    }

    /// <summary>
    /// Boss 3's charge before the beam fires, 0-1 (0 while it isn't charging, 1 from when the beam fires until it stops).
    /// </summary>
    public float ChargeProgress => Index != 2 || delay <= 0 ? 0 : delay > five * 20 ? 1 - (delay - five * 20) / (five * 24f) : 1;

    /// <summary>Ticks until the last hit of boss 3's beam (until then a counter-word still blocks one), or -1.</summary>
    public int BeamTicksLeft => Index == 2 && delay > 0 ? delay : -1;

    /// <summary>The scaled size of the boss art (the original's view.width / view.height).</summary>
    public Vec2 ViewSize => Game.Art[Art].Size * SpriteScale;

    /// <summary>Top-left corner of the art in game-view space.</summary>
    public Vec2 TopLeft => Position - ViewSize / 2;

    /// <summary>Boss 3's cannon muzzle in game-view space, where the beam comes from.</summary>
    public Vec2 Muzzle => TopLeft + MuzzleArt * SpriteScale;

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
                    if (delay == 35) Glowing = true;
                    else if (delay == 20) Glowing = false;
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
                foreach (var e in Game.Set.BuildHangar(c))
                {
                    var ship = Game.Spawn(e, show: false);
                    Game.Objects.Insert(0, ship);
                    objects.Add(ship);
                    ship.Position += TopLeft;
                    Game.Events.Spawned(ship, Layer.Back);
                }
                DoorsOpen = true;
                delay = 20;
                break;
            case 1:
                var p = TopLeft + GunPorts[(int)(Game.Rng.Rand() * 6)] * SpriteScale;
                var spawn = new SpawnEvent(c, 0, Health > 1 ? "nln" : "nls", p.X, p.Y,
                [
                    new ObjectCommand(c, p.X, 500, Health > 1 ? 1 : 3, false),
                    new ObjectCommand(c, p.X, 500, 20, false, true),
                ]);
                Game.Objects.Add(Game.Spawn(spawn));
                break;
            case 2:
                ChargeStage = 1;
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
                if (objects.Count == 0) DoorsOpen = true;
            }
            else if (delay == 1)
            {
                DoorsOpen = objects.Count > 0;
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

    /// <summary>Boss 3's beam, counted down from five*44 ticks (five = beam time / cps).</summary>
    void UpdateBeam()
    {
        if (delay < 0) return;
        var player = Game.Player;
        // The four counter-words appear in a row above the player, left to right, clear of the cockpit and with a gap
        // in the middle for the beam.
        if (delay == five * 43) MakeCounter(-190, -80);
        else if (delay == five * 42) MakeCounter(-70, -80);
        else if (delay == five * 41) MakeCounter(70, -80);
        else if (delay == five * 40) MakeCounter(190, -80);
        else if (delay == five * 36)
        {
            ChargeStage = 2;
            Game.Events.Sound(GameSound.BossCharge);
        }
        else if (delay == five * 28)
        {
            ChargeStage = 3;
            Game.Events.Sound(GameSound.BossCharge);
        }
        else if (delay == five * 20)
        {
            // The original left a stale shield frame here if the last beam ended with counters unused.
            BeamFrame = Count > 0 ? 1 : 0;
            Game.Events.Sound(GameSound.BossFire);
        }
        else if (delay < five * 15)
        {
            if (delay % 5 == 0)
            {
                if (Count == 0) Game.Events.BeamHitsPlayer();
                else BeamFrame = delay / 5 % 3 + 1;
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
                        BeamFrame = 0;
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
                BeamFrame = -1;
                ChargeStage = 0;
                foreach (var o in objects)
                    if (!o.Removed) o.Remove();
                objects.Clear();
            }
        }
        delay--;
    }

    /// <summary>A counter-word next to the player. Typing it (Game.ShootInstant) blocks one beam hit.</summary>
    void MakeCounter(float x, float y)
    {
        var counter = new Counter(Game);
        var p = Game.Player.Position;
        counter.Init(p.X + x, p.Y + y, 0, 0, 2);
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
        BeamFrame = -1;
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
    /// <summary>The scanner keeps at least this far below the top of the screen, so it and its word show.</summary>
    const float ScannerTopMargin = 20;
    /// <summary>The ring's radius, for drawing.</summary>
    public float Ring;

    public Scanner(Game game, Boss v) : base(game)
    {
        var t = game.Tuning;
        WordSize = t.ScannerWord;
        AddWord();
        // A random visible pixel of the boss (not in boss 1's hangar), away from the bottom and left edges and below
        // the top of the screen.
        Vec2 p;
        var art = game.Art[v.Art];
        do
        {
            var px = art.RandomOpaquePixel(game.Rng);
            if (v.Index == 0 && game.Art[Boss.HangarArt].Opaque(px.X, px.Y))
            {
                p = new Vec2(0, 500);
                continue;
            }
            p = v.TopLeft + px * v.SpriteScale;
        } while (p.Y > 475 || p.X < 35 || p.Y < Playfield.VisibleTop + ScannerTopMargin);
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
