namespace TypingShip;

// Run state for the current level, plus the save data (GameModel's SharedObject in the original).
public class GameModel
{
    public bool Running, CanType, BossMode, Perfect, Invincible;
    public double Distance, MaxDistance;
    public int ScoreC, Level = -1, MissedShips, TotalShips, Wpm = G.Normal;
    public List<ILevelEvent> LevelData = new();
    public readonly List<GameSprite> GObjects = new();
    public readonly List<Overflow> GOverflow = new();
    public BossShip? Boss;
    public PlayerShip PShip = null!;
    public ShipSystem Hull = null!;
    /// <summary>Tutorial pop-ups already shown (or switched off). Reset only when the game starts, as in the original.</summary>
    public readonly bool[] TutFlags = new bool[11];

    public SaveData Save = SaveData.Load();

    // Achievement progress: tier reached (0-3) and counters, per achievement.
    public int[] Achieve => Save.Achieve;
    public int[] AchieveC => Save.AchieveC;

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

    /// <summary>1 (easy) to 5 (insane). Dev runs can set any WPM; those count as the nearest difficulty below.</summary>
    public int Diff => Math.Max(1, Array.FindLastIndex(Strings.DiffWpm, w => w <= Wpm) + 1);

    public float HitRatio() => (TotalShips - MissedShips) / (float)TotalShips;
}
