using Godot;

namespace TypingShip;

// Scrolling background: backdrop image, drifting nebulae and three layers of stars, plus the explosions.
// Stars move in the 24 Hz tick and are drawn interpolated between ticks.
public partial class Starfield : Node2D
{
    const float TrueHeight = 550;
    const int Wpm = 60; // the original starfield always scrolled at 60, whatever the difficulty

    class Star
    {
        public float X, Y, V, Size, Alpha, Phase;
        public Color Colour;
        public int Layer;
    }

    // Per layer, far to near: new stars per tick, speed range (px/tick at 60 WPM), size range, brightness range.
    static readonly (float Rate, float V0, float V1, float S0, float S1, float A0, float A1)[] Layers =
    {
        (0.12f, 0.3f, 0.7f, 1, 1, 0.3f, 0.6f),
        (0.13f, 1, 2.2f, 1, 1.6f, 0.6f, 0.9f),
        (0.05f, 3, 5, 1.6f, 2.4f, 0.85f, 1),
    };

    static readonly Color[] StarColours =
    {
        Colors.White, new(0.75f, 0.85f, 1), new(0.7f, 0.78f, 1), new(1, 0.95f, 0.8f), new(1, 0.82f, 0.68f),
    };

    readonly List<Star> stars = new();
    readonly Explosions explosions = new();
    float time;
    readonly List<Sprite2D> nebulae = new();
    int nebTick = 9000;

    public readonly TextureRect Back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    public readonly Node2D NebulaLayer = new();
    public Starfield()
    {
        // Fill the sky to start with: as many stars as each layer has once it's been scrolling a while.
        for (int layer = 0; layer < Layers.Length; layer++)
        {
            var l = Layers[layer];
            int n = (int)(l.Rate * TrueHeight / ((l.V0 + l.V1) / 2));
            for (int i = 0; i < n; i++) stars.Add(NewStar(layer, G.Rand() * TrueHeight));
        }
    }

    Star NewStar(int layer, float y = 0)
    {
        var l = Layers[layer];
        return new Star
        {
            Layer = layer,
            X = G.Rand() * G.StageSize,
            Y = y,
            V = Mathf.Lerp(l.V0, l.V1, G.Rand()),
            Size = Mathf.Lerp(l.S0, l.S1, G.Rand()),
            Alpha = Mathf.Lerp(l.A0, l.A1, G.Rand()),
            Phase = G.Rand() * Mathf.Tau,
            Colour = StarColours[(int)(G.Rand() * StarColours.Length)],
        };
    }

    /// <summary>The explosions' drawing node; it lives in the game view, below the ships.</summary>
    public Node2D Particles => explosions;

    public void SetBack(int level) => Back.Texture = Sprites.Back[Math.Min(level / 4, Sprites.Back.Length - 1)];

    public void Update()
    {
        for (int layer = 0; layer < Layers.Length; layer++)
            if (G.Rand() < Layers[layer].Rate * Wpm / 60)
                stars.Add(NewStar(layer, -4));

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

        stars.RemoveAll(s => (s.Y += s.V * Wpm / 60f) > TrueHeight + 10);

        for (int i = nebulae.Count - 1; i >= 0; i--)
        {
            nebulae[i].Position += new Vector2(0, Wpm / 100f);
            if (nebulae[i].Position.Y > TrueHeight)
            {
                nebulae[i].QueueFree();
                nebulae.RemoveAt(i);
            }
        }

        explosions.Update();
    }

    public void Explosion(float x = -1, float y = -1, int c = 50)
    {
        G.Sound.Sound(c > 20 ? SoundControl.ExplodeB : SoundControl.ExplodeS);
        var point = x == -1 ? new Vector2(G.Rand() * 550, G.Rand() * 550) : new Vector2(x, y);
        explosions.Add(point, c);
    }

    /// <summary>A shockwave ring in the game view, with no explosion or sound.</summary>
    public void Shockwave(Vector2 at, float radius, int ticks) => explosions.Shockwave(at, radius, ticks);

    public void ClearParticles() => explosions.Clear();

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    // Stars, drawn in stage space: far ones twinkle, near ones glow.
    public override void _Draw()
    {
        float back = 1 - (float)Engine.GetPhysicsInterpolationFraction();
        foreach (var s in stars)
        {
            float v = s.V * Wpm / 60f;
            var at = new Vector2(s.X, s.Y - v * back);
            switch (s.Layer)
            {
                case 0:
                    float twinkle = 0.65f + 0.35f * Mathf.Sin(time * 3 + s.Phase);
                    DrawRect(new Rect2(at, Vector2.One), new Color(s.Colour, s.Alpha * twinkle));
                    break;
                case 1:
                    DrawRect(new Rect2(at, new Vector2(s.Size, s.Size)), new Color(s.Colour, s.Alpha));
                    break;
                default:
                    var c = new Vector2(s.Size, s.Size) / 2;
                    DrawCircle(at + c, s.Size * 1.8f, new Color(s.Colour, s.Alpha * 0.18f));
                    DrawRect(new Rect2(at, new Vector2(s.Size, s.Size)), new Color(s.Colour, s.Alpha));
                    break;
            }
        }
    }
}
