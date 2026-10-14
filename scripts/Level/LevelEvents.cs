namespace TypingShip;

// Something in a level script that runs once the ship has travelled Distance.
public interface ILevelEvent
{
    double Distance { get; set; }
    void Run();
}

public class LevelEvent : ILevelEvent
{
    public enum Kind { Loop, Wait, Jump }

    public double Distance { get; set; }
    public Kind Type;
    public double Index;

    public LevelEvent(double distance, Kind type, double index)
    {
        Distance = distance;
        Type = type;
        Index = index;
    }

    public void Run()
    {
        if (Type == Kind.Loop)
        {
            var events = LevelData.LoadData(G.Model.Level).Skip((int)Index);
            foreach (var e in events)
            {
                e.Distance += Distance;
                G.Model.LevelData.Add(e);
            }
        }
        else if (Type == Kind.Jump)
        {
            G.Model.Distance += Index;
        }
    }
}

// Ship codes are three letters: size, weapon, defence.
//   size:    [s]mall, [m]edium, [l]arge, x = stealth, n = invisible laser
//   weapon:  [m]issile, [l]aser, [s]uicide (small = landmine, medium = suicide ship)
//   defence: [s]hield, [t]urret, [b]oth, [n]one
public class SpawnEvent : ILevelEvent
{
    public double Distance { get; set; }
    public string Type;
    public float X, Y;
    public ObjectCommand[] Commands;
    public float A = 0.5f;
    public int Fires, KillBy = ActionControl.Missile, Value = 5, WordSize, Index;
    public bool Shielded, HasTurret;

    public SpawnEvent(double distance, string t, float x, float y, ObjectCommand[]? commands = null)
    {
        Type = t;
        Distance = distance;
        X = x;
        Y = y;
        Commands = commands ?? Array.Empty<ObjectCommand>();

        switch (t[..2])
        {
            case "sm": Index = 0; break;
            case "sl": Index = 1; break;
            case "ss": Index = 2; break;
            case "mm": Index = 3; break;
            case "ml": Index = 4; break;
            case "ms": Index = 5; break;
            case "lm": Index = 6; break;
            case "ll": Index = 7; break;
            case "xm": Index = 8; break;
            case "nl": Index = 9; KillBy = ActionControl.Laser; break;
            default: throw new ArgumentException($"{t[..2]} is not a recognized ship code.");
        }

        switch (t[0])
        {
            case 's': WordSize = 4; break;
            case 'm': WordSize = 6; Value += 1; A -= 0.2f; break;
            case 'l': WordSize = 8; Value += 3; A -= 0.25f; break;
            case 'x': WordSize = 7; Value += 3; A -= 0.1f; break;
            case 'n': WordSize = 7; break;
            default: WordSize = -1; break;
        }

        switch (t[1])
        {
            case 'm': Fires = ActionControl.Missile; break;
            case 'l': Fires = ActionControl.Laser; WordSize += 1; Value += 1; A -= 0.1f; break;
            case 's':
                Fires = ActionControl.Suicide;
                WordSize = 3;
                KillBy = ActionControl.Laser;
                Value -= 3;
                A += t[0] == 's' ? -0.2f : 0.2f;
                break;
        }
        if (t[0] == 'x') Fires = ActionControl.AutoMissile;

        switch (t[2])
        {
            case 's': Shielded = true; Value += 2; break;
            case 't': HasTurret = true; Value += 2; break;
            case 'b': Shielded = true; HasTurret = true; Value += 5; break;
        }
        A *= G.Model.Wpm / 100f;
    }

    public void Run() => G.Model.GObjects.Add(Spawn());

    public EnemyShip Spawn()
    {
        G.Model.TotalShips++;
        var ship = new EnemyShip();
        ship.Setup(Index, X, Y, Commands, A, WordSize, Fires, KillBy, Value, Shielded, HasTurret);
        return ship;
    }
}
