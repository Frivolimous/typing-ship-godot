namespace TypingShip.Core;

/// <summary>
/// A 2D point or vector in float, with the same arithmetic as Godot's Vector2 (operation for operation), so the
/// rules give the same numbers they did when they ran on Godot types.
/// </summary>
public readonly record struct Vec2(float X, float Y)
{
    public static readonly Vec2 Zero = new(0, 0);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator -(Vec2 a) => new(-a.X, -a.Y);
    public static Vec2 operator *(Vec2 a, float s) => new(a.X * s, a.Y * s);
    public static Vec2 operator /(Vec2 a, float s) => new(a.X / s, a.Y / s);

    public float DistanceTo(Vec2 to) => MathF.Sqrt((X - to.X) * (X - to.X) + (Y - to.Y) * (Y - to.Y));

    public Vec2 Rotated(float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        return new Vec2(X * cos - Y * sin, X * sin + Y * cos);
    }

    public override string ToString() => $"({X}, {Y})";
}
