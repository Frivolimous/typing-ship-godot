using TypingShip.Core.Data;
using TypingShip.Core.Levels;

namespace TypingShip.Core.Tests;

public class TuningTests
{
    static string Real => File.ReadAllText(Path.Combine(TestData.DataDir, "tuning.txt"));

    static DataError Fails(string text) => Assert.Throws<DataError>(() => Tuning.Parse(text));

    [Fact]
    public void Real_file_matches_the_original_numbers()
    {
        var t = Tuning.Parse(Real);
        var original = new Tuning(); // the defaults are the original game's numbers
        foreach (var f in typeof(Tuning).GetFields().Where(f => f.FieldType.IsPrimitive))
            Assert.True(Equals(f.GetValue(original), f.GetValue(t)), $"{f.Name}: {f.GetValue(t)}, originally {f.GetValue(original)}");
        Assert.Equal(original.Sizes, t.Sizes);
        Assert.Equal(original.DefencePoints, t.DefencePoints);
        Assert.Equal(original.HullHealth, t.HullHealth);
    }

    [Fact]
    public void Misspelt_setting_names_the_line_and_suggests()
    {
        var e = Fails(Real.Replace("turret range = 100", "turet range = 100"));
        Assert.Contains("turet range", e.Message);
        Assert.Contains("Did you mean \"turret range\"", e.Message);
        Assert.Equal(Real.Split('\n').ToList().FindIndex(l => l.StartsWith("turret range")) + 1, e.Line);
    }

    [Fact]
    public void Numbers_can_be_fractions()
    {
        Assert.Equal(100 / 3.0, Tuning.Parse(Real).HaltScale);
        Assert.Equal(2.5, Tuning.Parse(Real.Replace("stealth fade cps = 2.5", "stealth fade cps = 5 / 2")).StealthFadeCps);
        Assert.Contains("isn't a number", Fails(Real.Replace("stealth fade cps = 2.5", "stealth fade cps = 5/0")).Message);
    }

    [Fact]
    public void Missing_setting_is_an_error()
    {
        var e = Fails(Real.Replace("laser charge = 500", ""));
        Assert.Contains("[enemy weapons] laser charge", e.Message);
    }

    [Fact]
    public void Bad_number_is_an_error()
    {
        var e = Fails(Real.Replace("overflow = 30", "overflow = thirty"));
        Assert.Contains("\"thirty\" isn't a number", e.Message);
    }

    [Fact]
    public void Whole_numbers_must_be_whole()
    {
        var e = Fails(Real.Replace("overflow = 30", "overflow = 30.5"));
        Assert.Contains("overflow", e.Message);
    }

    [Fact]
    public void Duplicate_and_unknown_section_are_errors()
    {
        Assert.Contains("set twice", Fails(Real.Replace("turret range = 100", "turret range = 100\nturret range = 90")).Message);
        Assert.Contains("Did you mean \"bosses\"", Fails(Real.Replace("[bosses]", "[boses]")).Message);
    }

    [Fact]
    public void Ship_table_rows_need_every_field()
    {
        var e = Fails(Real.Replace("s = letters 4, points 0, speed 0", "s = letters 4, speed 0"));
        Assert.Contains("Missing points", e.Message);
    }

    [Fact]
    public void Hulls_must_use_known_sizes()
    {
        var e = Fails(Real.Replace("xm = 1", "xm = 1\nqm = 2"));
        Assert.Contains("\"qm\"", e.Message);
    }
}

