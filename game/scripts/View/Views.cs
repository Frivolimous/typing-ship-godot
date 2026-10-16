using Godot;

namespace TypingShip;

// Nodes that draw the rules' objects (TypingShip.Core entities). Each copies its entity's state once per tick in
// Sync(); the Presenter creates and frees them as the rules report sprites joining and leaving.

/// <summary>
/// The word carried by a ship, missile, scanner or the hull bar, coloured by how urgent it is (priority 0-3). The
/// letters the typing bar ends with light up as they're typed, and the word pops when it's fired.
/// </summary>
public partial class WordLabel : Node2D
{
    const int FontSize = 14;
    static readonly Color[] PriorityColors =
    {
        new(0xffffffff), new(0xffff99ff), new(0xffbb99ff), new(0xff8877ff),
    };
    static readonly LabelSettings[] Settings = PriorityColors
        .Select(c => Fonts.Settings(Fonts.Word, FontSize, c, outline: 5, outlineColor: new Color(0, 0, 0, 0.9f)))
        .ToArray();

    /// <summary>Typed letters, and a fired word.</summary>
    public static readonly Color MatchColor = new(0.35f, 1, 0.75f);
    static readonly LabelSettings MatchSettings =
        Fonts.Settings(Fonts.Word, FontSize, MatchColor, outline: 5, outlineColor: new Color(0, 0.12f, 0.06f, 0.95f));

    readonly Label label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        Position = new Vector2(-100, 0),
        Size = new Vector2(200, 18),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    // The typed letters, drawn over the start of the word.
    readonly Label typed = new()
    {
        Size = new Vector2(200, 18),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        LabelSettings = MatchSettings,
        Visible = false,
    };
    string? word;
    int priority = -1;

    public WordLabel()
    {
        AddChild(label);
        AddChild(typed);
        Visible = false;
    }

    // How many of the word's letters the typing bar ends with (0 when none, and never all of them).
    int matched;

    public override void _Process(double delta)
    {
        var bar = G.Model?.Typed;
        SetMatched(Visible && word != null && bar != null ? MatchOf(word, bar) : 0);
    }

    /// <summary>The longest start of the word that the typed text ends with.</summary>
    static int MatchOf(string word, string bar)
    {
        for (int k = Math.Min(word.Length - 1, bar.Length); k > 0; k--)
            if (bar.AsSpan().EndsWith(word.AsSpan(0, k), StringComparison.Ordinal)) return k;
        return 0;
    }

    void SetMatched(int k)
    {
        if (k == matched) return;
        matched = k;
        typed.Visible = k > 0;
        if (k == 0) return;
        typed.Text = word![..k];
        float width = Fonts.Word.GetStringSize(word, HorizontalAlignment.Left, -1, FontSize).X;
        typed.Position = new Vector2(-width / 2, 0);
    }

    /// <summary>The word was typed: a copy of it swells and fades where it was.</summary>
    public void Pop()
    {
        if (word == null || !IsVisibleInTree()) return;
        var pop = new Label
        {
            Text = word,
            LabelSettings = MatchSettings,
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = label.Size,
            PivotOffset = label.Size / 2,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        G.UI.AddChild(pop);
        pop.GlobalPosition = label.GlobalPosition;
        var tween = pop.CreateTween().SetParallel();
        tween.TweenProperty(pop, "scale", Vector2.One * 1.7f, 0.3).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(pop, "modulate:a", 0f, 0.3);
        tween.Chain().TweenCallback(Callable.From(pop.QueueFree));
    }

    /// <summary>Show the entity's word (none if null) at its priority. A new word is shown even if the old one was hidden.</summary>
    public void Set(string? text, int p)
    {
        if (text != word)
        {
            word = text;
            label.Text = text ?? "";
            Visible = text != null;
            SetMatched(0);
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
        EngineFlames.Attach(View);
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

/// <summary>
/// A weapon charging up on its owner: motes of energy spiral in to a core that grows with the charge, and a ring
/// keeps closing in on it. Drawn additively, in stage pixels.
/// </summary>
public partial class ChargeGlow : Node2D
{
    static readonly CanvasItemMaterial Additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

    /// <summary>An enemy laser's charge, and the player's EMP.</summary>
    public static readonly Color LaserTint = new(0.3f, 0.75f, 1), EmpTint = new(0.7f, 0.55f, 1);

    readonly Charge charge;
    readonly Color tint;
    Color color;
    float time;

    public ChargeGlow(Charge charge, Color tint)
    {
        this.charge = charge;
        this.tint = tint;
        color = tint;
        Material = Additive;
        Visible = false;
    }

    public ChargeGlow() : this(null!, Colors.White) { }

    /// <summary>
    /// Puts the glow on a sprite view: at its art's charge point (data/mounts.txt), or at <paramref name="fallback"/>
    /// in the view's own space when the art has none.
    /// </summary>
    public void Mount(SpriteView view, Vector2 fallback)
    {
        if (Mounts.Charge(view.View.Texture) is { } at)
        {
            view.View.AddChild(this);
            Position = at;
            Scale = Vector2.One / view.View.Scale; // the art is scaled; the glow is sized in stage pixels
        }
        else
        {
            view.AddChild(this);
            Position = fallback;
        }
    }

    /// <summary>A new random colour each tick of charging: 0x0000ff plus a random touch of green, minus a random touch of blue.</summary>
    public static Color NextColor() =>
        new Color(0, Mathf.Floor(G.Rand() * 0x40) / 255f, (255 - Mathf.Floor(G.Rand() * 0x40)) / 255f);

    public void SetColor(Color c) => color = c;

    public void Sync() => Visible = charge.Active;

    public override void _Process(double delta)
    {
        time += (float)delta;
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        float fill = charge.Fill;
        // The original's flickering blue, pulled towards the weapon's own colour.
        var c = tint.Lerp(color, 0.3f);
        var hot = c.Lerp(Colors.White, 0.6f);

        DrawCircle(Vector2.Zero, 5 + 14 * fill, new Color(c, 0.15f + 0.1f * fill));

        float k = time * 2.2f % 1;
        DrawArc(Vector2.Zero, 4 + 24 * (1 - k), 0, Mathf.Tau, 32, new Color(c, (0.15f + 0.45f * fill) * k), 1.5f, true);

        const int Motes = 9;
        for (int i = 0; i < Motes; i++)
        {
            float u = (time * 1.6f + i / (float)Motes) % 1; // 0 far out, 1 arrived
            float r = 26 * (1 - u);
            float angle = i * Mathf.Tau / Motes + time * 3 + u * 2.5f;
            var at = Vector2.FromAngle(angle) * r;
            DrawCircle(at, 1 + u, new Color(hot, 0.8f * Mathf.Sin(Mathf.Pi * u)));
        }

        float pulse = 0.85f + 0.15f * Mathf.Sin(time * 45);
        DrawCircle(Vector2.Zero, (2.5f + 6 * fill) * pulse, new Color(c, 0.9f));
        DrawCircle(Vector2.Zero, (1.5f + 3.5f * fill) * pulse, hot);
    }
}

public partial class EnemyShipView : SpriteView
{
    /// <summary>The word on the ship's turret, if it has one.</summary>
    public WordLabel? TurretWord => turret?.Word;

    readonly EnemyShip ship;
    readonly ShieldView shield = new();
    readonly ChargeGlow? glow;
    TurretView? turret;
    bool shieldShown;

    // Per hull: art index, scale, shield size and offset, and where a laser charges if its art has no charge point
    // in data/mounts.txt (only the invisible "nl" turret laser, which has no art).
    static readonly Dictionary<string, (int Art, float Scale, Vector4 Shield, Vector2? Charge)> Hulls = new()
    {
        ["sm"] = (0, Sprites.AiScale, new(45, 45, 0, 0), null),
        ["sl"] = (1, Sprites.AiScale, new(55, 55, -5, 0), null),
        ["ss"] = (2, Sprites.AiScale, new(30, 30, 0, 0), null),
        ["mm"] = (3, Sprites.AiScale, new(60, 60, 0, 2), null),
        ["ml"] = (4, Sprites.AiScale, new(70, 70, -2, 0), null),
        ["ms"] = (5, Sprites.AiScale, new(35, 35, 0, 0), null),
        ["lm"] = (6, Sprites.AiScale, new(116, 82, -7, 1), null),
        ["ll"] = (7, Sprites.AiScale, new(110, 65, -8, 0), null),
        ["xm"] = (8, Sprites.AiScale, new(50, 35, 0, 0), null),
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
            glow = new ChargeGlow(ship.Charge, ChargeGlow.LaserTint);
            glow.Mount(this, look.Charge ?? Vector2.Zero);
        }
        if (ship.Turret != null)
        {
            turret = new TurretView(ship.Turret);
            AddChild(turret);
        }
        SyncShield();
    }

    public EnemyShipView() : this(null!) { }

    // The shield is only in the tree while it's up, so free it with the ship when it isn't (most ships never have one).
    public override void _Notification(int what)
    {
        if (what == NotificationPredelete && IsInstanceValid(shield) && shield.GetParent() == null) shield.Free();
    }

    public ChargeGlow? Glow => glow;

    public override void Sync()
    {
        base.Sync();
        // A stealth ship fades in (its flames with it) once its ghost images have spiralled in; its word stays readable.
        View.Modulate = new Color(1, 1, 1, ship.Alpha);
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
        AddChild(new Sprite2D
        {
            Texture = Sprites.Turret,
            Centered = false,
            Offset = -Sprites.TurretPivot,
            Scale = new Vector2(Sprites.AiScale, Sprites.AiScale),
        });
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
}

public partial class PlayerView : SpriteView
{
    readonly PlayerShip ship;
    public readonly ShieldView Shield = new();
    public readonly ChargeGlow Glow;
    bool shieldShown;

    public PlayerView(PlayerShip ship) : base(ship, Sprites.Player, Sprites.AiScale)
    {
        this.ship = ship;
        Name = "Player";
        Shield.SetSize(100, 100);
        Glow = new ChargeGlow(ship.Charge, ChargeGlow.EmpTint);
        Glow.Mount(this, new Vector2(0, -20));
    }

    public PlayerView() : this(null!) { }

    // As for enemy ships: the shield is only in the tree while it's up.
    public override void _Notification(int what)
    {
        if (what == NotificationPredelete && IsInstanceValid(Shield) && Shield.GetParent() == null) Shield.Free();
    }

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
    readonly ShaderMaterial glowMaterial = new() { Shader = BossShaders.Glow };
    readonly Sprite2D? doors;
    readonly BossBeam? beam;
    float glow, open;

    public BossView(Boss boss) : base(boss, new[] { Sprites.Named(boss.Art) }, boss.SpriteScale, artPointsUp: false)
    {
        this.boss = boss;
        View.Material = glowMaterial;
        if (boss.Index == 0)
        {
            // The hangar doors sit in the game view, over the boss and the ships launched from inside.
            var mask = Sprites.Named(Boss.HangarArt);
            doors = new Sprite2D
            {
                Texture = mask,
                Centered = false,
                Scale = new Vector2(boss.SpriteScale, boss.SpriteScale),
                Material = new ShaderMaterial { Shader = BossShaders.Doors },
            };
            G.UI.GameView.AddChild(doors);
        }
        if (boss.Index == 2)
        {
            // The beam goes over the boss, out of the cannon's mouth.
            beam = new BossBeam(boss);
            G.UI.BossView.AddChild(beam);
        }
        Sync();
    }

    public BossView() : this(null!) { }

    public override void Sync()
    {
        base.Sync();
        Modulate = new Color(1, 1, 1, boss.Alpha);
        if (doors != null)
        {
            doors.Position = G.V(boss.TopLeft);
            doors.Modulate = Modulate;
            if (boss.Health == 0 && doors.GetParent() != G.UI.BossView)
            {
                // The doors fade out with the boss.
                doors.GetParent().RemoveChild(doors);
                G.UI.BossView.AddChild(doors);
            }
        }
        beam?.QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        // Boss 2's guns flare as it fires; boss 3's cannon brightens with each charge stage and blazes while firing.
        float target = boss.Index switch
        {
            1 => boss.Glowing ? 1 : 0,
            2 when boss.BeamFrame >= 0 => 1.2f + 0.3f * G.Rand(),
            2 => boss.ChargeStage * 0.3f + (boss.ChargeStage > 0 ? 0.15f * G.Rand() : 0),
            _ => 0,
        };
        glow = Mathf.MoveToward(glow, target, d * 6);
        glowMaterial.SetShaderParameter("glow", glow);
        if (boss.Index == 2) ChargePips(d);
        if (doors != null)
        {
            open = Mathf.MoveToward(open, boss.DoorsOpen ? 1 : 0, d * 3);
            ((ShaderMaterial)doors.Material).SetShaderParameter("open", open);
        }
    }

    /// <summary>The boss is blown apart: it (with its doors and beam) disappears at once.</summary>
    public void Vanish()
    {
        Visible = false;
        if (doors != null) doors.Visible = false;
        if (beam != null) beam.Visible = false;
    }

    // Boss 3's four cannon lights, in the order they light: the top pair, then the pair by the muzzle (texture pixels).
    static readonly Vector3[] Pips = { new(368, 172, 19), new(569, 172, 19), new(388, 257, 19), new(550, 257, 19) };
    // Its cannon: the glass tube down to the muzzle ring (texture pixels: left, top, right, bottom).
    static readonly Vector4 Cannon = new(415, 30, 523, 376);
    readonly float[] pipLit = new float[4], pipFlare = new float[4];
    float cannonPower;

    /// <summary>
    /// The cannon's glass and its four lights go dark between attacks. As it charges, the lights come on one by one
    /// (the last as it fires) and the glass brightens with the charge.
    /// </summary>
    void ChargePips(float d)
    {
        float progress = boss.ChargeProgress;
        cannonPower = progress > cannonPower ? Mathf.MoveToward(cannonPower, progress, d * 2) : Mathf.MoveToward(cannonPower, progress, d);
        glowMaterial.SetShaderParameter("cannon", Cannon);
        glowMaterial.SetShaderParameter("cannon_power", cannonPower);
        for (int i = 0; i < 4; i++)
        {
            float target = Mathf.Clamp(progress * 4 - i, 0, 1);
            bool lights = target >= 1 && pipLit[i] < 1;
            pipLit[i] = target > pipLit[i] ? Mathf.MoveToward(pipLit[i], target, d * 8) : target;
            if (lights && pipLit[i] >= 1) pipFlare[i] = 1;
            pipFlare[i] = Mathf.Max(0, pipFlare[i] - d * 3);
        }
        glowMaterial.SetShaderParameter("pip_count", 4);
        glowMaterial.SetShaderParameter("pips", Pips);
        glowMaterial.SetShaderParameter("lit", pipLit);
        glowMaterial.SetShaderParameter("flare", pipFlare);
    }

    public override void Detach()
    {
        doors?.GetParent()?.RemoveChild(doors);
        doors?.QueueFree();
        beam?.GetParent()?.RemoveChild(beam);
        beam?.QueueFree();
        base.Detach();
    }
}

/// <summary>Boss 3's beam, from its cannon to the player (or splashing on the player's shield).</summary>
public partial class BossBeam : Node2D
{
    const float ShieldRadius = 50;
    readonly Boss boss;

    public BossBeam(Boss boss) => this.boss = boss;

    public BossBeam() : this(null!) { }

    public override void _Draw()
    {
        int frame = boss.BeamFrame;
        if (frame < 0) return;
        var from = G.V(boss.Muzzle);
        var player = G.V(G.Model.Player.Position);
        var to = frame == 0 ? player : player + new Vector2(0, -ShieldRadius);
        float wobble = 1 + 0.15f * G.Rand();
        DrawLine(from, to, new Color(0.2f, 0.8f, 1, 0.25f), 34 * wobble);
        DrawLine(from, to, new Color(0.3f, 0.9f, 1, 0.6f), 20 * wobble);
        DrawLine(from, to, new Color(0.85f, 1, 1), 8 * wobble);
        DrawCircle(from, 16 * wobble, new Color(0.6f, 0.95f, 1, 0.8f));
        if (frame > 0)
        {
            // The splash on the shield grows and shrinks over the three frames.
            float r = 14 + 6 * frame;
            DrawSetTransform(to, 0, new Vector2(1.6f, 0.6f));
            DrawCircle(Vector2.Zero, r * 1.4f, new Color(0.3f, 0.9f, 1, 0.35f));
            DrawCircle(Vector2.Zero, r, new Color(0.85f, 1, 1, 0.9f));
            DrawSetTransform(Vector2.Zero);
        }
    }
}

static class BossShaders
{
    /// <summary>
    /// Brightens the art's cyan parts (cores, tubes, gun rings) by <c>glow</c>. Up to four round lights (<c>pips</c>:
    /// centre and radius in texture pixels) can be blacked out, coming back as their <c>lit</c> rises to 1 (with an
    /// extra <c>flare</c>): boss 3's charge.
    /// </summary>
    public static readonly Shader Glow = new()
    {
        Code = """
            shader_type canvas_item;
            uniform float glow = 0.0;
            uniform int pip_count = 0;
            uniform vec3 pips[4];
            uniform float lit[4];
            uniform float flare[4];
            // A region (texture pixels: left, top, right, bottom) whose cyan dims to a fifth at power 0.
            uniform vec4 cannon = vec4(0.0);
            uniform float cannon_power = 1.0;
            void fragment() {
                vec4 c = COLOR;
                // Only the bright, saturated cyan lights: boss 2's teal hull stays put.
                float cyan = smoothstep(0.35, 0.6, min(c.g, c.b) - c.r);
                vec2 px = UV / TEXTURE_PIXEL_SIZE;
                bool inCannon = px.x >= cannon.x && px.x <= cannon.z && px.y >= cannon.y && px.y <= cannon.w;
                if (inCannon) c.rgb = mix(c.rgb, c.rgb * mix(0.2, 1.0, cannon_power), cyan);
                c.rgb += glow * cyan * vec3(0.45, 0.85, 1.0);
                for (int i = 0; i < pip_count; i++) {
                    float inside = 1.0 - smoothstep(pips[i].z - 2.0, pips[i].z, distance(px, pips[i].xy));
                    // Dark until lit; when it lights, it flares white-cyan for a moment.
                    vec3 on = c.rgb + flare[i] * vec3(0.6, 0.95, 1.0);
                    c.rgb = mix(c.rgb, mix(c.rgb * 0.1, on, lit[i]), inside);
                }
                COLOR = c;
            }
            """,
    };

    /// <summary>
    /// Boss 1's hangar doors, drawn over the opening mask: a slatted shutter that rolls up into the top of each opening
    /// as <c>open</c> goes from 0 to 1, with a hazard-striped leading edge.
    /// </summary>
    public static readonly Shader Doors = new()
    {
        Code = """
            shader_type canvas_item;
            uniform float open = 0.0;
            // The openings' top and bottom rows on the mask, as UV.y.
            uniform float top = 0.21;
            uniform float bottom = 0.69;
            void fragment() {
                float a = texture(TEXTURE, UV).a;
                float v = (UV.y - top) / (bottom - top);
                float edge = 1.0 - open;
                if (a < 0.5 || v > edge) discard;
                vec2 px = UV / TEXTURE_PIXEL_SIZE;
                float slat = fract((v + open) * 14.0);
                vec3 col = mix(vec3(0.13, 0.27, 0.19), vec3(0.2, 0.38, 0.27), smoothstep(0.0, 0.5, slat));
                col *= slat > 0.9 ? 0.55 : 1.0;
                if (edge - v < 0.06) {
                    bool stripe = mod((px.x + px.y) / 12.0, 2.0) < 1.0;
                    col = stripe ? vec3(0.85, 0.68, 0.22) : vec3(0.08, 0.08, 0.08);
                }
                COLOR = vec4(col * COLOR.rgb, COLOR.a);
            }
            """,
    };
}

/// <summary>
/// The scanner on a boss: a targeting reticle that closes in as its words are typed, its brackets turning from orange
/// to red, and snaps into a pulsing lock (with a flash) when the final long word appears.
/// </summary>
public partial class ScannerView : Node2D
{
    static readonly Color Search = new(1, 0.55f, 0.1f), Locked = new(1, 0.15f, 0.1f);
    static readonly CanvasItemMaterial Additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

    readonly Scanner scanner;
    public readonly WordLabel Word = new();
    readonly Node2D reticle = new();
    float ring = -1, time, lockFlash;
    bool locked;

    public ScannerView(Scanner scanner)
    {
        this.scanner = scanner;
        Position = G.V(scanner.Position);
        reticle.Material = Additive;
        reticle.Draw += DrawReticle;
        AddChild(reticle);
        AddChild(Word);
        Sync();
    }

    public ScannerView() : this(null!) { }

    public void Sync()
    {
        Word.Set(scanner.Word, scanner.Priority);
        bool nowLocked = scanner.WordSize >= G.Model.Tuning.ScannerFinalWord;
        if (nowLocked && !locked) lockFlash = 1;
        locked = nowLocked;
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        time += d;
        // Glide to the rules' ring size rather than jumping 3 px a tick.
        ring = ring < 0 ? scanner.Ring : Mathf.Lerp(ring, scanner.Ring, 1 - Mathf.Exp(-d * 12));
        lockFlash = Mathf.Max(0, lockFlash - d * 2.5f);
        reticle.QueueRedraw();
    }

    void DrawReticle()
    {
        float start = G.Model.Tuning.ScannerRing;
        float closed = 1 - Mathf.Clamp((ring - 5) / (start - 5), 0, 1); // 0 wide open, 1 closed in
        var c = Search.Lerp(Locked, locked ? 1 : closed * 0.7f);
        float r = Mathf.Max(ring, 10);

        if (locked)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 14);
            r = 14 + 3 * pulse;
            reticle.DrawArc(Vector2.Zero, r + 6, 0, Mathf.Tau, 32, new Color(c, 0.35f + 0.3f * pulse), 1.5f, true);
        }
        else
        {
            reticle.DrawArc(Vector2.Zero, r, 0, Mathf.Tau, Math.Max(32, (int)r / 2), new Color(c, 0.3f), 1.5f, true);
        }

        // Four brackets turning around the ring (locked: square to the target, and still).
        float spin = locked ? Mathf.Pi / 4 : time * (0.6f + 1.4f * closed);
        float span = locked ? 0.55f : 0.32f;
        for (int k = 0; k < 4; k++)
        {
            float angle = spin + k * Mathf.Pi / 2;
            reticle.DrawArc(Vector2.Zero, r, angle - span, angle + span, 10, c, 3, true);
            var dir = Vector2.FromAngle(angle + Mathf.Pi / 4);
            reticle.DrawLine(dir * (r - 5), dir * (r + 5), new Color(c, 0.7f), 1.5f, true);
        }

        // The crosshair at the weak spot.
        float h = locked ? 6 : 4;
        reticle.DrawLine(new Vector2(-h, 0), new Vector2(h, 0), c, 1.5f, true);
        reticle.DrawLine(new Vector2(0, -h), new Vector2(0, h), c, 1.5f, true);
        reticle.DrawCircle(Vector2.Zero, locked ? 2.5f : 1.8f, new Color(1, 0.9f, 0.8f));

        if (lockFlash > 0)
        {
            float t = 1 - lockFlash;
            reticle.DrawArc(Vector2.Zero, 14 + 50 * t, 0, Mathf.Tau, 40, new Color(1, 0.8f, 0.6f, lockFlash), 3 * lockFlash + 0.5f, true);
            reticle.DrawCircle(Vector2.Zero, 10 * lockFlash, new Color(1, 0.9f, 0.8f, lockFlash * 0.8f));
        }
    }
}
