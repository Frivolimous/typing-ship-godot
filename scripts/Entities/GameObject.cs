using Godot;

namespace TypingShip;

// Anything that can carry a typeable word.
public partial class GameObject : Node2D
{
    public TextObject? TextObject;
    public int WordSize = -1;
    public float WordX;
    int priority;

    public void AddWord(int size = 0)
    {
        RecycleWord();
        if (WordSize < 0) return;
        TextObject = new TextObject(size == 0 ? WordSize : size, priority)
        {
            Rotation = -Rotation,
            Position = new Vector2(WordX, 0),
        };
        AddChild(TextObject);
    }

    public void RecycleWord()
    {
        if (TextObject == null) return;
        G.Words.Push(TextObject.Text);
        TextObject.QueueFree();
        TextObject = null;
    }

    public int Priority
    {
        get => priority;
        set
        {
            priority = value;
            TextObject?.SetPriority(value);
        }
    }

    public void ShowWord(bool b) => TextObject?.ShowWord(b);
}
