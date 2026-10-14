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
        // The "a boss is approaching" tutorial pop-up (tutFlags[9]) comes with the tutorial pop-ups.
        if (G.Model.Boss == null) G.Model.Boss = new BossShip(Type);
    }
}
