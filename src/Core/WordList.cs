namespace TypingShip.Core;

/// <summary>
/// Words grouped by length. Shift() takes a random word but never one of the last five, so words that just went
/// back into the list (via Push) aren't handed out again straight away.
/// </summary>
public sealed class WordList
{
    readonly List<string>[] list = new List<string>[11];
    readonly Rng rng;

    /// <param name="text">data/words.txt: one line per word length, starting at 3 letters.</param>
    public WordList(string text, Rng rng)
    {
        this.rng = rng;
        for (int i = 0; i < list.Length; i++) list[i] = new List<string>();
        int length = 3;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            foreach (var w in line.Split(' ', StringSplitOptions.RemoveEmptyEntries)) Push(w, length);
            length++;
        }
    }

    public string Shift(int size)
    {
        var l = list[size];
        int i = (int)Math.Floor(rng.Rand() * (l.Count - 5));
        var word = l[i];
        l.RemoveAt(i);
        return word;
    }

    /// <summary>The words currently available at this length (WordList.list[size]).</summary>
    public IReadOnlyList<string> Words(int size) => list[size];

    public void Push(string s) => Push(s, s.Length);

    void Push(string s, int length) => list[Math.Min(length, 10)].Add(s);
}
