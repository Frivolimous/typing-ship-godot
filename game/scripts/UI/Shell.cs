using Godot;

namespace TypingShip;

// The game shell: which screen is showing, the fade between screens (Transition), the pause and mute buttons and
// pop-ups over everything, and keyboard input outside the game. The game itself (GameUI) is "no screen".
public partial class Shell : Control, IViewTicker
{
    /// <summary>The fade between screens: a deep blue-black (white is kept for launching into a level).</summary>
    public static readonly Color Dark = new(0.01f, 0.02f, 0.04f);

    public MenuUI Menu = null!;
    public ScoreUI Score = null!;
    public GameOverUI GameOver = null!;
    public VictoryUI Victory = null!;
    public AchieveUI Achievements = null!;
    public CreditsUI Credits = null!;
    public TypingTest Test = null!;
    public OptionsUI Options = null!;
    public readonly PauseUI Pause = new();
    // In a level: a big PAUSE button in the cockpit's button screen.
    public readonly PauseButton PauseB = new() { Visible = false };

    readonly Control screens = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Control popups = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly ConfirmBox confirm = new();
    readonly ColorRect fade = new() { Size = new Vector2(G.StageSize, G.StageSize), Visible = false, MouseFilter = MouseFilterEnum.Stop };
    // What's been typed on a menu screen, at the top: the words act like clicking a button.
    readonly Label typed = new()
    {
        Position = new Vector2(0, 4),
        Size = new Vector2(G.StageSize, 20),
        HorizontalAlignment = HorizontalAlignment.Center,
        LabelSettings = Fonts.Settings(Fonts.Word, 14, new Color(0.55f, 1, 0.85f), 4, new Color(0, 0, 0, 0.8f)),
        MouseFilter = MouseFilterEnum.Ignore,
    };

    Screen? current; // null while the game is showing
    Screen? toAdd;
    bool toGame, fadingUp;
    float speed;

    public bool InGame => current == null;
    public bool ShowingScore => current == Score;
    public bool Transitioning => fade.Visible;

    public Shell()
    {
        G.Shell = this;
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Init()
    {
        Menu = new MenuUI();
        Score = new ScoreUI();
        GameOver = new GameOverUI();
        Victory = new VictoryUI();
        Achievements = new AchieveUI();
        Credits = new CreditsUI();
        Test = new TypingTest();
        Options = new OptionsUI();
        foreach (var s in new Screen[] { Menu, Score, GameOver, Victory, Achievements, Credits, Test, Options }) screens.AddChild(s);
        AddChild(screens);
        AddChild(Pause);
        AddChild(PauseB);
        AddChild(typed);
        AddChild(fade);
        AddChild(popups);
        AddChild(confirm);
        G.Ctrl.AddTicker(this);
        G.Ctrl.LevelOver += OnLevelOver;
    }

    /// <summary>Start on the title menu (the original's menuUI.navIn at start-up).</summary>
    public void ShowTitle(bool map = false)
    {
        G.UI.Visible = false;
        Show(Menu);
        if (map) Menu.ShowMapNow();
        G.Sound.Play(0);
    }

    /// <summary>Dev flags (--level, --autotype): straight into a level, no menus.</summary>
    public void StartDirect(int level)
    {
        current = null;
        G.UI.Visible = true;
        PauseB.Visible = true;
        G.Ctrl.NewGame(level);
    }

    /// <summary>DiffSelect.startGame: the level starts at once and runs under the fade.</summary>
    public void StartGame(int level, double cps)
    {
        G.Model.Cps = cps;
        G.Starfield.ClearParticles();
        G.Ctrl.NewGame(level);
        GoGame();
    }

    void Show(Screen s)
    {
        current = s;
        s.Visible = true;
        s.Nav.Clear();
        s.NavIn();
    }

    // Transition.add: fade to a colour, swap screens, fade back out. Speed 1 takes 10 ticks each way.
    public void Go(Screen to, Color color, int speed = 1) => Begin(to, false, color, speed);

    void GoGame() => Begin(null, true, Colors.White, 1);

    void Begin(Screen? to, bool game, Color color, int s)
    {
        toAdd = to;
        toGame = game;
        fade.Color = new Color(color, 0);
        fade.Visible = true;
        fadingUp = true;
        speed = 0.1f / s;
        typed.Text = "";
    }

    public bool Tick()
    {
        if (fade.Visible) TransitionTick();
        current?.OnTick();
        if (current != null) G.Starfield.Update(); // the stars keep drifting behind the menus
        return true;
    }

    void TransitionTick()
    {
        var c = fade.Color;
        if (fadingUp)
        {
            c.A += speed;
            if (c.A >= 1)
            {
                fadingUp = false;
                Swap();
            }
        }
        else
        {
            c.A -= speed;
            if (c.A <= 0) fade.Visible = false;
        }
        fade.Color = c;
    }

    void Swap()
    {
        if (current != null) current.Visible = false;
        G.UI.Visible = toGame;
        PauseB.Visible = toGame;
        if (toGame) current = null;
        else
        {
            if (toAdd == Menu) G.Sound.Play(0);
            Show(toAdd!);
        }
    }

    void OnLevelOver(bool cleared)
    {
        if (cleared)
        {
            Go(Score, Dark);
            return;
        }
        G.Model.RecordLoss(); // a lost level still keeps its best score
        GameOver.AddLose();
        Go(GameOver, Dark, 3);
    }

    /// <summary>Space on the score screen: the victory credits after the last level, otherwise the map.</summary>
    public void ScoreDone()
    {
        if (Transitioning) return;
        if (G.Model.Level >= 11)
        {
            Victory.Run();
            Go(Victory, Colors.White);
        }
        else Go(Menu, Dark);
    }

    public void AddPopup(Control c) => popups.AddChild(c);

    /// <summary>A short line of text at the top of the screen that fades away.</summary>
    public void Note(string text)
    {
        var l = Kit.Label(popups, text, Kit.Body(12, Colors.White), 0, 28, G.StageSize, HorizontalAlignment.Center);
        var t = l.CreateTween();
        t.TweenInterval(1.2);
        t.TweenProperty(l, "modulate:a", 0f, 0.5);
        t.TweenCallback(Callable.From(l.QueueFree));
    }

    /// <summary>"Exit the game?": quits on yes.</summary>
    public void AskQuit() => confirm.Ask("EXIT THE GAME?", () => GetTree().Quit());

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;

        // F2: the next art set, anywhere (even mid-level), to compare them.
        if (key.Keycode == Key.F2)
        {
            Settings.ArtSet = Sprites.NextArtSet();
            Settings.Apply();
            Settings.Save();
            Note($"ART SET: {Sprites.ArtSet.ToUpperInvariant()}");
            GetViewport().SetInputAsHandled();
            return;
        }

        // The original's dev keys, outside a level: Ctrl+Home invincible, Ctrl+Page Up unlock all, Ctrl+Page Down wipe.
        if (key.CtrlPressed && !(InGame && G.Model.Running) && key.Keycode is Key.Home or Key.Pageup or Key.Pagedown)
        {
            if (key.Keycode == Key.Home) G.Model.Invincible = true;
            else if (key.Keycode == Key.Pageup) G.Model.Save.UnlockAll();
            else G.Model.Save.Clear();
            if (current == Menu) Menu.NavIn();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (confirm.Visible)
        {
            confirm.OnKey(key);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (InGame || current == null) return;
        GetViewport().SetInputAsHandled();
        if (Transitioning || current.OnKey(key)) return;
        char ch = char.ToLowerInvariant((char)key.Unicode);
        if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
        {
            current.Nav.Type(ch);
            typed.Text = current.Nav.Buffer;
        }
    }
}

// PauseB: pause and resume the game with the mouse (Escape and typing "pause" still work). A slanted chip filling
// the cockpit's button screen, reading PAUSE, or RESUME while paused; it lights under the mouse.
public partial class PauseButton : Control
{
    bool paused, hover;

    public PauseButton()
    {
        var screen = Cockpit.ButtonScreen;
        Position = screen.Position + new Vector2(5, 4);
        Size = screen.Size - new Vector2(10, 8);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseEntered += () => { hover = true; QueueRedraw(); };
        MouseExited += () => { hover = false; QueueRedraw(); };
    }

    public void SetPaused(bool b)
    {
        paused = b;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var box = Kit.ButtonBox(hover || paused);
        box.ShadowSize = 0; // no glow spilling out of the cockpit's screen
        box.Draw(GetCanvasItem(), new Rect2(Vector2.Zero, Size));
        var font = Fonts.HudBold;
        const int size = 13;
        float h = font.GetAscent(size) - font.GetDescent(size);
        DrawString(font, new Vector2(0, (Size.Y + h) / 2 - 1), paused ? "RESUME" : "PAUSE", HorizontalAlignment.Center, Size.X, size,
            hover ? Colors.White : Kit.Ink);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        AcceptEvent();
        G.Sound.Sound(SoundControl.Click);
        if (!G.Model.CanType || G.Model.LevelEnded) return;
        G.Ctrl.PauseGame(!paused);
    }
}
