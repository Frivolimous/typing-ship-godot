using System.Globalization;
using System.Text.RegularExpressions;
using TypingShip.Core.Data;

namespace TypingShip.Core.Levels;

/// <summary>
/// A number in a level file: a plain number, a random number in a range ("2~10"), or one of two numbers at random
/// ("0|12"). Random ones are rolled each time the level is built, as the original's Math.random() calls were.
/// </summary>
public abstract record Num
{
    /// <param name="difficulty">0 (easy) to 4 (insane), for numbers that differ by difficulty.</param>
    public abstract double Eval(Rng rng, int difficulty);
    public virtual bool IsConstant => false;

    public sealed record Const(double Value) : Num
    {
        public override double Eval(Rng rng, int difficulty) => Value;
        public override bool IsConstant => true;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>"a~b": a + random x (b - a), worked out in float like the original's "a + Math.random() * (b - a)".</summary>
    public sealed record Range(double From, double To) : Num
    {
        public override double Eval(Rng rng, int difficulty) => (float)From + rng.Rand() * (float)(To - From);
        public override string ToString() => $"{From.ToString(CultureInfo.InvariantCulture)}~{To.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>"a|b": a or b, half the time each (the original's "Math.random() > 0.5 ? a : b").</summary>
    public sealed record Either(double First, double Second) : Num
    {
        public override double Eval(Rng rng, int difficulty) => rng.Rand() > 0.5 ? First : Second;
        public override string ToString() => $"{First.ToString(CultureInfo.InvariantCulture)}|{Second.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>"1/2/3/3/4": one number per difficulty, Easy to Insane.</summary>
    public sealed record ByDifficulty(double[] Values) : Num
    {
        public override double Eval(Rng rng, int difficulty) => Values[difficulty];
        public override string ToString() => string.Join('/', Values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
    }
}

/// <summary>
/// Which difficulties a line of a level file happens at, as a bit mask (bit 0 Easy to bit 4 Insane). Written
/// "hard" (only Hard), "hard+" (Hard and up), "normal-" (Normal and down), "normal-extreme" (a range) or "all".
/// </summary>
public static class DifficultyTag
{
    public const int All = 0b11111;

    public static bool Includes(int mask, int difficulty) => (mask & (1 << difficulty)) != 0;

    /// <summary>The mask for a tag, or null if it isn't one.</summary>
    public static int? Parse(string tag)
    {
        tag = tag.ToLowerInvariant();
        if (tag == "all") return All;
        int Index(string name) => Array.FindIndex(Difficulty.Names, n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
        int from, to;
        if (tag.EndsWith('+')) (from, to) = (Index(tag[..^1]), 4);
        else if (tag.EndsWith('-')) (from, to) = (0, Index(tag[..^1]));
        else if (tag.Split('-') is [var a, var b]) (from, to) = (Index(a), Index(b));
        else from = to = Index(tag);
        if (from < 0 || to < 0 || from > to) return null;
        return (1 << (to + 1)) - (1 << from);
    }

    public const string Help = "A difficulty tag is a difficulty (easy, normal, hard, extreme, insane), optionally with + " +
        "(that one and up) or - (that one and down), a range like normal-extreme, or all.";
}

public sealed record Point(Num X, Num Y)
{
    public override string ToString() => $"{X},{Y}";
}

/// <summary>One step of a "ship" line's flight plan.</summary>
public sealed record Step(Step.Kind Type, Point? To = null, int Halt = 0, bool Fire = false)
{
    public enum Kind { Move, Halt, Attack }
}

/// <summary>One line of a level file.</summary>
public abstract record Statement(int Line)
{
    /// <summary>The difficulties it happens at (<see cref="DifficultyTag"/>).</summary>
    public int Mask { get; init; } = DifficultyTag.All;

    public sealed record Missile(int Line, string Type, Num Dist, Point From, Point[] To) : Statement(Line);
    public sealed record Laser(int Line, string Type, Num Dist, Point At, Num Speed) : Statement(Line);
    public sealed record Suicide(int Line, string Type, Num Dist, Point From, Point To) : Statement(Line);
    public sealed record Stealth(int Line, Num Dist, Point From, Point To, bool Shield) : Statement(Line);
    public sealed record Ship(int Line, string Code, Num Dist, Point From, Step[] Steps) : Statement(Line);
    public sealed record Boss(int Line, Num Dist, int Number) : Statement(Line);
    public sealed record Mirror(int Line, Num Dist, int Back) : Statement(Line);
    public sealed record Copy(int Line, Num Dist, int Back) : Statement(Line);
    public sealed record Offset(int Line, Num Dist, Point By, int Back) : Statement(Line);
    public sealed record Repeat(int Line, Num Dist, Num Times, int Count, bool Mirrored) : Statement(Line);
    public sealed record Insert(int Line, int At) : Statement(Line);
    public sealed record Wait(int Line, Num Dist) : Statement(Line);
    public sealed record Jump(int Line, Num Dist) : Statement(Line);
    public sealed record Loop(int Line, int From) : Statement(Line);
}

/// <summary>
/// A parsed level file (or one [section] of bosses.txt). The format is described for level designers in
/// docs/level-format.md; each line is one of the original LevelData helpers, e.g.
/// <c>missile sn 0.05  5,0 5,6 4,12</c>.
/// </summary>
public sealed class LevelScript
{
    public string File { get; }
    public string Section { get; }
    public IReadOnlyList<Statement> Statements { get; }
    /// <summary>
    /// "pace hard": the level's clock runs at that difficulty's speed (in CPS) whatever the difficulty, so it lasts
    /// the same time on all of them. Null (no pace line): the Classic pacing, where the clock runs at the
    /// difficulty's own speed.
    /// </summary>
    public double? Pace { get; private set; }
    /// <summary>"density 40%/65%/100%/100%/100%": the share of ship waves kept at each difficulty (0 to 1), or null.</summary>
    public double[]? Density { get; private set; }

    LevelScript(string file, string section, List<Statement> statements)
    {
        File = file;
        Section = section;
        Statements = statements;
    }

    public static readonly string[] Commands =
    {
        "missile", "laser", "suicide", "stealth", "ship", "boss", "mirror", "clone", "offset", "repeat", "insert", "wait", "jump",
        "loop", "pace", "density", "only",
    };

    static readonly Dictionary<string, string> Usage = new()
    {
        ["missile"] = "missile TYPE DISTANCE  START  POINT [POINT [POINT]]   e.g. missile sn 0.05  5,0 5,6 4,12",
        ["laser"] = "laser TYPE DISTANCE  START [SPEED]   e.g. laser sn 0.1  6,0",
        ["suicide"] = "suicide TYPE DISTANCE  START  TARGET   e.g. suicide mn 0.14  3,0 3,3",
        ["stealth"] = "stealth DISTANCE  START  EXIT [shield]   e.g. stealth 0.05  6,7 6,0",
        ["ship"] = "ship CODE DISTANCE  START  STEPS...   e.g. ship lmt 0.1  3,0  move 3,3  halt 15  halt 10 fire  move 0,3",
        ["boss"] = "boss DISTANCE NUMBER   e.g. boss 0.2 1",
        ["mirror"] = "mirror [DISTANCE [BACK]]   e.g. mirror 0.01",
        ["clone"] = "clone [DISTANCE [BACK]]   e.g. clone 0.01 -2",
        ["offset"] = "offset DISTANCE  X,Y [BACK]   e.g. offset 0.015  0,-2",
        ["repeat"] = "repeat DISTANCE [TIMES [SHIPS [mirrored]]]   e.g. repeat 0.01 7 1 mirrored",
        ["insert"] = "insert POSITION   e.g. insert -4",
        ["wait"] = "wait DISTANCE",
        ["jump"] = "jump DISTANCE",
        ["loop"] = "loop [FROM]",
        ["pace"] = "pace DIFFICULTY   e.g. pace hard",
        ["density"] = "density PERCENT   or one per difficulty, Easy to Insane: density 40%/65%/100%/100%/100%",
        ["only"] = "only TAG   e.g. only hard+ (the lines below happen on Hard and up; \"only all\" ends it)",
    };

    static readonly Regex CommaSpaces = new(@"\s*,\s*", RegexOptions.Compiled);

    /// <summary>
    /// Parses a level file. Plain level files have no sections; bosses.txt is split into [sections], returned by
    /// name. Throws a <see cref="DataError"/> naming the line for any mistake.
    /// </summary>
    public static Dictionary<string, LevelScript> ParseSections(string text, string file)
    {
        var result = new Dictionary<string, LevelScript>();
        var current = new List<Statement>();
        string section = "";
        int lineNo = 0, block = DifficultyTag.All;
        double? pace = null;
        double[]? density = null;
        int paceLine = 0, densityLine = 0;

        void EndSection()
        {
            if (current.Count == 0 && section == "" && pace == null && density == null) return;
            result[section] = new LevelScript(file, section, current) { Pace = pace, Density = density };
        }

        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = StripComment(raw).Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('['))
            {
                if (!line.EndsWith(']')) throw new DataError(file, lineNo, $"A section name needs a closing ]: \"{line}\".");
                EndSection();
                section = string.Join(' ', line[1..^1].Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
                if (result.ContainsKey(section)) throw new DataError(file, lineNo, $"Section [{section}] appears twice.");
                current = new List<Statement>();
                block = DifficultyTag.All;
                continue;
            }

            var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            int mask = block;
            if (words[0].EndsWith(':'))
            {
                // "hard+: missile ..." happens only on those difficulties (and only where an "only" block allows).
                mask &= DifficultyTag.Parse(words[0][..^1])
                    ?? throw new DataError(file, lineNo, $"\"{words[0][..^1]}\" isn't a difficulty tag. {DifficultyTag.Help}");
                if (words.Length == 1) throw new DataError(file, lineNo, $"Nothing follows the tag \"{words[0]}\".");
                line = line[(line.IndexOf(':') + 1)..].Trim();
                words = words[1..];
            }

            switch (words[0].ToLowerInvariant())
            {
                case "only":
                    if (words.Length != 2 || DifficultyTag.Parse(words[1]) is not { } only)
                        throw new DataError(file, lineNo, $"Expected \"only TAG\", like \"only hard+\" or \"only all\". {DifficultyTag.Help}");
                    if (mask != block) throw new DataError(file, lineNo, "An \"only\" line can't have a tag of its own.");
                    block = only;
                    continue;
                case "pace":
                    if (section != "") throw new DataError(file, lineNo, "Only a level file can set its pace (the boss sections use the level's).");
                    if (paceLine > 0) throw new DataError(file, lineNo, $"The pace is already set on line {paceLine}.");
                    int i = words.Length == 2 ? Array.FindIndex(Difficulty.Names, n => n.Equals(words[1], StringComparison.OrdinalIgnoreCase)) : -1;
                    if (i < 0)
                        throw new DataError(file, lineNo, "Expected \"pace DIFFICULTY\", like \"pace hard\" " +
                            $"({string.Join(", ", Difficulty.Names).ToLowerInvariant()}).");
                    pace = Difficulty.Cps[i];
                    paceLine = lineNo;
                    continue;
                case "density":
                    if (section != "") throw new DataError(file, lineNo, "Only a level file can set the density (the boss sections use the level's).");
                    if (densityLine > 0) throw new DataError(file, lineNo, $"The density is already set on line {densityLine}.");
                    density = ParseDensity(words.Length == 2 ? words[1] : "", file, lineNo);
                    densityLine = lineNo;
                    continue;
            }
            var s = ParseLine(line, file, lineNo);
            current.Add(mask == DifficultyTag.All ? s : s with { Mask = mask });
        }
        EndSection();
        return result;
    }

    /// <summary>"40%" or "40%/65%/100%/100%/100%" (the % is optional): shares from 1% to 100%.</summary>
    static double[] ParseDensity(string s, string file, int lineNo)
    {
        var parts = s.Split('/');
        var values = new double[5];
        for (int k = 0; k < 5; k++)
        {
            var part = parts.Length == 1 ? parts[0] : k < parts.Length ? parts[k] : "";
            if (parts.Length is not (1 or 5) ||
                !double.TryParse(part.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v is < 1 or > 100)
                throw new DataError(file, lineNo, "Expected \"density PERCENT\", like \"density 60%\", or five percentages from 1% " +
                    "to 100% separated by / (Easy to Insane), like \"density 40%/65%/100%/100%/100%\".");
            values[k] = v / 100;
        }
        return values;
    }

    /// <summary>Parses a level file with no sections.</summary>
    public static LevelScript Parse(string text, string file)
    {
        var sections = ParseSections(text, file);
        if (sections.Keys.Any(k => k != ""))
            throw new DataError(file, 0, "A level file can't have [sections]; only bosses.txt has them.");
        return sections.TryGetValue("", out var s) ? s : new LevelScript(file, "", new List<Statement>());
    }

    static string StripComment(string line)
    {
        int i = line.IndexOf('#');
        int j = line.IndexOf("//", StringComparison.Ordinal);
        if (j >= 0 && (i < 0 || j < i)) i = j;
        return i >= 0 ? line[..i] : line;
    }

    static Statement ParseLine(string line, string file, int lineNo)
    {
        var tokens = CommaSpaces.Replace(line, ",").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var cmd = tokens[0].ToLowerInvariant();
        var p = new LineReader(tokens, file, lineNo, cmd);
        if (!Usage.ContainsKey(cmd))
            throw new DataError(file, lineNo, $"\"{tokens[0]}\" isn't a command.{DataError.DidYouMean(cmd, Commands)} " +
                $"The commands are: {string.Join(", ", Commands)}.");

        Statement s = cmd switch
        {
            "missile" => new Statement.Missile(lineNo, p.Type(), p.Number("distance"), p.Point("start point"),
                p.Points("point to fly to", 1, 3)),
            "laser" => new Statement.Laser(lineNo, p.Type(), p.Number("distance"), p.Point("start point"),
                p.More ? p.Number("speed") : new Num.Const(0)),
            "suicide" => new Statement.Suicide(lineNo, p.Type(), p.Number("distance"), p.Point("start point"), p.Point("target point")),
            "stealth" => new Statement.Stealth(lineNo, p.Number("distance"), p.Point("start point"), p.Point("exit point"),
                p.More && p.Word("shield")),
            "ship" => new Statement.Ship(lineNo, p.Code(), p.Number("distance"), p.Point("start point"), p.Steps()),
            "boss" => new Statement.Boss(lineNo, p.Number("distance"), p.Int("boss number (1, 2 or 3)", 1, 3)),
            "mirror" => new Statement.Mirror(lineNo, p.More ? p.Number("distance") : Zero, p.More ? p.Back() : -1),
            "clone" => new Statement.Copy(lineNo, p.More ? p.Number("distance") : Zero, p.More ? p.Back() : -1),
            "offset" => new Statement.Offset(lineNo, p.Number("distance"), p.Point("offset (x,y)"), p.More ? p.Back() : -1),
            "repeat" => new Statement.Repeat(lineNo, p.Number("distance"),
                p.More ? p.Number("number of times", 0, 1000) : One,
                p.More ? p.Int("number of ships to repeat", 1, 1000) : 1,
                p.More && p.Word("mirrored")),
            "insert" => new Statement.Insert(lineNo, p.Int("position", -10000, 10000)),
            "wait" => new Statement.Wait(lineNo, p.Number("distance")),
            "jump" => new Statement.Jump(lineNo, p.Number("distance")),
            "loop" => new Statement.Loop(lineNo, p.More ? p.Int("event to loop from", -10000, 10000) : 0),
            _ => throw new InvalidOperationException(),
        };
        p.End();
        if (s is Statement.Laser { At: var at } && at.X.IsConstant && at.Y.IsConstant)
        {
            double x = at.X.Eval(null!, 0), y = at.Y.Eval(null!, 0);
            if (x != 0 && x != 12 && y != 0 && y != 12)
                throw new DataError(file, lineNo, $"A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at {at}.");
        }
        return s;
    }

    static readonly Num Zero = new Num.Const(0), One = new Num.Const(1);

    /// <summary>Reads a line's words in order, with messages that say what was expected.</summary>
    sealed class LineReader(string[] tokens, string file, int line, string cmd)
    {
        int i = 1;

        public bool More => i < tokens.Length;

        DataError Err(string problem) => new(file, line, $"{problem}\n  Usage: {Usage[cmd]}");

        string Next(string what)
        {
            if (i >= tokens.Length) throw Err($"{cmd} is missing its {what}.");
            return tokens[i++];
        }

        public void End()
        {
            if (i < tokens.Length) throw Err($"Unexpected \"{string.Join(' ', tokens[i..])}\" at the end of the line.");
        }

        /// <summary>Two letters: size and defence (the weapon comes from the command).</summary>
        public string Type()
        {
            var t = Next("ship type").ToLowerInvariant();
            if (t.Length != 2 || !t.All(char.IsLetter))
                throw Err($"\"{t}\" isn't a ship type. It's two letters: the size (s, m, l...) and the defence (n, s, t, b), like sn or mt.");
            return t;
        }

        /// <summary>Three letters: size, weapon, defence.</summary>
        public string Code()
        {
            var t = Next("ship code").ToLowerInvariant();
            if (t.Length != 3 || !t.All(char.IsLetter))
                throw Err($"\"{t}\" isn't a ship code. It's three letters: size, weapon, defence, like lmt.");
            return t;
        }

        public Num Number(string what) => ParseNum(Next(what), what);

        /// <summary>A whole number from min to max, or one for each difficulty ("1/2/3/3/4").</summary>
        public Num Number(string what, int min, int max)
        {
            var s = Next(what);
            var parts = s.Split('/');
            if (parts.Length is not (1 or 5))
                throw Err($"\"{s}\" isn't a whole number (expected the {what}), or five of them separated by / (Easy to Insane).");
            var values = new double[parts.Length];
            for (int k = 0; k < parts.Length; k++)
            {
                if (!int.TryParse(parts[k], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v))
                    throw Err($"\"{parts[k]}\" isn't a whole number (expected the {what}).");
                if (v < min || v > max) throw Err($"The {what} must be from {min} to {max}, not {v}.");
                values[k] = v;
            }
            return values.Length == 1 ? new Num.Const(values[0]) : new Num.ByDifficulty(values);
        }

        Num ParseNum(string s, string what)
        {
            if (s.Contains('/'))
            {
                var parts = s.Split('/');
                var values = new double[parts.Length];
                if (parts.Length == 5 && parts.Select((part, k) => TryDouble(part, out values[k])).All(ok => ok))
                    return new Num.ByDifficulty(values);
                throw Err($"\"{s}\" isn't one number per difficulty for the {what}; write five, Easy to Insane, like 0.1/0.08/0.06/0.05/0.05.");
            }
            if (s.Contains('~'))
            {
                var parts = s.Split('~');
                if (parts.Length == 2 && TryDouble(parts[0], out var a) && TryDouble(parts[1], out var b)) return new Num.Range(a, b);
                throw Err($"\"{s}\" isn't a range for the {what}; write it like 3~6 (any number from 3 to 6).");
            }
            if (s.Contains('|'))
            {
                var parts = s.Split('|');
                if (parts.Length == 2 && TryDouble(parts[0], out var a) && TryDouble(parts[1], out var b)) return new Num.Either(a, b);
                throw Err($"\"{s}\" isn't a choice for the {what}; write it like 0|12 (0 or 12, at random).");
            }
            if (TryDouble(s, out var v)) return new Num.Const(v);
            throw Err($"\"{s}\" isn't a number (expected the {what}).");
        }

        static bool TryDouble(string s, out double v) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && double.IsFinite(v);

        public Point Point(string what)
        {
            var s = Next(what);
            var parts = s.Split(',');
            if (parts.Length != 2 || parts[0] == "" || parts[1] == "")
                throw Err($"\"{s}\" isn't a point (expected the {what}). Write points as x,y on the 12 x 12 grid, like 5,0.");
            return new Point(ParseNum(parts[0], what + " x"), ParseNum(parts[1], what + " y"));
        }

        public Point[] Points(string what, int min, int max)
        {
            var list = new List<Point>();
            while (More && list.Count < max) list.Add(Point(what));
            if (list.Count < min) throw Err($"{cmd} needs at least {min} {what}{(min > 1 ? "s" : "")} after the start point.");
            return list.ToArray();
        }

        public int Int(string what, int min, int max)
        {
            var s = Next(what);
            if (!int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v))
                throw Err($"\"{s}\" isn't a whole number (expected the {what}).");
            if (v < min || v > max) throw Err($"The {what} must be from {min} to {max}, not {v}.");
            return v;
        }

        /// <summary>How many spawns back to copy: -1 is the last one.</summary>
        public int Back()
        {
            int v = Int("ship to copy (-1 is the one just above)", -10000, -1);
            return v;
        }

        public bool Word(string word)
        {
            var s = Next(word).ToLowerInvariant();
            if (s != word) throw Err($"Expected \"{word}\" or nothing, not \"{s}\".");
            return true;
        }

        public Step[] Steps()
        {
            var steps = new List<Step>();
            while (More)
            {
                var kind = tokens[i++].ToLowerInvariant();
                switch (kind)
                {
                    case "move":
                        steps.Add(new Step(Step.Kind.Move, To: Point("point to move to"), Fire: FireFlag()));
                        break;
                    case "halt":
                        steps.Add(new Step(Step.Kind.Halt, Halt: Int("halt time", 0, 100000), Fire: FireFlag()));
                        break;
                    case "attack":
                        steps.Add(new Step(Step.Kind.Attack));
                        break;
                    default:
                        throw Err($"\"{kind}\" isn't a step. Steps are \"move X,Y\", \"halt N\" (either can end with \"fire\"), and \"attack\".");
                }
            }
            if (steps.Count == 0) throw Err("A ship needs at least one step after its start point.");
            return steps.ToArray();
        }

        bool FireFlag()
        {
            if (!More || !tokens[i].Equals("fire", StringComparison.OrdinalIgnoreCase)) return false;
            i++;
            return true;
        }
    }
}
