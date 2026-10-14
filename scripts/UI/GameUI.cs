using Godot;

namespace TypingShip;

// In-game HUD over the 550x550 playfield: the game view, cockpit frame, hull bar, typed text, score, progress.
public partial class GameUI : Control
{
    public readonly Node2D GameView = new() { Position = new Vector2(-25, -25) };
    public readonly Node2D BossView = new() { Position = new Vector2(-25, -25) };
    readonly Node2D effectsBack = new();
    readonly TextureRect dash = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Label diffText = new(), scoreText = new(), typeText = new(), centerText = new();
    public readonly PopWarning PopWarning = new();
    int dashC;

    public string Typed => typeText.Text;

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
        centerText.LabelSettings = Fonts.Settings(Fonts.GameBold, 28, Colors.White, 6);

        typeText.Position = new Vector2(4, G.UiHeight + 21);
        diffText.Position = new Vector2(G.UiWidth - 111, G.UiHeight - 17);
        diffText.Size = new Vector2(100, 20);
        diffText.HorizontalAlignment = HorizontalAlignment.Right;
        scoreText.Position = new Vector2(-40, G.UiHeight - 12);
        scoreText.Size = new Vector2(100, 22);
        scoreText.HorizontalAlignment = HorizontalAlignment.Right;
        centerText.Position = new Vector2(0, 220);
        centerText.Size = new Vector2(G.StageSize, 100);
        centerText.HorizontalAlignment = HorizontalAlignment.Center;

        var scoreCaption = new Label
        {
            Text = "SCORE:",
            Position = new Vector2(17, G.UiHeight - 22),
            LabelSettings = Fonts.Settings(Fonts.GameBold, 9, Colors.White, 3, new Color(green, 0.6f)),
        };

        G.Model.Hull.Position = new Vector2(169, G.UiHeight + 25);
        PopWarning.Position = new Vector2(G.UiWidth / 2, G.UiHeight - 20);

        GameView.AddChild(effectsBack);
        AddChild(BossView);
        AddChild(GameView);
        AddChild(dash);
        AddChild(G.Model.Hull);
        AddChild(diffText);
        AddChild(typeText);
        AddChild(scoreText);
        AddChild(scoreCaption);
        AddChild(PopWarning);
        AddChild(centerText);
        foreach (var l in new[] { diffText, scoreText, typeText, centerText, scoreCaption })
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

    public void OnTick()
    {
        int per = (int)(G.Model.Distance / G.Model.MaxDistance * 100);
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

    public void ShowScore(int i) => scoreText.Text = i.ToString();

    public void ShowMessage(string text) => centerText.Text = text;

    public void ResetLetter() => typeText.Text = "";

    public void AddLetter(string s)
    {
        typeText.Text += s;
        if (typeText.Text.Length > 20)
        {
            new PopScore(typeText.Position + new Vector2(5, -5), -1, false);
            typeText.Text = typeText.Text[1..];
        }
    }

    public void RemoveLetter()
    {
        if (typeText.Text.Length == 0) return;
        G.Model.Perfect = false;
        typeText.Text = typeText.Text[..^1];
        new PopScore(typeText.Position + new Vector2(5, -5), -1, false);
    }

    public void RemoveLetters(int n) => typeText.Text = typeText.Text[..^n];
}

// The hull bar under the cockpit. While damaged it carries a long word: type it to repair one point.
public partial class ShipSystem : GameObject
{
    readonly Sprite2D bar = new() { Texture = Sprites.HealthFull, Centered = false, RegionEnabled = true };

    public ShipSystem()
    {
        WordX = 180;
        WordSize = WordList.HealSize();
        AddChild(new Sprite2D { Texture = Sprites.HealthBase, Centered = false });
        AddChild(bar);
        DrawBar(PlayerShip.MaxHealth);
    }

    public void DrawBar(int health)
    {
        var size = Sprites.HealthFull.GetSize();
        bar.RegionRect = new Rect2(0, 0, size.X * health / PlayerShip.MaxHealth, size.Y);
    }
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
            if (G.Model.PShip.Health < 2) Add();
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
