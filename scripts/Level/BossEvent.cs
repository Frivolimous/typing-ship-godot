namespace TypingShip;

// The level-script event that brings in a boss (boss 0, 1 or 2 on levels 3, 7 and 11).
public class BossEvent : ILevelEvent
{
    public double Distance { get; set; }
    public int Type;

    public BossEvent(double distance, int type)
    {
        Distance = distance;
        Type = type;
    }

    public void Run()
    {
        if (G.Model.Boss != null) return;
        if (!G.Model.TutFlags[9])
        {
            new PopBox(9);
            G.Model.TutFlags[9] = true;
        }
        G.Model.Boss = new BossShip(Type);
    }
}
