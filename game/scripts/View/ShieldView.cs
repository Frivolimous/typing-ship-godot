using Godot;

namespace TypingShip;

// Blue energy bubble; flashes white and fades when knocked out. Drawn by a shader: a near-clear middle deepening to a
// bright rim, a soft glow outside it, a highlight at the top left and slow ripples running outwards.
public partial class ShieldView : Node2D, IViewTicker
{
    static readonly Shader Bubble = new()
    {
        Code = """
            shader_type canvas_item;
            uniform float breaking = 0.0;
            uniform float seed = 0.0;
            const float OUTER = 1.3; // the quad reaches past the rim, for the glow
            void fragment() {
                vec2 p = (UV * 2.0 - 1.0) * OUTER;
                float d = length(p);
                if (d > OUTER) discard;
                vec3 deep = vec3(0.0, 0.3, 0.85), mid = vec3(0.1, 0.7, 1.0), rim = vec3(0.75, 0.97, 1.0);
                vec3 col;
                float a;
                if (d <= 1.0) {
                    float f = d * d * d;
                    col = mix(deep, mid, f);
                    a = mix(0.05, 0.42, f);
                    float ripple = 0.5 + 0.5 * sin(d * 16.0 - TIME * 2.5 + seed);
                    a += ripple * 0.06 * f;
                    float line = smoothstep(0.84, 0.96, d) * (1.0 - smoothstep(0.97, 1.0, d) * 0.6);
                    col = mix(col, rim, line);
                    a = max(a, line * 0.9);
                    float sheen = smoothstep(0.55, 0.0, length(p - vec2(-0.38, -0.42)));
                    col += sheen * 0.35;
                    a += sheen * 0.18;
                } else {
                    float g = 1.0 - (d - 1.0) / (OUTER - 1.0);
                    col = mid;
                    a = 0.32 * g * g;
                }
                col = mix(col, vec3(1.0, 0.9, 0.9), breaking);
                a = mix(a, min(1.0, a * 1.5 + (d < 1.0 ? 0.35 : 0.0)), breaking);
                COLOR = vec4(col, clamp(a, 0.0, 1.0)) * vec4(1.0, 1.0, 1.0, COLOR.a);
            }
            """,
    };

    static readonly Texture2D White = MakeWhite();

    static Texture2D MakeWhite()
    {
        var img = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        img.Fill(Colors.White);
        return ImageTexture.CreateFromImage(img);
    }

    readonly ShaderMaterial material = new() { Shader = Bubble };
    Vector2 radius = new(10, 10);
    bool breaking, fading;

    public ShieldView()
    {
        material.SetShaderParameter("seed", G.Rand() * 10);
        Material = material;
    }

    public void SetSize(float w, float h, float x = 0, float y = 0)
    {
        radius = new Vector2(w / 2, h / 2);
        Position = new Vector2(x, y);
        QueueRedraw();
    }

    public void Add(Node2D target)
    {
        Modulate = Colors.White;
        breaking = false;
        fading = false;
        GetParent()?.RemoveChild(this);
        target.AddChild(this);
        QueueRedraw();
    }

    public float Alpha
    {
        get => Modulate.A;
        set => Modulate = new Color(1, 1, 1, Math.Clamp(value, 0, 1)); // Flash clamped alpha too
    }

    public void Remove()
    {
        breaking = true;
        QueueRedraw();
        if (!fading)
        {
            fading = true;
            G.Ctrl.AddTicker(this);
        }
    }

    public bool Tick()
    {
        if (!fading || !IsInstanceValid(this)) return false; // freed along with its ship
        var m = Modulate;
        m.A -= 0.1f;
        Modulate = m;
        if (m.A > 0) return true;
        fading = false;
        GetParent()?.RemoveChild(this);
        return false;
    }

    public override void _Draw()
    {
        // An ellipse width x height, as the original's stretched circle; the quad is bigger, for the outer glow.
        material.SetShaderParameter("breaking", breaking ? 1f : 0f);
        var half = radius * 1.3f;
        DrawTextureRect(White, new Rect2(-half, half * 2), false);
    }
}
