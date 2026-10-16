using System.Xml.Linq;

namespace TypingShip.Core.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Core_has_no_Godot_references()
    {
        var core = typeof(Game).Assembly;
        Assert.DoesNotContain(core.GetReferencedAssemblies(), a => a.Name!.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));

        var project = XDocument.Load(Path.Combine(TestData.RepoRoot, "src", "Core", "TypingShip.Core.csproj"));
        var refs = project.Descendants()
            .Where(e => e.Name.LocalName is "PackageReference" or "ProjectReference" or "Reference")
            .Select(e => (string?)e.Attribute("Include") ?? "")
            .Append((string?)project.Root!.Attribute("Sdk") ?? "");
        Assert.DoesNotContain(refs, r => r.Contains("Godot", StringComparison.OrdinalIgnoreCase));
    }
}

public class RngTests
{
    // RandomNumberGenerator.randf() bit patterns from Godot 4.7.2 (a GDScript run), seeds 1, 42 and 123456789.
    [Theory]
    [InlineData(1UL, new uint[] { 1051245560, 1049468375, 1057129186, 1061025058, 1057365087, 1027759798, 1059868722, 1060901844 })]
    [InlineData(42UL, new uint[] { 1039297554, 1059632729, 1050192881, 1025113858, 1061325116, 1042545562, 1064301760, 1037008156 })]
    [InlineData(123456789UL, new uint[] { 1050752200, 1048629768, 1062515739, 1063456638, 1047999266, 1062055499, 1059451142, 1036012058 })]
    public void Matches_Godots_RandomNumberGenerator(ulong seed, uint[] expected)
    {
        var rng = new Rng(seed);
        foreach (var bits in expected)
            Assert.Equal(bits, BitConverter.SingleToUInt32Bits(rng.Randf()));
    }

    [Fact]
    public void Rand_is_below_one()
    {
        var rng = new Rng(7);
        for (int i = 0; i < 100_000; i++) Assert.InRange(rng.Rand(), 0f, 0.99999994f);
    }
}

public class ArtMaskTests
{
    [Fact]
    public void Reads_boss_art_from_png()
    {
        var boss = TestData.Art["ai/boss0"];
        Assert.Equal(938, boss.Width);
        Assert.Equal(494, boss.Height);
        Assert.False(boss.Opaque(0, 0)); // a transparent corner
        Assert.True(boss.Opaque(boss.Width / 2f, boss.Height / 2f)); // the hull in the middle
        Assert.False(boss.Opaque(-1, 5));
        Assert.False(boss.Opaque(boss.Width, 5));
    }

    [Fact]
    public void Random_pixel_is_always_opaque()
    {
        var mask = TestData.Art["ai/boss1"];
        var rng = new Rng(3);
        for (int i = 0; i < 200; i++)
        {
            var p = mask.RandomOpaquePixel(rng);
            Assert.True(mask.Opaque(p.X, p.Y));
        }
    }
}

public class WordListTests
{
    [Fact]
    public void Groups_words_by_length_from_three_letters()
    {
        var words = new WordList("cat dog\nfish bird\n", new Rng(1));
        Assert.Equal(new[] { "cat", "dog" }, words.Words(3));
        Assert.Equal(new[] { "fish", "bird" }, words.Words(4));
    }

    [Fact]
    public void Shift_never_takes_one_of_the_last_five()
    {
        var words = new WordList(string.Join(' ', Enumerable.Range(0, 10).Select(i => $"w{i:00}")), new Rng(5));
        var last5 = words.Words(3).Skip(5).ToList();
        for (int i = 0; i < 5; i++) Assert.DoesNotContain(words.Shift(3), last5);
    }

    [Fact]
    public void Pushed_words_go_to_the_back_of_their_length()
    {
        var words = new WordList("aaa bbb", new Rng(1));
        words.Push("zzz");
        words.Push("longerthanten");
        Assert.Equal("zzz", words.Words(3)[^1]);
        Assert.Equal("longerthanten", words.Words(10)[^1]);
    }
}

public class ScrambleTests
{
    [Fact]
    public void Scrambles_into_a_non_word_with_the_same_letters()
    {
        var words = new WordList(File.ReadAllText(Path.Combine(TestData.DataDir, "words.txt")), new Rng(4));
        for (int i = 0; i < 20; i++)
        {
            var s = new Scramble(words, new Rng((ulong)i), 5, swaps: 2);
            Assert.Equal(s.Word.OrderBy(c => c), s.Text.OrderBy(c => c));
            Assert.DoesNotContain(s.Text, s.PArray);
        }
    }
}
