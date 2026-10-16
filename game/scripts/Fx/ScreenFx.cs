using Godot;

namespace TypingShip;

/// <summary>The main camera; it can shake (trauma that decays, squared for the offset, as is the usual trick).</summary>
public partial class ShakeCamera : Camera2D
{
    float trauma;
    float time;

    /// <summary>Adds a shake: 0.2 is a knock, 0.5 a hit, 1 the biggest there is.</summary>
    public void Shake(float amount)
    {
        if (Settings.Shake) trauma = Mathf.Min(1, trauma + amount);
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        time += d;
        trauma = Mathf.Max(0, trauma - d * 1.8f);
        float s = trauma * trauma * 9;
        // Smooth noise rather than a fresh random each frame: it reads as a jolt, not jitter.
        Offset = new Vector2(Mathf.Sin(time * 53) + Mathf.Sin(time * 31.7f), Mathf.Sin(time * 47.3f + 1) + Mathf.Sin(time * 27))
            * s / 2;
    }
}

/// <summary>
/// A flash over the playfield: red at the edges when the player is hit, or the whole screen white for a boss's last
/// blast. Fades out by itself.
/// </summary>
public partial class ScreenFlash : ColorRect
{
    static readonly Shader FlashShader = new()
    {
        Code = """
            shader_type canvas_item;
            uniform vec4 tint : source_color = vec4(1.0);
            uniform float strength = 0.0;
            uniform float edges = 1.0; // 1: a vignette, 0: the whole screen
            void fragment() {
                float d = length(UV - 0.5) * 1.414;
                float vignette = smoothstep(0.45, 1.0, d);
                float a = strength * mix(1.0, vignette, edges);
                COLOR = vec4(tint.rgb, a * tint.a);
            }
            """,
    };

    readonly ShaderMaterial material = new() { Shader = FlashShader };
    float strength, fadeRate;

    public ScreenFlash()
    {
        Material = material;
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(G.StageSize, G.StageSize);
        Visible = false;
    }

    /// <summary>Flashes the screen edges (or all of it) in a colour, fading over <paramref name="seconds"/>.</summary>
    public void Flash(Color tint, float strength, float seconds, bool edgesOnly)
    {
        // With flashes off, a hit's edge glow stays (faintly); full-screen flashes are skipped.
        if (!Settings.Flashes)
        {
            if (!edgesOnly) return;
            strength *= 0.4f;
        }
        if (strength < this.strength) return; // a weaker flash doesn't cut a stronger one short
        this.strength = strength;
        fadeRate = strength / seconds;
        material.SetShaderParameter("tint", tint);
        material.SetShaderParameter("edges", edgesOnly ? 1f : 0f);
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        strength -= fadeRate * (float)delta;
        if (strength <= 0)
        {
            strength = 0;
            Visible = false;
        }
        material.SetShaderParameter("strength", strength);
    }
}

/// <summary>
/// A soft bloom over the playfield. The 2D canvas is HDR (rendering/viewport/hdr_2d), so light brighter than white
/// survives: this blurs only that excess and adds it back. Additive effects that pile up (lasers, flames, explosions,
/// charges) and parts drawn overbright (the cockpit rim) glow; words and the HUD, at most plain white, stay crisp.
/// It reads the screen, so it sits above everything else in the playfield. --noglow leaves it out.
/// </summary>
public partial class Bloom : ColorRect
{
    static readonly Shader BloomShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode blend_add;
            uniform sampler2D screen : hint_screen_texture, filter_linear;
            uniform float intensity = 1.1;
            uniform float spread = 6.0; // the blur's sigma, in screen pixels
            vec3 excess(vec2 uv) { return max(texture(screen, uv).rgb - vec3(1.0), vec3(0.0)); }
            void fragment() {
                // A Gaussian blur of the excess: five rings of twelve taps, each ring turned so the taps don't line up.
                vec3 sum = excess(SCREEN_UV);
                float total = 1.0;
                for (int ring = 1; ring <= 5; ring++) {
                    float r = float(ring) * spread * 0.55;
                    float w = exp(-r * r / (2.0 * spread * spread));
                    for (int k = 0; k < 12; k++) {
                        float a = float(k) * 0.5235988 + float(ring) * 0.2618;
                        sum += excess(SCREEN_UV + vec2(cos(a), sin(a)) * r * SCREEN_PIXEL_SIZE) * w;
                        total += w;
                    }
                }
                COLOR = vec4(sum / total * intensity, 1.0);
            }
            """,
    };

    public Bloom()
    {
        Material = new ShaderMaterial { Shader = BloomShader };
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(G.StageSize, G.StageSize);
    }

    public override void _Process(double delta) => Visible = Settings.Glow;
}
