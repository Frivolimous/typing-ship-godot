using Godot;

namespace TypingShip;

// Nodes that draw the rules' objects (TypingShip.Core entities). Each copies its entity's state once per tick in
// Sync(); the Presenter creates and frees them as the rules report sprites joining and leaving.

/// <summary>The word carried by a ship, missile, scanner or the hull bar, coloured by how urgent it is (priority 0-3).</summary>
public partial class WordLabel : Node2D
{
    static readonly Color[] PriorityColors =
    {
        new(0xffffffff), new(0xffff99ff), new(0xffbb99ff), new(0xff8877ff),
    };
    static readonly LabelSettings[] Settings = PriorityColors
        .Select(c => Fonts.Settings(Fonts.Word, 14, c, outline: 5, outlineColor: new Color(0, 0, 0, 0.9f)))
        .ToArray();

    readonly Label label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        Position = new Vector2(-100, 0),
        Size = new Vector2(200, 18),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    string? word;
    int priority = -1;

    public WordLabel()
    {
        AddChild(label);
        Visible = false;
    }

    /// <summary>Show the entity's word (none if null) at its priority. A new word is shown even if the old one was hidden.</summary>
    public void Set(string? text, int p)
    {
        if (text != word)
        {
            word = text;
            label.Text = text ?? "";
            Visible = text != null;
        }
        if (p != priority)
        {
            priority = p;
            label.LabelSettings = Settings[Math.Clamp(p, 0, 3)];
        }
    }

    /// <summary>Pausing hides the words, so they can't be read ahead.</summary>
    public void ShowWord(bool b) => Visible = b && word != null;
}

/// <summary>A sprite in the game view: its art (animated if it has several frames) and its word.</summary>
public partial class SpriteView : Node2D
{
    public readonly Sprite Model;
    public readonly Sprite2D View = new();
    protected Texture2D[]? Frames;
    int frame;
    public readonly WordLabel Word = new();

    public SpriteView(Sprite model, Texture2D[]? frames, float scale, bool artPointsUp = true)
    {
        Model = model;
        Frames = frames;
        View.Texture = frames?[0];
        View.Scale = new Vector2(scale, scale);
        if (artPointsUp) View.Rotation = Mathf.Pi / 2; // art points up; heading 0 points right
        AddChild(View);
        AddChild(Word);
        Position = G.V(model.Position);
        Rotation = model.Rotation;
        Word.Set(model.Word, model.Priority);
        Word.Rotation = -Rotation;
    }

    public SpriteView() : this(null!, null, 1) { }

    public virtual void Sync()
    {
        Position = G.V(Model.Position);
        Rotation = Model.Rotation;
        Word.Set(Model.Word, Model.Priority);
        Word.Rotation = -Rotation;
    }

    /// <summary>SpriteSheets.update: step an animated sprite to its next frame (every other tick).</summary>
    public void AdvanceFrame()
    {
        if (Frames == null || Frames.Length <= 1) return;
        frame = (frame + 1) % Frames.Length;
        View.Texture = Frames[frame];
    }

    public virtual void ShowWord(bool b) => Word.ShowWord(b);

    /// <summary>The entity left play.</summary>
    public virtual void Detach()
    {
        GetParent()?.RemoveChild(this);
        QueueFree();
    }
}

/// <summary>A weapon charging up on its owner: a growing glow (blue, with a random flicker of green).</summary>
public partial class ChargeGlow : Node2D
{
    readonly Charge charge;
    Color color;

    public ChargeGlow(Charge charge, Vector2 at)
    {
        this.charge = charge;
        Position = at;
        Visible = false;
    }

    public ChargeGlow() : this(null!, Vector2.Zero) { }

    /// <summary>A new random colour each tick of charging: 0x0000ff plus a random touch of green, minus a random touch of blue.</summary>
    public static Color NextColor() =>
        new Color(0, Mathf.Floor(G.Rand() * 0x40) / 255f, (255 - Mathf.Floor(G.Rand() * 0x40)) / 255f);

    public void SetColor(Color c) => color = c;

    public void Sync()
    {
        Visible = charge.Active;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float r = 10f * charge.Fill;
        DrawCircle(Vector2.Zero, r * 1.8f, new Color(color, 0.25f));
        DrawCircle(Vector2.Zero, r, color);
    }
}

public partial class EnemyShipView : SpriteView
{
    readonly EnemyShip ship;
    readonly ShieldView shield = new();
    readonly ChargeGlow? glow;
    TurretView? turret;
    bool shieldShown;

    // Per hull: art index, scale, shield size and offset, and where a laser's charge glows.
    static readonly Dictionary<string, (int Art, float Scale, Vector4 Shield, Vector2? Charge)> Hulls = new()
    {
        ["sm"] = (0, 0.5f, new(45, 45, 0, 0), null),
        ["sl"] = (1, 0.5f, new(55, 55, -5, 0), new(18, 0)),
        ["ss"] = (2, 0.13f, new(30, 30, 0, 0), null),
        ["mm"] = (3, 0.65f, new(60, 60, 0, 2), null),
        ["ml"] = (4, 0.65f, new(70, 70, -2, 0), new(28, 1)),
        ["ms"] = (5, 0.25f, new(35, 35, 0, 0), null),
        ["lm"] = (6, 1, new(116, 82, -7, 1), null),
        ["ll"] = (7, 1, new(110, 65, -8, 0), new(35, 3)),
        ["xm"] = (8, 0.5f, new(50, 35, 0, 0), null),
        ["nl"] = (9, 1, new(50, 35, 0, 0), new(10, 0)),
    };

    static (int Art, float Scale, Vector4 Shield, Vector2? Charge) Look(string hull) =>
        Hulls.TryGetValue(hull, out var h) ? h : Hulls["sm"]; // a hull added in tuning.txt borrows the small ship's art

    public EnemyShipView(EnemyShip ship) : base(ship, Sprites.Enemy[Look(ship.Hull).Art], Look(ship.Hull).Scale)
    {
        this.ship = ship;
        var look = Look(ship.Hull);
        shield.SetSize(look.Shield.X, look.Shield.Y, look.Shield.Z, look.Shield.W);
        if (ship.Charge != null)
        {
            glow = new ChargeGlow(ship.Charge, look.Charge ?? Vector2.Zero);
            AddChild(glow);
            MoveChild(glow, 0);
        }
        if (ship.Turret != null)
        {
            turret = new TurretView(ship.Turret);
            AddChild(turret);
        }
        SyncShield();
    }

    public EnemyShipView() : this(null!) { }

    public ChargeGlow? Glow => glow;

    public override void Sync()
    {
        base.Sync();
        SyncShield();
        glow?.Sync();
        if (turret != null)
        {
            if (ship.Turret == null)
            {
                turret.QueueFree();
                turret = null;
            }
            else turret.Sync();
        }
    }

    void SyncShield()
    {
        if (ship.ShieldOn == shieldShown) return;
        shieldShown = ship.ShieldOn;
        if (shieldShown) shield.Add(this);
        else shield.Remove();
    }

    public override void ShowWord(bool b)
    {
        base.ShowWord(b);
        turret?.Word.ShowWord(b);
    }
}

/// <summary>Point-defence gun on an enemy ship (drawn, since the original's was a vector clip).</summary>
public partial class TurretView : Node2D
{
    readonly Turret turret;
    public readonly WordLabel Word = new();

    public TurretView(Turret turret)
    {
        this.turret = turret;
        Name = "Turret";
        Position = G.V(turret.Position);
        AddChild(Word);
        Sync();
    }

    public TurretView() : this(null!) { }

    public void Sync()
    {
        Rotation = turret.Rotation;
        Word.Set(turret.Word, turret.Priority);
        Word.Rotation = -Rotation - turret.Ship.Rotation;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, -3, 15, 6), new Color(0x666666ff));
        DrawRect(new Rect2(0, -3, 15, 6), new Color(0xaaaaaaff), false, 1);
        DrawCircle(Vector2.Zero, 3, new Color(0x666666ff));
        DrawArc(Vector2.Zero, 3, 0, Mathf.Tau, 16, new Color(0xaaaaaaff), 1);
    }
}

public partial class PlayerView : SpriteView
{
    readonly PlayerShip ship;
    public readonly ShieldView Shield = new();
    public readonly ChargeGlow Glow;
    bool shieldShown;

    public PlayerView(PlayerShip ship) : base(ship, Sprites.Player, 1)
    {
        this.ship = ship;
        Name = "Player";
        Shield.SetSize(100, 100);
        Glow = new ChargeGlow(ship.Charge, new Vector2(0, -20));
        AddChild(Glow);
        MoveChild(Glow, 0);
    }

    public PlayerView() : this(null!) { }

    public override void Sync()
    {
        base.Sync();
        if (ship.ShieldOn != shieldShown)
        {
            shieldShown = ship.ShieldOn;
            if (shieldShown) Shield.Add(this);
            else Shield.Remove();
        }
        if (shieldShown) Shield.Alpha = ship.ShieldAlpha;
        Glow.Sync();
    }
}

/// <summary>The boss art, its overlay (hangar doors, glow or beam), and its fade-out when it dies.</summary>
public partial class BossView : SpriteView
{
    readonly Boss boss;
    readonly Sprite2D over = new() { Centered = false };
    string art, overArt = "";

    public BossView(Boss boss) : base(boss, new[] { Sprites.Named(boss.Art) }, boss.SpriteScale, artPointsUp: false)
    {
        this.boss = boss;
        art = boss.Art;
        View.Position = G.V(boss.ArtOffset);
        over.Scale = boss.Index == 2 ? Vector2.One : new Vector2(boss.SpriteScale, boss.SpriteScale);
        SyncOver();
    }

    public BossView() : this(null!) { }

    public override void Sync()
    {
        base.Sync();
        if (boss.Art != art) View.Texture = Sprites.Named(art = boss.Art);
        SyncOver();
        Modulate = new Color(1, 1, 1, boss.Alpha);
        if (over.GetParent() != this) over.Modulate = Modulate;
    }

    void SyncOver()
    {
        var name = boss.Over ?? "";
        if (name != overArt)
        {
            overArt = name;
            if (name == "") over.GetParent()?.RemoveChild(over);
            else
            {
                over.Texture = Sprites.Named(name);
                if (over.GetParent() == null)
                {
                    switch (boss.Index)
                    {
                        // The hangar doors sit in the game view, over the boss and the ships launched from behind them.
                        case 0: G.UI.GameView.AddChild(over); break;
                        case 1: AddChild(over); break;
                        // The beam is behind the boss, from the gun at the bottom.
                        default: G.UI.BossView.AddChild(over); G.UI.BossView.MoveChild(over, 0); break;
                    }
                }
            }
        }
        over.Position = boss.Index switch
        {
            0 => G.V(boss.TopLeft),
            1 => -G.V(boss.ViewSize) / 2,
            _ => new Vector2(43, 160),
        };
        if (boss.Index == 0 && over.GetParent() != null && boss.Health == 0 && over.GetParent() != G.UI.BossView)
        {
            // The doors fade out with the boss.
            over.GetParent().RemoveChild(over);
            G.UI.BossView.AddChild(over);
        }
    }

    public override void Detach()
    {
        if (over.GetParent() != this) over.GetParent()?.RemoveChild(over);
        over.QueueFree();
        base.Detach();
    }
}

/// <summary>The scanner ring on a boss, closing in as its words are typed.</summary>
public partial class ScannerView : Node2D
{
    static readonly Color RingColor = new(0xff5500ff);
    readonly Scanner scanner;
    public readonly WordLabel Word = new();
    float ring;

    public ScannerView(Scanner scanner)
    {
        this.scanner = scanner;
        Position = G.V(scanner.Position);
        AddChild(Word);
        Sync();
    }

    public ScannerView() : this(null!) { }

    public void Sync()
    {
        Word.Set(scanner.Word, scanner.Priority);
        if (scanner.Ring != ring)
        {
            ring = scanner.Ring;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        DrawArc(Vector2.Zero, ring, 0, Mathf.Tau, Math.Max(32, (int)ring / 2), RingColor, 3, true);
        DrawCircle(Vector2.Zero, 2.5f, RingColor);
    }
}
