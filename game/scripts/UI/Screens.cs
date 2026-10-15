using Godot;

namespace TypingShip;

// The credits picture. A click (or any key) goes back to the menu.
public partial class CreditsUI : Screen
{
    public CreditsUI()
    {
        Picture(this, Sprites.Credits);
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Back();
    }

    public override bool OnKey(InputEventKey key)
    {
        Back();
        return true;
    }

    static void Back() => G.Shell.Go(G.Shell.Menu, Colors.White);
}

// Six trophies; pointing at one (or typing its name) shows what each of its tiers takes.
public partial class AchieveUI : Screen
{
    readonly TextureRect[] trophies = new TextureRect[6];
    readonly Label[] display = new Label[3];
    readonly TextureRect[] stars = new TextureRect[3];

    public AchieveUI()
    {
        Picture(this, Sprites.AchieveBack);
        for (int i = 0; i < 6; i++)
        {
            int index = i;
            var award = new Control
            {
                Position = new Vector2(13 + i % 3 * 187, 44 + i / 3 * 187),
                Size = new Vector2(160, 140),
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            trophies[i] = Picture(award, Sprites.Achieve[0]);
            Text(award, Strings.AchieveNames[i], Fonts.Title, 17, Colors.White, 10, 92, outline: 4, outlineColor: Palette.Gold);
            award.MouseEntered += () => PopText(index);
            award.MouseExited += () => PopText(-1);
            AddChild(award);
            Nav.Add(Strings.AchieveNames[i], () => PopText(index));
        }
        float[] y = { 390, 434, 488 };
        for (int i = 0; i < 3; i++)
        {
            display[i] = Text(this, "", Fonts.Title, 11, Palette.Gold, 75, y[i], 225);
            stars[i] = Picture(this, Sprites.Star[i * 2]);
        }
        var done = new TextButton(Strings.Done, () => G.Shell.Go(G.Shell.Menu, Colors.White), Palette.Gold, Palette.GoldOver)
        {
            Position = new Vector2(355, 440),
            Scale = new Vector2(2, 2),
        };
        AddChild(done);
        Nav.Add(done);
    }

    public override void NavIn()
    {
        for (int i = 0; i < 6; i++) trophies[i].Texture = Sprites.Achieve[G.Model.Save.Achieve[i]];
        PopText(-1);
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode != Key.Escape) return false;
        G.Shell.Go(G.Shell.Menu, Colors.White);
        return true;
    }

    /// <summary>Show award i's goals: bronze always, the next tiers once the one before is earned (-1 clears).</summary>
    void PopText(int i)
    {
        foreach (var d in display) d.Text = "";
        for (int j = 0; j < 3; j++) stars[j].Texture = Sprites.Star[j * 2];
        if (i < 0) return;
        int tier = G.Model.Save.Achieve[i];
        var text = Strings.AchieveText[i];
        display[0].Text = text[0];
        display[1].Text = tier >= 1 ? text[1] : "???";
        display[2].Text = tier >= 2 ? text[2] : "???";
        for (int j = 0; j < 3; j++)
            if (tier > j) stars[j].Texture = Sprites.Star[j * 2 + 1];
    }
}

// The typing test: a passage of the story from data/typing-test.txt, for up to 90 seconds from the first key. A
// wrong key flashes a red cross for a second and counts against accuracy. At the end it shows net WPM (correct
// characters), accuracy and the difficulty it suggests (the rules are Core's TypingTestRun). Escape cancels.
public partial class TypingTest : Screen
{
    readonly RichTextLabel top, bottom;
    readonly Label clock, hint;
    readonly Control errorS;
    readonly MenuBox finishB = new("", 135, 200, 280, 140);
    TypingTestRun run = new("");
    double errorT, finishT;

    static double Now => Time.GetTicksMsec() / 1000.0;

    public TypingTest()
    {
        top = Rich(30, Palette.Gold);
        bottom = Rich(290, Palette.Red);
        errorS = new Cross { Visible = false };
        AddChild(errorS);
        // The original's MenuBt cancelled the test with the mouse; it never made it onto the stage.
        var back = new TextButton(Strings.Menu, Cancel, Palette.Gold, Palette.GoldOver, true) { Position = new Vector2(10, 520) };
        AddChild(back);
        hint = new Label
        {
            Text = Strings.TestHint,
            Position = new Vector2(100, 524),
            Size = new Vector2(330, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            LabelSettings = Fonts.Settings(Fonts.Space, 13, Palette.Gold),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(hint);
        clock = new Label
        {
            Position = new Vector2(430, 1),
            Size = new Vector2(100, 28),
            HorizontalAlignment = HorizontalAlignment.Right,
            LabelSettings = Fonts.Settings(Fonts.Space, 22, Palette.Gold),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(clock);
        AddChild(finishB);
    }

    RichTextLabel Rich(float y, Color c)
    {
        var r = new RichTextLabel
        {
            Position = new Vector2(30, y),
            Size = new Vector2(490, 230),
            BbcodeEnabled = true,
            ScrollActive = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        r.AddThemeFontOverride("normal_font", Fonts.Space);
        r.AddThemeFontSizeOverride("normal_font_size", 24);
        r.AddThemeColorOverride("default_color", c);
        r.AddThemeConstantOverride("outline_size", 3);
        r.AddThemeColorOverride("font_outline_color", new Color(c, 0.35f));
        AddChild(r);
        return r;
    }

    public override void NavIn()
    {
        var passages = G.Model.Data.TestPassages;
        run = new TypingTestRun(passages[Math.Min((int)(G.Rand() * passages.Count), passages.Count - 1)]);
        bottom.Text = "";
        top.ScrollToLine(0);
        bottom.ScrollToLine(0);
        hint.Visible = true;
        Highlight();
        ShowClock();
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode == Key.Escape)
        {
            Cancel();
            return true;
        }
        if (key.Unicode == 0) return true; // Shift and the like do nothing
        if (finishB.Visible || run.Finished) return true;
        Update((char)key.Unicode);
        return true;
    }

    void Update(char c)
    {
        hint.Visible = false;
        bool right = run.Type(c, Now);
        if (right)
        {
            bottom.Text = Escape(run.Passage[..run.Typed]);
            Highlight();
            int lines = bottom.GetLineCount();
            if (lines > 3)
            {
                top.ScrollToLine(lines - 3);
                bottom.ScrollToLine(lines - 3);
            }
        }
        else if (!run.Finished)
        {
            errorS.Visible = true;
            errorT = 1;
        }
        if (run.Finished) Done();
    }

    void Highlight() =>
        top.Text = run.Typed == 0 ? Escape(run.Passage)
            : $"[bgcolor=#ffcc3355]{Escape(run.Passage[..run.Typed])}[/bgcolor]{Escape(run.Passage[run.Typed..])}";

    static string Escape(string s) => s.Replace("[", "[lb]");

    void ShowClock()
    {
        int s = (int)Math.Ceiling(run.Remaining(Now));
        clock.Text = $"{s / 60}:{s % 60:00}";
    }

    void Done()
    {
        ShowClock();
        int wpm = (int)run.NetWpm;
        G.Model.Save.TestWpm = wpm;
        G.Model.Save.Save();
        int suggest = TypingTestRun.Suggest(wpm);
        Finish(Strings.TestResult(wpm, (int)Math.Round(run.Accuracy * 100), Strings.Diffs[Math.Max(0, suggest)]));
    }

    void Finish(string message)
    {
        finishB.Message = message;
        finishB.Visible = true;
        finishT = 6;
    }

    public override void _Process(double delta)
    {
        if (errorT > 0 && (errorT -= delta) <= 0) errorS.Visible = false;
        if (finishT > 0 && (finishT -= delta) <= 0) FinishTick();
        if (!finishB.Visible)
        {
            ShowClock();
            if (run.Tick(Now)) Done(); // out of time
        }
    }

    void FinishTick()
    {
        finishT = 0;
        finishB.Close();
        G.Shell.Go(G.Shell.Menu, Colors.Black);
    }

    void Cancel()
    {
        if (finishB.Visible) FinishTick();
        else if (!G.Shell.Transitioning)
        {
            int last = G.Model.Save.TestWpm;
            Finish(last > 0 ? $"Test Cancelled.\n wpm remains at {last}." : "Test Cancelled.");
        }
    }

    partial class Cross : Control
    {
        public Cross() => MouseFilter = MouseFilterEnum.Ignore;

        public override void _Draw()
        {
            DrawLine(new Vector2(50, 50), new Vector2(500, 500), Colors.Red, 10);
            DrawLine(new Vector2(500, 50), new Vector2(50, 500), Colors.Red, 10);
        }
    }
}

// Level complete: the level score, then each bonus multiplier counting the score up, then the final score.
// Also where the level's best score, best difficulty and medals are recorded, and the defender and perfectionist
// achievements are checked.
public partial class ScoreUI : Screen
{
    const double Step = 1 / 60.0; // the original's 10 ms timer; Flash ran timers at most 60 times a second

    readonly Label title;
    readonly Label[][] rows = new Label[7][];
    int score, index, tickBy, shown;
    float cMult;
    int[] cLevel = null!;
    bool running;
    double time;

    static Game M => G.Model;

    public ScoreUI()
    {
        Picture(this, Sprites.ScoreBack, scale: G.StageSize / Sprites.ScoreBack.GetHeight());
        for (int i = 0; i < rows.Length; i++)
        {
            float y = i == 0 ? 90 : i == 6 ? 500 : 120 + 35 * i;
            var label = Text(this, "", Fonts.Space, 30, Colors.White, 25, y, outline: 4, outlineColor: Palette.Gold);
            var value = Text(this, "", Fonts.Space, 30, Colors.White, 70, y, outline: 4, outlineColor: Palette.Gold);
            value.Size = new Vector2(450, 0);
            value.HorizontalAlignment = HorizontalAlignment.Right;
            rows[i] = new[] { label, value };
        }
        // The final score: twice the size, its number two and a half times and a line lower.
        rows[5][0].LabelSettings = Fonts.Settings(Fonts.Space, 60, Colors.White, 6, Palette.Gold);
        rows[5][1].LabelSettings = Fonts.Settings(Fonts.Space, 75, Colors.White, 7, Palette.Gold);
        rows[5][1].Position = new Vector2(-480, 120 + 35 * 5 + 60);
        rows[5][1].Size = new Vector2(1000, 0);
        title = Text(this, "", Fonts.Space, 50, Colors.White, 30, 5, outline: 6, outlineColor: Palette.Gold);
    }

    public bool Counting => running;

    public override void NavIn()
    {
        foreach (var r in rows) r[0].Text = r[1].Text = "";
        score = M.Score - M.ScoreC;
        index = 0;
        time = 0;
        running = true;
    }

    public override void _Process(double delta)
    {
        if (!running || !Visible) return;
        time += delta;
        for (int n = 0; time >= Step && running && n < 4; n++)
        {
            time -= Step;
            ScoreTick();
        }
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode is not (Key.Space or Key.Enter)) return false;
        if (!running) G.Shell.ScoreDone();
        return true;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (!running && e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) G.Shell.ScoreDone();
    }

    void ScoreTick()
    {
        var label = rows[Math.Min(index, 6)][0];
        var value = rows[Math.Min(index, 6)][1];
        switch (index)
        {
            case 0: // the level score, less a point for each letter still in the typing bar
                if (label.Text == "")
                {
                    label.Text = Strings.LevelScore;
                    value.Text = score.ToString();
                    title.Text = Strings.LevelComplete(M.Level + 1);
                    cLevel = M.Save.LevelScore[M.Level];
                    if (M.Diff > cLevel[1]) cLevel[1] = M.Diff;
                }
                else if (M.Typed.Length > 0)
                {
                    M.RemoveLetter();
                    score--;
                    value.Text = score.ToString();
                }
                else index++;
                break;
            case 1: // difficulty bonus
                if (label.Text == "")
                {
                    cMult = Scoring.DifficultyBonus(M.Cps);
                    label.Text = Strings.DiffBonus + cMult.ToString("F2");
                }
                else Count(value);
                break;
            case 2: // health bonus
                if (label.Text == "")
                {
                    var ship = M.Player;
                    cMult = Scoring.HealthBonus(ship.Injured, ship.Health, ship.MaxHealth);
                    label.Text = (ship.Injured ? Strings.HealthBonus : Strings.HealthPerfect) + cMult.ToString("F2");
                    cLevel[2] = Math.Max(cLevel[2], Scoring.HealthMedal(ship.Injured, ship.Health, ship.MaxHealth));
                    if (!ship.Injured) M.CheckMedalAchievement(Achievement.Defender, 2);
                }
                else Count(value);
                break;
            case 3: // accuracy bonus
                if (label.Text == "")
                {
                    float ratio = M.HitRatio();
                    cMult = Scoring.AccuracyBonus(ratio);
                    label.Text = (ratio < 1 ? Strings.AccBonus : Strings.AccPerfect) + cMult.ToString("F2");
                }
                else Count(value);
                break;
            case 4:
                label.Text = "----------------------------------------------";
                index++;
                break;
            case 5:
                label.Text = Strings.FinalScore;
                value.Text = score.ToString();
                cLevel[0] = Math.Max(cLevel[0], score);
                float hit = M.HitRatio();
                cLevel[3] = hit == 1 ? 3 : Math.Max(cLevel[3], Scoring.AccuracyMedal(hit));
                if (hit == 1) M.CheckMedalAchievement(Achievement.Perfectionist, 3);
                M.Save.Save();
                index++;
                break;
            case 6:
                running = false;
                label.Text = Strings.Spacebar;
                break;
        }
    }

    // One bonus row: show the score so far, then count it up to score x cMult in about 72 steps.
    void Count(Label value)
    {
        if (value.Text == "")
        {
            value.Text = score.ToString();
            shown = score;
            (score, tickBy) = Scoring.ApplyBonus(score, cMult);
        }
        else if (shown < score)
        {
            shown += tickBy;
            value.Text = shown.ToString();
            G.Sound.Sound(SoundControl.Score);
        }
        else
        {
            value.Text = score.ToString();
            index++;
        }
    }
}

// Game over: "You have been destroyed!" with the level score, or after the victory credits, the win screen with
// the total of every level's best score.
public partial class GameOverUI : Screen
{
    readonly TextureRect back;
    readonly Label title, score;

    public GameOverUI()
    {
        back = Picture(this, Sprites.LoseBack);
        title = Text(this, "", Fonts.Space, 50, Colors.White, 30, 5, 500, 6, Palette.Gold);
        score = Text(this, "", Fonts.Space, 50, Colors.White, 5, 150, outline: 6, outlineColor: Palette.Gold);
        AddButton(Strings.Menu, () => G.Shell.Go(G.Shell.Menu, Colors.Black), 70, 420);
        AddButton(Strings.HsSubmit, () => G.Shell.ShowBox(Strings.SubmitStub), 270, 420);
        AddButton(Strings.MoreGames, () => G.Shell.GoSponsor(), 270, 480);
    }

    void AddButton(string text, Action f, float x, float y)
    {
        var b = new TextButton(text, f, Palette.Gold, Palette.GoldOver, true) { Position = new Vector2(x, y), Scale = new Vector2(1.5f, 1.5f) };
        AddChild(b);
        Nav.Add(b);
    }

    public void AddWin()
    {
        back.Texture = Sprites.WinBack;
        back.Scale = Vector2.One * (G.StageSize / Sprites.WinBack.GetHeight());
        title.Text = Strings.WonGame;
        score.Text = $"{Strings.FinalScore}\n        {G.Model.Save.Highscore}";
    }

    public void AddLose()
    {
        back.Texture = Sprites.LoseBack;
        back.Scale = Vector2.One * (G.StageSize / Sprites.LoseBack.GetHeight());
        title.Text = Strings.LoseGame;
        score.Text = $"{Strings.LevelScore}\n        {G.Model.Score}";
    }

    public override bool OnKey(InputEventKey key)
    {
        // New: Space or Enter for MENU.
        if (key.Keycode is not (Key.Space or Key.Enter)) return false;
        G.Shell.Go(G.Shell.Menu, Colors.Black);
        return true;
    }
}

// After the last level: the closing text and credits scroll up over the final picture. SKIP or the end goes to
// the win screen.
public partial class VictoryUI : Screen
{
    readonly Label scroll;
    bool done;

    public VictoryUI()
    {
        Picture(this, Sprites.FinalBack);
        scroll = Text(this, Strings.FinalCredits, Fonts.Space, 25, Colors.White, 50, 550, 450, 3, Palette.Gold);
        scroll.HorizontalAlignment = HorizontalAlignment.Center;
        var skip = new TextButton(Strings.Skip, Done, Palette.Gold, Palette.GoldOver) { Scale = new Vector2(0.5f, 0.5f) };
        AddChild(skip);
        skip.Position = new Vector2(550 - skip.GetMinimumSize().X * 0.5f - 10, 10);
        Nav.Add(skip);
    }

    public void Run()
    {
        G.Sound.Play(0);
        scroll.Position = new Vector2(50, 550);
        done = false;
    }

    public override void OnTick()
    {
        if (done) return;
        scroll.Position += new Vector2(0, -1);
        if (scroll.Position.Y < -scroll.Size.Y) Done();
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode is not (Key.Escape or Key.Space or Key.Enter)) return false;
        Done();
        return true;
    }

    void Done()
    {
        if (done) return;
        done = true;
        G.Shell.GameOver.AddWin();
        G.Shell.Go(G.Shell.GameOver, Colors.Black, 5);
    }
}

// The pause screen over the game: MENU asks to confirm; YES ends the level as a loss, NO resumes.
public partial class PauseUI : Control
{
    public readonly TypeNav Nav = new();
    readonly Control confirm = new() { Position = new Vector2(0, 290), MouseFilter = MouseFilterEnum.Ignore, Visible = false };

    public PauseUI()
    {
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        AddChild(new Label
        {
            Text = Strings.Paused,
            Position = new Vector2(125, 200),
            LabelSettings = Fonts.Settings(Fonts.Title, 50, Colors.White, 8, Palette.Gold),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        var menu = new TextButton(Strings.Menu, GoMenu, Palette.Gold, Palette.GoldOver, true) { Position = new Vector2(230, 270) };
        AddChild(menu);
        AddChild(confirm);
        var yes = new TextButton(Strings.Yes, MenuYes, Palette.Gold, Palette.GoldOver, true) { Position = new Vector2(200, 0) };
        var no = new TextButton(Strings.No, MenuNo, Palette.Gold, Palette.GoldOver, true) { Position = new Vector2(275, 0) };
        confirm.AddChild(yes);
        confirm.AddChild(no);
        Nav.Add(menu);
        Nav.Add(yes);
        Nav.Add(no);
    }

    public override void _Draw() => DrawRect(new Rect2(0, 0, G.StageSize, G.StageSize), new Color(0, 0, 0, 0.3f));

    public void Show(bool b)
    {
        Visible = b;
        confirm.Visible = false;
        Nav.Clear();
    }

    void GoMenu()
    {
        if (confirm.Visible) MenuYes();
        else confirm.Visible = true;
    }

    static void MenuYes()
    {
        G.Model.Resume();
        G.Model.FinalDeath();
    }

    static void MenuNo() => G.Model.Resume();
}
