using Godot;

namespace TypingShip;

// The options: display, the music and sound volumes, the effects (glow, shake, flashes) and the tutorials. Each row
// works with the mouse (click a switch, click or drag a slider), the arrow keys (up and down choose, left and right
// change, Space or Enter flips a switch), or by typing its name. Changes apply and save at once.
public partial class OptionsUI : Screen
{
    const float RowTop = 64, RowGap = 48;
    readonly List<OptionRow> rows = new();
    int selected;

    public OptionsUI()
    {
        Kit.Label(this, "OPTIONS", Kit.Title(18), 18, 14);
        Add(new OptionRow("DISPLAY", "display", () => Settings.Fullscreen ? "FULLSCREEN" : "WINDOWED",
            () => Settings.Fullscreen = !Settings.Fullscreen));
        Add(new OptionRow("MUSIC VOLUME", "music", () => Settings.MusicVolume, v => Settings.MusicVolume = v));
        Add(new OptionRow("SOUND VOLUME", "sound", () => Settings.SoundVolume, v => Settings.SoundVolume = v));
        Add(Switch("GLOW", "glow", () => Settings.Glow, () => Settings.Glow = !Settings.Glow));
        Add(Switch("SCREEN SHAKE", "shake", () => Settings.Shake, () => Settings.Shake = !Settings.Shake));
        Add(Switch("SCREEN FLASHES", "flashes", () => Settings.Flashes, () => Settings.Flashes = !Settings.Flashes));
        Add(Switch("TUTORIALS", "tutorials", () => Settings.Tutorials, () =>
        {
            Settings.Tutorials = !Settings.Tutorials;
            // Turned back on: the ones already seen this session can show again.
            if (Settings.Tutorials) Array.Fill(G.Model.TutFlags, false);
        }));
        Kit.Label(this, "UP AND DOWN CHOOSE  ·  LEFT AND RIGHT CHANGE  ·  OR TYPE A SETTING'S NAME", Kit.Caption(7),
            0, RowTop + RowGap * rows.Count + 6, G.StageSize, HorizontalAlignment.Center);
        var done = new TextButton(Strings.Done, Back, 13, boxed: true, width: 126) { Position = new Vector2(412, 506) };
        AddChild(done);
        Nav.Add(done);
    }

    static OptionRow Switch(string name, string word, Func<bool> get, Action flip) =>
        new(name, word, () => get() ? "ON" : "OFF", flip);

    void Add(OptionRow row)
    {
        int index = rows.Count;
        row.Position = new Vector2(40, RowTop + RowGap * index);
        row.Selected += () => Select(index);
        row.Changed += Changed;
        rows.Add(row);
        AddChild(row);
        // Typing a setting's name picks it; a switch also flips.
        Nav.Add(row.Word, () =>
        {
            Select(index);
            if (!row.IsSlider) row.Flip();
        });
    }

    void Select(int i)
    {
        selected = Math.Clamp(i, 0, rows.Count - 1);
        for (int k = 0; k < rows.Count; k++) rows[k].Highlight = k == selected;
    }

    static void Changed()
    {
        Settings.Apply();
        Settings.Save();
    }

    static void Back() => G.Shell.Go(G.Shell.Menu, Shell.Dark);

    public override void NavIn()
    {
        Select(0);
        foreach (var r in rows) r.Refresh();
    }

    public override bool OnKey(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Key.Escape: Back(); return true;
            case Key.Up: Select(selected - 1); return true;
            case Key.Down: Select(selected + 1); return true;
            case Key.Left: rows[selected].Step(-1); return true;
            case Key.Right: rows[selected].Step(1); return true;
            case Key.Space or Key.Enter or Key.KpEnter:
                if (!rows[selected].IsSlider) rows[selected].Flip();
                return true;
        }
        return false;
    }

    public override void _Draw() => Kit.Rule(this, new Vector2(18, 42), new Vector2(200, 42));
}

/// <summary>One option: a name, and either a switch (its value as text, flipped by a click) or a 0-100% slider.</summary>
public partial class OptionRow : Control
{
    const float W = 470, H = 38, BarLeft = 250, BarWidth = 150;
    public readonly string Word;
    public readonly bool IsSlider;
    readonly Func<string>? text;
    readonly Action? flip;
    readonly Func<float>? value;
    readonly Action<float>? set;
    readonly Label valueLabel;
    bool highlight, hover, dragging;

    public event Action? Selected, Changed;

    OptionRow(string name, string word)
    {
        Word = word;
        Size = new Vector2(W, H);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        Kit.Label(this, name, Kit.Body(12), 18, 12);
        valueLabel = Kit.Label(this, "", Kit.Body(12, Colors.White), BarLeft, 12, W - BarLeft - 18, HorizontalAlignment.Right);
        MouseEntered += () => { hover = true; QueueRedraw(); };
        MouseExited += () => { hover = false; QueueRedraw(); };
    }

    /// <summary>A switch: <paramref name="text"/> shows its value, <paramref name="flip"/> changes it.</summary>
    public OptionRow(string name, string word, Func<string> text, Action flip) : this(name, word)
    {
        this.text = text;
        this.flip = flip;
        Refresh();
    }

    /// <summary>A 0-1 slider.</summary>
    public OptionRow(string name, string word, Func<float> value, Action<float> set) : this(name, word)
    {
        IsSlider = true;
        this.value = value;
        this.set = set;
        Refresh();
    }

    public OptionRow() : this("", "") { }

    public bool Highlight
    {
        set
        {
            highlight = value;
            QueueRedraw();
        }
    }

    public void Refresh()
    {
        valueLabel.Text = IsSlider ? $"{Mathf.RoundToInt(value!() * 100)}%" : text!();
        // A slider's percentage sits to the right of its bar.
        valueLabel.Position = new Vector2(IsSlider ? BarLeft + BarWidth + 14 : BarLeft, 12);
        valueLabel.Size = new Vector2(IsSlider ? W - BarLeft - BarWidth - 26 : W - BarLeft - 18, 0);
        QueueRedraw();
    }

    public void Flip()
    {
        flip?.Invoke();
        G.Sound.Sound(SoundControl.Click);
        Refresh();
        Changed?.Invoke();
    }

    /// <summary>Left or right: a slider moves 10%, a switch flips.</summary>
    public void Step(int dir)
    {
        if (IsSlider) SetValue(value!() + 0.1f * dir);
        else Flip();
    }

    void SetValue(float v)
    {
        v = Mathf.Clamp(Mathf.Round(v * 20) / 20, 0, 1); // to the nearest 5%
        if (Mathf.IsEqualApprox(v, value!())) return;
        set!(v);
        G.Sound.Sound(SoundControl.Type); // so you can hear the effects' level
        Refresh();
        Changed?.Invoke();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } mb)
        {
            AcceptEvent();
            if (mb.Pressed)
            {
                Selected?.Invoke();
                if (IsSlider)
                {
                    dragging = true;
                    SetValue((mb.Position.X - BarLeft) / BarWidth);
                }
                else Flip();
            }
            else dragging = false;
        }
        else if (e is InputEventMouseMotion mm && dragging)
        {
            SetValue((mm.Position.X - BarLeft) / BarWidth);
        }
    }

    public override void _Draw()
    {
        Kit.Panel(this, new Rect2(Vector2.Zero, Size), highlight ? Kit.Rim : null, cut: 8, glow: highlight || hover ? 0.95f : 0.4f);
        if (!IsSlider)
        {
            // A switch's value in a lit chip: green when on, dim when off.
            bool on = valueLabel.Text is "ON" or "FULLSCREEN";
            valueLabel.LabelSettings = Kit.Body(12, on ? Kit.Good : new Color(Kit.Ink, 0.55f));
            return;
        }
        float v = value!();
        var bar = new Rect2(BarLeft, H / 2 - 3, BarWidth, 6);
        DrawRect(bar, new Color(Kit.Rim, 0.15f));
        DrawRect(new Rect2(bar.Position, new Vector2(BarWidth * v, bar.Size.Y)), new Color(Kit.Rim, 0.9f));
        var knob = new Vector2(BarLeft + BarWidth * v, H / 2);
        DrawCircle(knob, 7, new Color(Kit.Rim, 0.25f));
        DrawCircle(knob, 4.5f, Colors.White);
    }
}
