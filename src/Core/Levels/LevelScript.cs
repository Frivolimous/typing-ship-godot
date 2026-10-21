using System.Globalization;
using System.Text.RegularExpressions;
using TypingShip.Core.Data;

namespace TypingShip.Core.Levels;

/// <summary>
/// A number in a level file: a plain number, a random number in a range ("2~10"), one of two numbers at random
/// ("0|12"), or a header constant (which may have one value per difficulty). Random ones are rolled each time the
/// level is built, as the original's Math.random() calls were.
/// </summary>
public abstract record Num
{
    /// <param name="difficulty">0 (easy) to 4 (insane), for constants that differ by difficulty.</param>
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

    /// <summary>A header constant with one value per difficulty, Easy to Insane ("SWARM_GAP = 0.9/0.9/0.7/0.6/0.5").</summary>
    public sealed record ByDifficulty(string Name, double[] Values) : Num
    {
        public override double Eval(Rng rng, int difficulty) => Values[difficulty];
        public override string ToString() => Name;
    }
}

public sealed record Point(Num X, Num Y)
{
    public override string ToString() => $"{X},{Y}";
}

/// <summary>One step of a "ship" line's flight plan. Halt is in seconds as played on Hard.</summary>
public sealed record Step(Step.Kind Type, Point? To = null, Num? Halt = null, bool Fire = false)
{
    public enum Kind { Move, Halt, Attack }
}

/// <summary>
/// When a line happens: at an absolute time ("@1:37.5", seconds from the start of the level, or of the batch in a
/// boss section), a gap after the line above ("+0.6"), or (no prefix) the same moment as the line above.
/// </summary>
public sealed record Timing(Timing.Kind Type, double At = 0, Num? Gap = null)
{
    public enum Kind { Same, At, After }
    public static readonly Timing Same = new(Kind.Same);
}

/// <summary>One line of a level file.</summary>
public abstract record Statement(int Line)
{
    /// <summary>The easiest difficulty the line's ships are on (0 Easy to 4 Insane), from a tag like "normal+:".</summary>
    public int Easiest { get; init; }

    public sealed record Missile(int Line, string Type, Timing When, Point From, Point[] To) : Statement(Line);
    public sealed record Laser(int Line, string Type, Timing When, Point At, Num Speed) : Statement(Line);
    public sealed record Suicide(int Line, string Type, Timing When, Point From, Point To) : Statement(Line);
    public sealed record Stealth(int Line, Timing When, Point From, Point To, bool Shield) : Statement(Line);
    public sealed record Ship(int Line, string Code, Timing When, Point From, Step[] Steps) : Statement(Line);
    public sealed record Boss(int Line, Timing When, int Number) : Statement(Line);
    public sealed record Mirror(int Line, Timing When, int Back) : Statement(Line);
    public sealed record Copy(int Line, Timing When, int Back) : Statement(Line);
    public sealed record Offset(int Line, Timing When, Point By, int Back) : Statement(Line);
    /// <summary>Gap in seconds between copies (the first comes that long after the line above).</summary>
    public sealed record Repeat(int Line, Num Gap, Num Times, int Count, bool Mirrored) : Statement(Line);
    /// <summary>Copies spread evenly over Duration seconds: the last one comes Duration after the line above.</summary>
    public sealed record RepeatOver(int Line, Num Duration, Num Times, int Count, bool Mirrored) : Statement(Line);
    public sealed record Insert(int Line, int At) : Statement(Line);
    public sealed record Wait(int Line, Timing When) : Statement(Line);
    public sealed record Jump(int Line, Timing When, Num Seconds) : Statement(Line);
    public sealed record Loop(int Line, Timing When, int From) : Statement(Line);

    /// <summary>The line's timing, for lines that happen at a moment (not repeat or insert).</summary>
    public Timing? Moment => this switch
    {
        Missile m => m.When, Laser l => l.When, Suicide s => s.When, Stealth s => s.When, Ship s => s.When, Boss b => b.When,
        Mirror m => m.When, Copy c => c.When, Offset o => o.When, Wait w => w.When, Jump j => j.When, Loop l => l.When,
        _ => null,
    };
}

/// <summary>
/// A parsed level file (or one [section] of bosses.txt). The format is described for level designers in
/// docs/level-format.md. Times are seconds as played on Hard, and the same at every difficulty, e.g.
/// <c>@0:02.77 missile sn  5,0 5,6 4,12</c>.
/// </summary>
public sealed class LevelScript
{
    public string File { get; }
    public string Section { get; }
    public IReadOnlyList<Statement> Statements { get; }
    /// <summary>The header's named constants.</summary>
    public IReadOnlyDictionary<string, Num> Constants { get; }

    LevelScript(string file, string section, List<Statement> statements, Dictionary<string, Num> constants)
    {
        File = file;
        Section = section;
        Statements = statements;
        Constants = constants;
    }

    public static readonly string[] Commands =
    {
        "missile", "laser", "suicide", "stealth", "ship", "boss", "mirror", "clone", "offset", "repeat", "repeat_over", "insert", "wait", "jump", "loop",
    };

    // Words that were part of the format before the time-based rewrite, with what to do instead.
    static readonly Dictionary<string, string> Removed = new()
    {
        ["pace"] = "Levels no longer need a pace line: timing is in seconds and the same at every difficulty. Delete the line.",
        ["duration"] = "A level's length is now its content (the last @ time plus that wave). Delete the line.",
        ["density"] = "density was removed; the other difficulties are worked out later from Hard. Delete the line.",
        ["only"] = "Difficulty blocks (only ...) were removed. Delete the line; use a per-difficulty header constant if you need one.",
    };

    static readonly Dictionary<string, string> Usage = new()
    {
        ["missile"] = "[@M:SS | +GAP] missile TYPE  START  POINT [POINT [POINT]]   e.g. @0:02.8 missile sn  5,0 5,6 4,12",
        ["laser"] = "[@M:SS | +GAP] laser TYPE  START [SPEED]   e.g. +0.6 laser sn  6,0",
        ["suicide"] = "[@M:SS | +GAP] suicide TYPE  START  TARGET   e.g. suicide mn  3,0 3,3",
        ["stealth"] = "[@M:SS | +GAP] stealth  START  EXIT [shield]   e.g. stealth  6,7 6,0",
        ["ship"] = "[@M:SS | +GAP] ship CODE  START  STEPS...   e.g. ship lmt  3,0  move 3,3  halt 3.8  halt 2.5 fire  move 0,3",
        ["boss"] = "[@M:SS | +GAP] boss NUMBER   e.g. @2:10 boss 1",
        ["mirror"] = "[@M:SS | +GAP] mirror [BACK]   e.g. +0.55 mirror",
        ["clone"] = "[@M:SS | +GAP] clone [BACK]   e.g. +0.55 clone -2",
        ["offset"] = "[@M:SS | +GAP] offset  X,Y [BACK]   e.g. +0.8 offset  0,-2",
        ["repeat"] = "repeat GAP [TIMES [SHIPS [mirrored]]]   e.g. repeat 0.55 7 1 mirrored (GAP in seconds)",
        ["repeat_over"] = "repeat_over SECONDS TIMES [SHIPS [mirrored]]   e.g. repeat_over 4.5 9 1 mirrored (the last copy SECONDS after the line above)",
        ["insert"] = "insert POSITION   e.g. insert -4",
        ["wait"] = "@M:SS wait   or   +GAP wait",
        ["jump"] = "[@M:SS | +GAP] jump SECONDS",
        ["loop"] = "[@M:SS | +GAP] loop [FROM]",
    };

    static readonly Regex CommaSpaces = new(@"\s*,\s*", RegexOptions.Compiled);
    static readonly Regex ConstantLine = new(@"^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)$", RegexOptions.Compiled);
    static readonly Regex Anchor = new(@"^@(\d+):(\d\d(?:\.\d+)?)$", RegexOptions.Compiled);

    /// <summary>
    /// Parses a level file. Plain level files have no sections; bosses.txt is split into [sections], returned by
    /// name. Throws a <see cref="DataError"/> naming the line for any mistake.
    /// </summary>
    public static Dictionary<string, LevelScript> ParseSections(string text, string file)
    {
        var result = new Dictionary<string, LevelScript>();
        var current = new List<Statement>();
        var constants = new Dictionary<string, Num>(StringComparer.Ordinal);
        string section = "";
        int lineNo = 0;
        double lastAnchor = -1;
        int lastAnchorLine = 0;

        void EndSection()
        {
            if (current.Count == 0 && section == "") return;
            result[section] = new LevelScript(file, section, current, constants);
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
                lastAnchor = -1;
                continue;
            }

            // Header: NAME = value, before the first command.
            if (ConstantLine.Match(line) is { Success: true } cm)
            {
                if (current.Count > 0 || section != "")
                    throw new DataError(file, lineNo, $"Constants go in the header, before the first command (\"{cm.Groups[1].Value}\").");
                var name = cm.Groups[1].Value;
                if (constants.ContainsKey(name)) throw new DataError(file, lineNo, $"The constant {name} is already set.");
                if (Commands.Contains(name.ToLowerInvariant()))
                    throw new DataError(file, lineNo, $"{name} can't be a constant: it's the name of a command.");
                constants[name] = ParseConstant(name, cm.Groups[2].Value.Trim(), file, lineNo);
                continue;
            }

            var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            // Difficulty tag: "normal+:" puts the line's ships on Normal and harder only.
            int from = 0;
            if (words[0].EndsWith(':') && !words[0].StartsWith('@'))
            {
                from = ParseTag(words[0], file, lineNo);
                line = line[words[0].Length..].Trim();
                if (line.Length == 0) throw new DataError(file, lineNo, $"Nothing follows the tag \"{words[0]}\".");
                words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            }
            if (Removed.TryGetValue(words[0].ToLowerInvariant(), out var why)) throw new DataError(file, lineNo, why);

            // Timing prefix.
            var timing = Timing.Same;
            string rest = line;
            if (words[0].StartsWith('@'))
            {
                var m = Anchor.Match(words[0]);
                if (!m.Success)
                    throw new DataError(file, lineNo, $"\"{words[0]}\" isn't a time. Write it as @M:SS, with optional decimals: @0:07, @1:37.5.");
                double at = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * 60 +
                            double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) >= 60)
                    throw new DataError(file, lineNo, $"\"{words[0]}\": the seconds must be under 60.");
                if (at < lastAnchor)
                    throw new DataError(file, lineNo, $"{words[0]} is earlier than the @ time on line {lastAnchorLine}. @ times must be in order.");
                lastAnchor = at;
                lastAnchorLine = lineNo;
                timing = new Timing(Timing.Kind.At, At: at);
                rest = line[words[0].Length..].Trim();
            }
            else if (words[0].StartsWith('+'))
            {
                var gap = ParseNum(words[0][1..], "gap after the line above (seconds)", constants, file, lineNo);
                if (gap is Num.Const { Value: < 0 } || gap is Num.ByDifficulty { Values: var gv } && gv.Any(g => g < 0))
                    throw new DataError(file, lineNo, "A gap can't be negative.");
                timing = new Timing(Timing.Kind.After, Gap: gap);
                rest = line[words[0].Length..].Trim();
            }
            if (rest.Length == 0) throw new DataError(file, lineNo, $"Nothing follows the time \"{words[0]}\".");
            // The tag can also come after the time: "@0:14 normal+: missile ...".
            if (rest.Split(' ')[0] is var tag && tag.EndsWith(':'))
            {
                if (from > 0) throw new DataError(file, lineNo, "A line can only have one difficulty tag.");
                from = ParseTag(tag, file, lineNo);
                rest = rest[tag.Length..].Trim();
                if (rest.Length == 0) throw new DataError(file, lineNo, $"Nothing follows the tag \"{tag}\".");
            }

            var s = ParseLine(rest, timing, constants, file, lineNo);
            if (timing.Type != Timing.Kind.Same && s.Moment == null)
                throw new DataError(file, lineNo, $"{rest.Split(' ')[0]} can't have a time in front: it isn't a moment of its own.");
            current.Add(from > 0 ? s with { Easiest = from } : s);
        }
        EndSection();
        return result;
    }

    static readonly string[] Tags = Difficulty.Names.Select(n => n.ToLowerInvariant() + "+:").ToArray();

    /// <summary>"normal+:" and the like: the easiest difficulty (0 Easy to 4 Insane) the line is on.</summary>
    static int ParseTag(string tag, string file, int lineNo)
    {
        int d = Array.IndexOf(Tags, tag.ToLowerInvariant());
        if (d < 0)
            throw new DataError(file, lineNo, $"\"{tag}\" isn't a difficulty tag.{DataError.DidYouMean(tag.ToLowerInvariant(), Tags)} " +
                $"The tags are {string.Join(", ", Tags.Skip(1))}: the line is on that difficulty and the harder ones.");
        if (d == 0) throw new DataError(file, lineNo, "\"easy+:\" is every difficulty: leave the tag off.");
        return d;
    }

    /// <summary>"3.8", "2~4" or "0.9/0.9/0.7/0.6/0.5" (Easy to Insane), named for use in the lines below.</summary>
    static Num ParseConstant(string name, string value, string file, int lineNo)
    {
        var n = ParseNum(value.Replace(" ", ""), $"value of {name}", new Dictionary<string, Num>(), file, lineNo);
        return n is Num.ByDifficulty d ? d with { Name = name } : n;
    }

    /// <summary>Parses a level file with no sections.</summary>
    public static LevelScript Parse(string text, string file)
    {
        var sections = ParseSections(text, file);
        if (sections.Keys.Any(k => k != ""))
            throw new DataError(file, 0, "A level file can't have [sections]; only bosses.txt has them.");
        return sections.TryGetValue("", out var s) ? s : new LevelScript(file, "", new List<Statement>(), new Dictionary<string, Num>());
    }

    static string StripComment(string line)
    {
        int i = line.IndexOf('#');
        int j = line.IndexOf("//", StringComparison.Ordinal);
        if (j >= 0 && (i < 0 || j < i)) i = j;
        return i >= 0 ? line[..i] : line;
    }

    static Statement ParseLine(string line, Timing when, Dictionary<string, Num> constants, string file, int lineNo)
    {
        var tokens = CommaSpaces.Replace(line, ",").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var cmd = tokens[0].ToLowerInvariant();
        if (!Usage.ContainsKey(cmd))
            throw new DataError(file, lineNo, $"\"{tokens[0]}\" isn't a command.{DataError.DidYouMean(cmd, Commands)} " +
                $"The commands are: {string.Join(", ", Commands)}.");
        var p = new LineReader(tokens, file, lineNo, cmd, constants);

        Statement s = cmd switch
        {
            "missile" => new Statement.Missile(lineNo, p.Type(), when, p.Point("start point"), p.Points("point to fly to", 1, 3)),
            "laser" => new Statement.Laser(lineNo, p.Type(), when, p.Point("start point"), p.More ? p.Number("speed") : new Num.Const(0)),
            "suicide" => new Statement.Suicide(lineNo, p.Type(), when, p.Point("start point"), p.Point("target point")),
            "stealth" => new Statement.Stealth(lineNo, when, p.Point("start point"), p.Point("exit point"), p.More && p.Word("shield")),
            "ship" => new Statement.Ship(lineNo, p.Code(), when, p.Point("start point"), p.Steps()),
            "boss" => new Statement.Boss(lineNo, when, p.Int("boss number (1, 2 or 3)", 1, 3)),
            "mirror" => new Statement.Mirror(lineNo, when, p.More ? p.Back() : -1),
            "clone" => new Statement.Copy(lineNo, when, p.More ? p.Back() : -1),
            "offset" => new Statement.Offset(lineNo, when, p.Point("offset (x,y)"), p.More ? p.Back() : -1),
            "repeat" => new Statement.Repeat(lineNo, p.Number("gap between copies (seconds)"),
                p.More ? p.Number("number of times") : One,
                p.More ? p.Int("number of ships to repeat", 1, 1000) : 1,
                p.More && p.Word("mirrored")),
            "repeat_over" => new Statement.RepeatOver(lineNo, p.Number("seconds the copies are spread over"),
                p.Number("number of times"),
                p.More ? p.Int("number of ships to repeat", 1, 1000) : 1,
                p.More && p.Word("mirrored")),
            "insert" => new Statement.Insert(lineNo, p.Int("position", -10000, 10000)),
            "wait" => new Statement.Wait(lineNo, when),
            "jump" => new Statement.Jump(lineNo, when, p.Number("seconds to skip")),
            "loop" => new Statement.Loop(lineNo, when, p.More ? p.Int("event to loop from", -10000, 10000) : 0),
            _ => throw new InvalidOperationException(),
        };
        p.End();
        if (s is Statement.Laser { At: var at } && at.X.IsConstant && at.Y.IsConstant)
        {
            double x = at.X.Eval(null!, 0), y = at.Y.Eval(null!, 0);
            if (x != 0 && x != 12 && y != 0 && y != 12)
                throw new DataError(file, lineNo, $"A laser ship has to start on the edge of the grid (x 0 or 12, or y 0 or 12), not at {at}.");
        }
        if (s switch { Statement.Repeat r => r.Times, Statement.RepeatOver ro => ro.Times, _ => null } is { } n)
        {
            double[] times = n switch { Num.Const k => [k.Value], Num.ByDifficulty d => d.Values, _ => [] };
            foreach (double t in times)
                if (t < 0 || t > 1000 || t != Math.Floor(t))
                    throw new DataError(file, lineNo, $"{cmd}'s number of times must be a whole number from 0 to 1000, not {t}.\n  Usage: {Usage[cmd]}");
        }
        return s;
    }

    static readonly Num One = new Num.Const(1);

    static bool TryDouble(string s, out double v) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && double.IsFinite(v);

    /// <summary>A number, a range (3~6), a choice (0|12) or a header constant's name.</summary>
    static Num ParseNum(string s, string what, IReadOnlyDictionary<string, Num> constants, string file, int lineNo)
    {
        if (s.Length > 0 && (char.IsLetter(s[0]) || s[0] == '_'))
        {
            if (constants.TryGetValue(s, out var c)) return c;
            throw new DataError(file, lineNo, $"\"{s}\" isn't a number or a constant set in the header (expected the {what})." +
                (constants.Count > 0 ? $" The header has: {string.Join(", ", constants.Keys)}." : ""));
        }
        if (s.Contains('/'))
        {
            var parts = s.Split('/');
            var values = new double[parts.Length];
            if (parts.Length == 5 && parts.Select((p, k) => TryDouble(p, out values[k])).All(ok => ok)) return new Num.ByDifficulty(s, values);
            throw new DataError(file, lineNo, $"\"{s}\" isn't one number per difficulty for the {what}; write five, Easy to Insane, like 0.9/0.9/0.7/0.6/0.5.");
        }
        if (s.Contains('~'))
        {
            var parts = s.Split('~');
            if (parts.Length == 2 && TryDouble(parts[0], out var a) && TryDouble(parts[1], out var b)) return new Num.Range(a, b);
            throw new DataError(file, lineNo, $"\"{s}\" isn't a range for the {what}; write it like 3~6 (any number from 3 to 6).");
        }
        if (s.Contains('|'))
        {
            var parts = s.Split('|');
            if (parts.Length == 2 && TryDouble(parts[0], out var a) && TryDouble(parts[1], out var b)) return new Num.Either(a, b);
            throw new DataError(file, lineNo, $"\"{s}\" isn't a choice for the {what}; write it like 0|12 (0 or 12, at random).");
        }
        if (TryDouble(s, out var v)) return new Num.Const(v);
        throw new DataError(file, lineNo, $"\"{s}\" isn't a number (expected the {what}).");
    }

    /// <summary>Reads a line's words in order, with messages that say what was expected.</summary>
    sealed class LineReader(string[] tokens, string file, int line, string cmd, IReadOnlyDictionary<string, Num> constants)
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

        public Num Number(string what)
        {
            var s = Next(what);
            try
            {
                return ParseNum(s, what, constants, file, line);
            }
            catch (DataError e)
            {
                throw Err(e.Problem);
            }
        }

        public Point Point(string what)
        {
            var s = Next(what);
            var parts = s.Split(',');
            if (parts.Length != 2 || parts[0] == "" || parts[1] == "")
                throw Err($"\"{s}\" isn't a point (expected the {what}). Write points as x,y on the 12 x 12 grid, like 5,0.");
            return new Point(ParseNum(parts[0], what + " x", constants, file, line), ParseNum(parts[1], what + " y", constants, file, line));
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
        public int Back() => Int("ship to copy (-1 is the one just above)", -10000, -1);

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
                        steps.Add(new Step(Step.Kind.Halt, Halt: Number("halt time (seconds)"), Fire: FireFlag()));
                        break;
                    case "attack":
                        steps.Add(new Step(Step.Kind.Attack));
                        break;
                    default:
                        throw Err($"\"{kind}\" isn't a step. Steps are \"move X,Y\", \"halt SECONDS\" (either can end with \"fire\"), and \"attack\".");
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
