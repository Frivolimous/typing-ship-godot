using System.Reflection;
using Godot;
using TypingShip.Core.Data;

namespace TypingShip;

// Finds the data files (words, tuning, levels). In a checkout they're read straight from the repo's data/ folder,
// so a level edited while the game runs can be reloaded (F5 with --edit). An exported game carries them inside
// its assembly instead.
public static class DataFiles
{
    /// <summary>The repo's data/ folder next to the Godot project, or null when there isn't one (an export).</summary>
    public static string? Folder { get; } = FindFolder();

    static string? FindFolder()
    {
        var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"));
        return File.Exists(System.IO.Path.Combine(dir, "tuning.txt")) ? dir : null;
    }

    public static DataSource Source => Folder != null ? DataSource.FromDirectory(Folder) : DataSource.FromFunc(ReadEmbedded);

    static string? ReadEmbedded(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        // RecursiveDir in the .csproj gives "data/levels\level00.txt" on Windows builds.
        var resource = asm.GetManifestResourceNames().FirstOrDefault(r => r.Replace('\\', '/') == "data/" + name);
        if (resource == null) return null;
        using var stream = asm.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Loads and checks everything; a mistake throws a DataError naming the file and line.</summary>
    public static GameData Load() => GameData.Load(Source);
}
