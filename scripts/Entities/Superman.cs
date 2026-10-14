namespace TypingShip;

// The "superman" cheat: a little hero flies up the screen. It has no word; "kamikaze" blows it up for 99 points.
public partial class Superman : GameSprite
{
    public Superman() => ViewSource = new[] { Sprites.Superman };

    // The original never removed it, so typing "superman" stopped the level from ever ending.
    public override bool MoreUpdate()
    {
        if (Position.Y > -100) return true;
        Remove();
        return false;
    }
}
