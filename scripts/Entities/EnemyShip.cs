using Godot;

namespace TypingShip;

public partial class EnemyShip : GameSprite
{
    readonly ShieldView shieldV = new();
    public List<ObjectCommand> Commands = new();
    public Charge? Charge;

    public void Setup(int index, float x, float y, ObjectCommand[] commands, float a, int wordSize, int fires, int killBy,
        int value, bool shielded, bool turret)
    {
        Commands = commands.ToList();
        WordSize = wordSize;
        Fires = fires;
        KillBy = killBy;
        Value = value;
        ViewSource = Sprites.Enemy[index];

        switch (index)
        {
            case 0: SpriteScale = 0.5f; shieldV.SetSize(45, 45); break;
            case 1: SpriteScale = 0.5f; shieldV.SetSize(55, 55, -5); break;
            case 2: SpriteScale = 0.13f; shieldV.SetSize(30, 30); break;
            case 3: SpriteScale = 0.65f; shieldV.SetSize(60, 60, 0, 2); Health = 2; break;
            case 4: SpriteScale = 0.65f; shieldV.SetSize(70, 70, -2); Health = 2; break;
            case 5: SpriteScale = 0.25f; shieldV.SetSize(35, 35); break;
            case 6: SpriteScale = 1; shieldV.SetSize(116, 82, -7, 1); Health = 4; break;
            case 7: SpriteScale = 1; shieldV.SetSize(110, 65, -8); Health = 4; break;
            case 8: SpriteScale = 0.5f; shieldV.SetSize(50, 35); break;
            case 9: SpriteScale = 1; shieldV.SetSize(50, 35); TurnRate = 0; break;
        }

        if (fires == ActionControl.Laser)
        {
            int time = 6000 / G.Model.Wpm;
            Charge = index switch
            {
                1 => new Charge(18, 0, time),
                4 => new Charge(28, 1, time),
                7 => new Charge(35, 3, time),
                9 => new Charge(10, 0, time),
                _ => throw new InvalidOperationException($"shouldn't have Laser: {index}"),
            };
        }

        Init(x, y, Mathf.Atan2(Commands[0].Y - y, Commands[0].X - x), a, 1);

        if (turret)
        {
            Turret = new Turret { Position = new Vector2(-20, 0) };
            AddChild(Turret);
        }

        if (!Commands[0].Move) Halt = true;
        if (shielded) AddShield();
        if (index == 8) new StealthEffect(this);
    }

    public override bool MoreUpdate()
    {
        HomeTarget(new Vector2(Commands[0].X, Commands[0].Y));
        Turret?.Update();

        if (!Commands[0].Condition(this)) return true;

        Commands.RemoveAt(0);
        if (Commands.Count > 0)
        {
            if (Commands[0].Fire)
            {
                var player = G.Model.PShip;
                switch (Fires)
                {
                    case ActionControl.Missile: G.Action.ShootMissile(this, player); break;
                    case ActionControl.AutoMissile: G.Action.ShootMissile(this, player, true); break;
                    case ActionControl.Laser: G.Action.ShootLaser(this, player); break;
                    case ActionControl.Suicide: G.Action.ShootSuicide(this, player); break;
                }
            }
            Halt = !Commands[0].Move;
            return true;
        }

        if (Fires == ActionControl.Suicide)
        {
            G.Action.HitTarget(G.Model.PShip, ActionControl.Suicide);
            G.Starfield.Explosion(Position.X, Position.Y, 10);
        }
        Remove();
        G.Model.MissedShips++;
        return false;
    }

    public override void Remove()
    {
        if (TextObject != null) new Overflow(TextObject.Text);
        G.Model.GObjects.Remove(this);
        Charge?.Cancel();
        Detach();
    }

    public void AddShield()
    {
        shieldV.Add(this);
        ShieldOn = true;
        RecycleWord();
        Turret?.RecycleWord();
        AddWord(3);
    }

    public void RemoveShield()
    {
        if (!ShieldOn) return;
        shieldV.Remove();
        ShieldOn = false;
        RecycleWord();
        AddWord();
        Turret?.AddWord();
    }
}
