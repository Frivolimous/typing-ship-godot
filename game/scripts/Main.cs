using System.Globalization;
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
            // A release export takes no dev options, only the data check and turning Steam off.
            if (G.Dev || parts[0] is "check" or "nosteam") args[parts[0]] = parts.Length > 1 ? parts[1] : "";
        }

        GameData data;
        try
        {
            data = DataFiles.Load();
            Mounts.Load(DataFiles.Source);
        }
        catch (DataError e)
        {
            ShowDataError(e);
            return;
        }
        if (args.ContainsKey("check"))
        {
            GD.Print($"Data OK: words, tuning, the typing test, and {GameData.LevelCount} levels with their boss fights all load ({DataFiles.Folder ?? "embedded"}).");
            GetTree().Quit();
            return;
        }

        // The rules and the presentation have random numbers of their own, so how the game is drawn never changes
        // how it plays: a seed gives the same game here as in the tests and the simulator.
        ulong? seed = args.TryGetValue("seed", out var s) ? ulong.Parse(s) : null;
        var rules = seed is { } rs ? new Rng(rs) : new Rng();
        G.Rng = seed is { } fs ? new Rng(fs + 0x9E3779B97F4A7C15) : new Rng();
        bool bot = args.ContainsKey("autotype");
        SaveFile.MoveFromGodotFolder();
        // Steam: on in an export (unless --nosteam); a checkout only with --steam, so dev runs don't award the
        // achievements or post scores. Never for the bot.
        bool checkout = DataFiles.Folder != null;
        if (!bot && !args.ContainsKey("nosteam") && (!checkout || args.ContainsKey("steam")))
            G.Platform = SteamPlatform.Start(this, checkout);
        // Bot runs don't touch the player's save unless given a profile of their own.
        if (args.TryGetValue("profile", out var profile)) SaveFile.Path = $"user://{profile}.json";
        else if (bot) SaveFile.Path = null;

        G.Sound = new SoundControl { Name = "Sound" };
        AddChild(G.Sound);
        if (bot) Settings.Path = null;
        Settings.Load();
        if (args.TryGetValue("art", out var artSet)) Settings.ArtSet = artSet; // for this run only (not saved)
        Settings.Apply();
        G.World = new Presenter();
        G.Ctrl = new GameControl { Name = "GameControl" };
        AddChild(G.Ctrl);
        AddChild(new KeyboardControl { Name = "Keyboard" });

        G.Camera = new ShakeCamera
        {
            Position = new Vector2(G.StageSize / 2, G.StageSize / 2),
            ProcessCallback = Camera2D.Camera2DProcessCallback.Physics,
        };
        AddChild(G.Camera);
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
        G.Model = new Game(data, rules, new ArtLibrary(Sprites.Mask), SaveFile.Load(), G.World);
        G.Platform.Sync(G.Model.Save);
        if (args.TryGetValue("wpm", out var wpm)) G.Model.Cps = Speed.FromWpm(double.Parse(wpm, CultureInfo.InvariantCulture));
        if (args.TryGetValue("cps", out var cps)) G.Model.Cps = double.Parse(cps, CultureInfo.InvariantCulture);
        G.UI = new GameUI { Name = "GameUI" };
        G.Starfield.Back.Size = new Vector2(G.StageSize, G.StageSize);
        G.Starfield.SetBack(0); // behind the typing test before any level has been played
        playfield.AddChild(G.Starfield.Back);
        playfield.AddChild(G.Starfield.NebulaLayer);
        playfield.AddChild(G.Starfield);
        playfield.AddChild(G.UI);
        if (!args.ContainsKey("noglow")) playfield.AddChild(new Bloom { Name = "Bloom" });
        G.Flash = new ScreenFlash { Name = "Flash" };
        playfield.AddChild(G.Flash);
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
        if (args.ContainsKey("realistic")) DevAutoTyper.Realistic = true; // the bot types like a real player (F12 switches)
        if (bot)
        {
            double botCps = args.TryGetValue("botcps", out var bc) ? double.Parse(bc, CultureInfo.InvariantCulture)
                : args.TryGetValue("botwpm", out var b) ? Speed.FromWpm(double.Parse(b, CultureInfo.InvariantCulture)) : 0;
            AddChild(new DevAutoTyper { BotCps = botCps });
            G.Model.TutOff(); // tutorial pop-ups would wait for Space
        }

        var shell = new Shell { Name = "Shell" };
        playfield.AddChild(shell);
        shell.Init();
        bool edit = args.ContainsKey("edit");
        if (args.ContainsKey("daily"))
            shell.StartDailyDirect(args.TryGetValue("date", out var dd) ? DateOnly.Parse(dd, CultureInfo.InvariantCulture) : null);
        else if (args.ContainsKey("endless"))
            shell.StartEndlessDirect(args.TryGetValue("seed", out var es) ? ulong.Parse(es) : null);
        else if (args.TryGetValue("level", out var level) || (bot && !args.ContainsKey("menu")) || edit)
            shell.StartDirect(level != null ? int.Parse(level) : 0);
        else shell.ShowTitle(args.ContainsKey("map"));
        if (edit || args.ContainsKey("from") || args.ContainsKey("line"))
            AddChild(new LevelEditor(args.GetValueOrDefault("from"), args.GetValueOrDefault("line"), edit) { Name = "LevelEditor" });
        if (!G.Dev) return;
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

    // The player's ship is out of the tree between levels (menus, typing test) and after it's destroyed; free it
    // on the way out so it isn't reported as leaked.
    public override void _ExitTree()
    {
        if (G.World?.Player is { } p && IsInstanceValid(p) && p.GetParent() == null) p.Free();
    }
}

// Placeholder decoration for the space either side of the square playfield.
public partial class SidePanels : Node2D
{
    public override void _Draw()
    {
        const float s = G.StageSize, w = 2000;
        var panel = new Color(0x04070eff);
        DrawRect(new Rect2(-w, -w, w, s + 2 * w), panel);
        DrawRect(new Rect2(s, -w, w, s + 2 * w), panel);
        var edge = new Color(Kit.Highlight, 0.6f);
        DrawLine(new Vector2(-1, 0), new Vector2(-1, s), edge, 2);
        DrawLine(new Vector2(s + 1, 0), new Vector2(s + 1, s), edge, 2);
    }
}
