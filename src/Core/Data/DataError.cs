namespace TypingShip.Core.Data;

/// <summary>A mistake in a data file, with where it is, written for whoever edits the file.</summary>
public sealed class DataError : Exception
{
    public string File { get; }
    public int Line { get; }
    public string Problem { get; }

    public DataError(string file, int line, string problem)
        : base(line > 0 ? $"{file}, line {line}: {problem}" : $"{file}: {problem}")
    {
        File = file;
        Line = line;
        Problem = problem;
    }

    /// <summary>The closest of the candidates to a misspelt word, for "did you mean" hints, or null.</summary>
    public static string? Suggest(string word, IEnumerable<string> candidates)
    {
        string? best = null;
        int bestD = int.MaxValue;
        foreach (var c in candidates)
        {
            int d = Distance(word, c);
            if (d < bestD) (best, bestD) = (c, d);
        }
        return best != null && bestD <= Math.Max(2, word.Length / 3) ? best : null;
    }

    static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        for (int j = 1; j <= b.Length; j++)
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }

    public static string DidYouMean(string word, IEnumerable<string> candidates) =>
        Suggest(word, candidates) is { } s ? $" Did you mean \"{s}\"?" : "";
}
