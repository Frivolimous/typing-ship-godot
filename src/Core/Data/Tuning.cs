using System.Globalization;

namespace TypingShip.Core.Data;

/// <summary>
/// The game's tuning numbers, read from data/tuning.txt. Every number in the file must be there, once; anything
/// the loader doesn't know is an error, so a typo can't silently do nothing. The defaults below are the original
/// game's numbers, used only by tests that build a Tuning by hand.
/// </summary>
public sealed class Tuning
{
    // [enemy ships]
    public int BasePoints = 5;
    public float BaseSpeed = 0.5f;
    /// <summary>A ship's speed (BaseSpeed plus its letters') is multiplied by cps x this.</summary>
    public double SpeedPerCps = 0.12;
    public double PaceAboveHard = 0.5;

    /// <summary>
    /// The speed things move and count down at (ships, missiles, halts, laser charges), as a cps: the typing speed up to
    /// Hard, and above it Hard x (cps / Hard)^"pace above hard", so the hardest difficulties bring more ships rather than
    /// ships too fast to read.
    /// </summary>
    public double Pace(double cps) => cps <= Difficulty.Hard ? cps : Difficulty.Hard * Math.Pow(cps / Difficulty.Hard, PaceAboveHard);
    /// <summary>Size letter (1st of the ship code) to letters, points and speed.</summary>
    public Dictionary<char, SizeStats> Sizes = new()
    {
        ['s'] = new(4, 0, 0), ['m'] = new(6, 1, -0.2f), ['l'] = new(8, 3, -0.25f), ['x'] = new(7, 3, -0.1f), ['n'] = new(7, 0, 0),
    };
    public int LaserExtraLetters = 1, LaserPoints = 1;
    public float LaserSpeed = -0.1f;
    public int SuicideLetters = 3, SuicidePoints = -3;
    public float SuicideSpeed = 0.2f, MineSpeed = -0.2f;
    /// <summary>Defence letter (3rd of the ship code) to extra points.</summary>
    public Dictionary<char, int> DefencePoints = new() { ['n'] = 0, ['s'] = 2, ['t'] = 2, ['b'] = 5 };
    /// <summary>Hull (first two letters of the ship code) to health: how many player missiles it takes.</summary>
    public Dictionary<string, int> HullHealth = new()
    {
        ["sm"] = 1, ["sl"] = 1, ["ss"] = 1, ["mm"] = 2, ["ml"] = 2, ["ms"] = 1, ["lm"] = 4, ["ll"] = 4, ["xm"] = 1, ["nl"] = 1,
    };

    // [enemy weapons]
    public double EnemyMissileSpeed = 0.096;
    public int EnemyMissilePoints = 1;
    public double FireRateCps = 12.5;
    public float FireRateMin = 0.3f, FireRateMax = 1;
    public float FireRateHealth2 = 1.5f, FireRateHealth3 = 2;
    public double LaserCharge = 500;
    public float TurretRange = 100;
    public double StealthFadeCps = 2.5;

    // [player]
    public int PlayerHealth = 5;
    public int TypingBar = 20;
    public int HealWordShortest = 8, HealWordLongest = 9;
    public int OverflowTicks = 30;
    public double PlayerMissileSpeed = 1.2;
    public float MissileHitRange = 30;
    public int EmpCharge = 5;

    // [level scripts]
    public float Jitter = 10;
    public float ArriveDistance = 20;
    public double HaltScale = 100 / 3.0;
    public double EndWait = 2.77, CopyGap = 0.55; // seconds

    // [bosses]
    public int BossHealth = 3, BossPoints = 100;
    public int ScannerRing = 300, ScannerStep = 30, ScannerWord = 3, ScannerFinalWord = 10;
    public int BossFirstAttack = 6;
    public int Boss0Attack = 48, Boss1Attack = 23, Boss1AttackPerHealth = 3, Boss2Attack = 70;
    public double Boss2BeamTime = 25;
    public int CounterWord = 11;

    // [endless]
    public double EndlessFirstWave = 2.5, EndlessAnnounce = 1.5, EndlessStartOffset = -1, EndlessBudgetPerWave = 1;
    public double EndlessWavesPerPattern = 3, EndlessPatternDelay = 3, EndlessPatternDelayPerWave = 0.1, EndlessMinPatternDelay = 1;
    public double EndlessPatternDelayPerPattern = 0, EndlessPatternsPerMissilePattern = 3;
    public double EndlessCpsPerWave = 0.125;

    public readonly record struct SizeStats(int Letters, int Points, float Speed);

    // ---------------------------------------------------------------------------------------------------------
    // Loading

    /// <summary>One "name = number" entry the file must have: its section, name, and how to store it.</summary>
    sealed record Entry(string Section, string Key, Action<Tuning, string> Set);

    static Entry I(string s, string k, Action<Tuning, int> set) => new(s, k, (t, v) => set(t, int.Parse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)));
    static Entry F(string s, string k, Action<Tuning, float> set) => new(s, k, (t, v) => set(t, float.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture)));
    static Entry D(string s, string k, Action<Tuning, double> set) => new(s, k, (t, v) => set(t, ToDouble(v)));

    const string Ships = "enemy ships", Size = "size", Weapon = "weapon", Defence = "defence", Health = "hull health",
        EnemyWeapons = "enemy weapons", Player = "player", Scripts = "level scripts", Bosses = "bosses", Endless = "endless";

    static readonly Entry[] Entries =
    {
        I(Ships, "points", (t, v) => t.BasePoints = v),
        F(Ships, "speed", (t, v) => t.BaseSpeed = v),
        D(Ships, "speed per cps", (t, v) => t.SpeedPerCps = v),
        D(Ships, "pace above hard", (t, v) => t.PaceAboveHard = v),

        I(Weapon, "laser extra letters", (t, v) => t.LaserExtraLetters = v),
        I(Weapon, "laser points", (t, v) => t.LaserPoints = v),
        F(Weapon, "laser speed", (t, v) => t.LaserSpeed = v),
        I(Weapon, "suicide letters", (t, v) => t.SuicideLetters = v),
        I(Weapon, "suicide points", (t, v) => t.SuicidePoints = v),
        F(Weapon, "suicide speed", (t, v) => t.SuicideSpeed = v),
        F(Weapon, "mine speed", (t, v) => t.MineSpeed = v),

        D(EnemyWeapons, "missile speed", (t, v) => t.EnemyMissileSpeed = v),
        I(EnemyWeapons, "missile points", (t, v) => t.EnemyMissilePoints = v),
        D(EnemyWeapons, "fire rate cps", (t, v) => t.FireRateCps = v),
        F(EnemyWeapons, "fire rate min", (t, v) => t.FireRateMin = v),
        F(EnemyWeapons, "fire rate max", (t, v) => t.FireRateMax = v),
        F(EnemyWeapons, "fire rate health 2", (t, v) => t.FireRateHealth2 = v),
        F(EnemyWeapons, "fire rate health 3", (t, v) => t.FireRateHealth3 = v),
        D(EnemyWeapons, "laser charge", (t, v) => t.LaserCharge = v),
        F(EnemyWeapons, "turret range", (t, v) => t.TurretRange = v),
        D(EnemyWeapons, "stealth fade cps", (t, v) => t.StealthFadeCps = v),

        I(Player, "health", (t, v) => t.PlayerHealth = v),
        I(Player, "typing bar", (t, v) => t.TypingBar = v),
        I(Player, "heal word shortest", (t, v) => t.HealWordShortest = v),
        I(Player, "heal word longest", (t, v) => t.HealWordLongest = v),
        I(Player, "overflow", (t, v) => t.OverflowTicks = v),
        D(Player, "missile speed", (t, v) => t.PlayerMissileSpeed = v),
        F(Player, "missile hit range", (t, v) => t.MissileHitRange = v),
        I(Player, "emp charge", (t, v) => t.EmpCharge = v),

        F(Scripts, "jitter", (t, v) => t.Jitter = v),
        F(Scripts, "arrive distance", (t, v) => t.ArriveDistance = v),
        D(Scripts, "halt scale", (t, v) => t.HaltScale = v),
        D(Scripts, "end wait", (t, v) => t.EndWait = v),
        D(Scripts, "copy gap", (t, v) => t.CopyGap = v),

        I(Bosses, "health", (t, v) => t.BossHealth = v),
        I(Bosses, "points", (t, v) => t.BossPoints = v),
        I(Bosses, "scanner ring", (t, v) => t.ScannerRing = v),
        I(Bosses, "scanner step", (t, v) => t.ScannerStep = v),
        I(Bosses, "scanner word", (t, v) => t.ScannerWord = v),
        I(Bosses, "scanner final word", (t, v) => t.ScannerFinalWord = v),
        I(Bosses, "first attack", (t, v) => t.BossFirstAttack = v),
        I(Bosses, "boss 1 attack", (t, v) => t.Boss0Attack = v),
        I(Bosses, "boss 2 attack", (t, v) => t.Boss1Attack = v),
        I(Bosses, "boss 2 attack per health", (t, v) => t.Boss1AttackPerHealth = v),
        I(Bosses, "boss 3 attack", (t, v) => t.Boss2Attack = v),
        D(Bosses, "boss 3 beam", (t, v) => t.Boss2BeamTime = v),
        I(Bosses, "counter word", (t, v) => t.CounterWord = v),

        D(Endless, "first wave", (t, v) => t.EndlessFirstWave = v),
        D(Endless, "announce", (t, v) => t.EndlessAnnounce = v),
        D(Endless, "start offset", (t, v) => t.EndlessStartOffset = v),
        D(Endless, "budget per wave", (t, v) => t.EndlessBudgetPerWave = v),
        D(Endless, "waves per pattern", (t, v) => t.EndlessWavesPerPattern = Math.Max(1, v)),
        D(Endless, "pattern delay", (t, v) => t.EndlessPatternDelay = v),
        D(Endless, "pattern delay per wave", (t, v) => t.EndlessPatternDelayPerWave = v),
        D(Endless, "min pattern delay", (t, v) => t.EndlessMinPatternDelay = v),
        D(Endless, "pattern delay per pattern", (t, v) => t.EndlessPatternDelayPerPattern = v),
        D(Endless, "cps per wave", (t, v) => t.EndlessCpsPerWave = v),
        D(Endless, "patterns per missile pattern", (t, v) => t.EndlessPatternsPerMissilePattern = Math.Max(1, v)),
    };

    static readonly string[] Sections = { Ships, Size, Weapon, Defence, Health, EnemyWeapons, Player, Scripts, Bosses, Endless };

    /// <summary>Reads data/tuning.txt. Throws a <see cref="DataError"/> naming the line for any mistake.</summary>
    public static Tuning Parse(string text, string fileName = "tuning.txt")
    {
        var t = new Tuning { Sizes = new(), DefencePoints = new(), HullHealth = new() };
        var seen = new HashSet<(string, string)>();
        string? section = null;
        int lineNo = 0;
        DataError Err(string problem) => new(fileName, lineNo, problem);

        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = StripComment(raw).Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('['))
            {
                if (!line.EndsWith(']')) throw Err($"A section name needs a closing ]: \"{line}\".");
                section = line[1..^1].Trim().ToLowerInvariant();
                if (!Sections.Contains(section))
                    throw Err($"There's no section called [{section}].{DataError.DidYouMean(section, Sections)}");
                continue;
            }
            if (section == null) throw Err("Put a [section] line before the first number.");
            int eq = line.IndexOf('=');
            if (eq < 0) throw Err($"Expected \"name = value\", got \"{line}\".");
            var key = string.Join(' ', line[..eq].Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            var value = line[(eq + 1)..].Trim();
            if (value.Length == 0) throw Err($"\"{key}\" has no value after the =.");
            if (!seen.Add((section, key))) throw Err($"\"{key}\" is set twice in [{section}].");

            try
            {
                switch (section)
                {
                    case Size:
                        if (key.Length != 1 || !char.IsLetter(key[0])) throw Err($"A size is one letter (the 1st of a ship code), not \"{key}\".");
                        var f = Fields(value, Err, "letters", "points", "speed");
                        t.Sizes[key[0]] = new SizeStats(ToInt(f["letters"]), ToInt(f["points"]), ToFloat(f["speed"]));
                        break;
                    case Defence:
                        if (key.Length != 1 || !char.IsLetter(key[0])) throw Err($"A defence is one letter (the 3rd of a ship code), not \"{key}\".");
                        t.DefencePoints[key[0]] = ToInt(Fields(value, Err, "points")["points"]);
                        break;
                    case Health:
                        if (key.Length != 2 || !key.All(char.IsLetter)) throw Err($"A hull is two letters (the start of a ship code), not \"{key}\".");
                        t.HullHealth[key] = ToInt(value);
                        break;
                    default:
                        var entry = Entries.FirstOrDefault(e => e.Section == section && e.Key == key);
                        if (entry == null)
                            throw Err($"[{section}] has no setting called \"{key}\"." +
                                DataError.DidYouMean(key, Entries.Where(e => e.Section == section).Select(e => e.Key)));
                        entry.Set(t, value);
                        break;
                }
            }
            catch (FormatException)
            {
                throw Err($"\"{value}\" isn't a number (for \"{key}\").");
            }
            catch (OverflowException)
            {
                throw Err($"\"{value}\" is out of range (for \"{key}\").");
            }
        }

        lineNo = 0;
        var missing = Entries.Where(e => !seen.Contains((e.Section, e.Key))).Select(e => $"[{e.Section}] {e.Key}").ToList();
        if (missing.Count > 0) throw Err("Missing settings: " + string.Join(", ", missing) + ".");
        foreach (var hull in t.HullHealth.Keys)
        {
            if (!t.Sizes.ContainsKey(hull[0])) throw Err($"Hull \"{hull}\" starts with size \"{hull[0]}\", which [size] doesn't have.");
            if (hull[1] is not ('m' or 'l' or 's')) throw Err($"Hull \"{hull}\": the weapon letter must be m (missile), l (laser) or s (suicide).");
        }
        if (t.HealWordShortest > t.HealWordLongest) throw Err("[player] heal word shortest is more than heal word longest.");
        if (t.HealWordShortest < 3 || t.HealWordLongest > 10) throw Err("[player] heal words must be 3 to 10 letters.");
        return t;
    }

    static string StripComment(string line)
    {
        int i = line.IndexOf('#');
        return i >= 0 ? line[..i] : line;
    }

    /// <summary>"letters 4, points 0, speed -0.2" with exactly the given field names.</summary>
    static Dictionary<string, string> Fields(string value, Func<string, DataError> err, params string[] names)
    {
        var result = new Dictionary<string, string>();
        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            int sp = part.LastIndexOf(' ');
            if (sp < 0) throw err($"Expected \"name number\" in \"{part}\" (like \"{names[0]} 4\").");
            var name = string.Join(' ', part[..sp].Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            if (!names.Contains(name)) throw err($"Unknown field \"{name}\"; this line takes {string.Join(", ", names)}.{DataError.DidYouMean(name, names)}");
            if (!result.TryAdd(name, part[(sp + 1)..])) throw err($"\"{name}\" is given twice.");
        }
        var missing = names.Where(n => !result.ContainsKey(n)).ToList();
        if (missing.Count > 0) throw err($"Missing {string.Join(", ", missing)} (this line needs {string.Join(", ", names)}).");
        return result;
    }

    static int ToInt(string s) => int.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    static float ToFloat(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>A number, or a fraction such as "100/3" for one that has no exact decimal.</summary>
    static double ToDouble(string s)
    {
        int slash = s.IndexOf('/');
        if (slash < 0) return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        double num = ToDouble(s[..slash].Trim()), den = ToDouble(s[(slash + 1)..].Trim());
        if (den == 0) throw new FormatException();
        return num / den;
    }
}
