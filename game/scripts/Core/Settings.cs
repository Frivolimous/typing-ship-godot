using Godot;
using FileAccess = Godot.FileAccess;

namespace TypingShip;

// The player's options (the options screen), kept apart from the save game: JSON in user://settings.json, written
// whenever one changes. They're about the presentation only, so they never touch the rules.
public static class Settings
{
    /// <summary>File name in user://, or null to keep them in memory (bot runs leave the player's file alone).</summary>
    public static string? Path = "user://settings.json";

    public static bool Fullscreen;
    /// <summary>0-1.</summary>
    public static float MusicVolume = 1, SoundVolume = 1;
    public static bool Glow = true, Shake = true, Flashes = true, Tutorials = true;

    public static void Load()
    {
        if (Path == null || !FileAccess.FileExists(Path)) return;
        if (Json.ParseString(FileAccess.GetFileAsString(Path)).Obj is not Godot.Collections.Dictionary d) return;
        bool B(string key, bool fallback) => d.TryGetValue(key, out var v) && v.VariantType == Variant.Type.Bool ? v.AsBool() : fallback;
        float F(string key, float fallback) => d.TryGetValue(key, out var v) && v.VariantType is Variant.Type.Float or Variant.Type.Int
            ? Mathf.Clamp((float)v.AsDouble(), 0, 1) : fallback;
        Fullscreen = B("fullscreen", Fullscreen);
        MusicVolume = F("music", MusicVolume);
        SoundVolume = F("sound", SoundVolume);
        Glow = B("glow", Glow);
        Shake = B("shake", Shake);
        Flashes = B("flashes", Flashes);
        Tutorials = B("tutorials", Tutorials);
    }

    public static void Save()
    {
        if (Path == null) return;
        var d = new Godot.Collections.Dictionary
        {
            ["fullscreen"] = Fullscreen, ["music"] = MusicVolume, ["sound"] = SoundVolume, ["glow"] = Glow, ["shake"] = Shake,
            ["flashes"] = Flashes, ["tutorials"] = Tutorials,
        };
        using var f = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning($"Can't write {Path}: {FileAccess.GetOpenError()}");
            return;
        }
        f.StoreString(Json.Stringify(d, "  "));
    }

    /// <summary>Puts the display and volume settings into effect (the others are read where they're used).</summary>
    public static void Apply()
    {
        if (DisplayServer.GetName() != "headless")
        {
            var mode = Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
            if (DisplayServer.WindowGetMode() != mode) DisplayServer.WindowSetMode(mode);
        }
        G.Sound.MusicVolume = MusicVolume;
        G.Sound.SoundVolume = SoundVolume;
    }
}
