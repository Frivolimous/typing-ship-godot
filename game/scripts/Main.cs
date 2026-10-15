using Godot;
using TypingShip.Core.Data;

namespace TypingShip;

// Builds the game: the 550x550 playfield centred in the window, with side panels filling the rest.
// Starts on the title menu. Dev options (after "--", see docs/commands.md) can go straight into a level instead.
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

        GameData data;
        try
        {
            data = DataFiles.Load();
        }
        catch (DataError e)
        {
            ShowDataError(e);
            return;
        }
        if (args.ContainsKey("check"))
        {
            GD.Print($"Data OK: words, tuning, {GameData.LevelCount} levels and the boss fights all load ({DataFiles.Folder ?? "embedded"}).");
            GetTree().Quit();
            return;
        }

        G.Rng = args.TryGetValue("seed", out var seed) ? new Rng(ulong.Parse(seed)) : new Rng();
        bool bot = args.ContainsKey("autotype");
        // Bot runs don't touch the player's save unless given a profile of their own.
        if (args.TryGetValue("profile", out var profile)) SaveFile.Path = $"user://{profile}.json";
        else if (bot) SaveFile.Path = null;

        G.Sound = new SoundControl { Name = "Sound" };
        AddChild(G.Sound);
        G.World = new Presenter();
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
        G.Model = new Game(data, G.Rng, new ArtLibrary(Sprites.Mask), SaveFile.Load(), G.World);
        if (args.TryGetValue("wpm", out var wpm)) G.Model.Wpm = int.Parse(wpm);
        G.UI = new GameUI { Name = "GameUI" };
        G.Starfield.Back.Size = new Vector2(G.StageSize, G.StageSize);
        G.Starfield.SetBack(0); // behind the typing test before any level has been played
        playfield.AddChild(G.Starfield.Back);
        playfield.AddChild(G.Starfield.NebulaLayer);
        playfield.AddChild(G.Starfield);
        playfield.AddChild(G.UI);
        G.UI.Init();
        G.UI.SetParticleLayer(G.Starfield.Particles);
        G.World.Init();

        if (args.TryGetValue("trace", out var trace)) G.Model.Trace = new StreamWriter(trace);
        G.Ctrl.LevelOver += cleared =>
        {
            G.Model.Trace?.Flush();
            GD.Print(G.Model.ResultLine(cleared));
        };
        if (args.ContainsKey("quit-on-end")) G.Ctrl.LevelOver += _ => GetTree().Quit();
        if (bot)
        {
            AddChild(new DevAutoTyper { BotWpm = args.TryGetValue("botwpm", out var b) ? int.Parse(b) : 0 });
            G.Model.TutOff(); // tutorial pop-ups would wait for Space
        }

        var shell = new Shell { Name = "Shell" };
        playfield.AddChild(shell);
        shell.Init();
        bool edit = args.ContainsKey("edit");
        if (args.TryGetValue("level", out var level) || (bot && !args.ContainsKey("menu")) || edit)
            shell.StartDirect(level != null ? int.Parse(level) : 0);
        else shell.ShowTitle(args.ContainsKey("map"));
        if (edit || args.ContainsKey("from") || args.ContainsKey("line"))
            AddChild(new LevelEditor(args.GetValueOrDefault("from"), args.GetValueOrDefault("line"), edit) { Name = "LevelEditor" });
        DevKeys.AddIfRequested(this);
        Screenshot.CaptureIfRequested(this);
    }

    /// <summary>A data file has a mistake: say where, on screen and in the console, instead of starting.</summary>
    void ShowDataError(DataError e)
    {
        GD.PrintErr("Data error: " + e.Message);
        if (args.ContainsKey("check") || DisplayServer.GetName() == "headless")
        {
            GD.Print("Data error: " + e.Message); // stdout, so ts 9 shows it
            GetTree().Quit(1);
            return;
        }
        AddChild(new Camera2D { Position = new Vector2(G.StageSize / 2, G.StageSize / 2) });
        AddChild(new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart, // first: setting Text before it would widen the label
            Position = new Vector2(20, 20),
            Size = new Vector2(G.StageSize - 40, G.StageSize - 40),
            Text = "A data file has a mistake, so the game can't start:\n\n" + e.Message +
                   "\n\nFix the file and start the game again.",
        });
    }

    public override void _Notification(int what)
    {
        // The soldier kill count isn't saved on every kill.
        if (what == NotificationWMCloseRequest && G.Model != null) G.Model.Save.Save();
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
