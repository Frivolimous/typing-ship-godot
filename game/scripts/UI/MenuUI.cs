using Godot;

namespace TypingShip;

// The title menu: the title over the player's ship, idling with its engines lit, and the buttons. START sends the
// ship off the top of the screen, the title and buttons away, and fades the level map in, with the other buttons in
// a row along the bottom; from then on the menu is the map.
public partial class MenuUI : Screen
{
    readonly Control title = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Control menuButtons = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Control mapBar = new() { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
    readonly Sprite2D ship = new() { Texture = Sprites.Player[0], Scale = new Vector2(0.7f, 0.7f) };
    readonly Label best;
    public readonly MapUI Map = new() { Visible = false };
    bool introDone, leaving;
    float time;

    static readonly Vector2 ShipHome = new(275, 292);

    public bool MapShown => Map.Visible;

    public MenuUI()
    {
        AddChild(Map);

        // The logo (assets/ui/logo.png, drawn by Jeremy in a vector editor; source in art/logo.png): the name and the
        // tagline, trimmed to its edges.
        AddChild(title);
        var logo = GD.Load<Texture2D>("res://assets/ui/logo.png");
        const float logoWidth = 440;
        float logoHeight = logoWidth * logo.GetHeight() / logo.GetWidth();
        title.AddChild(new TextureRect
        {
            Texture = logo,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            // Centred on the lettering: the R's tail runs out to the right, so the image's middle is 2% right of it.
            Position = new Vector2((G.StageSize - logoWidth) / 2 + 0.022f * logoWidth, 40),
            Size = new Vector2(logoWidth, logoHeight),
            TextureFilter = TextureFilterEnum.Linear,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        ship.Position = ShipHome;
        EngineFlames.Attach(ship);
        Sprites.ArtChanged += () =>
        {
            ship.Texture = Sprites.Player[0];
            EngineFlames.Reattach(ship);
        };
        AddChild(ship);

        AddChild(menuButtons);
        string[] names = { "START GAME", "ACHIEVEMENTS", "TYPING TEST", "CREDITS" };
        Action[] actions = { StartGame, GoAchievements, GoTest, GoCredits };
        for (int i = 0; i < names.Length; i++)
        {
            var b = new TextButton(names[i], actions[i], i == 0 ? 15 : 12, boxed: true, width: 200,
                colour: i == 0 ? Kit.Gold : null, bold: i == 0) { Position = new Vector2(175, 382 + 36 * i) };
            menuButtons.AddChild(b);
            Nav.Add(b);
        }

        // Under the map: the other screens, in a row.
        AddChild(mapBar);
        string[] barNames = { "ACHIEVEMENTS", "TYPING TEST", "CREDITS" };
        Action[] barActions = { GoAchievements, GoTest, GoCredits };
        for (int i = 0; i < barNames.Length; i++)
        {
            var b = new TextButton(barNames[i], barActions[i], 9, boxed: true, width: 122) { Position = new Vector2(12 + 132 * i, 512) };
            mapBar.AddChild(b);
            Nav.Add(b);
        }

        // Bottom right, on the title and the map: the options.
        var options = new TextButton("OPTIONS", () => G.Shell.Go(G.Shell.Options, Shell.Dark), 13, boxed: true, width: 126)
        {
            Position = new Vector2(412, 506),
        };
        AddChild(options);
        Nav.Add(options);

        best = Kit.Label(this, "", Kit.Body(12), 376, 22, 162, HorizontalAlignment.Right);
        Kit.Label(this, "BEST TOTAL", Kit.Caption(7), 376, 12, 162, HorizontalAlignment.Right);

        Map.AddWords(Nav);
    }

    static void GoAchievements() => G.Shell.Go(G.Shell.Achievements, Shell.Dark);
    static void GoTest() => G.Shell.Go(G.Shell.Test, Shell.Dark);
    static void GoCredits() => G.Shell.Go(G.Shell.Credits, Shell.Dark);

    public override void NavIn()
    {
        best.Text = G.Model.Save.Highscore.ToString();
        if (MapShown) Map.NavIn();
        else if (!introDone) Intro();
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        // The ship idles: a slow bob and sway.
        if (!leaving && introDone) ship.Position = ShipHome + new Vector2(Mathf.Sin(time * 0.7f) * 3, Mathf.Sin(time * 1.3f) * 4);
    }

    public override bool OnKey(InputEventKey key)
    {
        if (key.Keycode != Key.Escape) return false;
        // Escape closes the difficulty box, or else asks whether to quit.
        if (Map.Diff.Visible) Map.Diff.Visible = false;
        else G.Shell.AskQuit();
        return true;
    }

    /// <summary>The first time: the title fades down into place, the ship rises from below, the buttons slide in.</summary>
    void Intro()
    {
        introDone = true;
        var t = CreateTween().SetParallel();
        title.Modulate = new Color(1, 1, 1, 0);
        title.Position = new Vector2(0, -20);
        t.TweenProperty(title, "modulate:a", 1f, 0.8).SetDelay(0.2);
        t.TweenProperty(title, "position", Vector2.Zero, 0.8).SetDelay(0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        ship.Position = ShipHome + new Vector2(0, 320);
        t.TweenProperty(ship, "position", ShipHome, 1.4).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        int i = 0;
        foreach (var b in menuButtons.GetChildren().OfType<Control>())
        {
            var to = b.Position;
            b.Position = to + new Vector2(0, 40);
            b.Modulate = new Color(1, 1, 1, 0);
            t.TweenProperty(b, "position", to, 0.5).SetDelay(0.6 + 0.12 * i).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            t.TweenProperty(b, "modulate:a", 1f, 0.4).SetDelay(0.6 + 0.12 * i);
            i++;
        }
    }

    /// <summary>START: the ship launches off the top, the title and buttons go, and the map fades in.</summary>
    void StartGame()
    {
        if (MapShown || leaving) return;
        leaving = true;
        G.Sound.Sound(SoundControl.Click);
        var t = CreateTween().SetParallel();
        t.TweenProperty(ship, "position", new Vector2(ship.Position.X, -160), 0.9).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        t.TweenProperty(title, "modulate:a", 0f, 0.4);
        t.TweenProperty(title, "position", new Vector2(0, -40), 0.4).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        t.TweenProperty(menuButtons, "modulate:a", 0f, 0.3);
        t.TweenProperty(menuButtons, "position", new Vector2(0, 30), 0.3);
        t.Chain().TweenCallback(Callable.From(ShowMap));
    }

    void ShowMap()
    {
        leaving = false;
        title.Visible = menuButtons.Visible = ship.Visible = false;
        Map.Visible = mapBar.Visible = true;
        Map.NavIn();
        Map.Modulate = mapBar.Modulate = new Color(1, 1, 1, 0);
        var t = CreateTween().SetParallel();
        t.TweenProperty(Map, "modulate:a", 1f, 0.6);
        t.TweenProperty(mapBar, "modulate:a", 1f, 0.6).SetDelay(0.2);
    }

    /// <summary>Dev shortcut (and the screenshot tool): skip the intro and show the map straight away.</summary>
    public void ShowMapNow()
    {
        if (MapShown) return;
        introDone = true;
        title.Visible = menuButtons.Visible = ship.Visible = false;
        Map.Visible = mapBar.Visible = true;
        Map.NavIn();
    }

    public override void _Input(InputEvent e)
    {
        // A click anywhere but the difficulty box closes it (KeyboardControl.click). A level button reopens it.
        if (IsVisibleInTree() && Map.Diff.Visible && e is InputEventMouseButton { Pressed: true } &&
            Map.Diff.MakeInputLocal(e) is InputEventMouseButton local && !new Rect2(Vector2.Zero, Map.Diff.Size).HasPoint(local.Position))
            Map.Diff.Visible = false;
    }
}

// The level map, drawn as a star chart: a faint grid, the route from level to level (lit as far as you've reached,
// with light flowing along it), and the twelve levels, a boss every fourth.
public partial class MapUI : Control
{
    static readonly Vector2[] Spots =
    {
        new(145, 377), new(223, 326), new(314, 326), new(412, 336), new(485, 297), new(460, 220),
        new(381, 216), new(303, 215), new(200, 205), new(113, 177), new(155, 84), new(258, 70),
    };

    readonly LevelButton[] levels = new LevelButton[12];
    readonly EndlessButton endless;
    public readonly DiffSelect Diff = new();
    Vector2[] route = Array.Empty<Vector2>();
    int[] routeLevel = Array.Empty<int>(); // which level each route point leads to
    int reached;
    float time;

    public MapUI()
    {
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Ignore;
        Kit.Label(this, "SECTOR MAP", Kit.Title(16), 16, 10);
        Kit.Label(this, "TYPE A LEVEL'S NAME TO PLAY IT", Kit.Caption(7), 16, 34);
        BuildRoute();
        for (int i = 0; i < 12; i++)
        {
            // Levels 1-9 by number; the bosses were all "B" and are B1-B3 here, so each has a word to type.
            string name = (i + 1) % 4 == 0 ? $"B{(i + 1) / 4}" : (i + 1 - (i + 1) / 4).ToString();
            levels[i] = new LevelButton(name, i, OpenDiff) { Position = Spots[i] };
            AddChild(levels[i]);
        }
        endless = new EndlessButton(OpenEndless) { Position = EndlessButton.Spot };
        AddChild(endless);
        AddChild(Diff);
    }

    /// <summary>A smooth curve through the levels (Catmull-Rom), remembering which level each stretch leads to.</summary>
    void BuildRoute()
    {
        var pts = new List<Vector2>();
        var leads = new List<int>();
        for (int i = 0; i < Spots.Length - 1; i++)
        {
            Vector2 p0 = Spots[Math.Max(i - 1, 0)], p1 = Spots[i], p2 = Spots[i + 1], p3 = Spots[Math.Min(i + 2, Spots.Length - 1)];
            for (int k = 0; k < 16; k++)
            {
                float t = k / 16f, t2 = t * t, t3 = t2 * t;
                pts.Add(0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3));
                leads.Add(i + 1);
            }
        }
        pts.Add(Spots[^1]);
        leads.Add(Spots.Length - 1);
        route = pts.ToArray();
        routeLevel = leads.ToArray();
    }

    public void AddWords(TypeNav nav)
    {
        foreach (var l in levels)
        {
            var b = l;
            nav.Add(b.Word, () => OpenDiff(b), () => IsVisibleInTree() && b.Unlocked);
        }
        nav.Add("endless", OpenEndless, () => IsVisibleInTree() && endless.Unlocked);
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
        reached = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            var d = save.LevelScore[i];
            if (d[1] == 0) levels[i].SetState(save.Unlocked(i) ? 0 : -1);
            else levels[i].SetState(d[1], d[2], d[3]);
            if (levels[i].Unlocked) reached = i;
        }
        // Endless opens once the campaign is beaten (boss 3 cleared on any difficulty).
        endless.SetState(save.LevelScore[TypingShip.Core.Data.GameData.LevelCount - 1][1] > 0, save.EndlessBest.Max());
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // The chart's grid, fading out towards the edges.
        for (float x = 25; x < G.StageSize; x += 50)
            DrawLine(new Vector2(x, 60), new Vector2(x, 490), new Color(Kit.Rim, 0.05f), 1);
        for (float y = 75; y < 500; y += 50)
            DrawLine(new Vector2(0, y), new Vector2(G.StageSize, y), new Color(Kit.Rim, 0.05f), 1);

        // The route: dim where it's still locked, lit as far as you've reached.
        for (int i = 1; i < route.Length; i++)
        {
            bool lit = routeLevel[i] <= reached;
            if (!lit && (i / 3) % 2 == 1) continue; // locked stretches are dashed
            DrawLine(route[i - 1], route[i], lit ? new Color(Kit.Rim, 0.18f) : new Color(1, 1, 1, 0.08f), lit ? 6 : 2, true);
            if (lit) DrawLine(route[i - 1], route[i], new Color(Kit.Rim, 0.7f), 1.5f, true);
        }
        // Light flowing along the lit part, towards the next level.
        int litEnd = Array.FindLastIndex(routeLevel, l => l <= reached);
        for (int k = 0; k < 8 && litEnd > 0; k++)
        {
            float u = (time * 0.12f + k / 8f) % 1;
            var p = route[(int)(u * litEnd)];
            DrawCircle(p, 2.2f, new Color(Kit.Rim.Lerp(Colors.White, 0.5f), Mathf.Sin(u * Mathf.Pi)));
        }
    }

    void OpenDiff(LevelButton b)
    {
        if (!b.Unlocked) return;
        G.Sound.Sound(SoundControl.Click);
        Diff.Open(b.Index, b.Position);
    }

    /// <summary>The difficulty box for an endless run, to the left of its circle.</summary>
    public void OpenEndless()
    {
        if (!endless.Unlocked) return;
        G.Sound.Sound(SoundControl.Click);
        Diff.Open(-1, EndlessButton.Spot - new Vector2(EndlessButton.Size + 145, 0));
    }
}

// Endless mode on the map: a big circle apart from the route, with ENDLESS in the middle. Locked (dim) until the
// campaign is beaten; then it turns like the next level's ring, with the best endless score beneath.
public partial class EndlessButton : Control
{
    public static readonly Vector2 Spot = new(400, 432);
    public new const float Size = 40;
    public bool Unlocked { get; private set; }
    readonly Action click;
    int best;
    bool hover;
    float time;

    float Radius => Size * (hover && Unlocked ? 1.08f : 1);

    public EndlessButton(Action onClick)
    {
        click = onClick;
        MouseFilter = MouseFilterEnum.Stop;
        base.Size = new Vector2(1, 1); // hits are tested against the ring (_HasPoint)
        MouseEntered += () => hover = true;
        MouseExited += () => hover = false;
    }

    public void SetState(bool unlocked, int best)
    {
        Unlocked = unlocked;
        this.best = best;
        MouseDefaultCursorShape = unlocked ? CursorShape.PointingHand : CursorShape.Arrow;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float r = Radius;
        var colour = Unlocked ? Kit.Rim : Kit.Locked;
        if (Unlocked)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 2.5f);
            DrawCircle(Vector2.Zero, r + 14, new Color(colour, 0.06f + 0.08f * pulse));
            for (int k = 0; k < 10; k++)
            {
                float a = time * 0.6f + k * Mathf.Tau / 10;
                DrawArc(Vector2.Zero, r + 8, a, a + 0.38f, 6, new Color(colour, 0.8f), 1.5f, true);
            }
        }
        DrawCircle(Vector2.Zero, r, new Color(Kit.Fill, 0.95f));
        if (Unlocked) DrawCircle(Vector2.Zero, r, new Color(colour, 0.12f));
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(colour, Unlocked ? 0.95f : 0.4f), Unlocked ? 2.5f : 1.2f, true);
        DrawArc(Vector2.Zero, r - 6, 0, Mathf.Tau, 48, new Color(colour, Unlocked ? 0.35f : 0.15f), 1, true);

        var font = Fonts.HudBold;
        float h = font.GetAscent(10) - font.GetDescent(10);
        DrawString(font, new Vector2(-r, h / 2 - 1), "ENDLESS", HorizontalAlignment.Center, r * 2, 10,
            Unlocked ? Colors.White : new Color(colour, 0.6f));
        string under = Unlocked ? (best > 0 ? $"BEST {best}" : "") : "BEAT THE CAMPAIGN";
        if (under != "")
            DrawString(Fonts.Hud, new Vector2(-70, Size + 22), under, HorizontalAlignment.Center, 140, 8, new Color(colour, Unlocked ? 0.9f : 0.6f));
    }

    public override bool _HasPoint(Vector2 point) => point.Length() <= Radius + 6;

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        AcceptEvent();
        if (Unlocked) click();
    }
}

// A level on the map: a ring lit by the best difficulty cleared (dark when locked, pulsing cyan for the next level
// to play), the boss's own ship as a boss level's node, and the best health and accuracy medals beneath as small
// icons in bronze, silver or gold.
public partial class LevelButton : Control
{
    public readonly int Index;
    public readonly string Word;
    public bool Unlocked { get; private set; }
    readonly Action<LevelButton> click;
    readonly string name;
    readonly Texture2D? boss;
    readonly Label healthT, targetT;
    int diff = -1, healthMedal, targetMedal;
    bool hover;
    float time;

    float Radius => (boss != null ? 22 : 14) * (hover ? 1.15f : 1);

    public LevelButton(string name, int index, Action<LevelButton> onClick)
    {
        Index = index;
        this.name = name;
        Word = name.ToLowerInvariant();
        click = onClick;
        if ((index + 1) % 4 == 0) boss = Sprites.Named(Boss.Arts[(index + 1) / 4 - 1]);
        MouseFilter = MouseFilterEnum.Stop;
        Size = new Vector2(1, 1); // hits are tested against the ring (_HasPoint)
        healthT = Kit.Label(this, Strings.Health, Kit.Caption(7, Colors.White), -60, 0, 50, HorizontalAlignment.Right);
        targetT = Kit.Label(this, Strings.Accuracy, Kit.Caption(7, Colors.White), 10, 0, 60);
        healthT.Visible = targetT.Visible = false;
        MouseEntered += () => Over(true);
        MouseExited += () => Over(false);
    }

    /// <param name="diff">-1 locked, 0 next to play, 1-5 the best difficulty cleared.</param>
    public void SetState(int diff, int healthMedal = 0, int targetMedal = 0)
    {
        this.diff = diff;
        this.healthMedal = healthMedal;
        this.targetMedal = targetMedal;
        Unlocked = diff >= 0;
        MouseDefaultCursorShape = Unlocked ? CursorShape.PointingHand : CursorShape.Arrow;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float r = Radius;
        var colour = diff < 0 ? Kit.Locked : diff == 0 ? Kit.Rim : Kit.DiffColours[diff - 1];
        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 4);

        if (diff == 0)
        {
            // The next level: a soft pulsing glow and a ring of dashes turning around it.
            DrawCircle(Vector2.Zero, r + 10, new Color(colour, 0.08f + 0.1f * pulse));
            for (int k = 0; k < 6; k++)
            {
                float a = time * 1.2f + k * Mathf.Tau / 6;
                DrawArc(Vector2.Zero, r + 6, a, a + 0.6f, 6, new Color(colour, 0.8f), 1.5f, true);
            }
        }
        DrawCircle(Vector2.Zero, r, new Color(Kit.Fill, 0.95f));
        if (diff > 0) DrawCircle(Vector2.Zero, r, new Color(colour, 0.15f));
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 32, new Color(colour, diff < 0 ? 0.4f : 0.95f), diff < 0 ? 1.2f : 2, true);

        if (boss != null)
        {
            // The boss's own ship, small, inside its ring (dark while locked).
            var size = boss.GetSize();
            float s = (r * 1.7f) / Math.Max(size.X, size.Y);
            var drawn = size * s;
            DrawTextureRect(boss, new Rect2(-drawn / 2, drawn), false, diff < 0 ? Kit.Unearned : Colors.White);
            DrawString(Fonts.HudBold, new Vector2(-30, -r - 6), name, HorizontalAlignment.Center, 60, 10,
                diff < 0 ? new Color(colour, 0.6f) : Colors.White);
        }
        else
        {
            var font = Fonts.HudBold;
            float h = font.GetAscent(13) - font.GetDescent(13);
            DrawString(font, new Vector2(-20, h / 2 - 1), name, HorizontalAlignment.Center, 40, 13,
                diff < 0 ? new Color(colour, 0.6f) : Colors.White);
        }

        // The medals: a cross for health, a target for accuracy.
        float y = r + 9;
        if (healthMedal > 0)
        {
            var c = Kit.TierColours[healthMedal];
            var at = new Vector2(-9, y);
            DrawCircle(at, 5, new Color(Kit.Fill, 1));
            DrawArc(at, 5, 0, Mathf.Tau, 16, c, 1.2f, true);
            DrawLine(at + new Vector2(-2.5f, 0), at + new Vector2(2.5f, 0), c, 1.6f);
            DrawLine(at + new Vector2(0, -2.5f), at + new Vector2(0, 2.5f), c, 1.6f);
        }
        if (targetMedal > 0)
        {
            var c = Kit.TierColours[targetMedal];
            var at = new Vector2(9, y);
            DrawCircle(at, 5, new Color(Kit.Fill, 1));
            DrawArc(at, 5, 0, Mathf.Tau, 16, c, 1.2f, true);
            DrawArc(at, 2.5f, 0, Mathf.Tau, 12, c, 1.2f, true);
            DrawCircle(at, 0.9f, c);
        }
        healthT.Position = new Vector2(-64, y + 6);
        targetT.Position = new Vector2(4, y + 6);

        // Hovered: three stars under each label, as many filled as the medal's tier.
        if (hover && Unlocked)
        {
            Stars(new Vector2(-44, y + 24), healthMedal);
            Stars(new Vector2(10, y + 24), targetMedal);
        }
    }

    /// <summary>Three stars from <paramref name="left"/>: the first <paramref name="tier"/> filled in that tier's metal.</summary>
    void Stars(Vector2 left, int tier)
    {
        for (int i = 0; i < 3; i++)
        {
            var at = left + new Vector2(i * 10, 0);
            var pts = new Vector2[10];
            for (int k = 0; k < 10; k++)
                pts[k] = at + Vector2.FromAngle(-Mathf.Pi / 2 + k * Mathf.Pi / 5) * (k % 2 == 0 ? 4.5f : 2);
            if (i < tier) DrawColoredPolygon(pts, Kit.TierColours[tier]);
            else DrawPolyline(pts.Append(pts[0]).ToArray(), new Color(Kit.Ink, 0.35f), 1, true);
        }
    }

    void Over(bool b)
    {
        hover = b;
        healthT.Visible = targetT.Visible = b && Unlocked;
    }

    public override bool _HasPoint(Vector2 point) => point.Length() <= Radius + 6;

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        AcceptEvent();
        click(this);
    }
}

// The difficulty picker beside a level: the level's best score, then a button per difficulty, with the one the
// typing test suggests marked.
public partial class DiffSelect : Control
{
    const float RowTop = 48, RowGap = 25;
    readonly Label heading, highscore;
    int level, recommended = -1;

    public DiffSelect()
    {
        Size = new Vector2(112, RowTop + RowGap * 5 + 6);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        heading = Kit.Label(this, "", Kit.Caption(7), 12, 7);
        Kit.Label(this, "BEST", Kit.Caption(7), 12, 22);
        highscore = Kit.Label(this, "", Kit.Body(12), 30, 17, 72, HorizontalAlignment.Right);
        for (int i = 0; i < Strings.Diffs.Length; i++)
        {
            double cps = Difficulty.Cps[i];
            AddChild(new TextButton(Strings.Diffs[i], () => StartGame(cps), 9, boxed: true, colour: Kit.DiffColours[i], width: 86)
            {
                Position = new Vector2(12, RowTop + RowGap * i),
            });
        }
    }

    public void Open(int level, Vector2 at)
    {
        this.level = level;
        var save = G.Model.Save;
        heading.Text = level < 0 ? "ENDLESS" : Banner.LevelName(level);
        highscore.Text = (level < 0 ? save.EndlessBest.Max() : save.LevelScore[level][0]).ToString();
        // Beside the level, kept on the stage.
        Position = new Vector2(Math.Min(at.X + 30, G.StageSize - Size.X - 4), Math.Clamp(at.Y - 30, 4, G.StageSize - Size.Y - 50));
        // Point out the hardest difficulty the typing test says you can keep up with.
        recommended = TypingTestRun.Suggest(G.Model.Save.TestWpm);
        Visible = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Kit.Panel(this, new Rect2(Vector2.Zero, Size), cut: 8);
        if (recommended < 0) return;
        float y = RowTop + RowGap * recommended + 11;
        DrawColoredPolygon(new[] { new Vector2(Size.X - 3, y - 5), new Vector2(Size.X - 9, y), new Vector2(Size.X - 3, y + 5) },
            Kit.DiffColours[recommended]);
    }

    public void StartGame(double cps)
    {
        Visible = false;
        G.Sound.Sound(SoundControl.Click);
        if (level < 0) G.Shell.StartEndless(cps);
        else G.Shell.StartGame(level, cps);
    }
}
