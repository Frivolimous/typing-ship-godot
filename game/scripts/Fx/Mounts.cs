using System.Globalization;
using Godot;
using TypingShip.Core.Data;

namespace TypingShip;

/// <summary>A nozzle on a piece of art: where it is (texture pixels from the art's centre), how big its flame is.</summary>
public readonly record struct EngineMount(Vector2 At, float Width, float Length, Color Colour);

/// <summary>Where effects sit on each piece of art (data/mounts.txt): engine flames and the weapon's charge point.</summary>
public static class Mounts
{
    const string File = "mounts.txt";

    static readonly Dictionary<string, Color> Colours = new()
    {
        ["blue"] = new(0.35f, 0.6f, 1),
        ["cyan"] = new(0.3f, 0.95f, 1),
        ["orange"] = new(1, 0.55f, 0.15f),
        ["red"] = new(1, 0.25f, 0.15f),
        ["purple"] = new(0.7f, 0.35f, 1),
        ["white"] = new(0.9f, 0.9f, 1),
    };

    static Dictionary<string, EngineMount[]> flames = new();
    static Dictionary<string, Vector2> charges = new();

    /// <summary>Reads mounts.txt; a mistake throws a DataError naming the line. A missing file means no mounts.</summary>
    public static void Load(DataSource source)
    {
        var text = source.Read(File);
        var foundFlames = new Dictionary<string, List<EngineMount>>();
        var foundCharges = new Dictionary<string, Vector2>();
        var lines = text?.Split('\n') ?? [];
        for (int n = 0; n < lines.Length; n++)
        {
            var line = lines[n];
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            var f = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length == 0) continue;
            DataError Error(string problem) => new(File, n + 1, problem);
            if (f.Length < 3) throw Error("expected: sprite  x,y  flame width length colour   or   sprite  x,y  charge");
            var xy = f[1].Split(',');
            if (xy.Length != 2 || !Num(xy[0], out float x) || !Num(xy[1], out float y))
                throw Error($"\"{f[1]}\" isn't a position like -28,94");
            var at = new Vector2(x, y);
            switch (f[2])
            {
                case "flame":
                    if (f.Length != 6) throw Error("expected: sprite  x,y  flame  width  length  colour");
                    if (!Num(f[3], out float w) || w <= 0) throw Error($"\"{f[3]}\" isn't a width");
                    if (!Num(f[4], out float l) || l <= 0) throw Error($"\"{f[4]}\" isn't a length");
                    if (!Colours.TryGetValue(f[5], out var c))
                        throw Error($"\"{f[5]}\" isn't a colour (use {string.Join(", ", Colours.Keys)})");
                    if (!foundFlames.TryGetValue(f[0], out var list)) foundFlames[f[0]] = list = new();
                    list.Add(new EngineMount(at, w, l, c));
                    break;
                case "charge":
                    if (f.Length != 3) throw Error("expected: sprite  x,y  charge");
                    if (foundCharges.ContainsKey(f[0])) throw Error($"{f[0]} already has a charge point");
                    foundCharges[f[0]] = at;
                    break;
                default:
                    throw Error($"\"{f[2]}\" should be flame or charge");
            }
        }
        flames = foundFlames.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
        charges = foundCharges;
    }

    static bool Num(string s, out float v) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    /// <summary>A texture's sprite name (its path under assets/sprites, without .png), or null.</summary>
    static string? Name(Texture2D? tex)
    {
        const string prefix = "res://assets/sprites/";
        var path = tex?.ResourcePath ?? "";
        return path.StartsWith(prefix) && path.EndsWith(".png") ? path[prefix.Length..^4] : null;
    }

    /// <summary>The engine flames on a piece of art, or null if it has none.</summary>
    public static EngineMount[]? Flames(Texture2D? tex) => Name(tex) is { } n ? flames.GetValueOrDefault(n) : null;

    /// <summary>Where the art's weapon charges up, in texture pixels from its centre, or null if it isn't listed.</summary>
    public static Vector2? Charge(Texture2D? tex) =>
        Name(tex) is { } n && charges.TryGetValue(n, out var at) ? at : null;
}
