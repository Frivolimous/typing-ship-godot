using Godot;

namespace TypingShip;

// The three bosses (levels 3, 7, 11). A boss flies in, stops, and a Scanner appears on it; typing the scanner's
// words zeroes in on a weak spot, and its final long word damages the boss. Three hits kill it.
// Meanwhile it attacks on a timer:
//   0: opens its hangar and launches six suicide ships.
//   1: drops an invisible laser turret at one of six gun ports.
//   2: charges a big beam. Four counter-words appear around the player; each one typed shields one of the
//      beam's four hits.
// Lives in G.UI.BossView (behind the ships), not in G.Model.GObjects; GameControl updates it.
public partial class BossShip : GameSprite
{
    public List<ObjectCommand>? Commands = new();
    public Scanner? Scanner;
    public int Index;
    /// <summary>Counter-words typed during boss 2's beam (ActionControl.ShootInstant).</summary>
    public int Count;
    public readonly Sprite2D Over = new() { Centered = false };
    readonly List<GameSprite> objects = new();
    int delay = -1;
    readonly int five;

    public BossShip() { }

    public BossShip(int i)
    {
        Health = 3;
        five = 300 / G.Model.Wpm;
        Index = i;
        WordSize = -1;
        Commands.Add(new ObjectCommand(G.ViewWidth / 2, 100));
        ViewSource = new[] { Sprites.Boss[i] };
        switch (Index)
        {
            case 0: Commands.Add(new ObjectCommand(G.ViewWidth / 2, 100)); SpriteScale = 0.5f; break;
            case 1: Commands.Add(new ObjectCommand(G.ViewWidth / 2, 170)); SpriteScale = 0.7f; break;
            case 2: Commands.Add(new ObjectCommand(G.ViewWidth / 2, 170)); SpriteScale = 0.5f; break;
            default: throw new ArgumentOutOfRangeException(nameof(i), $"Boss int is out of bounds: {i}");
        }
        Commands.Add(new ObjectCommand(G.ViewWidth / 2, 500, 6, false, true));

        Init(G.ViewWidth / 2, -50, Mathf.Pi / 2, 0.1f, 0, true);
        GetParent().RemoveChild(this);
        G.UI.BossView.AddChild(this);
        ResetPhysicsInterpolation();

        switch (Index)
        {
            case 0:
                // The hangar doors sit in the game view, over the boss and the ships launched from behind them.
                Over.Texture = Sprites.Door[0];
                Over.Scale = new Vector2(SpriteScale, SpriteScale);
                Over.Position = TopLeft;
                G.UI.GameView.AddChild(Over);
                Over.ResetPhysicsInterpolation();
                break;
            case 1:
                Over.Texture = Sprites.Boss1Glow;
                Over.Scale = new Vector2(SpriteScale, SpriteScale);
                Over.Position = -ViewSize / 2;
                break;
            case 2:
                Over.Texture = Sprites.Boss2Beam[0];
                Over.Position = new Vector2(43, 160);
                View.Position += new Vector2(12, 0);
                break;
        }
    }

    /// <summary>Ticks until the last hit of boss 2's beam (until then a counter-word still blocks one), or -1.</summary>
    public int BeamTicksLeft => Index == 2 && delay > 0 ? delay : -1;

    /// <summary>The scaled size of the boss art (the original's view.width / view.height).</summary>
    public Vector2 ViewSize => View.Texture.GetSize() * SpriteScale;

    /// <summary>Top-left corner of the art in game-view space.</summary>
    public Vector2 TopLeft => Position + View.Position - ViewSize / 2;

    GameModel M => G.Model;

    public override bool MoreUpdate()
    {
        if (Scanner == null)
        {
            if (VT == 0 && !M.BossMode && Health > 0)
            {
                Scanner = new Scanner(this);
                M.BossMode = true;
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
                    if (delay == 35) AddChild(Over);
                    else if (delay == 20) RemoveChild(Over);
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
                int timer = Index switch
                {
                    0 => 48,
                    1 => 23 + Health * 3,
                    _ => 70,
                };
                Commands.Add(new ObjectCommand(G.ViewWidth / 2, 500, timer, false, true));
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
                foreach (var e in LevelData.boss0Suicides())
                {
                    var ship = e.Spawn();
                    M.GObjects.Insert(0, ship);
                    objects.Add(ship);
                    ship.Position += TopLeft;
                    G.UI.AddToGameViewBack(ship);
                    ship.ResetPhysicsInterpolation();
                }
                Over.Texture = Sprites.Door[1];
                delay = 20;
                break;
            case 1:
                var port = (int)(G.Rand() * 6) switch
                {
                    0 => new Vector2(96, 115),
                    1 => new Vector2(205, 235),
                    2 => new Vector2(250, 185),
                    3 => new Vector2(390, 185),
                    4 => new Vector2(430, 235),
                    _ => new Vector2(540, 115),
                };
                var p = TopLeft + port * SpriteScale;
                new SpawnEvent(0, Health > 1 ? "nln" : "nls", p.X, p.Y,
                [
                    new ObjectCommand(p.X, 500, Health > 1 ? 1 : 3, false),
                    new ObjectCommand(p.X, 500, 20, false, true),
                ]).Run();
                break;
            case 2:
                View.Texture = Sprites.Boss2Charge[0];
                G.Sound.Sound(SoundControl.BossCharge);
                Count = 0;
                delay = five * 44;
                break;
        }
    }

    /// <summary>Boss 0: the doors open as the ships launch and close once they've all left (or died).</summary>
    void UpdateHangar()
    {
        Over.Position = TopLeft;
        if (delay > 0)
        {
            if (delay == 10)
            {
                if (objects.Count == 0) Over.Texture = Sprites.Door[1];
            }
            else if (delay == 1)
            {
                Over.Texture = objects.Count > 0 ? Sprites.Door[2] : Sprites.Door[0];
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

    /// <summary>Boss 2's beam, counted down from five*44 ticks (five = 300/wpm).</summary>
    void UpdateBeam()
    {
        if (delay < 0) return;
        var player = M.PShip;
        if (delay == five * 43) MakeCounter(30, 20);
        else if (delay == five * 42) MakeCounter(-75, 20);
        else if (delay == five * 41) MakeCounter(30, -20);
        else if (delay == five * 40) MakeCounter(-75, -20);
        else if (delay == five * 36)
        {
            View.Texture = Sprites.Boss2Charge[1];
            G.Sound.Sound(SoundControl.BossCharge);
        }
        else if (delay == five * 28)
        {
            View.Texture = Sprites.Boss2Charge[2];
            G.Sound.Sound(SoundControl.BossCharge);
        }
        else if (delay == five * 20)
        {
            G.UI.BossView.AddChild(Over);
            G.UI.BossView.MoveChild(Over, 0);
            // The original left a stale shield frame here if the last beam ended with counters unused.
            Over.Texture = Count > 0 ? Sprites.Boss2Beam[1] : Sprites.Boss2Beam[0];
            G.Sound.Sound(SoundControl.BossFire);
        }
        else if (delay < five * 15)
        {
            if (delay % 5 == 0)
            {
                if (Count == 0)
                {
                    var size = player.View.Texture.GetSize() * player.View.Scale;
                    G.Starfield.Explosion(player.Position.X - size.X / 2 + G.Rand() * size.X,
                        player.Position.Y - size.Y / 2 + G.Rand() * size.Y);
                }
                else
                {
                    Over.Texture = Sprites.Boss2Beam[delay / 5 % 3 + 1];
                }
            }
            if (delay % (five * 4) == 0)
            {
                if (delay > 0) G.Sound.Sound(SoundControl.BossFire);
                if (Count > 0)
                {
                    Count--;
                    player.ShieldV.Alpha -= 0.2f;
                    if (Count == 0)
                    {
                        Over.Texture = Sprites.Boss2Beam[0];
                        player.RemoveShield();
                    }
                }
                else
                {
                    G.Action.HitTarget(player, ActionControl.Suicide);
                }
            }
            if (delay == 0)
            {
                player.RemoveShield();
                Over.GetParent()?.RemoveChild(Over);
                View.Texture = Sprites.Boss[2];
                foreach (var o in objects)
                    if (!o.Removed) o.Remove();
                objects.Clear();
            }
        }
        delay--;
    }

    /// <summary>A counter-word next to the player. Typing it (ActionControl.ShootInstant) blocks one beam hit.</summary>
    void MakeCounter(float x, float y)
    {
        var c = new GameSprite();
        var p = M.PShip.Position;
        c.Init(p.X + x - 5, p.Y + y, 0, 0, 2);
        c.WordSize = 0;
        c.KillBy = ActionControl.Instant;
        c.AddWord(11 - Health);
        objects.Insert(0, c);
        M.GObjects.Add(c);
    }

    public override void Remove()
    {
        Scanner?.Remove();
        if (Over.GetParent() != this) Over.GetParent()?.RemoveChild(Over);
        Over.QueueFree();
        RecycleWord();
        M.Boss = null;
        Detach();
    }

    public void Injure()
    {
        // The "two more shots" tutorial pop-up (tutFlags[10]) comes with the tutorial pop-ups.
        Health--;
        if (Health > 0)
        {
            Scanner = new Scanner(this);
        }
        else
        {
            new PopScore(P, 100);
            G.Ctrl.KillBoss();
            if (Index == 0)
            {
                // The doors fade out with the boss.
                Over.GetParent().RemoveChild(Over);
                G.UI.BossView.AddChild(Over);
            }
        }
    }

    /// <summary>Fade-out alpha while it explodes (GameControl.finalBoss).</summary>
    public void SetAlpha(float a)
    {
        Modulate = new Color(1, 1, 1, a);
        if (Over.GetParent() != this) Over.Modulate = Modulate;
    }

    /// <summary>A random point on the visible boss art, in game-view space.</summary>
    public Vector2 RandomPoint() => TopLeft + Sprites.RandomOpaquePixel(View.Texture) * SpriteScale;
}
