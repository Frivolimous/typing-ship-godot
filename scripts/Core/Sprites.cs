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
    public static readonly Texture2D Superman = T("Superman");
    public static readonly Texture2D HealthFull = T("HealthFull");
    public static readonly Texture2D HealthBase = T("HealthBase");
}
