namespace TypingShip;

// Run state for the current level. (Save data, achievements and level scores come with the menus.)
public class GameModel
{
    public bool Running, CanType, BossMode, Perfect, Invincible;
    public double Distance, MaxDistance;
    public int ScoreC, Level = -1, MissedShips, TotalShips, Wpm = G.Normal;
    public List<ILevelEvent> LevelData = new();
    public readonly List<GameSprite> GObjects = new();
    public readonly List<Overflow> GOverflow = new();
    public PlayerShip PShip = null!;
    public ShipSystem Hull = null!;
    public readonly bool[] TutFlags = new bool[11];

    // Achievement progress: tier reached (0-3) and counters, per achievement.
    public readonly int[] Achieve = new int[6];
    public readonly int[] AchieveC = new int[6];

    int score;
    public int Score
    {
        get => score;
        set
        {
            score = value;
            G.UI.ShowScore(score);
        }
    }

    public void TutReset()
    {
        if (Wpm > 50) Array.Fill(TutFlags, true);
    }

    public void TutOff() => Array.Fill(TutFlags, true);

    public float HitRatio() => (TotalShips - MissedShips) / (float)TotalShips;
}
