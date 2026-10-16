using Godot;

namespace TypingShip;

/// <summary>Per-tick animation outside the rules (the Flash ENTER_FRAME listeners that only drew). False = done.</summary>
public interface IViewTicker
{
    bool Tick();
}

/// <summary>A beam effect: lives a few ticks in the game view behind the ships, drawn additively and interpolated.</summary>
public abstract partial class BeamFx : Node2D, IViewTicker
{
    static readonly CanvasItemMaterial Additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

    protected readonly Vector2 To;
    readonly int life;
    int age;

    protected BeamFx() { }

    protected BeamFx(Vector2 from, Vec2 to, int life)
    {
        this.life = life;
        Position = from;
        To = G.V(to) - Position;
        Material = Additive;
        G.UI.AddToGameViewBack(this);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    /// <summary>The beam for a laser or EMP shot, from a point in the game view (the gun's muzzle).</summary>
    public static BeamFx Make(Vector2 from, Vec2 to, BeamKind kind) => kind == BeamKind.Emp
        ? new EmpEffect(from, to)
        : new LaserEffect(from, to, kind == BeamKind.PlayerLaser);

    /// <summary>0 when fired, 1 when gone; smooth between ticks.</summary>
    protected float T => Mathf.Clamp((age + (float)Engine.GetPhysicsInterpolationFraction()) / life, 0, 1);

    public override void _Process(double delta) => QueueRedraw();

    public bool Tick()
    {
        if (++age < life)
        {
            OnTick();
            return true;
        }
        QueueFree();
        return false;
    }

    protected virtual void OnTick() { }
}

/// <summary>
/// A laser: a soft glow, the coloured beam and a white-hot core that flash wide and then thin out, pulses of energy
/// running down it, a flare at the gun and a burst where it hits. Red from the player, blue from enemies.
/// </summary>
public partial class LaserEffect : BeamFx
{
    readonly Color glow, beam;

    public LaserEffect() { }

    public LaserEffect(Vector2 from, Vec2 to, bool player) : base(from, to, 10)
    {
        (glow, beam) = player
            ? (new Color(1, 0.15f, 0.25f), new Color(1, 0.45f, 0.5f))
            : (new Color(0.15f, 0.45f, 1), new Color(0.45f, 0.8f, 1));
    }

    public override void _Draw()
    {
        float t = T, fade = 1 - t;
        float w = (t < 0.15f ? 1.5f : 1) * Mathf.Pow(fade, 0.7f);
        DrawLine(Vector2.Zero, To, new Color(glow, 0.3f), 12 * w, true);
        DrawLine(Vector2.Zero, To, new Color(beam, 0.8f), 4.5f * w, true);
        DrawLine(Vector2.Zero, To, new Color(1.6f, 1.6f, 1.6f, fade), 1.6f * w, true); // overbright: it blooms

        for (int k = 0; k < 3; k++)
        {
            float u = (t * 2.5f + k / 3f) % 1;
            DrawCircle(To * u, 2.5f * w + 0.5f, new Color(1, 1, 1, 0.8f * fade));
        }

        DrawCircle(Vector2.Zero, 7 * w, new Color(beam, 0.6f));
        DrawCircle(To, 11 * w, new Color(glow, 0.45f));
        DrawCircle(To, 5 * w, new Color(1, 1, 1, fade));
        DrawArc(To, 4 + 16 * t, 0, Mathf.Tau, 24, new Color(beam, 0.6f * fade), 1.5f, true);
    }
}

/// <summary>An EMP: forked lightning that re-forms every tick, and a shock ring spreading from where it hits.</summary>
public partial class EmpEffect : BeamFx
{
    static readonly Color Glow = new(0.5f, 0.4f, 1), Core = new(0.85f, 0.9f, 1);
    readonly List<Vector2[]> bolts = new();

    public EmpEffect() { }

    public EmpEffect(Vector2 from, Vec2 to) : base(from, to, 9) => OnTick();

    protected override void OnTick()
    {
        bolts.Clear();
        var main = Bolt(Vector2.Zero, To, 10);
        bolts.Add(main);
        for (int i = 0; i < 2; i++)
        {
            var start = main[1 + (int)(G.Rand() * (main.Length - 2))];
            var dir = To.Normalized().Rotated((G.Rand() - 0.5f) * 1.6f);
            bolts.Add(Bolt(start, start + dir * To.Length() * (0.12f + G.Rand() * 0.12f), 5));
        }
    }

    /// <summary>A jagged line: points scattered sideways, most in the middle, none at the ends.</summary>
    static Vector2[] Bolt(Vector2 a, Vector2 b, float amplitude)
    {
        int n = Math.Max(5, (int)(a.DistanceTo(b) / 14));
        var side = (b - a).Normalized().Orthogonal();
        var pts = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n;
            pts[i] = a.Lerp(b, u) + side * (G.Rand() * 2 - 1) * amplitude * Mathf.Sin(Mathf.Pi * u);
        }
        return pts;
    }

    public override void _Draw()
    {
        float t = T, fade = 1 - t;
        for (int i = 0; i < bolts.Count; i++)
        {
            float w = i == 0 ? 1 : 0.6f;
            DrawPolyline(bolts[i], new Color(Glow, 0.35f * fade), 7 * w, true);
            DrawPolyline(bolts[i], new Color(Core, fade), 2 * w, true);
        }
        DrawCircle(Vector2.Zero, 6 * fade, new Color(Glow, 0.6f));
        DrawCircle(To, 9 * fade, new Color(Glow, 0.5f));
        DrawArc(To, 6 + 40 * t, 0, Mathf.Tau, 40, new Color(Core, 0.7f * fade), 2.5f * fade + 0.5f, true);
        DrawArc(To, 3 + 22 * t, 0, Mathf.Tau, 32, new Color(Glow, 0.5f * fade), 1.5f, true);
    }
}

// Floating "+5" / "-1" where points were won or lost: it pops in big, settles, then rises and fades. Bigger scores
// get bigger numbers.
public partial class PopScore : Node2D, IViewTicker
{
    static readonly Color Gain = new(0.45f, 1, 0.65f), Loss = new(1, 0.35f, 0.3f);
    readonly float rate;
    float pop = 1;

    public PopScore() { }

    /// <param name="inGameView">True when the position is in game-view space (a ship), false for HUD space.</param>
    public PopScore(Vector2 pos, int s, bool inGameView = true)
    {
        int size = (int)Mathf.Clamp(12 + s / 8f, 11, 26);
        var label = new Label
        {
            Text = (s > 0 ? "+" : "") + s,
            LabelSettings = Fonts.Settings(Fonts.HudBold, size, s > 0 ? Gain : Loss, outline: 5,
                outlineColor: new Color(0, 0, 0, 0.85f)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = new Vector2(120, size * 2),
            Position = new Vector2(-60, -size),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(label);
        rate = 1 - Mathf.Min(s, 100) / 120f;
        Position = pos;
        Scale = Vector2.One * 1.6f;
        if (inGameView) G.UI.GameView.AddChild(this);
        else G.UI.AddChild(this);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    public override void _Process(double delta)
    {
        // Settle from the pop-in size, smoothly between ticks.
        pop = Mathf.Max(0, pop - (float)delta * 6);
        Scale = Vector2.One * (1 + 0.6f * pop * pop);
    }

    public bool Tick()
    {
        var m = Modulate;
        if (m.A <= 0)
        {
            QueueFree();
            return false;
        }
        if (pop <= 0) m.A -= 0.025f * rate;
        Modulate = m;
        Position += new Vector2(0, -0.6f * rate);
        return true;
    }
}

// Stealth ships materialise: red and blue ghost images spiral in before the ship fades up (the rules count the
// fade-in; this draws it). A shimmer runs over the ghosts and the ship until it's solid.
public partial class StealthEffect : Node2D, IViewTicker
{
    static readonly Shader Shimmer = new()
    {
        Code = """
            shader_type canvas_item;
            uniform float amount = 1.0; // 1 just appearing, 0 solid
            uniform float seed = 0.0;
            varying vec4 tint;
            void vertex() { tint = COLOR; }
            float hash(vec2 p) { return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453); }
            void fragment() {
                // A heat-haze ripple across the rows.
                float wave = sin(UV.y * 60.0 + TIME * 18.0 + seed) * 0.6 + sin(UV.y * 23.0 - TIME * 11.0 + seed) * 0.4;
                vec4 c = texture(TEXTURE, UV + vec2(wave * 0.035 * amount, 0.0));
                // Scanline bands rolling down it.
                float band = smoothstep(0.3, 0.7, fract(UV.y * 36.0 - TIME * 5.0));
                c.rgb *= mix(1.0, 0.6 + 0.6 * band, amount);
                // Sparkles winking on and off over the hull.
                vec2 cell = floor(UV * vec2(24.0, 40.0));
                float sparkle = step(1.0 - 0.07 * amount, hash(cell + floor(TIME * 16.0) + seed));
                c.rgb += sparkle * amount * 1.5;
                c.a *= mix(1.0, 0.7 + 0.3 * sin(TIME * 37.0 + UV.y * 12.0), amount);
                COLOR = c * tint;
            }
            """,
    };

    readonly EnemyShipView toSpawn = null!;
    readonly EnemyShip ship = null!;
    readonly Sprite2D red = new(), blue = new();
    readonly ShaderMaterial shimmer = new() { Shader = Shimmer };

    public StealthEffect() { }

    public StealthEffect(EnemyShipView v)
    {
        toSpawn = v;
        ship = (EnemyShip)v.Model;
        v.GetParent().AddChild(this);
        Position = v.Position;
        shimmer.SetShaderParameter("seed", G.Rand() * 100);
        foreach (var (s, tint) in new[] { (red, new Color(2, 0.4f, 0.4f)), (blue, new Color(0.4f, 0.4f, 2)) })
        {
            s.Texture = v.View.Texture;
            s.Scale = v.View.Scale;
            s.Rotation = v.View.Rotation;
            s.SelfModulate = tint;
            s.Material = shimmer;
            AddChild(s);
        }
        v.View.Material = shimmer;
        Modulate = new Color(1, 1, 1, 0);
        ResetPhysicsInterpolation();
        G.Ctrl.AddTicker(this);
    }

    public bool Tick()
    {
        if (ship.Removed || !IsInstanceValid(toSpawn))
        {
            if (IsInstanceValid(toSpawn)) toSpawn.View.Material = null;
            QueueFree();
            return false;
        }
        Rotation = toSpawn.Rotation;
        Position = toSpawn.Position;
        float counter = ship.StealthCounter;
        float a = counter / 100 * Mathf.Tau;
        float r = 20 * (1 - counter / 100);
        red.Position = new Vector2(r * Mathf.Sin(a), r * Mathf.Cos(a));
        blue.Position = -red.Position;
        Modulate = new Color(1, 1, 1, 0.1f + counter / 150);
        shimmer.SetShaderParameter("amount", 1 - counter / 100);
        if (counter < 100) return true;
        toSpawn.View.Material = null;
        QueueFree();
        return false;
    }
}

/// <summary>
/// The player's ship dying: small explosions burst across it as it flickers red and the shake builds, then one big
/// blast (a hot flash, a shockwave, the full shake) and the ship is gone. About a second, well inside the rules'
/// 100 ticks before the level ends.
/// </summary>
public sealed class DeathSequence(PlayerView view, Action gone) : IViewTicker
{
    const int BlastTick = 24;
    static readonly Color Burning = new(1, 0.4f, 0.35f);
    int tick;

    public bool Tick()
    {
        if (!GodotObject.IsInstanceValid(view)) return false;
        var at = G.V(G.Model.Player.Position);
        var size = view.View.Texture != null ? view.View.Texture.GetSize() * view.View.Scale : new Vector2(40, 40);
        if (tick < BlastTick)
        {
            if (tick % 3 == 0)
            {
                var p = at + new Vector2((G.Rand() - 0.5f) * size.X, (G.Rand() - 0.5f) * size.Y);
                G.Starfield.Explosion(p.X, p.Y, 15 + tick / 2);
                G.Camera.Shake(0.12f + tick * 0.01f);
            }
            view.View.Modulate = tick % 4 < 2 ? Burning : Colors.White;
            tick++;
            return true;
        }
        G.Starfield.Explosion(at.X, at.Y, 150);
        for (int i = 0; i < 5; i++)
        {
            var p = at + Vector2.FromAngle(G.Rand() * Mathf.Tau) * (10 + G.Rand() * 25);
            G.Starfield.Explosion(p.X, p.Y, 50);
        }
        G.Starfield.Shockwave(at, 320, 18);
        G.Flash.Flash(new Color(1, 0.55f, 0.35f), 1, 1.2f, edgesOnly: false);
        G.Camera.Shake(1);
        gone();
        return false;
    }
}
