using Godot;

namespace TypingShip;

// The game shell: which screen is showing, the fade between screens (Transition), the pause and mute buttons and
// pop-ups over everything, and keyboard input outside the game. The game itself (GameUI) is "no screen".
public partial class Shell : Control, IViewTicker
{
    public MenuUI Menu = null!;
    public ScoreUI Score = null!;
    public GameOverUI GameOver = null!;
    public VictoryUI Victory = null!;
    public AchieveUI Achievements = null!;
    public CreditsUI Credits = null!;
    public TypingTest Test = null!;
    public readonly PauseUI Pause = new();
    public readonly PauseButton PauseB = new() { Position = new Vector2(435, 522), Scale = new Vector2(0.8f, 0.8f), Visible = false };
    public readonly MuteButton MuteB = new() { Position = new Vector2(505, 522), Scale = new Vector2(0.8f, 0.8f) };

    readonly Control screens = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Control popups = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly ColorRect fade = new() { Size = new Vector2(G.StageSize, G.StageSize), Visible = false, MouseFilter = MouseFilterEnum.Stop };
    readonly MenuBox tempBox = new("", 50, 50, 450, 450, 40);
    readonly Label typed = new()
    {
        Position = new Vector2(0, 2),
        Size = new Vector2(G.StageSize, 20),
        HorizontalAlignment = HorizontalAlignment.Center,
        LabelSettings = Fonts.Settings(Fonts.Word, 15, Palette.Gold, 3),
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
        foreach (var s in new Screen[] { Menu, Score, GameOver, Victory, Achievements, Credits, Test }) screens.AddChild(s);
        AddChild(screens);
        AddChild(Pause);
        AddChild(PauseB);
        AddChild(MuteB);
        AddChild(typed);
        AddChild(fade);
        AddChild(popups);
        popups.AddChild(tempBox);
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
    public void StartGame(int level, int wpm)
    {
        G.Model.Wpm = wpm;
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
            Go(Score, Colors.White);
            return;
        }
        G.Model.RecordLoss(); // a lost level still keeps its best score
        GameOver.AddLose();
        Go(GameOver, Colors.Black, 3);
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
        else Go(Menu, Colors.White);
    }

    public void ShowBox(string text)
    {
        tempBox.Message = text;
        tempBox.Visible = true;
    }

    /// <summary>Facade.goSponsor: a placeholder for the sponsor link, worth the first explorer tier.</summary>
    public void GoSponsor()
    {
        if (G.Model.Save.Achieve[Achievement.Explorer] == 0) G.Model.Award(Achievement.Explorer, 1);
        ShowBox(Strings.SponsorStub);
    }

    public void AddPopup(Control c) => popups.AddChild(c);

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;

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

        if (tempBox.Visible)
        {
            tempBox.Close();
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

// PauseB: pause and resume the game with the mouse (Escape and typing "pause" still work).
public partial class PauseButton : Control
{
    bool paused;

    public PauseButton()
    {
        Size = new Vector2(30, 30);
        PivotOffset = Vector2.Zero;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public void SetPaused(bool b)
    {
        paused = b;
        QueueRedraw();
    }

    // Placeholder art (the original was a vector MovieClip): pause bars, or a play arrow while paused.
    public override void _Draw()
    {
        var r = new Rect2(-15, -15, 30, 30);
        DrawRect(r, new Color(Palette.MenuBlue, 0.8f));
        DrawRect(r, Palette.BorderBlue, false, 2);
        if (paused) DrawColoredPolygon(new[] { new Vector2(-6, -9), new Vector2(9, 0), new Vector2(-6, 9) }, Palette.BorderBlue);
        else
        {
            DrawRect(new Rect2(-7, -9, 5, 18), Palette.BorderBlue);
            DrawRect(new Rect2(2, -9, 5, 18), Palette.BorderBlue);
        }
    }

    public override bool _HasPoint(Vector2 p) => new Rect2(-15, -15, 30, 30).HasPoint(p);

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        AcceptEvent();
        G.Sound.Sound(SoundControl.Click);
        if (!G.Model.CanType || G.Model.LevelEnded) return;
        G.Ctrl.PauseGame(!paused);
    }
}

// MuteB: the left half toggles the music, the right half the sound effects.
public partial class MuteButton : Control
{
    public MuteButton()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public override bool _HasPoint(Vector2 p) => new Rect2(-30, -15, 60, 30).HasPoint(p);

    public override void _Process(double delta) => QueueRedraw(); // typing "mute" changes it too

    // Placeholder art: a note for music and a speaker for sound, crossed out when muted.
    public override void _Draw()
    {
        var back = new Color(Palette.MenuBlue, 0.8f);
        var c = Palette.BorderBlue;
        DrawRect(new Rect2(-30, -15, 60, 30), back);
        DrawRect(new Rect2(-30, -15, 60, 30), c, false, 2);
        DrawLine(new Vector2(0, -15), new Vector2(0, 15), c, 1);
        // note
        DrawCircle(new Vector2(-18, 6), 4, c);
        DrawLine(new Vector2(-14.5f, 6), new Vector2(-14.5f, -9), c, 2);
        DrawLine(new Vector2(-14.5f, -9), new Vector2(-9, -6), c, 2);
        // speaker
        DrawColoredPolygon(new[] { new Vector2(8, -4), new Vector2(12, -4), new Vector2(18, -9), new Vector2(18, 9), new Vector2(12, 4), new Vector2(8, 4) }, c);
        DrawArc(new Vector2(18, 0), 6, -0.9f, 0.9f, 8, c, 1.5f);
        var red = new Color(0xff5555ff);
        if (G.Sound.MuteMusic) DrawLine(new Vector2(-26, 11), new Vector2(-4, -11), red, 3);
        if (G.Sound.MuteSound) DrawLine(new Vector2(4, 11), new Vector2(26, -11), red, 3);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mb) return;
        AcceptEvent();
        if (mb.Position.X > 0) G.Sound.MuteSound = !G.Sound.MuteSound;
        else G.Sound.MuteMusic = !G.Sound.MuteMusic;
    }
}
