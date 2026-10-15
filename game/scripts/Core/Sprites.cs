using Godot;

namespace TypingShip;

// The bitmaps the Flash SpriteSheets class held, loaded by their old linkage names.
public static class Sprites
{
    static Texture2D T(string name) => GD.Load<Texture2D>($"res://assets/sprites/{name}.png");

    public static readonly Texture2D[] Player = { T("PShip0"), T("PShip1"), T("PShip2") };

    // Each enemy is a single frame or an animation; index 9 (the invisible "nl" laser) has no art.
    public static readonly Texture2D[]?[] Enemy =
    {
        new[] { T("Enemy0") },
        new[] { T("Enemy1") },
        new[]
        {
            T("Enemy2a"), T("Enemy2a"), T("Enemy2a"), T("Enemy2a"), T("Enemy2a"), T("Enemy2b"), T("Enemy2c"),
            T("Enemy2d"), T("Enemy2d"), T("Enemy2d"), T("Enemy2b"),
        },
        new[] { T("Enemy3") },
        new[] { T("Enemy4") },
        new[] { T("Enemy5") },
        new[] { T("Enemy6") },
        new[] { T("Enemy7") },
        new[] { T("Enemy8") },
        null,
    };

    public static readonly Texture2D[] PMissile = { T("PMiss0"), T("PMiss1"), T("PMiss2") };
    public static readonly Texture2D[] EMissile = { T("EMiss0"), T("EMiss1"), T("EMiss2") };
    public static readonly Texture2D[] Nebula = { T("Nebula0"), T("Nebula1"), T("Nebula2") };
    public static readonly Texture2D[] Dash = { T("Dash0"), T("Dash1"), T("Dash2"), T("Dash3"), T("Dash4"), T("Dash5") };
    public static readonly Texture2D[] Back = { T("StarBack0"), T("StarBack1"), T("StarBack2") };
    public static readonly Texture2D[] Boss = { T("Boss0"), T("Boss1a"), T("Boss2") };

    // SpriteSheets.bRes: boss 0's hangar doors (closed, open, open with ships), boss 1's glow,
    // boss 2's charge-up frames and its beam (plain, then the three frames of the beam hitting the player's shield).
    public static readonly Texture2D[] Door = { T("Door0"), T("Door1"), T("Door2") };
    public static readonly Texture2D Boss1Glow = T("Boss1b");
    public static readonly Texture2D[] Boss2Charge = { T("Boss2a"), T("Boss2b"), T("Boss2c") };
    public static readonly Texture2D[] Boss2Beam = { T("Boss2L0"), T("Boss2L1a"), T("Boss2L1c"), T("Boss2L1b") };

    static readonly Dictionary<string, Texture2D> named = new();

    /// <summary>A sprite by its linkage name ("Boss0", "Door1"), as the rules name boss art.</summary>
    public static Texture2D Named(string name) => named.TryGetValue(name, out var t) ? t : named[name] = T(name);

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
    public static readonly Texture2D HealthFull = T("HealthFull");
    public static readonly Texture2D HealthBase = T("HealthBase");

    // Menu art. Each full-stage bitmap is 550x550 and drawn at the stage origin.
    public static readonly Texture2D MenuBack = T("MenuB"), MapBack = T("MapB"), ScoreBack = T("ScoreB"), LoseBack = T("LoseB"),
        WinBack = T("WinB"), FinalBack = T("FinalB"), AchieveBack = T("AchieveB"), Credits = T("CreditsB");
    public static readonly Texture2D[] Twinkle = { T("MenuB1"), T("MenuB2"), T("MenuB3") };
    public static readonly Texture2D Title0 = T("Title0"), Title1 = T("Title1");
    public static readonly Texture2D StartBt = T("StartBt"), HighBt = T("HighBt"), AchieveBt = T("AchieveBt"), MoreBt = T("MoreBt"),
        CreditsBt = T("CreditsBt");
    public static readonly Texture2D LockC = T("LockC"), NextC = T("NextC"), CompleteC = T("CompleteC");
    // Medals under the map's level buttons, by tier; tier 0 has none.
    public static readonly Texture2D?[] Health = { null, T("Health1"), T("Health2"), T("Health3") };
    public static readonly Texture2D?[] Target = { null, T("Target1"), T("Target2"), T("Target3") };
    // Trophies by tier (0 = not earned), and the achievements screen's bronze/silver/gold stars, off and on.
    public static readonly Texture2D[] Achieve = { T("Achieve0"), T("Achieve1"), T("Achieve2"), T("Achieve3") };
    public static readonly Texture2D[] Star = { T("Star00"), T("Star01"), T("Star10"), T("Star11"), T("Star20"), T("Star21") };
}
