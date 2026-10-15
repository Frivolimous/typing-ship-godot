using Godot;

namespace TypingShip;

// The title menu. START slides the title away, tucks the other buttons into a row along the bottom and wipes the
// level map up from the bottom of the screen; from then on the menu is the map.
public partial class MenuUI : Screen
{
    const int Intro2 = 100;

    readonly TextureRect twinkle, title0, title1;
    readonly BitButton startB, highB, achieveB, moreB, creditsB;
    readonly TextButton testB;
    readonly Label highC;
    readonly Control mapClip = new() { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
    public readonly MapUI Map = new();
    int counter, twinkleC, introCounter = 1;

    public bool MapShown => mapClip.Visible;

    public MenuUI()
    {
        Picture(this, Sprites.MenuBack);
        AddChild(mapClip);
        mapClip.AddChild(Map);
        twinkle = Picture(this, Sprites.Twinkle[0]);
        twinkle.Texture = null; // the original's back2 starts empty
        title0 = Picture(this, Sprites.Title0);
        title1 = Picture(this, Sprites.Title1);
        highC = Text(this, "", Fonts.Title, 16, Colors.White, 5, 560, outline: 4, outlineColor: Palette.Blue);
        highC.Size = new Vector2(100, 0);
        highC.HorizontalAlignment = HorizontalAlignment.Right;

        startB = Button(Sprites.StartBt, StartGame, new Rect2(330, 175, 210, 60), "start");
        highB = Button(Sprites.HighBt, () => G.Shell.ShowBox(Strings.HighscoreStub), new Rect2(390, 235, 150, 42), "high");
        achieveB = Button(Sprites.AchieveBt, () => G.Shell.Go(G.Shell.Achievements, Colors.White), new Rect2(380, 277, 160, 40), "achievements");
        moreB = Button(Sprites.MoreBt, () => G.Shell.GoSponsor(), new Rect2(395, 317, 145, 40), "more");
        creditsB = Button(Sprites.CreditsBt, () => G.Shell.Go(G.Shell.Credits, Colors.White), new Rect2(420, 357, 120, 50), "credits");
        Nav.Add(startB.Word, startB.Press, () => !MapShown);
        foreach (var b in new[] { highB, achieveB, moreB, creditsB }) Nav.Add(b.Word, b.Press);

        // New: the original had a typing test (MenuUI.goTest) but no way to reach it.
        testB = new TextButton(Strings.TypingTest, () => G.Shell.Go(G.Shell.Test, Colors.Black), Palette.Gold, Palette.GoldOver, true, "test");
        AddChild(testB);
        Nav.Add(testB);

        Map.AddWords(Nav);
        Intro1();
    }

    BitButton Button(Texture2D t, Action f, Rect2 hit, string word)
    {
        var b = new BitButton(t, f, hit, word) { Position = new Vector2(250, 0) };
        AddChild(b);
        return b;
    }

    public override void NavIn()
    {
        highC.Text = G.Model.Save.Highscore.ToString();
        if (MapShown) Map.NavIn();
    }

    public override void OnTick()
    {
        counter++;
        twinkle.Modulate = new Color(1, 1, 1, counter <= 20 ? counter / 20f : (40 - counter) / 20f);
        if (counter == 40)
        {
            twinkleC = (twinkleC + 1) % 3;
            twinkle.Texture = Sprites.Twinkle[twinkleC];
            counter = 0;
        }
        if (introCounter > 0)
        {
            if (introCounter < Intro2) Intro1();
            else Intro2Tick();
        }
        if (MapShown) Map.OnTick();
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode == Key.Escape && Map.Diff.Visible)
        {
            Map.Diff.Visible = false;
            return true;
        }
        return false;
    }

    // Buttons slide in from the right one after another; the two halves of the title meet in the middle.
    void Intro1()
    {
        int c = ++introCounter;
        if (c <= 10) startB.Position = new Vector2((10 - c) * 50, 0);
        if (c > 3 && c <= 13) highB.Position = new Vector2((13 - c) * 50, 0);
        if (c > 6 && c <= 16) achieveB.Position = new Vector2((16 - c) * 50, 0);
        if (c > 9 && c <= 19) moreB.Position = new Vector2((19 - c) * 50, 0);
        if (c > 12 && c <= 22)
        {
            creditsB.Position = new Vector2((22 - c) * 50, 0);
            testB.Position = new Vector2(430 + (22 - c) * 50, 412);
        }
        if (c <= 28)
        {
            title0.Position = new Vector2(-(28 - c) * 100, 0);
            title1.Position = new Vector2((28 - c) * 100, 0);
        }
        if (c > 30) introCounter = 0;
    }

    // START: the title leaves, the buttons shrink into the bottom row and the map wipes in from below.
    void Intro2Tick()
    {
        int c = ++introCounter - Intro2;
        if (c <= 20)
        {
            title0.Position = new Vector2(30 * c, 0);
            title1.Position = new Vector2(-30 * c, 0);
        }
        Tuck(creditsB, c, 0, 7, 180);
        Tuck(moreB, c, 3, -87, 220);
        Tuck(achieveB, c, 6, -217, 255);
        Tuck(highB, c, 9, -348, 275);
        if (c > 9 && c <= 19) highC.Position = new Vector2(5, 563 - 40 * (c - 9) / 10f);
        if (c > 12 && c <= 22) startB.Position = new Vector2(300 * (c - 12) / 10f, 0);
        if (c <= 10) testB.Position = new Vector2(430, 412).Lerp(new Vector2(435, 8), c / 10f);
        if (c <= 30) SetMask(550 * (30 - c) / 30f);
        if (c > 30) introCounter = 0;
    }

    static void Tuck(BitButton b, int c, int start, float x, float y)
    {
        if (c <= start || c > start + 10) return;
        float f = (c - start) / 10f;
        b.Scale = Vector2.One * (1 - 0.1f * f);
        b.Position = new Vector2(x * f, y * f);
    }

    void SetMask(float y)
    {
        mapClip.Position = new Vector2(0, y);
        mapClip.Size = new Vector2(G.StageSize, G.StageSize - y);
        Map.Position = new Vector2(0, -y);
    }

    void StartGame()
    {
        if (MapShown) return;
        introCounter = Intro2;
        SetMask(G.StageSize);
        mapClip.Visible = true;
        Map.NavIn();
    }

    /// <summary>Dev shortcut (and the screenshot tool): skip the intro and show the map straight away.</summary>
    public void ShowMapNow()
    {
        if (!MapShown) StartGame();
        while (introCounter > 0) Intro2Tick();
    }

    public override void _Input(InputEvent e)
    {
        // A click anywhere but the difficulty box closes it (KeyboardControl.click). A level button reopens it.
        if (IsVisibleInTree() && Map.Diff.Visible && e is InputEventMouseButton { Pressed: true } &&
            Map.Diff.MakeInputLocal(e) is InputEventMouseButton local && !new Rect2(Vector2.Zero, Map.Diff.Size).HasPoint(local.Position))
            Map.Diff.Visible = false;
    }
}

// The level map: twelve level buttons along a winding path, a boss (B) every fourth level.
public partial class MapUI : Control
{
    static readonly Vector2[] Spots =
    {
        new(138, 365), new(216, 314), new(307, 314), new(405, 324), new(478, 285), new(453, 208),
        new(374, 204), new(296, 203), new(193, 193), new(106, 165), new(148, 72), new(251, 40),
    };

    readonly LevelButton[] levels = new LevelButton[12];
    public readonly DiffSelect Diff = new();

    public MapUI()
    {
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(new TextureRect { Texture = Sprites.MapBack, MouseFilter = MouseFilterEnum.Ignore });
        for (int i = 0; i < 12; i++)
        {
            // Levels 1-9 by number; the bosses were all "B" and are B1-B3 here, so each has a word to type.
            string name = (i + 1) % 4 == 0 ? $"B{(i + 1) / 4}" : (i + 1 - (i + 1) / 4).ToString();
            levels[i] = new LevelButton(name, i, OpenDiff) { Position = Spots[i] };
            AddChild(levels[i]);
        }
        AddChild(Diff);
    }

    public void AddWords(TypeNav nav)
    {
        foreach (var l in levels)
        {
            var b = l;
            nav.Add(b.Word, () => OpenDiff(b), () => IsVisibleInTree() && b.Unlocked);
        }
        for (int i = 0; i < Strings.Diffs.Length; i++)
        {
            double cps = Difficulty.Cps[i];
            nav.Add(Strings.Diffs[i], () => Diff.StartGame(cps), () => Diff.IsVisibleInTree());
        }
    }

    public void NavIn()
    {
        Diff.Visible = false;
        var save = G.Model.Save;
        for (int i = 0; i < levels.Length; i++)
        {
            var d = save.LevelScore[i];
            if (d[1] == 0) levels[i].SetState(save.Unlocked(i) ? 0 : -1);
            else levels[i].SetState(d[1], d[2], d[3]);
        }
    }

    public void OnTick()
    {
        foreach (var l in levels) l.OnTick();
    }

    void OpenDiff(LevelButton b)
    {
        if (!b.Unlocked) return;
        G.Sound.Sound(SoundControl.Click);
        Diff.Open(b.Index, b.Position);
    }
}

// A level on the map: a spinning ring coloured by the best difficulty cleared (grey and locked, white and pulsing
// for the next level), with the best health and accuracy medals beneath.
public partial class LevelButton : Control
{
    static readonly Vector2 Center = new(7, 12);
    static readonly Color[] DiffColors =
        { new(0xffffffff), new(0x88aaffff), new(0x88ff88ff), new(0xffff88ff), new(0xffbb88ff), new(0xff8888ff) };

    public readonly int Index;
    public readonly string Word;
    public bool Unlocked { get; private set; }
    readonly Action<LevelButton> click;
    readonly Label label;
    readonly Sprite2D spin = new() { Position = Center };
    readonly Sprite2D health = new() { Centered = false }, target = new() { Centered = false };
    readonly Label healthT, targetT;
    Color color;
    bool isNew, fadingIn, hover;
    float newAlpha = 1;

    public LevelButton(string name, int index, Action<LevelButton> onClick)
    {
        Index = index;
        Word = name.ToLowerInvariant();
        click = onClick;
        MouseFilter = MouseFilterEnum.Stop;
        Size = new Vector2(1, 1); // hits are tested against the ring (_HasPoint)
        AddChild(spin);
        label = new Label
        {
            Text = name,
            Position = new Vector2(-13, -1),
            Size = new Vector2(40, 25),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(label);
        health.Position = new Vector2(Center.X - 20, Center.Y + 28);
        target.Position = new Vector2(Center.X + 8, Center.Y + 28);
        AddChild(health);
        AddChild(target);
        healthT = MedalText(Center.X - 60, Strings.Health);
        targetT = MedalText(Center.X + 3, Strings.Accuracy);
        MouseEntered += () => Over(true);
        MouseExited += () => Over(false);
    }

    Label MedalText(float x, string text)
    {
        var l = new Label
        {
            Text = text,
            Position = new Vector2(x, Center.Y + 39),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(l);
        return l;
    }

    /// <param name="diff">-1 locked, 0 next to play, 1-5 the best difficulty cleared.</param>
    public void SetState(int diff, int healthMedal = 0, int targetMedal = 0)
    {
        Unlocked = diff >= 0;
        color = diff < 0 ? new Color(0x333333ff) : DiffColors[diff];
        spin.Texture = diff < 0 ? Sprites.LockC : diff == 0 ? Sprites.NextC : Sprites.CompleteC;
        // The original's ColorTransform multiplied each channel by (colour + 0.8).
        spin.Modulate = new Color(color.R + 0.8f, color.G + 0.8f, color.B + 0.8f);
        label.LabelSettings = Fonts.Settings(Fonts.Space, 20, color, 2);
        isNew = diff == 0;
        MouseDefaultCursorShape = Unlocked ? CursorShape.PointingHand : CursorShape.Arrow;
        health.Texture = Sprites.Health[healthMedal];
        target.Texture = Sprites.Target[targetMedal];
        healthT.LabelSettings = Fonts.Settings(Fonts.Title, 10, Colors.White, 3, MedalColor(healthMedal));
        targetT.LabelSettings = Fonts.Settings(Fonts.Title, 10, Colors.White, 3, MedalColor(targetMedal));
        healthT.Text = healthMedal > 0 ? Strings.Health : "";
        targetT.Text = targetMedal > 0 ? Strings.Accuracy : "";
        QueueRedraw();
    }

    static Color MedalColor(int tier) => tier switch { 1 => Palette.Bronze, 2 => Palette.Silver, _ => Palette.Gold };

    public void OnTick()
    {
        if (fadingIn)
        {
            if (newAlpha >= 1) fadingIn = false;
            else newAlpha += 0.1f;
        }
        else
        {
            if (newAlpha <= 0) fadingIn = true;
            else newAlpha -= 0.1f;
        }
        spin.RotationDegrees += 1;
        if (isNew) QueueRedraw();
    }

    // newOver: a soft glow around the centre of the next level to play, pulsing in and out.
    public override void _Draw()
    {
        if (!isNew) return;
        for (int r = 30; r >= 6; r -= 3)
            DrawCircle(Center, r, new Color(color, Mathf.Clamp(newAlpha, 0, 1) * 0.2f));
    }

    void Over(bool b)
    {
        hover = b;
        float s = b ? 1.2f : 1;
        spin.Scale = health.Scale = target.Scale = new Vector2(s, s);
        target.Position = new Vector2(Center.X + 8 - (b ? 2 : 0), Center.Y + 28);
        healthT.Visible = targetT.Visible = b;
    }

    public override bool _HasPoint(Vector2 point) => point.DistanceTo(Center) <= 32 * (hover ? 1.2f : 1);

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        AcceptEvent();
        click(this);
    }
}

// The difficulty box beside a level button: its best score, then one button per difficulty.
public partial class DiffSelect : Control
{
    static readonly Color[] Backs = { new(0x003355ff), new(0x005500ff), new(0x555500ff), new(0x553300ff), new(0x550000ff) };
    static readonly Color[] Fores = { new(0x88aaffff), new(0x88ff88ff), new(0xffff88ff), new(0xffdd88ff), new(0xff8888ff) };

    readonly Label highscore;
    int level, recommended = -1;

    public DiffSelect()
    {
        Size = new Vector2(70, 135);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        highscore = new Label
        {
            Position = new Vector2(0, 3),
            Size = new Vector2(65, 20),
            HorizontalAlignment = HorizontalAlignment.Right,
            LabelSettings = Fonts.Settings(Fonts.Space, 16, Palette.Gold),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(highscore);
        for (int i = 0; i < Strings.Diffs.Length; i++)
        {
            double cps = Difficulty.Cps[i];
            var b = new Label
            {
                Text = Strings.Diffs[i],
                Position = new Vector2(5, 30 + 20 * i),
                Size = new Vector2(60, 20),
                LabelSettings = Fonts.Settings(Fonts.Space, 11, Fores[i]),
                ClipText = true,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            b.AddThemeStyleboxOverride("normal", new StyleBoxFlat
            {
                BgColor = Backs[i],
                BorderColor = new Color(0x000002ff),
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                ContentMarginLeft = 2,
            });
            b.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                b.AcceptEvent();
                StartGame(cps);
            };
            AddChild(b);
        }
    }

    public void Open(int level, Vector2 at)
    {
        this.level = level;
        highscore.Text = G.Model.Save.LevelScore[level][0].ToString();
        // The original put the box 20 px right of the button, so near the right edge it hung off the stage.
        Position = new Vector2(Math.Min(at.X + 20, G.StageSize - Size.X - 2), Math.Min(at.Y, G.StageSize - Size.Y - 2));
        // New: point out the hardest difficulty the typing test says you can keep up with.
        int test = G.Model.Save.TestWpm;
        recommended = test <= 0 ? -1 : Math.Max(0, Array.FindLastIndex(Strings.DiffWpm, w => w <= test));
        Visible = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Colors.Black);
        DrawRect(new Rect2(Vector2.Zero, Size), Palette.BorderBlue, false, 2);
        if (recommended < 0) return;
        float y = 40 + 20 * recommended;
        DrawColoredPolygon(new[] { new Vector2(66, y), new Vector2(69, y - 4), new Vector2(69, y + 4) }, Palette.Gold);
    }

    public void StartGame(double cps)
    {
        Visible = false;
        G.Sound.Sound(SoundControl.Click);
        G.Shell.StartGame(level, cps);
    }
}
