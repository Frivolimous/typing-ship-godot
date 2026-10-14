using Godot;

namespace TypingShip;

// Builds the game: the 550x550 playfield centred in the window, with side panels filling the rest.
// Command-line (after "--"): --level=N, --wpm=N, --seed=N, --autotype, --screenshot=path, --frames=N.
public partial class Main : Node2D
{
    readonly Dictionary<string, string> args = new();

    public override void _Ready()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var parts = a.TrimStart('-').Split('=', 2);
            args[parts[0]] = parts.Length > 1 ? parts[1] : "";
        }
        if (args.TryGetValue("seed", out var seed)) G.Seed(ulong.Parse(seed));

        G.Words = new WordList();
        G.Model = new GameModel();
        G.Action = new ActionControl();
        if (args.TryGetValue("wpm", out var wpm)) G.Model.Wpm = int.Parse(wpm);

        G.Sound = new SoundControl { Name = "Sound" };
        AddChild(G.Sound);
        G.Ctrl = new GameControl { Name = "GameControl" };
        AddChild(G.Ctrl);
        AddChild(new KeyboardControl { Name = "Keyboard" });

        AddChild(new Camera2D
        {
            Position = new Vector2(G.StageSize / 2, G.StageSize / 2),
            ProcessCallback = Camera2D.Camera2DProcessCallback.Physics,
        });
        AddChild(new SidePanels());

        var playfield = new Control
        {
            Name = "Playfield",
            Size = new Vector2(G.StageSize, G.StageSize),
            ClipContents = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(playfield);

        G.Starfield = new Starfield { Name = "Stars" };
        G.Model.Hull = new ShipSystem { Name = "Hull" };
        G.UI = new GameUI { Name = "GameUI" };
        G.Starfield.Back.Size = new Vector2(G.StageSize, G.StageSize);
        playfield.AddChild(G.Starfield.Back);
        playfield.AddChild(G.Starfield.NebulaLayer);
        playfield.AddChild(G.Starfield);
        playfield.AddChild(G.UI);
        G.UI.Init();
        G.UI.SetParticleLayer(G.Starfield.Particles);

        G.Model.PShip = new PlayerShip { Name = "Player" };
        G.Model.PShip.Setup();

        G.Ctrl.LevelOver += cleared => GD.Print(
            $"Level {G.Model.Level} {(cleared ? "cleared" : "lost")}: score {G.Model.Score}, ships {G.Model.TotalShips}, " +
            $"missed {G.Model.MissedShips}, health {G.Model.PShip.Health}, ticks {Engine.GetPhysicsFrames()}");
        G.Ctrl.LevelOver += cleared =>
            G.UI.ShowMessage(cleared ? $"LEVEL COMPLETE\nScore {G.Model.Score}  ·  Space to replay"
                                     : "SHIP DESTROYED\nSpace to retry");

        if (args.ContainsKey("quit-on-end")) G.Ctrl.LevelOver += _ => GetTree().Quit();
        if (args.ContainsKey("autotype"))
            AddChild(new DevAutoTyper { BotWpm = args.TryGetValue("botwpm", out var b) ? int.Parse(b) : 0 });
        G.Ctrl.NewGame(args.TryGetValue("level", out var level) ? int.Parse(level) : 0);
        Screenshot.CaptureIfRequested(this);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space } && G.Ctrl.LevelEnded)
        {
            G.Starfield.ClearParticles();
            G.Ctrl.NewGame(Math.Max(G.Model.Level, 0));
            GetViewport().SetInputAsHandled();
        }
    }
}

// Placeholder decoration for the space either side of the square playfield.
public partial class SidePanels : Node2D
{
    public override void _Draw()
    {
        const float s = G.StageSize, w = 2000;
        var panel = new Color(0x05050fff);
        DrawRect(new Rect2(-w, -w, w, s + 2 * w), panel);
        DrawRect(new Rect2(s, -w, w, s + 2 * w), panel);
        var edge = new Color(0xccccff55);
        DrawLine(new Vector2(-1, 0), new Vector2(-1, s), edge, 2);
        DrawLine(new Vector2(s + 1, 0), new Vector2(s + 1, s), edge, 2);
    }
}
