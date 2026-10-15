namespace TypingShip.Core;

/// <summary>
/// An anagram (the original's ScrambleObject): a random word with its letters swapped around, never into another
/// real word of the same length. The original defined this but never used it (it was likely meant for a mode that
/// didn't ship); kept as is, with no view.
/// </summary>
public sealed class Scramble
{
    /// <summary>The other words of this length that are anagrams of the word (pArray).</summary>
    public readonly List<string> PArray = new();
    /// <summary>The unscrambled word. The original didn't keep it.</summary>
    public readonly string Word;
    /// <summary>What's shown: the word with its letters swapped.</summary>
    public readonly string Text;
    readonly Rng rng;

    public Scramble(WordList words, Rng rng, int size, int swaps = 1)
    {
        this.rng = rng;
        Word = words.Shift(size);
        var cWord = Word;
        foreach (var w in words.Words(size))
            if (IsAnagram(cWord, w)) PArray.Add(w);
        for (int i = 0; i < swaps; i++) cWord = SwapLetters(cWord);
        Text = cWord;
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
            int i = (int)(rng.Rand() * tWord.Length), j = (int)(rng.Rand() * tWord.Length);
            var c = tWord.ToCharArray();
            (c[i], c[j]) = (c[j], c[i]);
            var cWord = new string(c);
            if (!PArray.Contains(cWord)) return cWord;
        }
    }
}
