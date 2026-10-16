using Godot;

namespace TypingShip;

/// <summary>
/// Explosions in the game view: a flash and a shockwave ring, fireballs that cool from white to red, sparks, and
/// smoke that lingers. Everything moves in the 24 Hz tick and is drawn interpolated between ticks. Smoke is drawn
/// normally, under the fire, which is drawn additively.
/// </summary>
public partial class Explosions : Node2D
{
    enum Kind { Flash, Ring, Fire, Spark, Smoke }

    class Particle
    {
        public Kind Kind;
        public Vector2 P, V;
        public float R, Grow, Drag = 1, Age, Life, Delay;
    }

    readonly List<Particle> particles = new();
    readonly Layer smoke, fire;

    public Explosions()
    {
        smoke = new Layer(this, k => k == Kind.Smoke);
        fire = new Layer(this, k => k != Kind.Smoke)
        {
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
        };
        AddChild(smoke);
        AddChild(fire);
    }

    /// <summary>An explosion at a point in game-view space. Size is the original's particle count (10 to 300).</summary>
    public void Add(Vector2 at, int size)
    {
        float s = Mathf.Clamp(Mathf.Sqrt(size / 20f), 0.6f, 3.5f);
        Add(Kind.Flash, at, Vector2.Zero, 9 * s, life: 5, grow: 2.5f * s);
        if (size >= 20) Add(Kind.Ring, at, Vector2.Zero, 6 * s, life: 9, grow: 4.5f * s);

        int fires = (int)Mathf.Clamp(size * 0.5f, 6, 40);
        for (int i = 0; i < fires; i++)
        {
            var dir = Vector2.FromAngle(G.Rand() * Mathf.Tau);
            float speed = (0.3f + G.Rand() * 1.4f) * s;
            Add(Kind.Fire, at + dir * G.Rand() * 5 * s, dir * speed, (2.5f + G.Rand() * 4) * s,
                life: 9 + G.Rand() * 10, grow: 0.15f * s, drag: 0.9f);
        }

        int sparks = (int)Mathf.Clamp(size * 0.4f, 5, 28);
        for (int i = 0; i < sparks; i++)
        {
            var dir = Vector2.FromAngle(G.Rand() * Mathf.Tau);
            Add(Kind.Spark, at, dir * (2.5f + G.Rand() * 4) * Mathf.Sqrt(s), 1, life: 7 + G.Rand() * 8, drag: 0.88f);
        }

        int puffs = (int)Mathf.Clamp(size * 0.35f, 4, 26);
        for (int i = 0; i < puffs; i++)
        {
            var dir = Vector2.FromAngle(G.Rand() * Mathf.Tau);
            Add(Kind.Smoke, at + dir * G.Rand() * 8 * s, dir * (0.2f + G.Rand() * 0.6f) * s, (3 + G.Rand() * 3) * s,
                life: 26 + G.Rand() * 26, grow: 0.25f * s, drag: 0.95f, delay: 2 + G.Rand() * 4);
        }
    }

    /// <summary>A lone shockwave ring spreading to about <paramref name="radius"/> over <paramref name="ticks"/>.</summary>
    public void Shockwave(Vector2 at, float radius, int ticks) =>
        Add(Kind.Ring, at, Vector2.Zero, 10, life: ticks, grow: (radius - 10) / ticks);

    void Add(Kind kind, Vector2 p, Vector2 v, float r, float life, float grow = 0, float drag = 1, float delay = 0) =>
        particles.Add(new Particle { Kind = kind, P = p, V = v, R = r, Life = life, Grow = grow, Drag = drag, Delay = delay });

    /// <summary>One tick.</summary>
    public void Update()
    {
        particles.RemoveAll(p =>
        {
            if (p.Delay > 0)
            {
                p.Delay--;
                return false;
            }
            p.P += p.V;
            p.V *= p.Drag;
            p.R += p.Grow;
            p.Age++;
            return p.Age >= p.Life;
        });
    }

    public void Clear() => particles.Clear();

    public override void _Process(double delta)
    {
        smoke.QueueRedraw();
        fire.QueueRedraw();
    }

    // White, yellow, orange, red, then a dull ember as a fireball cools.
    static readonly Color[] FireRamp =
    {
        new(1, 1, 0.9f), new(1, 0.85f, 0.35f), new(1, 0.5f, 0.12f), new(0.85f, 0.18f, 0.06f), new(0.3f, 0.06f, 0.03f),
    };

    static Color Ramp(Color[] ramp, float t)
    {
        t = Mathf.Clamp(t, 0, 1) * (ramp.Length - 1);
        int i = Math.Min((int)t, ramp.Length - 2);
        return ramp[i].Lerp(ramp[i + 1], t - i);
    }

    partial class Layer : Node2D
    {
        readonly Explosions owner = null!;
        readonly Func<Kind, bool> draws = null!;

        public Layer() { }
        public Layer(Explosions owner, Func<Kind, bool> draws)
        {
            this.owner = owner;
            this.draws = draws;
        }

        public override void _Draw()
        {
            // Drawn a fraction of a tick behind, like the ships.
            float back = 1 - (float)Engine.GetPhysicsInterpolationFraction();
            foreach (var p in owner.particles)
            {
                if (p.Delay > 0 || !draws(p.Kind)) continue;
                var at = p.P - p.V * back;
                float t = (p.Age + 1 - back) / p.Life, r = p.R - p.Grow * back;
                switch (p.Kind)
                {
                    case Kind.Flash:
                        DrawCircle(at, r * 1.6f, new Color(1, 0.8f, 0.5f, 0.35f * (1 - t)));
                        DrawCircle(at, r, new Color(1, 1, 0.9f, 0.9f * (1 - t)));
                        break;
                    case Kind.Ring:
                        DrawArc(at, r, 0, Mathf.Tau, 32, new Color(1, 0.75f, 0.45f, 0.6f * (1 - t)), 2.5f * (1 - t) + 0.5f, true);
                        break;
                    case Kind.Fire:
                        var c = Ramp(FireRamp, t);
                        DrawCircle(at, r * 1.5f, new Color(c, 0.25f * (1 - t)));
                        DrawCircle(at, r, new Color(c, 0.85f * (1 - t * t)));
                        break;
                    case Kind.Spark:
                        var tail = at - p.V * 2.5f;
                        DrawLine(tail, at, new Color(1, 0.9f, 0.55f, 1 - t), 1.5f, true);
                        break;
                    case Kind.Smoke:
                        float a = 0.45f * Mathf.Min(1, t * 6) * (1 - t);
                        DrawCircle(at, r, new Color(0.32f, 0.3f, 0.3f, a));
                        DrawCircle(at - new Vector2(r, r) * 0.25f, r * 0.6f, new Color(0.5f, 0.48f, 0.46f, a * 0.5f));
                        break;
                }
            }
        }
    }
}
