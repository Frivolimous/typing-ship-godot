using Godot;

namespace TypingShip;

// The word carried by a ship, missile or the hull bar, coloured by how urgent it is (priority 0-3).
public partial class TextObject : Node2D
{
    static readonly Color[] PriorityColors =
    {
        new(0xffffffff), new(0xffff99ff), new(0xffbb99ff), new(0xff8877ff),
    };
    static readonly LabelSettings[] Settings = PriorityColors
        .Select(c => Fonts.Settings(Fonts.Word, 14, c, outline: 5, outlineColor: new Color(0, 0, 0, 0.9f)))
        .ToArray();

    readonly Label label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        Position = new Vector2(-100, 0),
        Size = new Vector2(200, 18),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    public int WordSize { get; }
    public string Text { get => label.Text; set => label.Text = value; }

    public TextObject() { }

    public TextObject(int size, int priority)
    {
        if (size <= 0) return;
        WordSize = size;
        Text = G.Words.Shift(size);
        AddChild(label);
        SetPriority(priority);
    }

    public void SetPriority(int i) => label.LabelSettings = Settings[Math.Clamp(i, 0, 3)];

    public void ShowWord(bool b) => label.Visible = b;
}
