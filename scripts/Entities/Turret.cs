using Godot;

namespace TypingShip;

// Point-defence gun on an enemy ship: shoots down player missiles that come within 100 px.
public partial class Turret : GameSprite
{
    public Turret()
    {
        Name = "Turret";
        A = 0;
        TurnRate = 1;
        WordSize = 4;
        KillBy = ActionControl.Laser;
        AddWord();
        Priority = 1;
    }

    EnemyShip Ship => (EnemyShip)GetParent();

    public override bool Update()
    {
        if (TextObject != null) TextObject.Rotation = -Rotation - Ship.Rotation;
        return true;
    }

    public void TestTarget(Missile t)
    {
        var ship = Ship;
        var pos = ship.Position + Position;
        Rotation = Mathf.Atan2(t.Position.Y - pos.Y, t.Position.X - pos.X) - ship.Rotation;
        if (ship.Position.DistanceTo(t.Position) < 100)
        {
            G.Action.ShootLaser(ship, t, true);
            ship.AddWord();
        }
        if (TextObject != null) TextObject.Rotation = -Rotation - ship.Rotation;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, -3, 15, 6), new Color(0x666666ff));
        DrawRect(new Rect2(0, -3, 15, 6), new Color(0xaaaaaaff), false, 1);
        DrawCircle(Vector2.Zero, 3, new Color(0x666666ff));
        DrawArc(Vector2.Zero, 3, 0, Mathf.Tau, 16, new Color(0xaaaaaaff), 1);
    }
}
