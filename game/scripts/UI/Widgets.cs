using Godot;

namespace TypingShip;

// "Type to navigate the menu" (the original's TODO): each screen lists words that act like clicking a button.
// Letters build up a buffer; as soon as it ends with an active word, that word fires (longest word first,
// so "b1" wins over "1").
public class TypeNav
{
    readonly List<(string Word, Action Run, Func<bool>? Active)> words = new();
    public string Buffer { get; private set; } = "";

    public void Add(string word, Action run, Func<bool>? active = null)
    {
        words.Add((word.ToLowerInvariant(), run, active));
        words.Sort((a, b) => b.Word.Length.CompareTo(a.Word.Length));
    }

    public void Add(TextButton b) => Add(b.Word, b.Press, () => b.IsVisibleInTree());

    public void Clear() => Buffer = "";

    public void Type(char c)
    {
        Buffer = (Buffer + char.ToLowerInvariant(c)) is var s && s.Length > 20 ? s[^20..] : s;
        G.Sound.Sound(SoundControl.Type);
        foreach (var (word, run, active) in words)
        {
            if (!Buffer.EndsWith(word, StringComparison.Ordinal) || active?.Invoke() == false) continue;
            Buffer = "";
            run();
            return;
        }
    }
}

// One full-stage screen of the shell (title/map, score, game over, ...). The game itself is GameUI.
public partial class Screen : Control
{
    public readonly TypeNav Nav = new();

    public Screen()
    {
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    /// <summary>Called when the screen comes on, at the peak of the transition.</summary>
    public virtual void NavIn() { }

    /// <summary>ENTER_FRAME: every 24 Hz tick while the screen is showing.</summary>
    public virtual void OnTick() { }

    /// <summary>Keys other than navigation letters (Escape, Space, ...). Return true if used.</summary>
    public virtual bool OnKey(InputEventKey key) => false;

    protected static Label Text(Control parent, string text, Font font, int size, Color color, float x, float y,
        float width = 0, int outline = 0, Color? outlineColor = null)
    {
        var l = new Label
        {
            Position = new Vector2(x, y),
            LabelSettings = Fonts.Settings(font, size, color, outline, outlineColor),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        if (width > 0)
        {
            // Wrapping and width before the text: unwrapped text would widen the label for good.
            l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            l.Size = new Vector2(width, 0);
        }
        l.Text = text;
        parent.AddChild(l);
        return l;
    }

    protected static TextureRect Picture(Control parent, Texture2D t, float x = 0, float y = 0, float scale = 1)
    {
        var r = new TextureRect
        {
            Texture = t,
            Position = new Vector2(x, y),
            Scale = new Vector2(scale, scale),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        parent.AddChild(r);
        return r;
    }
}

// UIButton: a text button that lights up under the mouse. Its typed word is its first word, lower-cased, unless given.
public partial class TextButton : Label
{
    readonly Action output;
    readonly LabelSettings normal, over;
    readonly StyleBox? box, lit;
    bool focused;
    public readonly string Word;

    /// <summary>
    /// A button in the cockpit's style: Orbitron, lit under the mouse. Boxed, it sits on a slanted chip (in
    /// <paramref name="colour"/>, brass by default), at least <paramref name="width"/> wide with its text centred. A <paramref name="bold"/> one (the screen's main button) has a thicker rim and text in
    /// the full colour.
    /// </summary>
    public TextButton(string text, Action f, int size, string? word = null, bool boxed = false, Color? colour = null, float width = 0,
        bool bold = false)
    {
        Text = text;
        Word = word ?? text.Split(' ')[0].ToLowerInvariant();
        output = f;
        var ink = !boxed ? Kit.Ink : bold ? (colour ?? Kit.Rim).Lerp(Colors.White, 0.15f) : Colors.White.Lerp(colour ?? Kit.Ink, 0.35f);
        normal = Fonts.Settings(Fonts.HudBold, size, ink, boxed ? 0 : 4, new Color(0, 0, 0, 0.7f));
        over = Fonts.Settings(Fonts.HudBold, size, Colors.White, boxed ? 0 : 6, new Color(colour ?? Cockpit.Rim, 0.7f));
        LabelSettings = normal;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        if (width > 0)
        {
            CustomMinimumSize = new Vector2(width, 0);
            HorizontalAlignment = HorizontalAlignment.Center;
        }
        box = boxed ? Kit.ButtonBox(false, colour, bold) : null;
        lit = boxed ? Kit.ButtonBox(true, colour, bold) : null;
        Light(false);
        MouseEntered += () => Light(true);
        MouseExited += () => Light(focused);
    }

    /// <summary>The keyboard's choice: lit as if under the mouse, so Space or Enter would press it.</summary>
    public bool Focused
    {
        get => focused;
        set
        {
            focused = value;
            Light(value);
        }
    }

    void Light(bool on)
    {
        LabelSettings = on ? over : normal;
        var b = on ? lit : box;
        if (b != null) AddThemeStyleboxOverride("normal", b);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            Press();
        }
    }

    public void Press()
    {
        Light(focused);
        G.Sound.Sound(SoundControl.Click);
        output();
    }
}

// BitButton: a full-stage bitmap with a hit rectangle; brightens x2 under the mouse.
public partial class BitButton : Control
{
    readonly TextureRect art;
    readonly Action output;
    public readonly string Word;

    public BitButton(Texture2D t, Action f, Rect2 hit, string word)
    {
        output = f;
        Word = word;
        MouseFilter = MouseFilterEnum.Ignore;
        art = new TextureRect { Texture = t, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(art);
        var area = new Control
        {
            Position = hit.Position,
            Size = hit.Size,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
        };
        area.MouseEntered += () => art.SelfModulate = new Color(2, 2, 2);
        area.MouseExited += () => art.SelfModulate = Colors.White;
        area.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            area.AcceptEvent();
            Press();
        };
        AddChild(area);
    }

    public void Press()
    {
        art.SelfModulate = Colors.White;
        G.Sound.Sound(SoundControl.Click);
        output();
    }
}

// MenuBox: a message panel over the whole stage; a click (or any key) dismisses it.
public partial class MenuBox : Control
{
    readonly Rect2 box;
    readonly Label display;

    public MenuBox(string text, float x, float y, float width, float height, int size = 20)
    {
        box = new Rect2(x, y, width, height);
        Visible = false; // shown by its owner; it stays in the tree
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        display = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart, // first: setting Text before it would widen the label
            Text = text,
            Position = new Vector2(x + 16, y + 14),
            Size = new Vector2(width - 32, height - 28),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LabelSettings = Kit.Body(Math.Max(10, size - 6)),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(display);
    }

    public string Message
    {
        get => display.Text;
        set => display.Text = value;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(Kit.Shade, 0.5f)); // dims what's behind
        Kit.Panel(this, box);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            Close();
        }
    }

    /// <summary>Raised when the box is dismissed.</summary>
    public event Action? Closed;

    public void Close()
    {
        if (!Visible) return;
        Visible = false;
        Closed?.Invoke();
    }
}

// A yes-or-no question over the whole stage ("EXIT THE GAME?"): YES or NO by click or by typing the word; Escape is
// no.
public partial class ConfirmBox : Control
{
    static readonly Rect2 Box = new(160, 196, 230, 138);
    readonly Label question;
    readonly TypeNav nav = new();
    Action? yes;

    public ConfirmBox()
    {
        Size = new Vector2(G.StageSize, G.StageSize);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        question = Kit.Label(this, "", Kit.Title(16), Box.Position.X, Box.Position.Y + 24, Box.Size.X, HorizontalAlignment.Center);
        var yesB = new TextButton(Strings.Yes, () => Answer(true), 13, boxed: true, colour: Kit.Bad, width: 90)
        {
            Position = new Vector2(Box.Position.X + 14, Box.Position.Y + 78),
        };
        var noB = new TextButton(Strings.No, () => Answer(false), 13, boxed: true, width: 90)
        {
            Position = new Vector2(Box.End.X - 104, Box.Position.Y + 78),
        };
        AddChild(yesB);
        AddChild(noB);
        nav.Add(yesB);
        nav.Add(noB);
    }

    /// <summary>Asks the question; <paramref name="onYes"/> runs if the answer is yes.</summary>
    public void Ask(string text, Action onYes)
    {
        question.Text = text;
        yes = onYes;
        nav.Clear();
        Visible = true;
    }

    void Answer(bool answer)
    {
        Visible = false;
        if (answer) yes?.Invoke();
    }

    /// <summary>A key while the question is up: Escape is no, letters spell YES or NO.</summary>
    public void OnKey(InputEventKey key)
    {
        if (key.Keycode == Key.Escape)
        {
            Answer(false);
            return;
        }
        char c = char.ToLowerInvariant((char)key.Unicode);
        if (c is >= 'a' and <= 'z') nav.Type(c);
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(Kit.Shade, 0.6f));
        Kit.Panel(this, Box, Kit.Bad);
    }
}
