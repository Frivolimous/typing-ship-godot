using Godot;

namespace TypingShip;

// "New Achievement!" box at the top of the stage: fades in over 1 s, stays 2 s, fades out over 1 s.
public partial class PopAchieve : Control
{
    const double Step = 0.05; // the original's 50 ms timer, 80 steps
    double time;

    public PopAchieve(int achieve, int tier)
    {
        // Top centre, except on the score screen, where that would cover the title: under "FINAL SCORE:" instead.
        Position = G.Shell.ShowingScore ? new Vector2(30, 365) : new Vector2(175, 20);
        Size = new Vector2(250, 100);
        MouseFilter = MouseFilterEnum.Ignore;
        Modulate = new Color(1, 1, 1, 0);
        // The trophy was drawn at (-10, -10) into a bitmap at (5, 5), so its top-left 10 px are cut off.
        var clip = new Control { Position = new Vector2(5, 5), Size = new Vector2(245, 95), ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        clip.AddChild(new TextureRect { Texture = Sprites.Achieve[tier], Position = new Vector2(-10, -10), MouseFilter = MouseFilterEnum.Ignore });
        AddChild(clip);
        AddChild(new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart, // first: setting Text before it would widen the label
            Text = $"{Strings.NewAchieve}\n\n{Strings.Tiers[tier]} {Strings.AchieveNames[achieve]}",
            Position = new Vector2(105, 5),
            Size = new Vector2(150, 90),
            LabelSettings = Fonts.Settings(Fonts.Space, 15, Colors.White, 3),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        G.Shell.AddPopup(this);
    }

    public override void _Process(double delta)
    {
        time += delta;
        int count = (int)(time / Step);
        if (count >= 80)
        {
            QueueFree();
            return;
        }
        float a = count <= 20 ? count / 20f : count >= 60 ? (80 - count) / 20f : 1;
        Modulate = new Color(1, 1, 1, a);
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 250, 100), new Color(0, 0, 0, 0.5f));
        DrawRect(new Rect2(0, 0, 250, 100), Palette.BorderBlue, false, 2);
    }
}

// A tutorial pop-up in the game view. It pauses the game (without the pause screen); Space continues, Escape
// continues and turns the tutorial off. An optional pointer runs from the box to the thing it's about.
public partial class PopBox : Node2D
{
    static readonly Rect2 Box = new(175, 150, 200, 150);
    readonly Vector2[] pointer = Array.Empty<Vector2>();

    /// <summary>Shown when the rules pause for a tutorial (G.Model.ShowTutorial).</summary>
    public PopBox(int tut, Vector2? o = null, PointerDir dir = PointerDir.Auto)
    {
        G.UI.TutBox?.Remove();
        G.UI.TutBox = this;
        AddChild(new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart, // first: setting Text before it would widen the label
            Text = Strings.Tutorial[tut],
            Position = Box.Position,
            Size = new Vector2(200, 0),
            LabelSettings = Fonts.Settings(Fonts.GameBold, 13, Colors.White),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        AddChild(new Label
        {
            Text = Strings.TutorialKeys,
            Position = new Vector2(175, 270),
            LabelSettings = Fonts.Settings(Fonts.GameBold, 10, Colors.White),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        G.UI.GameView.AddChild(this);
        ResetPhysicsInterpolation();

        if (o is not { } p) return;
        if (dir == PointerDir.Auto)
            dir = p.Y < 150 ? PointerDir.Up : p.Y > 300 ? PointerDir.Down : p.X < 175 ? PointerDir.Left : p.X > 375 ? PointerDir.Right : PointerDir.Auto;
        pointer = dir switch
        {
            PointerDir.Up => new[] { new Vector2(275, 150), new Vector2(275, p.Y), p },
            PointerDir.Down => new[] { new Vector2(275, 300), new Vector2(275, p.Y), p },
            PointerDir.Left => new[] { new Vector2(175, 225), new Vector2(p.X, 225), p },
            PointerDir.Right => new[] { new Vector2(375, 225), new Vector2(p.X, 225), p },
            _ => pointer,
        };
    }

    public override void _Draw()
    {
        DrawRect(Box, new Color(0, 0, 0, 0.2f));
        DrawRect(Box, Colors.White, false, 3);
        if (pointer.Length > 0) DrawPolyline(pointer, Colors.White, 3);
    }

    public void Remove()
    {
        if (G.UI.TutBox != this) return;
        G.UI.TutBox = null;
        GetParent()?.RemoveChild(this);
        QueueFree();
        if (G.Shell.InGame) G.Model.CloseTutorial();
    }
}
