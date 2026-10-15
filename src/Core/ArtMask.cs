using System.IO.Compression;

namespace TypingShip.Core;

/// <summary>
/// Which pixels of a picture are visible. The bosses' scanner rings land on a random visible pixel of the boss art,
/// so that much of the art is part of the rules. The game builds masks from its textures; tests and tools read the
/// PNG files directly (<see cref="FromPng"/>).
/// </summary>
public sealed class ArtMask
{
    readonly bool[] opaque;

    public int Width { get; }
    public int Height { get; }

    public ArtMask(int width, int height, bool[] opaque)
    {
        if (opaque.Length != width * height) throw new ArgumentException("Mask size doesn't match width x height.");
        Width = width;
        Height = height;
        this.opaque = opaque;
    }

    public Vec2 Size => new(Width, Height);

    /// <summary>BitmapData.getPixel32(x, y) >= 0x01000000: is this pixel not fully transparent?</summary>
    public bool Opaque(float x, float y)
    {
        int px = (int)x, py = (int)y;
        if (px < 0 || py < 0 || px >= Width || py >= Height) return false;
        return opaque[py * Width + px];
    }

    /// <summary>A random visible pixel, in picture pixels.</summary>
    public Vec2 RandomOpaquePixel(Rng rng)
    {
        Vec2 p;
        do p = new Vec2(rng.Rand() * Width, rng.Rand() * Height);
        while (!Opaque(p.X, p.Y));
        return p;
    }

    /// <summary>Reads the alpha of an 8-bit, non-interlaced PNG (greyscale, RGB or RGBA; the art is RGBA).</summary>
    public static ArtMask FromPng(byte[] png)
    {
        if (png.Length < 33 || png[1] != 'P' || png[2] != 'N' || png[3] != 'G') throw new InvalidDataException("Not a PNG.");
        int width = 0, height = 0, colorType = 0;
        var idat = new MemoryStream();
        for (int pos = 8; pos + 8 <= png.Length;)
        {
            int length = (png[pos] << 24) | (png[pos + 1] << 16) | (png[pos + 2] << 8) | png[pos + 3];
            var type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
            int data = pos + 8;
            if (type == "IHDR")
            {
                width = (png[data] << 24) | (png[data + 1] << 16) | (png[data + 2] << 8) | png[data + 3];
                height = (png[data + 4] << 24) | (png[data + 5] << 16) | (png[data + 6] << 8) | png[data + 7];
                int bitDepth = png[data + 8];
                colorType = png[data + 9];
                if (bitDepth != 8 || png[data + 12] != 0 || colorType is not (0 or 2 or 4 or 6))
                    throw new InvalidDataException("Only 8-bit, non-interlaced greyscale/RGB/RGBA PNGs are supported.");
            }
            else if (type == "IDAT") idat.Write(png, data, length);
            else if (type == "IEND") break;
            pos = data + length + 4; // skip the CRC
        }

        int channels = colorType switch { 0 => 1, 2 => 3, 4 => 2, _ => 4 };
        int stride = width * channels;
        idat.Position = 0;
        using var z = new ZLibStream(idat, CompressionMode.Decompress);
        var raw = new byte[(stride + 1) * height];
        z.ReadExactly(raw);

        var prev = new byte[stride];
        var line = new byte[stride];
        var mask = new bool[width * height];
        for (int y = 0; y < height; y++)
        {
            int filter = raw[y * (stride + 1)];
            Array.Copy(raw, y * (stride + 1) + 1, line, 0, stride);
            for (int i = 0; i < stride; i++)
            {
                int a = i >= channels ? line[i - channels] : 0, b = prev[i], c = i >= channels ? prev[i - channels] : 0;
                line[i] = (byte)(line[i] + filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException($"Bad PNG filter {filter}."),
                });
            }
            for (int x = 0; x < width; x++)
                mask[y * width + x] = channels is 2 or 4 ? line[x * channels + channels - 1] > 0 : true;
            (prev, line) = (line, prev);
        }
        return new ArtMask(width, height, mask);
    }

    static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}

/// <summary>The art masks the rules need, by sprite name (the original's linkage names: "Boss0", "Door0", ...).</summary>
public sealed class ArtLibrary
{
    readonly Dictionary<string, ArtMask> masks = new();
    readonly Func<string, ArtMask>? loader;

    /// <param name="loader">Loads a mask the first time it's asked for.</param>
    public ArtLibrary(Func<string, ArtMask>? loader = null) => this.loader = loader;

    public void Add(string name, ArtMask mask) => masks[name] = mask;

    public ArtMask this[string name] =>
        masks.TryGetValue(name, out var m) ? m
        : loader != null ? masks[name] = loader(name)
        : throw new KeyNotFoundException($"No art mask for {name}.");

    /// <summary>Reads the PNGs in a folder (game/assets/sprites in the repo).</summary>
    public static ArtLibrary FromFolder(string folder) =>
        new(name => ArtMask.FromPng(File.ReadAllBytes(Path.Combine(folder, name + ".png"))));
}
