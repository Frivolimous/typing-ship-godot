using Godot;
using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip;

// Level-editing aids. --from=N starts the level N% of the way in (or --from=1:37 at that time), --line=N at line N
// of its file. With --edit:
//   F5  reload the data files and restart the level where you are (a mistake in a file is shown, and the old
//       level keeps running)
//   F6  restart from the start point (--from/--line, or the beginning)
//   F7  jump back 10 s       F8  jump ahead 10 s
//   F9  show or hide the 12 x 12 grid, the ships' flight paths, and each ship's line and time (--overlay starts with it on)
//   F10 invincible on or off
public partial class LevelEditor : Node
{
    readonly string? from, line;
    readonly bool keys;
    readonly Overlay overlay = new() { Name = "EditOverlay", Visible = false };
    readonly Label banner = new()
    {
        Position = new Vector2(4, 22),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        LabelSettings = Fonts.Settings(Fonts.GameBold, 11, new Color(0xffff99ff), 3, Colors.Black),
    };
    readonly Label error = new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart, // first: setting Text before it would widen the label
        Position = new Vector2(20, 60),
        Size = new Vector2(G.StageSize - 40, 0),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        LabelSettings = Fonts.Settings(Fonts.GameBold, 13, new Color(0xff8877ff), 4, Colors.Black),
    };
    int errorTicks;

    public LevelEditor(string? from, string? line, bool keys)
    {
        this.from = from;
        this.line = line;
        this.keys = keys;
    }

    public LevelEditor() : this(null, null, false) { }

    public override void _Ready()
    {
        G.UI.GameView.AddChild(overlay);
        if (keys)
        {
            G.UI.AddChild(banner);
            G.UI.AddChild(error);
            overlay.Visible = OS.GetCmdlineUserArgs().Contains("--overlay");
        }
        GoToStart();
    }

    /// <summary>Where --from or --line say to start, as a time in the current level (0 = the beginning).</summary>
    double StartTime()
    {
        var m = G.Model;
        if (line != null && int.TryParse(line, out int n))
        {
            var events = m.LevelData.Where(e => e.Line >= n).ToList();
            if (events.Count > 0) return events.Min(e => e.Time);
            Warn($"--line={n}: no ship comes from line {n} or later in {m.Set.LevelFile(m.Level)}.");
            return 0;
        }
        if (from != null && from.Contains(':') && from.Split(':') is [var mins, var secs] &&
            int.TryParse(mins, out int mm) && double.TryParse(secs, System.Globalization.CultureInfo.InvariantCulture, out double ss))
            return mm * 60 + ss;
        if (from != null && double.TryParse(from.TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture, out double pct))
            return Math.Clamp(pct, 0, 100) / 100 * m.MaxTime;
        return 0;
    }

    void GoToStart()
    {
        double t = StartTime();
        if (t > 0) G.Model.SkipTo(t);
    }

    /// <summary>Starts the level again (same difficulty), then skips to a time (seconds).</summary>
    static void Restart(double time)
    {
        var m = G.Model;
        if (!m.Running) m.Resume(); // closes the pause screen or a tutorial
        bool invincible = m.Invincible;
        G.Shell.StartDirect(m.Level);
        m.Invincible = invincible;
        if (time > 0) m.SkipTo(Math.Min(time, m.MaxTime));
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!keys || e is not InputEventKey { Pressed: true, Echo: false } key || !G.Shell.InGame) return;
        var m = G.Model;
        if (m.LevelEnded && key.Keycode is >= Key.F5 and <= Key.F8) return;
        switch (key.Keycode)
        {
            case Key.F5:
                try
                {
                    var data = DataFiles.Load();
                    double at = m.Time;
                    m.Reload(data);
                    Restart(0);
                    m.SkipTo(Math.Min(at, m.MaxTime));
                    Warn("Reloaded " + (DataFiles.Folder ?? "the built-in data") + ".", 48);
                }
                catch (DataError err)
                {
                    Warn("Not reloaded: " + err.Message, 24 * 15);
                }
                break;
            case Key.F6: Restart(0); GoToStart(); break;
            case Key.F7: Restart(m.Time - 10); break;
            case Key.F8: Restart(m.Time + 10); break;
            case Key.F9: overlay.Visible = !overlay.Visible; break;
            case Key.F10: m.Invincible = !m.Invincible; break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    void Warn(string text, int ticks = 24 * 8)
    {
        GD.Print(text);
        error.Text = text;
        errorTicks = ticks;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (errorTicks > 0 && --errorTicks == 0) error.Text = "";
        overlay.QueueRedraw();
        if (!keys) return;
        var m = G.Model;
        var next = m.LevelData.FirstOrDefault(e => e.Line > 0);
        banner.Text = $"EDIT  {m.Set.LevelFile(m.Level)}  {Clock(m.Time)} of {Clock(m.MaxTime)}  " +
            (next != null ? $"next: line {next.Line} at {Clock(next.Time)}" : m.BossMode ? "boss escorts" : "no more ships") +
            (m.Invincible ? "  INVINCIBLE" : "") +
            "\nF5 reload  F6 restart  F7/F8 back/ahead 10 s  F9 grid  F10 invincible";
    }

    /// <summary>m:ss.s, the way level files write times.</summary>
    static string Clock(double seconds)
    {
        int mm = (int)(seconds / 60);
        return $"{mm}:{(seconds - mm * 60).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture)}";
    }

    /// <summary>The 12 x 12 grid the level files use, and every ship's flight plan, drawn over the game view.</summary>
    partial class Overlay : Node2D
    {
        static readonly Color Grid = new(1, 1, 1, 0.12f), Axis = new(1, 1, 1, 0.35f), Path = new(0.4f, 1, 0.6f, 0.8f),
            Fire = new(1, 0.4f, 0.3f, 0.9f), Halt = new(1, 0.9f, 0.3f, 0.9f), Text = new(0.8f, 1, 0.85f);

        public override void _Draw()
        {
            var font = Fonts.GameBold;
            for (int i = 0; i <= 12; i++)
            {
                float x = i * Playfield.GridX, y = i * Playfield.GridY;
                var c = i % 6 == 0 ? Axis : Grid;
                DrawLine(new Vector2(x, 0), new Vector2(x, Playfield.ViewHeight), c, 1);
                DrawLine(new Vector2(0, y), new Vector2(Playfield.ViewWidth, y), c, 1);
                // Labels just inside the visible stage (the view is 25 px bigger than it on each side).
                DrawString(font, new Vector2(Math.Clamp(x, 28, 565) - 4, 40), i.ToString(), fontSize: 10, modulate: Axis);
                DrawString(font, new Vector2(28, Math.Clamp(y, 40, 520) + 4), i.ToString(), fontSize: 10, modulate: Axis);
            }

            foreach (var o in G.Model.Objects)
            {
                if (o is not EnemyShip ship) continue;
                var at = G.V(ship.Position);
                foreach (var cmd in ship.Commands)
                {
                    if (cmd.Move)
                    {
                        var to = new Vector2(cmd.X, cmd.Y);
                        DrawLine(at, to, Path, 1.5f, true);
                        DrawCircle(to, 3, cmd.Fire ? Fire : Path);
                        at = to;
                    }
                    else
                    {
                        DrawArc(at, 7, 0, Mathf.Tau, 16, cmd.Fire ? Fire : Halt, 1.5f, true);
                        if (cmd.Timer > 0) DrawString(font, at + new Vector2(9, 4), cmd.Timer.ToString(), fontSize: 9, modulate: Halt);
                    }
                }
                if (ship.Line > 0)
                    DrawString(font, G.V(ship.Position) + new Vector2(-24, -18), $"L{ship.Line} {Clock(ship.SpawnTime)}", fontSize: 10, modulate: Text);
            }
        }
    }
}
