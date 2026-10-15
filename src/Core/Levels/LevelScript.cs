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
    public abstract double Eval(Rng rng);
    public virtual bool IsConstant => false;

    public sealed record Const(double Value) : Num
    {
        public override double Eval(Rng rng) => Value;
        public override bool IsConstant => true;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>"a~b": a + random x (b - a), worked out in float like the original's "a + Math.random() * (b - a)".</summary>
    public sealed record Range(double From, double To) : Num
    {
        public override double Eval(Rng rng) => (float)From + rng.Rand() * (float)(To - From);
        public override string ToString() => $"{From.ToString(CultureInfo.InvariantCulture)}~{To.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>"a|b": a or b, half the time each (the original's "Math.random() > 0.5 ? a : b").</summary>
    public sealed record Either(double First, double Second) : Num
    {
        public override double Eval(Rng rng) => rng.Rand() > 0.5 ? First : Second;
        public override string ToString() => $"{First.ToString(CultureInfo.InvariantCulture)}|{Second.ToString(CultureInfo.InvariantCulture)}";
    }
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
    public sealed record Missile(int Line, string Type, Num Dist, Point From, Point[] To) : Statement(Line);
    public sealed record Laser(int Line, string Type, Num Dist, Point At, Num Speed) : Statement(Line);
    public sealed record Suicide(int Line, string Type, Num Dist, Point From, Point To) : Statement(Line);
    public sealed record Stealth(int Line, Num Dist, Point From, Point To, bool Shield) : Statement(Line);
    public sealed record Ship(int Line, string Code, Num Dist, Point From, Step[] Steps) : Statement(Line);
    public sealed record Boss(int Line, Num Dist, int Number) : Statement(Line);
    public sealed record Mirror(int Line, Num Dist, int Back) : Statement(Line);
    public sealed record Copy(int Line, Num Dist, int Back) : Statement(Line);
    public sealed record Offset(int Line, Num Dist, Point By, int Back) : Statement(Line);
    public sealed record Repeat(int Line, Num Dist, int Times, int Count, bool Mirrored) : Statement(Line);
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

    LevelScript(string file, string section, List<Statement> statements)
    {
        File = file;
        Section = section;
        Statements = statements;
    }

    public static readonly string[] Commands =
        { "missile", "laser", "suicide", "stealth", "ship", "boss", "mirror", "clone", "offset", "repeat", "insert", "wait", "jump", "loop" };

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
        int lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = StripComment(raw).Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('['))
            {
                if (!line.EndsWith(']')) throw new DataError(file, lineNo, $"A section name needs a closing ]: \"{line}\".");
                if (current.Count > 0 || section != "") result[section] = new LevelScript(file, section, current);
                section = string.Join(' ', line[1..^1].Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
                if (result.ContainsKey(section)) throw new DataError(file, lineNo, $"Section [{section}] appears twice.");
                current = new List<Statement>();
                continue;
            }
            current.Add(ParseLine(line, file, lineNo));
        }
        if (current.Count > 0 || section != "") result[section] = new LevelScript(file, section, current);
        return result;
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
                p.More ? p.Int("number of times", 1, 1000) : 1,
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
            double x = at.X.Eval(null!), y = at.Y.Eval(null!);
            if (x != 0 && x != 12 && y != 0 && y != 12)
                throw new DataError(file, lineNo, $"A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at {at}.");
        }
        return s;
    }

    static readonly Num Zero = new Num.Const(0);

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

        Num ParseNum(string s, string what)
        {
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
