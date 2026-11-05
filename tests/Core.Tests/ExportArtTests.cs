using System.Text.RegularExpressions;

namespace TypingShip.Core.Tests;

/// <summary>
/// Exports carry the "custom" art set only: export_presets.cfg leaves out every other sprite but the "ai" bosses the
/// rules measure, so "custom" must have all the art that's left out.
/// </summary>
public class ExportArtTests
{
    static readonly string Presets = Path.Combine(TestData.RepoRoot, "game", "export_presets.cfg");

    static List<Regex[]> Filters() => File.ReadLines(Presets)
        .Where(l => l.StartsWith("exclude_filter=", StringComparison.Ordinal))
        .Select(l => l["exclude_filter=".Length..].Trim('"').Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(g => new Regex("^" + Regex.Escape(g).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase))
            .ToArray())
        .ToList();

    static string Res(string file) => Path.GetRelativePath(Path.Combine(TestData.RepoRoot, "game"), file).Replace(Path.DirectorySeparatorChar, '/');

    [Fact]
    public void Every_preset_leaves_out_all_but_the_custom_art_and_the_measured_bosses()
    {
        var measured = Boss.Arts.Append(Boss.HangarArt).Select(a => $"assets/sprites/{a}.png").ToHashSet();
        var filters = Filters();
        Assert.NotEmpty(filters);
        foreach (var dir in Directory.GetDirectories(TestData.SpritesDir).Where(d => Path.GetFileName(d) != "custom"))
            foreach (var png in Directory.GetFiles(dir, "*.png"))
            {
                string path = Res(png);
                foreach (var preset in filters)
                    Assert.True(preset.Any(f => f.IsMatch(path)) != measured.Contains(path),
                        measured.Contains(path) ? $"{path} is measured by the rules, so exports must keep it" : $"exports should leave out {path}");
            }
    }

    [Fact]
    public void Custom_has_every_sprite_the_export_leaves_out()
    {
        var measured = Boss.Arts.Append(Boss.HangarArt).Select(a => Path.GetFileName(a) + ".png").ToHashSet();
        var custom = Path.Combine(TestData.SpritesDir, "custom");
        foreach (var png in Directory.GetFiles(Path.Combine(TestData.SpritesDir, "ai"), "*.png"))
        {
            string name = Path.GetFileName(png);
            if (measured.Contains(name) && name.Contains("hangar")) continue; // only measured, never drawn as art
            Assert.True(File.Exists(Path.Combine(custom, name)), $"custom has no {name}, and exports have no ai/{name} to borrow");
        }
    }
}
