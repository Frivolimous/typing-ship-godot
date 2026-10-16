using Godot;

namespace TypingShip;

// Runs the rules at 24 Hz (one G.Model.Step per physics tick), then brings the picture up to date and ticks the
// purely visual effects.
public partial class GameControl : Node
{
    readonly List<IViewTicker> tickers = new();

    /// <summary>Called when the level ends: true = cleared, false = destroyed.</summary>
    public event Action<bool>? LevelOver;

    public GameControl() => G.World.LevelFinished += cleared => LevelOver?.Invoke(cleared);

    public bool LevelEnded => G.Model.LevelEnded;

    public void AddTicker(IViewTicker t) => tickers.Add(t);

    public void NewGame(int level) => G.Model.NewGame(level);

    public override void _PhysicsProcess(double delta)
    {
        G.Model.Step();
        G.World.Sync();
        for (int i = 0; i < tickers.Count;)
        {
            if (tickers[i].Tick()) i++;
            else tickers.RemoveAt(i);
        }
    }

    /// <summary>Pause or resume. A tutorial pop-up pauses without the pause screen; resuming closes it.</summary>
    public void PauseGame(bool paused)
    {
        if (paused) G.Model.Pause();
        else G.Model.Resume();
    }

    public void FinalDeath() => G.Model.FinalDeath();
}
