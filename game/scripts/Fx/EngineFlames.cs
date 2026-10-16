using Godot;

namespace TypingShip;

/// <summary>
/// Flickering engine flames, drawn behind a sprite's art (add it as a child of the Sprite2D). They live in the art's
/// texture space, so they turn and scale with it.
/// </summary>
public partial class EngineFlames : Node2D
{
    static readonly CanvasItemMaterial Additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

    readonly EngineMount[] mounts = [];
    readonly float[] phase = [];
    float time;

    public EngineFlames() { }

    public EngineFlames(EngineMount[] mounts)
    {
        this.mounts = mounts;
        phase = mounts.Select(_ => G.Rand() * 100).ToArray();
        Material = Additive;
        ShowBehindParent = true;
    }

    /// <summary>Puts flames on a sprite if its art has any; returns them, or null.</summary>
    public static EngineFlames? Attach(Sprite2D art)
    {
        var mounts = Mounts.Flames(art.Texture);
        if (mounts == null) return null;
        var flames = new EngineFlames(mounts);
        art.AddChild(flames);
        return flames;
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int i = 0; i < mounts.Length; i++)
        {
            var m = mounts[i];
            float t = time * 30 + phase[i];
            // Two out-of-step wobbles plus a little noise: a flame that never quite repeats.
            float flicker = 0.85f + 0.1f * Mathf.Sin(t) + 0.08f * Mathf.Sin(t * 2.7f + 1) + 0.06f * G.Rand();
            float len = m.Length * flicker, w = m.Width;
            Flame(m.At, w * 1.5f, len * 1.15f, new Color(m.Colour, 0.25f));
            Flame(m.At, w, len, new Color(m.Colour, 0.6f));
            Flame(m.At, w * 0.5f, len * 0.55f, new Color(Colors.White.Lerp(m.Colour, 0.3f), 0.9f));
            DrawCircle(m.At, w * 0.45f, new Color(Colors.White.Lerp(m.Colour, 0.4f), 0.7f));
        }
    }

    /// <summary>A teardrop pointing down from the nozzle: round at the top, tapering to a point.</summary>
    void Flame(Vector2 at, float width, float length, Color colour)
    {
        const int Steps = 10;
        var pts = new Vector2[Steps * 2 + 1];
        for (int s = 0; s <= Steps; s++)
        {
            float v = s / (float)Steps;
            // Full width just below the nozzle, narrowing along a curve to the tip.
            float half = width / 2 * Mathf.Sqrt(Mathf.Sin(Mathf.Pi * (0.5f + 0.5f * v))) * (1 - v * 0.15f);
            pts[s] = at + new Vector2(-half, v * length);
            pts[Steps * 2 - s] = at + new Vector2(half, v * length);
        }
        pts[Steps] = at + new Vector2(0, length);
        DrawColoredPolygon(pts, colour);
    }
}
