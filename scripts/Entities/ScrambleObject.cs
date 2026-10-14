using Godot;

namespace TypingShip;

// An anagram: a random word with its letters swapped around, never into another real word of the same length.
// The original defined this but never used it (it was likely meant for a mode that didn't ship); ported as is.
public partial class ScrambleObject : Node2D
{
    /// <summary>The other words of this length that are anagrams of the word (pArray).</summary>
    public readonly List<string> PArray = new();
    /// <summary>The unscrambled word. The original didn't keep it.</summary>
    public readonly string Word = "";
    readonly int wordSize, swaps;
    readonly Label label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        Position = new Vector2(-150, 0),
        Size = new Vector2(300, 30),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        LabelSettings = new LabelSettings
        {
            Font = Fonts.Word,
            FontSize = 25,
            FontColor = Colors.White,
            ShadowColor = Colors.Black,
            ShadowOffset = new Vector2(-1, -1),
            ShadowSize = 0,
        },
    };

    public ScrambleObject() { }

    public ScrambleObject(int size, int swaps = 1)
    {
        wordSize = size;
        this.swaps = swaps;
        Word = G.Words.Shift(size);
        AddChild(label);
        SetText(Word);
    }

    public string Text => label.Text;

    void SetText(string s)
    {
        var cWord = s;
        foreach (var w in G.Words.Words(wordSize))
            if (IsAnagram(cWord, w)) PArray.Add(w);
        for (int i = 0; i < swaps; i++) cWord = SwapLetters(cWord);
        label.Text = cWord;
    }

    /// <summary>Does every letter of word appear in other (as many times)?</summary>
    static bool IsAnagram(string word, string other)
    {
        var t = other;
        foreach (char c in word)
        {
            int k = t.IndexOf(c);
            if (k < 0) return false;
            t = t.Remove(k, 1);
        }
        return true;
    }

    string SwapLetters(string tWord)
    {
        while (true)
        {
            int i = (int)(G.Rand() * tWord.Length), j = (int)(G.Rand() * tWord.Length);
            var c = tWord.ToCharArray();
            (c[i], c[j]) = (c[j], c[i]);
            var cWord = new string(c);
            if (!PArray.Contains(cWord)) return cWord;
        }
    }
}
