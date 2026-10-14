using Godot;

namespace TypingShip;

// The per-tick game loop (GameControl.onFrame in the original), run at 24 Hz in _PhysicsProcess.
public partial class GameControl : Node
{
    public int Delay;
    public bool LevelEnded { get; private set; }
    bool finalDelay, bossDying;
    Action? toRun;
    int spriteCount;
    readonly List<ITicker> tickers = new();

    /// <summary>Called when the level ends: true = cleared, false = destroyed.</summary>
    public event Action<bool>? LevelOver;

    GameModel M => G.Model;

    public void AddTicker(ITicker t) => tickers.Add(t);

    public void NewGame(int level = 0)
    {
        while (M.GObjects.Count > 0) M.GObjects[0].Remove();
        M.Boss?.Remove();
        G.Action.MRate = Math.Clamp(M.Wpm / 150f, 0.3f, 1f);
        M.TutReset();
        M.Score = 0;
        M.Level = level - 1;
        NextLevel();
    }

    public void NextLevel()
    {
        var ship = M.PShip;
        ship.RemoveShield();
        M.Level++;
        M.Distance = 0;
        M.TotalShips = M.MissedShips = 0;
        M.Perfect = true;
        ship.Injured = false;
        M.ScoreC = M.Score;
        G.Starfield.SetBack(M.Level);
        G.UI.ResetLetter();
        G.UI.ShowMessage("");
        G.Action.Heal(100);
        M.Running = true;
        LevelEnded = false;
        M.CanType = true;
        M.BossMode = false;
        M.LevelData = LevelData.LoadData(M.Level);
        M.MaxDistance = M.LevelData[^1].Distance;
        if (ship.GetParent() == null) G.UI.GameView.AddChild(ship);
        else G.UI.GameView.MoveChild(ship, -1);
        ship.ResetPhysicsInterpolation();
        Delay = 0;
        finalDelay = false;
        bossDying = false;
        toRun = null;
        G.Sound.Play(1 + M.Level % 4);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (M.Running) Tick();
        for (int i = 0; i < tickers.Count;)
        {
            if (tickers[i].Tick()) i++;
            else tickers.RemoveAt(i);
        }
    }

    void Tick()
    {
        G.Starfield.Update();
        if (Delay > 0)
        {
            Delay--;
            if (Delay == 1) toRun?.Invoke();
            else if (bossDying) BossExplosions();
        }
        else if (M.PShip.Health <= 0)
        {
            PlayerDeath();
            return;
        }

        M.Distance += M.Wpm / (double)G.TickRate / 3600;
        G.UI.OnTick();
        SpriteTick();

        if (M.LevelData.Count > 0)
        {
            while (M.LevelData.Count > 0 && M.Distance > M.LevelData[0].Distance)
            {
                var e = M.LevelData[0];
                M.LevelData.RemoveAt(0);
                e.Run();
            }
        }
        else if (M.BossMode)
        {
            M.LevelData = LevelData.bossLoop();
        }
        else if (M.Boss == null && M.GObjects.Count == 0)
        {
            if (Delay == 0 && finalDelay) EndLevel();
            else
            {
                M.LevelData.Add(new LevelEvent(M.Distance + 0.05, LevelEvent.Kind.Wait, 0.05));
                finalDelay = true;
            }
        }

        // An update returns false when the object removed itself, so the next one slides into slot i.
        for (int i = 0; i < M.GObjects.Count;)
            if (M.GObjects[i].Update()) i++;

        for (int i = 0; i < M.GOverflow.Count;)
            if (M.GOverflow[i].Update()) i++;

        M.Boss?.Update();

        G.UI.PopWarning.Update();
    }

    /// <summary>SpriteSheets.onTick: animations advance every other tick.</summary>
    void SpriteTick()
    {
        if (++spriteCount <= 1) return;
        spriteCount = 0;
        M.PShip.AdvanceFrame();
        foreach (var o in M.GObjects) o.AdvanceFrame();
    }

    public void EndLevel()
    {
        while (M.GObjects.Count > 0) M.GObjects[0].Remove();
        M.Boss?.Remove();
        M.Running = false;
        LevelEnded = true;
        LevelOver?.Invoke(true);
    }

    public void KillBoss()
    {
        M.BossMode = false;
        M.Boss!.Commands = null;
        M.LevelData = new List<ILevelEvent>();
        Delay = 200;
        toRun = EndLevel; // finalBoss
        bossDying = true;
        // Conqueror achievement tiers (the pop-ups come with the achievements screen).
        if ((M.Achieve[1], M.Level) is (0, 3) or (1, 7) or (2, 11)) M.Achieve[1]++;
    }

    /// <summary>The dying boss fades out over 200 ticks, bursting into explosions all over.</summary>
    void BossExplosions()
    {
        var boss = M.Boss!;
        boss.SetAlpha(Math.Min(Delay / 100f, 1));
        if (G.Rand() > 0.6f)
        {
            var p = boss.RandomPoint();
            G.Starfield.Explosion(p.X, p.Y);
        }
    }

    public void PlayerDeath()
    {
        M.CanType = false;
        Delay = 100;
        M.PShip.GetParent()?.RemoveChild(M.PShip);
        G.Starfield.Explosion(M.PShip.Position.X, M.PShip.Position.Y, 100);
        toRun = FinalDeath;
        M.TutOff();
    }

    void FinalDeath()
    {
        M.Running = false;
        LevelEnded = true;
        LevelOver?.Invoke(false);
    }

    public void PauseGame(bool paused)
    {
        M.Running = !paused;
        ShowAll(!paused);
        G.UI.ShowMessage(paused ? "-PAUSED-" : "");
    }

    void ShowAll(bool b)
    {
        foreach (var o in M.GObjects)
        {
            o.ShowWord(b);
            o.Turret?.ShowWord(b);
        }
        M.Hull.ShowWord(b);
        M.Boss?.Scanner?.ShowWord(b);
    }
}
