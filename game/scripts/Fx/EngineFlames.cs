using System.Globalization;
using Godot;
using TypingShip.Core.Data;

namespace TypingShip;

/// <summary>A nozzle on a piece of art: where it is (texture pixels from the art's centre), how big its flame is.</summary>
public readonly record struct EngineMount(Vector2 At, float Width, float Length, Color Colour);

/// <summary>The engine flames in data/engines.txt, by sprite name.</summary>
public static class EngineMounts
{
    const string File = "engines.txt";

    static readonly Dictionary<string, Color> Colours = new()
    {
        ["blue"] = new(0.35f, 0.6f, 1),
        ["cyan"] = new(0.3f, 0.95f, 1),
        ["orange"] = new(1, 0.55f, 0.15f),
        ["red"] = new(1, 0.25f, 0.15f),
        ["purple"] = new(0.7f, 0.35f, 1),
        ["white"] = new(0.9f, 0.9f, 1),
    };

    static Dictionary<string, EngineMount[]> mounts = new();

    /// <summary>Reads engines.txt; a mistake throws a DataError naming the line. A missing file means no flames.</summary>
    public static void Load(DataSource source)
    {
        var text = source.Read(File);
        var found = new Dictionary<string, List<EngineMount>>();
        if (text != null)
        {
            var lines = text.Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                var line = lines[n];
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line[..hash];
                var f = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length == 0) continue;
                if (f.Length != 5)
                    throw new DataError(File, n + 1, "expected: sprite  x,y  width  length  colour");
                var xy = f[1].Split(',');
                if (xy.Length != 2 || !Num(xy[0], out float x) || !Num(xy[1], out float y))
                    throw new DataError(File, n + 1, $"\"{f[1]}\" isn't a position like -28,94");
                if (!Num(f[2], out float w) || w <= 0) throw new DataError(File, n + 1, $"\"{f[2]}\" isn't a width");
                if (!Num(f[3], out float l) || l <= 0) throw new DataError(File, n + 1, $"\"{f[3]}\" isn't a length");
                if (!Colours.TryGetValue(f[4], out var c))
                    throw new DataError(File, n + 1, $"\"{f[4]}\" isn't a colour (use {string.Join(", ", Colours.Keys)})");
                if (!found.TryGetValue(f[0], out var list)) found[f[0]] = list = new();
                list.Add(new EngineMount(new Vector2(x, y), w, l, c));
            }
        }
        mounts = found.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    static bool Num(string s, out float v) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    /// <summary>The flames on a texture loaded from assets/sprites, or null if it has none.</summary>
    public static EngineMount[]? For(Texture2D? tex)
    {
        const string prefix = "res://assets/sprites/";
        var path = tex?.ResourcePath ?? "";
        if (!path.StartsWith(prefix) || !path.EndsWith(".png")) return null;
        return mounts.GetValueOrDefault(path[prefix.Length..^4]);
    }
}

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
        var mounts = EngineMounts.For(art.Texture);
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
