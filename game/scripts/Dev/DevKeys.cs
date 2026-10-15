using Godot;

namespace TypingShip;

// Dev aid: "--keys=60:start,120:1,140:normal,900:{space}" plays scripted input, so windowed screenshot runs can
// walk through the menus. Each item is tick:text, counting ticks (1/24 s of game time) from start-up. Letters and
// digits are typed; {space}, {enter}, {esc}, {backspace}, {f1}-{f12} press those keys; {click:XxY} clicks at a stage
// position (x, y).
public partial class DevKeys : Node
{
    readonly List<(int Frame, string Text)> script = new();
    int frame;

    public static void AddIfRequested(Node owner)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (!arg.StartsWith("--keys=")) continue;
            var keys = new DevKeys { Name = "DevKeys" };
            foreach (var item in arg["--keys=".Length..].Split(','))
            {
                var parts = item.Split(':', 2);
                keys.script.Add((int.Parse(parts[0]), parts[1]));
            }
            owner.AddChild(keys);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        frame++;
        foreach (var (f, text) in script)
            if (f == frame) Play(text);
    }

    void Play(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                int end = text.IndexOf('}', i);
                Special(text[(i + 1)..end]);
                i = end;
            }
            else Key(char.IsDigit(text[i]) ? Godot.Key.Key0 + (text[i] - '0') : Godot.Key.A + (char.ToUpperInvariant(text[i]) - 'A'), text[i]);
        }
    }

    void Special(string s)
    {
        switch (s)
        {
            case "space": Key(Godot.Key.Space, ' '); break;
            case "enter": Key(Godot.Key.Enter, '\0'); break;
            case "esc": Key(Godot.Key.Escape, '\0'); break;
            case "backspace": Key(Godot.Key.Backspace, '\0'); break;
            case ['f', .. var n] when int.TryParse(n, out int f) && f is >= 1 and <= 12: Key(Godot.Key.F1 + (f - 1), '\0'); break;
            default:
                if (!s.StartsWith("click:")) throw new ArgumentException($"--keys: unknown {{{s}}}");
                var xy = s["click:".Length..].Split('x').Select(float.Parse).ToArray();
                Click(new Vector2(xy[0], xy[1]));
                break;
        }
    }

    static void Key(Key key, char c)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = c, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = c, Pressed = false });
    }

    // Through Input like a real click: stage -> viewport (camera) -> window (stretch).
    void Click(Vector2 stage)
    {
        var vp = GetViewport();
        var at = vp.GetFinalTransform() * (vp.GetCanvasTransform() * stage);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = MouseButton.Left, Pressed = false });
    }
}
