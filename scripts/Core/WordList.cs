using Godot;

namespace TypingShip;

// Words grouped by length. shift() takes a random word but never one of the last five, so words that just went
// back into the list (via push) aren't handed out again straight away.
public class WordList
{
    readonly List<string>[] list = new List<string>[11];

    public WordList()
    {
        for (int i = 0; i < list.Length; i++) list[i] = new List<string>();
        using var file = Godot.FileAccess.Open("res://data/words.txt", Godot.FileAccess.ModeFlags.Read);
        int length = 3;
        while (!file.EofReached())
        {
            var line = file.GetLine().Trim();
            if (line.Length == 0) continue;
            foreach (var w in line.Split(' ', StringSplitOptions.RemoveEmptyEntries)) Push(w, length);
            length++;
        }
    }

    public string Shift(int size)
    {
        var l = list[size];
        int i = (int)Math.Floor(G.Rand() * (l.Count - 5));
        var word = l[i];
        l.RemoveAt(i);
        return word;
    }

    public void Push(string s) => Push(s, s.Length);

    void Push(string s, int length) => list[Math.Min(length, 10)].Add(s);

    public static int HealSize() => 8 + (int)Math.Floor(G.Rand() * 2);
}
