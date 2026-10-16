using Godot;

namespace TypingShip;

// In-game HUD over the 550x550 playfield: the game view, cockpit frame, hull bar, typed text, score, progress.
public partial class GameUI : Control
{
    public readonly Node2D GameView = new() { Position = new Vector2(-25, -25) };
    public readonly Node2D BossView = new() { Position = new Vector2(-25, -25) };
    readonly Node2D effectsBack = new();
    readonly TextureRect dash = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Label diffText = new(), scoreText = new(), typeText = new();
    public readonly PopWarning PopWarning = new();
    public PopBox? TutBox;
    int dashC;

    /// <summary>Where the typing bar is (for its "-1" pop-ups).</summary>
    public Vector2 TypedPosition => typeText.Position;

    /// <summary>The hull bar and its repair word.</summary>
    public readonly HullBar Hull = new() { Name = "Hull" };

    public GameUI()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Size = new Vector2(G.StageSize, G.StageSize);
    }

    public void Init()
    {
        dash.Texture = Sprites.Dash[0];
        float dashScale = G.StageSize / Sprites.Dash[0].GetWidth(); // the original stretched it to the stage width
        dash.Scale = new Vector2(dashScale, dashScale);

        var green = new Color(0x55ff55ff);
        diffText.LabelSettings = Fonts.Settings(Fonts.GameBold, 15, Colors.White, 3, new Color(green, 0.6f));
        scoreText.LabelSettings = Fonts.Settings(Fonts.GameBold, 18, Colors.White, 3, new Color(green, 0.6f));
        typeText.LabelSettings = Fonts.Settings(Fonts.GameBold, 15, Colors.White, 3, new Color(0xff5555aa));

        typeText.Position = new Vector2(4, G.UiHeight + 21);
        diffText.Position = new Vector2(G.UiWidth - 111, G.UiHeight - 17);
        diffText.Size = new Vector2(100, 20);
        diffText.HorizontalAlignment = HorizontalAlignment.Right;
        scoreText.Position = new Vector2(-40, G.UiHeight - 12);
        scoreText.Size = new Vector2(100, 22);
        scoreText.HorizontalAlignment = HorizontalAlignment.Right;

        var scoreCaption = new Label
        {
            Text = "SCORE:",
            Position = new Vector2(17, G.UiHeight - 22),
            LabelSettings = Fonts.Settings(Fonts.GameBold, 9, Colors.White, 3, new Color(green, 0.6f)),
        };

        Hull.Position = new Vector2(169, G.UiHeight + 25);
        PopWarning.Position = new Vector2(G.UiWidth / 2, G.UiHeight - 20);

        GameView.AddChild(effectsBack);
        AddChild(BossView);
        AddChild(GameView);
        AddChild(dash);
        AddChild(Hull);
        AddChild(diffText);
        AddChild(typeText);
        AddChild(scoreText);
        AddChild(scoreCaption);
        AddChild(PopWarning);
        foreach (var l in new[] { diffText, scoreText, typeText, scoreCaption })
            l.MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Particle layer sits at the very back of the game view.</summary>
    public void SetParticleLayer(Node2D layer)
    {
        GameView.AddChild(layer);
        GameView.MoveChild(layer, 0);
    }

    /// <summary>Beams and player missiles go behind the ships (the original's addChildAt(…, 0)).</summary>
    public void AddToGameViewBack(Node2D node)
    {
        if (node.GetParent() != GameView) GameView.AddChild(node);
        GameView.MoveChild(node, effectsBack.GetIndex() + 1);
    }

    /// <summary>The typing bar, score and hull follow the rules every frame.</summary>
    public override void _Process(double delta)
    {
        if (typeText.Text != G.Model.Typed) typeText.Text = G.Model.Typed;
        var score = G.Model.Score.ToString();
        if (scoreText.Text != score) scoreText.Text = score;
        Hull.Sync();
    }

    public void OnTick()
    {
        int per = G.Model.MaxTime > 0 ? (int)(G.Model.Time / G.Model.MaxTime * 100) : 0;
        diffText.Text = per > 100 ? "100%" : $"{per}%";

        if (dashC == 0)
        {
            if (G.Rand() < 0.01f) dashC = (int)Mathf.Ceil(G.Rand() * 5) * 5;
        }
        else
        {
            dashC--;
            if (dashC == 0) dash.Texture = Sprites.Dash[0];
            else if (dashC % 3 == 0) dash.Texture = Sprites.Dash[(int)Mathf.Ceil(G.Rand() * 5)];
        }
    }
}

// The hull bar under the cockpit. While damaged it carries a long word: type it to repair one point.
public partial class HullBar : Node2D
{
    readonly Sprite2D bar = new() { Texture = Sprites.HealthFull, Centered = false, RegionEnabled = true };
    readonly WordLabel word = new() { Position = new Vector2(180, 0) };
    int health = -1;

    public HullBar()
    {
        AddChild(new Sprite2D { Texture = Sprites.HealthBase, Centered = false });
        AddChild(bar);
        AddChild(word);
    }

    public void Sync()
    {
        var player = G.Model.Player;
        if (player.Health != health)
        {
            health = player.Health;
            var size = Sprites.HealthFull.GetSize();
            bar.RegionRect = new Rect2(0, 0, size.X * health / player.MaxHealth, size.Y);
        }
        word.Set(G.Model.Hull.Word, G.Model.Hull.Priority);
    }

    public void ShowWord(bool b) => word.ShowWord(b);
}

// Flashing "Low Health!" while the hull is down to its last point.
public partial class PopWarning : Node2D
{
    readonly Label text = new()
    {
        Text = "Low Health!",
        HorizontalAlignment = HorizontalAlignment.Center,
        Position = new Vector2(-200, 0),
        Size = new Vector2(400, 20),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
    readonly LabelSettings red = Fonts.Settings(Fonts.Game, 14, new Color(0xff0000ff), 2);
    readonly LabelSettings white = Fonts.Settings(Fonts.Game, 14, Colors.White, 2);
    int textSwitch, textChange;
    bool isRed;

    public PopWarning()
    {
        text.LabelSettings = red;
        AddChild(text);
        Visible = false;
    }

    void Add()
    {
        Visible = true;
        textChange = 40;
    }

    public void Update()
    {
        if (textChange == 0)
        {
            if (G.Model.Player.Health < 2) Add();
            else Visible = false;
            return;
        }
        textChange--;
        if (++textSwitch > 2)
        {
            isRed = !isRed;
            text.LabelSettings = isRed ? red : white;
            textSwitch = 0;
        }
    }
}
