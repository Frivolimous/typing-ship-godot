using Godot;

namespace TypingShip;

// Scrolling background: backdrop image, drifting nebulae, pixel stars, and explosion particles.
// Stars and particles move in the 24 Hz tick and are drawn interpolated between ticks.
public partial class Starfield : Node2D
{
    const float TrueHeight = 550;
    const int Wpm = 60; // the original starfield always scrolled at 60, whatever the difficulty

    class Star
    {
        public float X, Y, V;
        public Star(float v, float x, float y = 0) { V = v; X = x; Y = y; }
    }

    class Particle
    {
        public float X, Y, VX, VY, R, Timer, Life;
    }

    readonly List<Star> stars = new();
    readonly List<Particle> particles = new();
    readonly List<Sprite2D> nebulae = new();
    int nebTick = 9000;

    public readonly TextureRect Back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    public readonly Node2D NebulaLayer = new();
    readonly ParticleLayer particleLayer;

    public Starfield()
    {
        particleLayer = new ParticleLayer(this);
        for (int i = 0; i < 40; i++)
            stars.Add(new Star(1 + G.Rand(), Mathf.Floor(G.Rand() * 550), G.Rand() * TrueHeight));
    }

    /// <summary>The particle drawing node; it lives in the game view, below the ships.</summary>
    public Node2D Particles => particleLayer;

    public void SetBack(int level) => Back.Texture = Sprites.Back[Math.Min(level / 4, Sprites.Back.Length - 1)];

    public void Update()
    {
        if (G.Rand() < 0.3f * Wpm / 60)
        {
            var s = new Star(1 + G.Rand() * 3, Mathf.Floor(G.Rand() * G.StageSize));
            stars.Add(s);
            if (s.V > 3)
            {
                stars.Add(new Star(s.V, s.X + 1));
                stars.Add(new Star(s.V, s.X + 1, -1));
                stars.Add(new Star(s.V, s.X, -1));
            }
            else if (s.V > 2)
            {
                stars.Add(new Star(s.V, s.X + 1));
            }
        }

        if (nebTick > 100000 / Wpm)
        {
            var tex = Sprites.Nebula[(int)Mathf.Floor(G.Rand() * 3)];
            float width = 50 + G.Rand() * 100;
            float scale = width / tex.GetWidth();
            var neb = new Sprite2D { Texture = tex, Centered = false, Scale = new Vector2(scale, scale) };
            neb.Position = new Vector2(20 + G.Rand() * (G.StageSize - 20 - width), -tex.GetHeight() * scale);
            NebulaLayer.AddChild(neb);
            neb.ResetPhysicsInterpolation();
            nebulae.Add(neb);
            nebTick = 0;
        }
        else
        {
            nebTick++;
        }

        stars.RemoveAll(s => (s.Y += s.V * Wpm / 60f) > TrueHeight);

        for (int i = nebulae.Count - 1; i >= 0; i--)
        {
            nebulae[i].Position += new Vector2(0, Wpm / 100f);
            if (nebulae[i].Position.Y > TrueHeight)
            {
                nebulae[i].QueueFree();
                nebulae.RemoveAt(i);
            }
        }

        particles.RemoveAll(p =>
        {
            p.X += p.VX * Wpm / 60f;
            p.Y += p.VY * Wpm / 60f;
            p.Timer += 5f * Wpm / 60f;
            return p.Timer > p.Life;
        });
    }

    public void Explosion(float x = -1, float y = -1, int c = 50)
    {
        G.Sound.Sound(c > 20 ? SoundControl.ExplodeB : SoundControl.ExplodeS);
        var point = x == -1 ? new Vector2(G.Rand() * 550, G.Rand() * 550) : new Vector2(x, y);
        for (int i = 0; i < c; i++)
        {
            float r = 1 + G.Rand() * 4, d = G.Rand() * Mathf.Tau, v = G.Rand() * 0.5f;
            var p = new Particle { R = r, VX = v * Mathf.Sin(d), VY = v * Mathf.Cos(d), Life = 200 + G.Rand() * 100 };
            p.X = point.X + p.VX * 10;
            p.Y = point.Y + p.VY * 10;
            particles.Add(p);
        }
    }

    public void ClearParticles() => particles.Clear();

    public override void _Process(double delta) => QueueRedraw();

    // Stars, drawn in stage space.
    public override void _Draw()
    {
        float back = 1 - (float)Engine.GetPhysicsInterpolationFraction();
        foreach (var s in stars)
            DrawRect(new Rect2(s.X, s.Y - s.V * Wpm / 60f * back, 1, 1), Colors.White);
    }

    partial class ParticleLayer : Node2D
    {
        readonly Starfield owner = null!;
        static readonly Color Grey = new(0xccccccff);

        public ParticleLayer() { }
        public ParticleLayer(Starfield owner) => this.owner = owner;

        public override void _Process(double delta) => QueueRedraw();

        public override void _Draw()
        {
            float back = (1 - (float)Engine.GetPhysicsInterpolationFraction()) * Wpm / 60f;
            foreach (var p in owner.particles)
            {
                float alpha = 0.2f + 0.8f * (1 - p.Timer / p.Life);
                DrawCircle(new Vector2(p.X - p.VX * back, p.Y - p.VY * back), p.R, new Color(Grey, alpha));
            }
        }
    }
}
