using Godot;

namespace TypingShip;

// The bitmaps the Flash SpriteSheets class held, loaded by their old linkage names.
public static class Sprites
{
    static Texture2D T(string name) => GD.Load<Texture2D>($"res://assets/sprites/{name}.png");

    // The ship art (every set) points up and is drawn at AiScale.
    public const float AiScale = 0.5f;
    /// <summary>Missiles are drawn smaller than their art's AiScale size (Jeremy: they read too big).</summary>
    public const float MissileScale = AiScale * 0.55f;

    // ---- Art sets ----
    // The ships, missiles, turret and medals come from an art set: a folder under assets/sprites with a player.png
    // ("ai", "custom", ...), chosen in the options, with F2, or with --art=NAME. Art a set doesn't have is borrowed
    // from "ai", so a half-finished set still plays. Flames and charge points are per set (data/mounts.txt). The
    // rules always measure the "ai" bosses (gun ports, hangars, where the scanner lands); a set's boss art is only
    // drawn in their place (BossLook).

    public const string DefaultSet = "ai";
    const string Root = "res://assets/sprites";

    /// <summary>The art set in use.</summary>
    public static string ArtSet { get; private set; } = DefaultSet;

    /// <summary>The art sets there are: "ai" first, then the rest by name.</summary>
    public static IReadOnlyList<string> ArtSets { get; } = FindSets();

    static List<string> FindSets()
    {
        var sets = DirAccess.GetDirectoriesAt(Root)
            .Where(d => ResourceLoader.Exists($"{Root}/{d}/player.png"))
            .OrderBy(d => d == DefaultSet ? "" : d, StringComparer.Ordinal)
            .ToList();
        if (!sets.Contains(DefaultSet)) sets.Insert(0, DefaultSet);
        return sets;
    }

    /// <summary>Raised when the art set changes, so what's on screen can swap its art.</summary>
    public static event Action? ArtChanged;

    /// <summary>Switches to an art set (an unknown name means "ai").</summary>
    public static void UseArtSet(string set)
    {
        set = ArtSets.Contains(set) ? set : DefaultSet;
        if (set == ArtSet) return;
        ArtSet = set;
        LoadArt();
        ArtChanged?.Invoke();
    }

    /// <summary>The set after the current one, round and round.</summary>
    public static string NextArtSet() => ArtSets[(ArtSets.ToList().IndexOf(ArtSet) + 1) % ArtSets.Count];

    /// <summary>A sprite from the current set, or from "ai" if the set hasn't got it.</summary>
    static Texture2D A(string name) => ResourceLoader.Exists($"{Root}/{ArtSet}/{name}.png") ? T($"{ArtSet}/{name}") : T($"{DefaultSet}/{name}");

    /// <summary>A sprite only if the current set has it.</summary>
    static Texture2D? Own(string name) => ResourceLoader.Exists($"{Root}/{ArtSet}/{name}.png") ? T($"{ArtSet}/{name}") : null;

    static Sprites() => LoadArt();

    static void LoadArt()
    {
        Player = new[] { A("player") };
        // Each enemy is a single frame or an animation; index 9 (the invisible "nl" laser) has no art.
        Enemy = new[]
        {
            new[] { A("missile_s") }, new[] { A("laser_s") }, new[] { A("mine") }, new[] { A("missile_m") }, new[] { A("laser_m") },
            new[] { A("bomber") }, new[] { A("missile_l") }, new[] { A("laser_l") }, new[] { A("stealth") }, null,
        };
        PMissile = new[] { A("projectile_blue") };
        EMissile = new[] { A("projectile_red") };
        Turret = A("turret");
        // A set may give the missile ships (turret_m) and laser ships (turret_l) turrets of their own.
        Turrets = Enumerable.Range(0, Enemy.Length).Select(art => art switch
        {
            0 or 3 or 6 => Own("turret_m") ?? Turret,
            1 or 4 or 7 => Own("turret_l") ?? Turret,
            _ => Turret,
        }).ToArray();
        Achieve = new[] { A("medal_locked"), A("medal_bronze"), A("medal_silver"), A("medal_gold") };
    }

    public static Texture2D[] Player { get; private set; } = null!;
    public static Texture2D[]?[] Enemy { get; private set; } = null!;
    public static Texture2D[] PMissile { get; private set; } = null!;
    public static Texture2D[] EMissile { get; private set; } = null!;
    /// <summary>Points right, with the pivot at the centre of its round base (texture pixels from the top left).</summary>
    public static Texture2D Turret { get; private set; } = null!;
    /// <summary>The turret for each enemy art (as <see cref="Enemy"/>): Turret, or the set's missile or laser turret.</summary>
    public static Texture2D[] Turrets { get; private set; } = null!;
    /// <summary>Medals by tier (0 = not earned yet: a dark, locked medal). The art is twice its usual on-screen size.</summary>
    public static Texture2D[] Achieve { get; private set; } = null!;

    public static readonly Vector2 TurretPivot = new(18, 18);
    public static readonly Texture2D[] Nebula = { T("Nebula0"), T("Nebula1"), T("Nebula2") };
    public static readonly Texture2D[] Back = { T("StarBack0"), T("StarBack1"), T("StarBack2") };

    static readonly Dictionary<string, Texture2D> named = new();

    /// <summary>A sprite by its path under assets/sprites, as the rules name boss art ("ai/boss0").</summary>
    public static Texture2D Named(string name) => named.TryGetValue(name, out var t) ? t : named[name] = T(name);

    /// <summary>
    /// How a boss looks: the current set's own copy of its art ("custom/boss0" for "ai/boss0"), or the art itself. A
    /// set's copy must be the same picture at any size; the rules keep using the original.
    /// </summary>
    public static Texture2D BossLook(string art) => Own(art[(art.IndexOf('/') + 1)..]) ?? Named(art);

    /// <summary>Which pixels of a sprite are visible, for the rules (the boss scanner lands on visible pixels).</summary>
    public static ArtMask Mask(string name)
    {
        var img = Named(name).GetImage();
        img.Convert(Image.Format.Rgba8);
        var data = img.GetData();
        var opaque = new bool[img.GetWidth() * img.GetHeight()];
        for (int i = 0; i < opaque.Length; i++) opaque[i] = data[i * 4 + 3] > 0;
        return new ArtMask(img.GetWidth(), img.GetHeight(), opaque);
    }

    // The chameleon cheat (SpriteSheets.recolour): a random hue shift with boosted saturation on the player's ship.
    // Flash's AdjustColor filter is approximated with the same luminance-weighted hue rotation matrix.
    static ShaderMaterial? playerMaterial;

    public static void Recolour(Sprite2D player)
    {
        playerMaterial ??= new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type canvas_item;
                    uniform float hue = 0.0;
                    uniform float saturation = 1.0;
                    void fragment() {
                        vec3 c = COLOR.rgb;
                        float l = dot(c, vec3(0.3086, 0.6094, 0.0820));
                        c = mix(vec3(l), c, saturation);
                        float cs = cos(hue), sn = sin(hue);
                        const vec3 lum = vec3(0.213, 0.715, 0.072);
                        mat3 m = mat3(
                            vec3(lum.r + cs * (1.0 - lum.r) - sn * lum.r, lum.r - cs * lum.r + sn * 0.143, lum.r - cs * lum.r - sn * (1.0 - lum.r)),
                            vec3(lum.g - cs * lum.g - sn * lum.g, lum.g + cs * (1.0 - lum.g) + sn * 0.140, lum.g - cs * lum.g + sn * lum.g),
                            vec3(lum.b - cs * lum.b + sn * (1.0 - lum.b), lum.b - cs * lum.b - sn * 0.283, lum.b + cs * (1.0 - lum.b) + sn * lum.b));
                        COLOR.rgb = clamp(m * c, 0.0, 1.0);
                    }
                    """,
            },
        };
        playerMaterial.SetShaderParameter("hue", Mathf.DegToRad(G.Rand() * 360 - 180));
        playerMaterial.SetShaderParameter("saturation", 2.0f);
        player.Material = playerMaterial;
    }

    public static readonly Texture2D Superman = T("Superman");

}
