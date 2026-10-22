using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip.Core;

/// <summary>
/// The rules of a game session: the current level's state (the original's GameModel), the tick loop
/// (GameControl), weapons, damage and scoring (ActionControl) and typing (KeyboardControl). Plain C#: the Godot
/// game drives it one 24 Hz tick at a time (Step) and draws what it holds; tests and the simulator drive it alone.
/// </summary>
public sealed partial class Game
{
    public GameData Data { get; private set; }
    public Tuning Tuning => Data.Tuning;
    public readonly Rng Rng;
    public readonly ArtLibrary Art;
    public readonly WordList Words;
    public IGameEvents Events;
    public readonly SaveData Save;

    // ---- State (GameModel) ----
    public bool Running, CanType, BossMode, Perfect, Invincible;
    /// <summary>Ticks since the level started. Level time is these over 24: the same at every difficulty.</summary>
    public long LevelTicks;
    /// <summary>Seconds since the level started.</summary>
    public double Time => LevelTicks / (double)Playfield.TickRate;
    /// <summary>The time of the level's last event (its length, give or take the ships still flying).</summary>
    public double MaxTime;
    public int ScoreC, Level = -1, MissedShips, TotalShips;
    /// <summary>The difficulty, as a typing speed in characters per second (see <see cref="Speed"/>).</summary>
    public double Cps = Difficulty.Normal;
    /// <summary>The speed things move and count down at: Cps, flattened above Hard (Tuning.Pace), and sped up as an
    /// endless run goes on.</summary>
    public double Pace => Tuning.Pace(Cps) * PaceScale;
    /// <summary>Endless mode's speed-up (1 in the levels).</summary>
    public double PaceScale => Endless?.Speed ?? 1;
    /// <summary>The endless run being played, or null in the levels.</summary>
    public EndlessRun? Endless;
    public int Score;
    public List<LevelEvent> LevelData = new();
    public readonly List<Sprite> Objects = new();
    public readonly List<Overflow> Overflows = new();
    public Boss? Boss;
    public readonly PlayerShip Player;
    public readonly Hull Hull;
    /// <summary>The letters typed so far (the typing bar).</summary>
    public string Typed { get; private set; } = "";
    /// <summary>Tutorial pop-ups already shown (or switched off). Reset only when the game starts, as in the original.</summary>
    public readonly bool[] TutFlags = new bool[11];
    /// <summary>The tutorial pop-up showing (the game is paused while it is), or null.</summary>
    public int? Tutorial { get; private set; }

    // ---- Tick loop (GameControl) ----
    public int Delay;
    public bool LevelEnded { get; private set; }
    bool finalDelay, bossDying;
    Action? toRun;
    readonly List<ITicker> tickers = new();
    /// <summary>Ticks since the game was made (the original's frame count).</summary>
    public long Ticks { get; private set; }

    // ---- Enemy fire (ActionControl) ----
    public float MCount, MRate = 0.4f;

    public Game(GameData data, Rng rng, ArtLibrary art, SaveData? save = null, IGameEvents? events = null)
    {
        Data = data;
        Rng = rng;
        Art = art;
        Save = save ?? new SaveData();
        Events = events ?? NoEvents.Instance;
        Words = new WordList(data.Words, rng);
        Hull = new Hull(this);
        Player = new PlayerShip(this);
    }

    public LevelSet Set => Data.Main;

    /// <summary>The current level's context: the difficulty, and the level's pace and density.</summary>
    public LevelContext Context => Set.Context(Level, Rng, Tuning, Cps) with { PaceScale = PaceScale };

    /// <summary>Editing aid: use newly loaded data files from the next level start (or restart) on.</summary>
    public void Reload(GameData data) => Data = data;

    /// <summary>1 (easy) to 5 (insane).</summary>
    public int Diff => Difficulty.Of(Cps);

    public float HitRatio() => (TotalShips - MissedShips) / (float)TotalShips;

    public void AddTicker(ITicker t) => tickers.Add(t);

    public void TutReset()
    {
        if (Cps > Speed.FromWpm(50)) Array.Fill(TutFlags, true);
    }

    public void TutOff() => Array.Fill(TutFlags, true);

    // =============================================================================================================
    // Levels

    public void NewGame(int level = 0)
    {
        Endless = null;
        Start(level);
    }

    /// <summary>How often enemies fire at a typing speed: each fire command adds this to <see cref="MCount"/>.</summary>
    public float FireRate(double cps) => Math.Clamp((float)(cps / Tuning.FireRateCps), Tuning.FireRateMin, Tuning.FireRateMax);

    /// <summary>An endless run: waves until the player is destroyed (Endless.cs). The seed picks the waves.</summary>
    public void NewEndless(ulong seed)
    {
        Endless = new EndlessRun(this, seed);
        Start(0);
    }

    void Start(int level)
    {
        while (Objects.Count > 0) Objects[0].Remove();
        Boss?.Remove();
        MRate = FireRate(Cps);
        TutReset();
        Score = 0;
        Level = level - 1;
        NextLevel();
    }

    public void NextLevel()
    {
        var ship = Player;
        ship.RemoveShield();
        Level++;
        LevelTicks = 0;
        TotalShips = MissedShips = 0;
        Perfect = true;
        ship.Injured = false;
        ScoreC = Score;
        Typed = "";
        Heal(100);
        Running = true;
        LevelEnded = false;
        CanType = true;
        BossMode = false;
        LevelData = Endless != null ? new List<LevelEvent>() : Set.BuildLevel(Level, Context);
        MaxTime = LevelData.Count > 0 ? LevelData.Max(e => e.Time) : 0;
        ship.Destroyed = false;
        Delay = 0;
        finalDelay = false;
        bossDying = false;
        toRun = null;
        Events.LevelStarted();
        Events.Music(1 + Level % 4);
    }

    /// <summary>
    /// Editing aid: skip ahead to a time in the current level (seconds), as if the events before it had been left
    /// out. Ships already on screen are cleared.
    /// </summary>
    public void SkipTo(double time)
    {
        while (Objects.Count > 0) Objects[0].Remove();
        Boss?.Remove();
        BossMode = false;
        LevelData.RemoveAll(e => e.Time < time);
        LevelTicks = (long)Math.Floor(time * Playfield.TickRate);
    }

    /// <summary>One 24 Hz tick (GameControl's physics step): the game if it's running, then the tickers.</summary>
    public void Step()
    {
        Ticks++;
        if (Trace != null && Running) WriteTrace();
        if (Running) Tick();

        for (int i = 0; i < tickers.Count;)
        {
            if (tickers[i].Tick()) i++;
            else tickers.RemoveAt(i);
        }
    }

    void Tick()
    {
        Events.TickStarted();
        if (Delay > 0)
        {
            Delay--;
            if (Delay == 1) toRun?.Invoke();
            else if (bossDying) BossExplosions();
        }
        else if (Player.Health <= 0)
        {
            PlayerDeath();
            return;
        }

        LevelTicks++;
        Events.TickAdvanced();

        Endless?.Feed();
        if (LevelData.Count > 0)
        {
            while (LevelData.Count > 0 && Time > LevelData[0].Time)
            {
                var e = LevelData[0];
                LevelData.RemoveAt(0);
                Run(e);
            }
        }
        else if (BossMode)
        {
            LevelData = Set.BuildBossLoop(Boss!.Index, Context, Time);
        }
        else if (Boss == null && Objects.Count == 0 && Endless == null)
        {
            if (Delay == 0 && finalDelay) EndLevel();
            else
            {
                LevelData.Add(new MarkerEvent(Time + Tuning.EndWait, MarkerEvent.Kind.Wait, Tuning.EndWait));
                if (!finalDelay) Events.LevelClearing();
                finalDelay = true;
            }
        }

        // An update returns false when the object removed itself, so the next one slides into slot i.
        for (int i = 0; i < Objects.Count;)
            if (Objects[i].Update()) i++;

        for (int i = 0; i < Overflows.Count;)
            if (Overflows[i].Update()) i++;

        Boss?.Update();
    }

    void Run(LevelEvent e)
    {
        switch (e)
        {
            case SpawnEvent s:
                Objects.Add(Spawn(s));
                break;
            case BossEvent b:
                if (Boss != null) return;
                ShowTutorial(9);
                Boss = new Boss(this, b.Type, Context);
                Events.Spawned(Boss, Layer.Boss);
                break;
            case MarkerEvent { Type: MarkerEvent.Kind.Loop } m:
                foreach (var ev in Set.BuildLevel(Level, Context).Skip((int)m.Index))
                {
                    ev.Time += m.Time;
                    LevelData.Add(ev);
                }
                break;
            case MarkerEvent { Type: MarkerEvent.Kind.Jump } m:
                LevelTicks += (long)Math.Round(m.Index * Playfield.TickRate);
                break;
        }
    }

    /// <summary>SpawnEvent.spawn: a new enemy ship (the caller adds it to Objects).</summary>
    public EnemyShip Spawn(SpawnEvent e, bool show = true)
    {
        TotalShips++;
        var ship = new EnemyShip(this);
        ship.Setup(e);
        if (show) Events.Spawned(ship, Layer.Front);
        return ship;
    }

    public void AddOverflow(string word) => Overflows.Add(new Overflow(this, word));

    public void EndLevel()
    {
        while (Objects.Count > 0) Objects[0].Remove();
        Boss?.Remove();
        Running = false;
        LevelEnded = true;
        Events.LevelOver(true);
    }

    public void KillBoss()
    {
        BossMode = false;
        Boss!.Commands = null;
        LevelData = new List<LevelEvent>();
        Delay = 200;
        toRun = EndLevel; // finalBoss
        bossDying = true;
        Events.LevelClearing();
        // Conqueror: one tier per boss, in order.
        if ((Save.Achieve[Achievement.Conqueror], Level) is (0, 3) or (1, 7) or (2, 11))
            Award(Achievement.Conqueror, Save.Achieve[Achievement.Conqueror] + 1);
    }

    /// <summary>The dying boss fades out over 200 ticks, bursting into explosions all over.</summary>
    void BossExplosions()
    {
        var boss = Boss!;
        boss.Alpha = Math.Min(Delay / 100f, 1);
        Events.BossBurning(boss);
    }

    public void PlayerDeath()
    {
        CanType = false;
        Delay = 100;
        Player.Destroyed = true;
        Events.PlayerDestroyed();
        toRun = FinalDeath;
        TutOff();
    }

    /// <summary>The level is lost (after the death explosion, or quitting from the pause screen).</summary>
    public void FinalDeath()
    {
        Running = false;
        LevelEnded = true;
        Events.LevelOver(false);
    }

    // =============================================================================================================
    // Pausing and tutorials

    public void Pause()
    {
        Running = false;
        Events.Paused(true);
    }

    public void Resume()
    {
        Tutorial = null;
        Running = true;
        Events.Paused(false);
    }

    /// <summary>A tutorial pop-up, once per game (TutFlags). It pauses the game until it's closed.</summary>
    public void ShowTutorial(int index, Vec2? pointer = null, PointerDir dir = PointerDir.Auto)
    {
        if (TutFlags[index]) return;
        TutFlags[index] = true;
        if (Tutorial != null) Resume(); // a new one replaces the one showing
        Tutorial = index;
        Running = false;
        Events.TutorialShown(index, pointer, dir);
        Events.Paused(true);
    }

    /// <summary>Space on a tutorial; Escape also switches the tutorials off.</summary>
    public void CloseTutorial(bool turnOff = false)
    {
        if (Tutorial == null) return;
        if (turnOff) TutOff();
        Resume();
    }

    // =============================================================================================================
    // Achievements

    /// <summary>Raise achievement i to the given tier, announce it and save.</summary>
    public void Award(int i, int tier)
    {
        Save.Achieve[i] = tier;
        Events.AchievementAwarded(i, tier);
        Save.Save();
    }

    /// <summary>Defender (health) and perfectionist (accuracy): any gold medal, then 5 gold medals, then all 12.</summary>
    public void CheckMedalAchievement(int achievement, int slot)
    {
        int tier = Save.Achieve[achievement];
        int golds = Save.GoldMedals(slot);
        if (tier == 0) Award(achievement, 1);
        else if (tier == 1 && golds >= 5) Award(achievement, 2);
        else if (tier == 2 && golds >= 12) Award(achievement, 3);
    }

    /// <summary>Soldier: ships destroyed by missile; one tier at a time at 10, 1,000 and 20,000.</summary>
    void CountKill()
    {
        int kills = ++Save.AchieveC[Achievement.Soldier];
        int tier = Save.Achieve[Achievement.Soldier];
        if (tier < 3 && kills >= new[] { 10, 1000, 20000 }[tier]) Award(Achievement.Soldier, tier + 1);
    }

    /// <summary>GameControl.finalDeath: a lost level still keeps its best score.</summary>
    /// <summary>A lost level keeps its best score; an ended endless run its best score and time. True for a new best
    /// endless score.</summary>
    public bool RecordLoss()
    {
        if (Endless != null) return Save.RecordEndless(Diff - 1, Score, (int)Time);
        var best = Save.LevelScore[Level];
        best[0] = Math.Max(best[0], Score);
        Save.Save();
        return false;
    }
}
