using TypingShip.Core.Data;

namespace TypingShip.Core;

/// <summary>
/// The typing test: type a passage of prose (data/typing-test.txt) within <see cref="TimeLimit"/> seconds. The
/// clock starts on the first key. A wrong key counts as an error and doesn't move on, as in a level. The result is
/// net WPM (correct characters, 5 to a word) and accuracy, and the difficulty it suggests.
/// </summary>
public sealed class TypingTestRun
{
    public const double TimeLimit = 90;

    public string Passage { get; }
    /// <summary>Characters typed correctly so far (the position in the passage).</summary>
    public int Typed { get; private set; }
    public int Errors { get; private set; }
    public bool Started => start != null;
    public bool Finished { get; private set; }
    /// <summary>Seconds from the first key to the last correct one (or to the time limit).</summary>
    public double Seconds { get; private set; }

    double? start;

    public TypingTestRun(string passage) => Passage = passage;

    /// <summary>A key at <paramref name="now"/> seconds (any clock). Returns whether it was right.</summary>
    public bool Type(char c, double now)
    {
        if (Finished) return false;
        start ??= now;
        if (Tick(now)) return false;
        if (char.ToLowerInvariant(c) != char.ToLowerInvariant(Passage[Typed]))
        {
            Errors++;
            return false;
        }
        Typed++;
        Seconds = now - start.Value;
        if (Typed == Passage.Length) Finished = true;
        return true;
    }

    /// <summary>Call as time passes: ends the test at the time limit. Returns whether it has finished.</summary>
    public bool Tick(double now)
    {
        if (start != null && !Finished && now - start.Value >= TimeLimit)
        {
            Seconds = TimeLimit;
            Finished = true;
        }
        return Finished;
    }

    /// <summary>Seconds left on the clock (the whole limit until the first key).</summary>
    public double Remaining(double now) => start == null ? TimeLimit : Math.Max(0, TimeLimit - (now - start.Value));

    /// <summary>Correct characters per second.</summary>
    public double NetCps => Seconds <= 0 ? 0 : Typed / Seconds;
    public double NetWpm => Speed.ToWpm(NetCps);
    /// <summary>0 to 1: correct keys out of all keys.</summary>
    public double Accuracy => Typed + Errors == 0 ? 1 : Typed / (double)(Typed + Errors);

    /// <summary>
    /// The difficulty a test result suggests (0 = Easy to 4 = Insane), or -1 for no result: the hardest difficulty
    /// at or below the typist's WPM, and Easy below that.
    /// </summary>
    public static int Suggest(int wpm) => wpm <= 0 ? -1 : Math.Max(0, Array.FindLastIndex(Difficulty.Wpm, w => w <= wpm));

    /// <summary>
    /// Reads data/typing-test.txt: passages separated by blank lines; lines next to each other join into one
    /// passage. Anything after # is a note. Throws a <see cref="DataError"/> naming the line for any mistake.
    /// </summary>
    public static IReadOnlyList<string> ParsePassages(string text, string fileName)
    {
        var passages = new List<string>();
        var current = new List<string>();
        int firstLine = 0, lineNo = 0;

        void End()
        {
            if (current.Count == 0) return;
            var p = string.Join(' ', string.Join(' ', current).Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (p.Length < MinLength || p.Length > MaxLength)
                throw new DataError(fileName, firstLine,
                    $"This passage is {p.Length} characters; a typing test passage should be {MinLength} to {MaxLength} (about 250).");
            passages.Add(p);
            current.Clear();
        }

        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            int hash = raw.IndexOf('#');
            var line = (hash >= 0 ? raw[..hash] : raw).Trim();
            if (line.Length == 0)
            {
                End();
                continue;
            }
            int bad = line.ToList().FindIndex(ch => ch < ' ' || ch > '~');
            if (bad >= 0)
                throw new DataError(fileName, lineNo,
                    $"\"{line[bad]}\" can't be typed on every keyboard. Use plain letters, digits, spaces and punctuation.");
            if (current.Count == 0) firstLine = lineNo;
            current.Add(line);
        }
        End();
        if (passages.Count == 0) throw new DataError(fileName, 0, "There are no passages.");
        return passages;
    }

    public const int MinLength = 150, MaxLength = 300;
}
