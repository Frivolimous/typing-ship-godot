using Godot;

namespace TypingShip;

// The credits, as text over the stars. Any key or a click goes back to the menu. Visiting them earns the bronze
// Explorer medal (the original's "visit our sponsors", whose link is gone).
public partial class CreditsUI : Screen
{
    public CreditsUI()
    {
        MouseDefaultCursorShape = CursorShape.PointingHand;
        Kit.Label(this, "CREDITS", Kit.Title(22), 0, 22, G.StageSize, HorizontalAlignment.Center);
        float y = 76;
        foreach (var (heading, names) in Strings.Credits)
        {
            Kit.Label(this, heading, Kit.Caption(8), 60, y, 430, HorizontalAlignment.Center);
            var settings = Kit.Body(names.Length > 60 ? 11 : 14);
            Kit.Label(this, names, settings, 60, y + 12, 430, HorizontalAlignment.Center, wrap: true);
            y += 12 + Kit.TextHeight(names, settings, 430) + 12;
        }
        Kit.Label(this, "PRESS ANY KEY TO RETURN", Kit.Caption(8), 0, 512, G.StageSize, HorizontalAlignment.Center);
    }

    public override void NavIn()
    {
        if (G.Model.Save.Achieve[Achievement.Explorer] == 0) G.Model.Award(Achievement.Explorer, 1);
    }

    public override void _Draw() => Kit.Rule(this, new Vector2(190, 56), new Vector2(360, 56));

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Back();
    }

    public override bool OnKey(InputEventKey key)
    {
        Back();
        return true;
    }

    static void Back() => G.Shell.Go(G.Shell.Menu, Shell.Dark);
}

// Six medals; pointing at one (or typing its name) shows what each of its tiers takes, the earned ones in their
// metal's colour.
public partial class AchieveUI : Screen
{
    const float CardW = 160, CardH = 150;
    static readonly Rect2 Detail = new(18, 382, 514, 108);
    readonly TextureRect[] medals = new TextureRect[6];
    readonly Label[] tiers = new Label[6];
    readonly Label[] lines = new Label[3];
    readonly TextureRect[] lineMedals = new TextureRect[3];
    readonly Label hint;
    int showing = -1;

    static Rect2 Card(int i) => new(18 + i % 3 * 177, 52 + i / 3 * 163, CardW, CardH);

    public AchieveUI()
    {
        Kit.Label(this, "ACHIEVEMENTS", Kit.Title(18), 18, 14);
        for (int i = 0; i < 6; i++)
        {
            int index = i;
            var r = Card(i);
            var card = new Control
            {
                Position = r.Position,
                Size = r.Size,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            var size = Sprites.Achieve[0].GetSize() / 2; // half the art's size
            medals[i] = new TextureRect
            {
                Texture = Sprites.Achieve[0],
                Scale = new Vector2(0.5f, 0.5f),
                Position = new Vector2((CardW - size.X) / 2, 10),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            card.AddChild(medals[i]);
            Kit.Label(card, Strings.AchieveNames[i], Kit.Body(12), 0, 104, CardW, HorizontalAlignment.Center);
            tiers[i] = Kit.Label(card, "", Kit.Caption(8), 0, 124, CardW, HorizontalAlignment.Center);
            card.MouseEntered += () => Show(index);
            card.MouseExited += () => Show(-1);
            AddChild(card);
            Nav.Add(Strings.AchieveNames[i], () => Show(index));
        }
        hint = Kit.Label(this, "POINT AT AN ACHIEVEMENT, OR TYPE ITS NAME", Kit.Caption(8), Detail.Position.X, Detail.Position.Y + 48,
            Detail.Size.X, HorizontalAlignment.Center);
        for (int j = 0; j < 3; j++)
        {
            float y = Detail.Position.Y + 12 + 31 * j;
            var art = Sprites.Achieve[j + 1];
            float s = 26 / art.GetHeight();
            lineMedals[j] = new TextureRect
            {
                Texture = art,
                Scale = new Vector2(s, s),
                Position = new Vector2(Detail.Position.X + 16, y - 3),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(lineMedals[j]);
            lines[j] = Kit.Label(this, "", Kit.Body(11), Detail.Position.X + 44, y + 3, Detail.Size.X - 60);
        }
        var done = new TextButton(Strings.Done, () => G.Shell.Go(G.Shell.Menu, Shell.Dark), 13, boxed: true, width: 110)
        {
            Position = new Vector2(330, 506),
        };
        AddChild(done);
        Nav.Add(done);
    }

    public override void NavIn()
    {
        for (int i = 0; i < 6; i++)
        {
            int tier = G.Model.Save.Achieve[i];
            medals[i].Texture = Sprites.Achieve[tier];
            if (i < 3) lineMedals[i].Texture = Sprites.Achieve[i + 1]; // the art set may have changed
            tiers[i].Text = tier > 0 ? Strings.Tiers[tier] : "NOT EARNED";
            tiers[i].LabelSettings = Kit.Caption(8, tier > 0 ? Kit.TierColours[tier] : new Color(Kit.Ink, 0.4f));
        }
        Show(-1);
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode != Key.Escape) return false;
        G.Shell.Go(G.Shell.Menu, Shell.Dark);
        return true;
    }

    /// <summary>Show award i's goals: bronze always, the next tiers once the one before is earned (-1 clears).</summary>
    void Show(int i)
    {
        showing = i;
        hint.Visible = i < 0;
        for (int j = 0; j < 3; j++)
        {
            lines[j].Visible = lineMedals[j].Visible = i >= 0;
            if (i < 0) continue;
            int tier = G.Model.Save.Achieve[i];
            bool earned = tier > j, known = tier >= j;
            lines[j].Text = known ? Strings.AchieveText[i][j] : "???";
            lines[j].LabelSettings = Kit.Body(11, earned ? Kit.TierColours[j + 1] : new Color(Kit.Ink, known ? 0.8f : 0.4f));
            lineMedals[j].Modulate = earned ? Colors.White : Kit.Unearned;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Kit.Rule(this, new Vector2(18, 42), new Vector2(200, 42));
        for (int i = 0; i < 6; i++)
        {
            int tier = G.Model?.Save.Achieve[i] ?? 0;
            Kit.Panel(this, Card(i), tier > 0 ? Kit.TierColours[tier] : null, glow: i == showing ? 1 : 0.45f);
        }
        Kit.Panel(this, Detail);
    }
}

// The typing test: a passage of the story from data/typing-test.txt, for up to 90 seconds from the first key. A
// wrong key flashes the passage's frame red and counts against accuracy. At the end it shows net WPM (correct
// characters), accuracy and the difficulty it suggests (the rules are Core's TypingTestRun), until Space, Enter,
// Escape or a click goes back to the menu. Escape during the test cancels it.
public partial class TypingTest : Screen
{
    static readonly Rect2 PassageBox = new(18, 50, 514, 250), TypedBox = new(18, 316, 514, 170);
    readonly RichTextLabel top, bottom;
    readonly Label clock, hint;
    readonly MenuBox finishB = new("", 135, 190, 280, 150, 20);
    TypingTestRun run = new("");
    double errorT;
    bool reported; // the result (or the cancel message) is showing, or has been

    static double Now => Time.GetTicksMsec() / 1000.0;

    public TypingTest()
    {
        Kit.Label(this, "TYPING TEST", Kit.Title(18), 18, 14);
        top = Rich(PassageBox, Kit.Ink);
        bottom = Rich(TypedBox, Kit.Good);
        // The original's MenuBt cancelled the test with the mouse; it never made it onto the stage.
        var back = new TextButton(Strings.Menu, Cancel, 12, boxed: true, width: 90) { Position = new Vector2(18, 506) };
        AddChild(back);
        hint = Kit.Label(this, Strings.TestHint, Kit.Caption(8), 120, 514, 340, HorizontalAlignment.Center);
        clock = Kit.Label(this, "", Kit.Title(20), 380, 12, 152, HorizontalAlignment.Right);
        AddChild(finishB);
        finishB.Closed += () =>
        {
            if (IsVisibleInTree() && !G.Shell.Transitioning) G.Shell.Go(G.Shell.Menu, Shell.Dark);
        };
    }

    RichTextLabel Rich(Rect2 box, Color c)
    {
        var r = new RichTextLabel
        {
            Position = box.Position + new Vector2(16, 14),
            Size = box.Size - new Vector2(32, 24),
            BbcodeEnabled = true,
            ScrollActive = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        r.AddThemeFontOverride("normal_font", Fonts.Word);
        r.AddThemeFontSizeOverride("normal_font_size", 19);
        r.AddThemeColorOverride("default_color", c);
        r.AddThemeConstantOverride("line_separation", 6);
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
        reported = false;
        finishB.Visible = false;
        Highlight();
        ShowClock();
    }

    public override bool OnKey(InputEventKey key)
    {
        if (finishB.Visible)
        {
            // The result is up: Space, Enter or Escape closes it (and goes back to the menu).
            if (key.Keycode is Key.Space or Key.Enter or Key.KpEnter or Key.Escape) finishB.Close();
            return true;
        }
        if (key.Keycode == Key.Escape)
        {
            Cancel();
            return true;
        }
        if (key.Unicode == 0) return true; // Shift and the like do nothing
        if (reported || run.Finished) return true;
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
            errorT = 0.35;
            QueueRedraw();
        }
        if (run.Finished) Done();
    }

    void Highlight() =>
        top.Text = run.Typed == 0 ? Escape(run.Passage)
            : $"[color=#73ffa6][bgcolor=#5cd9ff26]{Escape(run.Passage[..run.Typed])}[/bgcolor][/color]{Escape(run.Passage[run.Typed..])}";

    static string Escape(string s) => s.Replace("[", "[lb]");

    void ShowClock()
    {
        int s = (int)Math.Ceiling(run.Remaining(Now));
        clock.Text = $"{s / 60}:{s % 60:00}";
    }

    /// <summary>The test is over (the passage typed, or the time up): record and show the result, once.</summary>
    void Done()
    {
        if (reported) return;
        ShowClock();
        int wpm = (int)run.NetWpm;
        G.Model.Save.TestWpm = wpm;
        G.Model.Save.Save();
        int suggest = TypingTestRun.Suggest(wpm);
        Finish(Strings.TestResult(wpm, (int)Math.Round(run.Accuracy * 100), Strings.Diffs[Math.Max(0, suggest)]));
    }

    void Finish(string message)
    {
        reported = true;
        finishB.Message = message + "\n\nPRESS SPACE TO CONTINUE";
        finishB.Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return; // a screen keeps processing while hidden
        if (errorT > 0 && (errorT -= delta) <= 0) QueueRedraw();
        if (reported) return;
        ShowClock();
        if (run.Tick(Now)) Done(); // out of time
    }

    public override void _Draw()
    {
        Kit.Rule(this, new Vector2(18, 42), new Vector2(200, 42));
        Kit.Panel(this, PassageBox);
        Kit.Panel(this, TypedBox, Kit.Good);
        if (errorT > 0)
        {
            // A miss: the passage's frame flashes red.
            var pts = Kit.Chamfer(PassageBox);
            DrawPolyline(pts.Append(pts[0]).ToArray(), new Color(Kit.Bad, 0.35f), 8, true);
            DrawPolyline(pts.Append(pts[0]).ToArray(), Kit.Bad, 2, true);
        }
    }

    void Cancel()
    {
        if (finishB.Visible) finishB.Close();
        else if (!G.Shell.Transitioning && !reported)
        {
            int last = G.Model.Save.TestWpm;
            Finish(last > 0 ? $"Test cancelled.\nYour speed stays at {last} WPM." : "Test cancelled.");
        }
    }
}

// Level complete, as a mission report: the level score, then each bonus multiplier counting the score up, then the
// final score and the medals this run earned, and three buttons: RETRY, NEXT LEVEL (the keyboard's choice, so Space or
// Enter plays on; left and right change it) and STAR MAP. Also where the level's best score, best difficulty and medals are
// recorded, and the defender and perfectionist achievements are checked.
public partial class ScoreUI : Screen
{
    const double Step = 1 / 60.0; // the original's 10 ms timer; Flash ran timers at most 60 times a second
    static readonly Rect2 Report = new(40, 92, 470, 360);

    readonly Label title, subtitle;
    readonly TextButton[] buttons;
    int focus = 1;
    readonly Label[][] rows = new Label[6][];
    readonly TextureRect[] runMedals = new TextureRect[2];
    readonly Label[] runMedalNames = new Label[2];
    int score, index, tickBy, shown, healthMedal, accuracyMedal;
    float cMult;
    int[] cLevel = null!;
    bool running;
    double time;

    static Game M => G.Model;

    public ScoreUI()
    {
        title = Kit.Label(this, "MISSION COMPLETE", Kit.Title(26, Kit.Good, Kit.BrassEdge), 0, 26, G.StageSize, HorizontalAlignment.Center);
        subtitle = Kit.Label(this, "", Kit.Caption(9), 0, 64, G.StageSize, HorizontalAlignment.Center);
        float[] y = { 110, 160, 192, 224, 0, 290 };
        for (int i = 0; i < rows.Length; i++)
        {
            if (i == 4) continue; // the rule, drawn
            var label = Kit.Label(this, "", Kit.Body(15), 62, y[i]);
            var value = Kit.Label(this, "", Kit.Body(17, Colors.White), 260, y[i] - 1, 228, HorizontalAlignment.Right);
            rows[i] = new[] { label, value };
        }
        // The final score: big, centred under its caption.
        rows[5][0].LabelSettings = Kit.Caption(10);
        rows[5][0].Size = new Vector2(Report.Size.X, 0);
        rows[5][0].Position = new Vector2(Report.Position.X, 272);
        rows[5][0].HorizontalAlignment = HorizontalAlignment.Center;
        rows[5][1].LabelSettings = Kit.Title(46, edge: Kit.BrassEdge);
        rows[5][1].Position = new Vector2(Report.Position.X, 288);
        rows[5][1].Size = new Vector2(Report.Size.X, 0);
        rows[5][1].HorizontalAlignment = HorizontalAlignment.Center;
        for (int i = 0; i < 2; i++)
        {
            runMedals[i] = new TextureRect { Scale = new Vector2(0.22f, 0.22f), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            AddChild(runMedals[i]);
            runMedalNames[i] = Kit.Label(this, i == 0 ? Strings.Health : Strings.Accuracy, Kit.Caption(8), 0, 0, 120, HorizontalAlignment.Center);
            runMedalNames[i].Visible = false;
        }
        buttons = new[]
        {
            new TextButton("RETRY", Retry, 13, boxed: true, width: 140),
            new TextButton("NEXT LEVEL", Next, 13, boxed: true, colour: Kit.Good, width: 140),
            new TextButton("STAR MAP", StarMap, 13, word: "map", boxed: true, width: 140),
        };
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].Position = new Vector2(50 + 155 * i, 470);
            buttons[i].Visible = false;
            buttons[i].MouseEntered += () => Focus(index);
            AddChild(buttons[i]);
            Nav.Add(buttons[i]);
        }
    }

    void Focus(int i)
    {
        focus = (i + buttons.Length) % buttons.Length;
        for (int k = 0; k < buttons.Length; k++) buttons[k].Focused = k == focus;
    }

    static void Retry()
    {
        if (!G.Shell.Transitioning) G.Shell.StartGame(M.Level, M.Cps);
    }

    /// <summary>The next level at the same difficulty; after the last, the victory credits.</summary>
    static void Next()
    {
        if (G.Shell.Transitioning) return;
        if (M.Level >= TypingShip.Core.Data.GameData.LevelCount - 1) G.Shell.ScoreDone();
        else G.Shell.StartGame(M.Level + 1, M.Cps);
    }

    static void StarMap()
    {
        if (G.Shell.Transitioning) return;
        G.Shell.Menu.ShowMapNow();
        G.Shell.Go(G.Shell.Menu, Shell.Dark);
    }

    public bool Counting => running;

    public override void NavIn()
    {
        foreach (var r in rows)
            if (r != null) r[0].Text = r[1].Text = "";
        foreach (var m in runMedals) m.Visible = false;
        foreach (var n in runMedalNames) n.Visible = false;
        foreach (var b in buttons) b.Visible = false;
        buttons[1].Text = M.Level >= TypingShip.Core.Data.GameData.LevelCount - 1 ? "FINISH" : "NEXT LEVEL";
        subtitle.Text = $"{Banner.LevelName(M.Level)}  ·  {Difficulty.Names[Difficulty.Of(M.Cps) - 1].ToUpperInvariant()}";
        score = M.Score - M.ScoreC;
        index = 0;
        time = 0;
        running = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        if (!running) return;
        time += delta;
        for (int n = 0; time >= Step && running && n < 4; n++)
        {
            time -= Step;
            ScoreTick();
        }
    }

    public override bool OnKey(InputEventKey key)
    {
        if (running) return key.Keycode is Key.Space or Key.Enter; // nothing to choose yet
        switch (key.Keycode)
        {
            case Key.Left: Focus(focus - 1); return true;
            case Key.Right or Key.Tab: Focus(focus + 1); return true;
            case Key.Space or Key.Enter or Key.KpEnter: buttons[focus].Press(); return true;
        }
        return false;
    }

    public override void _Draw()
    {
        Kit.Panel(this, Report, Kit.Good);
        if (index > 4) Kit.Rule(this, new Vector2(62, 258), new Vector2(488, 258), alpha: 0.5f);
    }

    void ScoreTick()
    {
        var row = rows[Math.Min(index, 5)];
        Label label = row?[0]!, value = row?[1]!;
        switch (index)
        {
            case 0: // the level score, less a point for each letter still in the typing bar
                if (label.Text == "")
                {
                    label.Text = Strings.LevelScore;
                    value.Text = score.ToString();
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
                    healthMedal = Scoring.HealthMedal(ship.Injured, ship.Health, ship.MaxHealth);
                    cLevel[2] = Math.Max(cLevel[2], healthMedal);
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
            case 4: // the rule
                index++;
                QueueRedraw();
                break;
            case 5:
                label.Text = Strings.FinalScore;
                value.Text = score.ToString();
                cLevel[0] = Math.Max(cLevel[0], score);
                float hit = M.HitRatio();
                accuracyMedal = Scoring.AccuracyMedal(hit);
                cLevel[3] = hit == 1 ? 3 : Math.Max(cLevel[3], accuracyMedal);
                if (hit == 1) M.CheckMedalAchievement(Achievement.Perfectionist, 3);
                M.Save.Save();
                ShowRunMedals();
                running = false;
                foreach (var b in buttons) b.Visible = true;
                Focus(1);
                break;
        }
    }

    /// <summary>This run's health and accuracy medals, side by side under the final score (dark if none).</summary>
    void ShowRunMedals()
    {
        int[] tiers = { healthMedal, accuracyMedal };
        for (int i = 0; i < 2; i++)
        {
            var art = Sprites.Achieve[tiers[i]];
            float cx = 275 + (i == 0 ? -70 : 70);
            runMedals[i].Texture = art;
            runMedals[i].Position = new Vector2(cx - art.GetWidth() * 0.22f / 2, 352);
            runMedals[i].Visible = true;
            runMedalNames[i].Position = new Vector2(cx - 60, 396);
            runMedalNames[i].LabelSettings = Kit.Caption(8, tiers[i] > 0 ? Kit.TierColours[tiers[i]] : new Color(Kit.Ink, 0.4f));
            runMedalNames[i].Visible = true;
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

// Game over: "SHIP DESTROYED" with the level score and a RETRY (the same level, at the same difficulty), or after
// the victory credits, the win screen with the total of every level's best score.
public partial class GameOverUI : Screen
{
    static readonly Rect2 ScoreBox = new(150, 170, 250, 120);
    readonly Label title, subtitle, caption, score, ranks;
    /// <summary>Counts endless runs shown, so a late leaderboard answer for an earlier run is dropped.</summary>
    int shown;
    readonly TextButton retry;
    bool won;

    public GameOverUI()
    {
        title = Kit.Label(this, "", Kit.Title(30), 0, 70, G.StageSize, HorizontalAlignment.Center);
        subtitle = Kit.Label(this, "", Kit.Caption(9), 0, 116, G.StageSize, HorizontalAlignment.Center);
        caption = Kit.Label(this, "", Kit.Caption(9), ScoreBox.Position.X, ScoreBox.Position.Y + 22, ScoreBox.Size.X, HorizontalAlignment.Center);
        score = Kit.Label(this, "", Kit.Title(40, edge: Kit.BrassEdge), ScoreBox.Position.X, ScoreBox.Position.Y + 42, ScoreBox.Size.X, HorizontalAlignment.Center);
        // Endless runs: the player's place on the Steam leaderboards, under the score.
        ranks = Kit.Label(this, "", Kit.Caption(8), 0, ScoreBox.End.Y + 14, G.StageSize, HorizontalAlignment.Center);
        retry = new TextButton("RETRY", Retry, 14, boxed: true, colour: Kit.Good, width: 170) { Position = new Vector2(190, 340) };
        AddChild(retry);
        Nav.Add(retry);
        var menu = new TextButton(Strings.Menu, () => G.Shell.Go(G.Shell.Menu, Shell.Dark), 14, boxed: true, width: 170)
        {
            Position = new Vector2(190, 386),
        };
        AddChild(menu);
        Nav.Add(menu);
        Kit.Label(this, "TYPE A BUTTON'S WORD, OR SPACE FOR THE MENU", Kit.Caption(7), 0, 440, G.StageSize, HorizontalAlignment.Center);
    }

    static void Retry()
    {
        if (G.Shell.Transitioning) return;
        if (G.Model.Endless != null) G.Shell.StartEndless(G.Model.Cps);
        else G.Shell.StartGame(G.Model.Level, G.Model.Cps);
    }

    public void AddWin()
    {
        won = true;
        title.Text = Strings.WonGame;
        title.LabelSettings = Kit.Title(30, Kit.Good, Kit.BrassEdge);
        subtitle.Text = "THE EMPEROR IS DEFEATED";
        caption.Text = "TOTAL SCORE";
        score.Text = G.Model.Save.Highscore.ToString();
        retry.Visible = false;
        ranks.Text = "";
        shown++;
        QueueRedraw();
    }

    public void AddLose(bool newBest = false)
    {
        var m = G.Model;
        won = false;
        title.Text = Strings.LoseGame;
        title.LabelSettings = Kit.Title(30, Kit.Bad);
        string diff = Difficulty.Names[Difficulty.Of(m.Cps) - 1].ToUpperInvariant();
        if (m.Endless is { } run)
        {
            int s = (int)m.Time, best = m.Save.EndlessBest[m.Diff - 1];
            subtitle.Text = $"ENDLESS  ·  {diff}  ·  WAVE {run.Wave}  ·  {s / 60}:{s % 60:00}";
            caption.Text = (newBest ? "NEW BEST SCORE" : $"SCORE  ·  BEST {best}") + $"  ·  x{run.ScoreFactor:0.##} FOR {diff}";
        }
        else
        {
            subtitle.Text = $"{Banner.LevelName(m.Level)}  ·  {diff}";
            caption.Text = Strings.LevelScore.TrimEnd(' ', ':');
        }
        score.Text = G.Model.Score.ToString();
        retry.Visible = true;
        ranks.Text = m.Endless != null && G.Platform.Name is { } store ? $"SENDING THE SCORE TO {store.ToUpperInvariant()}..." : "";
        int token = ++shown;
        if (m.Endless != null) Leaderboards.SubmitEndless(m, line =>
        {
            if (token == shown) ranks.Text = line ?? "";
        });
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!won)
        {
            // A red glow creeping in from the edges.
            for (int i = 0; i < 10; i++)
                DrawRect(new Rect2(i * 6, i * 6, G.StageSize - i * 12, G.StageSize - i * 12), new Color(Kit.Bad, 0.035f * (10 - i) / 10), false, 6);
        }
        Kit.Panel(this, ScoreBox, won ? Kit.Good : Kit.Bad);
    }

    public override bool OnKey(InputEventKey key)
    {
        // New: Space or Enter for MENU.
        if (key.Keycode is not (Key.Space or Key.Enter)) return false;
        G.Shell.Go(G.Shell.Menu, Shell.Dark);
        return true;
    }
}

// After the last level: the closing text and credits scroll up over the stars. SKIP or the end goes to the win
// screen.
public partial class VictoryUI : Screen
{
    readonly Label scroll;
    bool done;

    public VictoryUI()
    {
        scroll = Kit.Label(this, Strings.FinalCredits, Kit.Body(15), 60, 550, 430, HorizontalAlignment.Center, wrap: true);
        var skip = new TextButton(Strings.Skip, Done, 10, boxed: true) { Position = new Vector2(470, 12) };
        AddChild(skip);
        Nav.Add(skip);
    }

    public void Run()
    {
        G.Sound.Play(0);
        scroll.Position = new Vector2(60, 550);
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
        G.Shell.Go(G.Shell.GameOver, Shell.Dark, 5);
    }
}

// The pause screen over the game: the playfield dims under a cockpit-style panel. MENU asks to confirm; YES ends the
// level as a loss, NO resumes.
public partial class PauseUI : Control
{
    static readonly Rect2 Panel = new(135, 170, 280, 170);
    public readonly TypeNav Nav = new();
    readonly Control confirm = new() { Position = new Vector2(0, 288), MouseFilter = MouseFilterEnum.Ignore, Visible = false };

    public PauseUI()
    {
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        AddChild(new Label
        {
            Text = "PAUSED",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(Panel.Position.X, 188),
            Size = new Vector2(Panel.Size.X, 40),
            LabelSettings = Fonts.Settings(new FontVariation { BaseFont = Fonts.HudBold, SpacingGlyph = 5 }, 28, Colors.White, 5,
                new Color(Cockpit.Accent, 0.35f)),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddChild(new Label
        {
            Text = "ESC OR TYPE \"PAUSE\" TO RESUME",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(Panel.Position.X, 228),
            Size = new Vector2(Panel.Size.X, 14),
            LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(Cockpit.Rim, 0.8f)),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        var menu = new TextButton(Strings.Menu, GoMenu, 15) { Position = new Vector2(244, 252) };
        AddChild(menu);
        AddChild(confirm);
        confirm.AddChild(new Label
        {
            Text = "QUIT TO THE MENU?",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(Panel.Position.X, -14),
            Size = new Vector2(Panel.Size.X, 14),
            LabelSettings = Fonts.Settings(Fonts.Hud, 8, new Color(1, 0.6f, 0.5f)),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        var yes = new TextButton(Strings.Yes, MenuYes, 13) { Position = new Vector2(214, 0) };
        var no = new TextButton(Strings.No, MenuNo, 13) { Position = new Vector2(300, 0) };
        confirm.AddChild(yes);
        confirm.AddChild(no);
        Nav.Add(menu);
        Nav.Add(yes);
        Nav.Add(no);
        Nav.Add("pause", MenuNo); // typing it again resumes, as Escape does
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, G.StageSize, G.StageSize), new Color(Kit.Shade, 0.55f));
        DrawRect(Panel.Grow(3), new Color(0, 0, 0, 0.5f));
        DrawRect(Panel, Kit.Fill);
        DrawRect(Panel, new Color(Cockpit.Rim, 0.5f), false, 1.5f);
        DrawLine(Panel.Position + new Vector2(3, 3), new Vector2(Panel.End.X - 3, Panel.Position.Y + 3), new Color(Cockpit.Highlight, 0.35f), 2);
    }

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
