using Godot;
using FileAccess = Godot.FileAccess;

namespace TypingShip;

// The licences of everything the game is built on, reached from the credits: Godot (the MIT licence its exports
// must carry, and the third-party code inside the engine, both from the engine itself), the .NET runtime that ships
// with a C# export, Steamworks.NET and the fonts. The texts other than Godot's are files in assets/licenses and
// assets/fonts (export_presets.cfg's include filter puts them in exports). The arrow keys, Page Up/Down, Home, End
// and the mouse wheel scroll; Escape, Enter, Backspace or BACK return to the credits.
public partial class LicensesUI : Screen
{
    static readonly (string Heading, string Path)[] Files =
    {
        (".NET RUNTIME", "res://assets/licenses/dotnet-LICENSE.txt"),
        (".NET RUNTIME: THIRD-PARTY NOTICES", "res://assets/licenses/dotnet-THIRD-PARTY-NOTICES.txt"),
        ("STEAMWORKS.NET", "res://assets/licenses/Steamworks.NET-LICENSE.txt"),
        ("ORBITRON FONT", "res://assets/fonts/Orbitron-OFL.txt"),
        ("JETBRAINS MONO FONT", "res://assets/fonts/JetBrainsMono-OFL.txt"),
    };

    static readonly Rect2 Box = new(18, 50, 514, 446);
    readonly RichTextLabel text;
    bool filled;

    public LicensesUI()
    {
        Kit.Label(this, "LICENSES", Kit.Title(18), 18, 14);
        var box = Box;
        text = new RichTextLabel
        {
            Position = box.Position + new Vector2(12, 10),
            Size = box.Size - new Vector2(18, 20),
            ScrollActive = true,
            ScrollFollowing = false,
            SelectionEnabled = false,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
        };
        text.AddThemeFontOverride("normal_font", Fonts.Word);
        text.AddThemeFontSizeOverride("normal_font_size", 9);
        text.AddThemeColorOverride("default_color", new Color(Kit.Ink, 0.85f));
        AddChild(text);
        var back = new TextButton("BACK", Back, 12, boxed: true, width: 90) { Position = new Vector2(18, 506) };
        AddChild(back);
    }

    public override void _Draw() => Kit.Panel(this, Box);

    public override void NavIn()
    {
        // Built the first time it's shown: a few hundred KB of text the menus otherwise never need.
        if (!filled) Fill();
        filled = true;
        text.ScrollToLine(0);
    }

    void Heading(string s)
    {
        text.PushFont(Fonts.HudBold);
        text.PushFontSize(11);
        text.PushColor(Kit.Gold);
        text.AddText(s);
        text.Pop();
        text.Pop();
        text.Pop();
        text.AddText("\n\n");
    }

    void Fill()
    {
        Heading("MILLENNIUM TYPER IS MADE WITH THE GODOT ENGINE");
        text.AddText("https://godotengine.org\n\n" + Engine.GetLicenseText().Trim() + "\n\n\n");

        Heading("THIRD-PARTY COMPONENTS IN THE GODOT ENGINE");
        foreach (var component in Engine.GetCopyrightInfo())
        {
            text.AddText(component["name"].AsString() + "\n");
            foreach (var part in component["parts"].AsGodotArray<Godot.Collections.Dictionary>())
            {
                foreach (var line in part["copyright"].AsStringArray()) text.AddText("  (c) " + line + "\n");
                text.AddText("  License: " + part["license"].AsString() + "\n");
            }
            text.AddText("\n");
        }
        text.AddText("\n");

        Heading("LICENSE TEXTS FOR THE GODOT ENGINE'S THIRD-PARTY COMPONENTS");
        var licences = Engine.GetLicenseInfo();
        foreach (var name in licences.Keys)
            text.AddText(name.AsString() + "\n\n" + licences[name].AsString().Trim() + "\n\n\n");

        foreach (var (heading, path) in Files)
        {
            Heading(heading);
            string body = FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path).Trim() : $"(missing: {path})";
            text.AddText(body.Replace("\r\n", "\n") + "\n\n\n");
        }
    }

    public override bool OnKey(InputEventKey key)
    {
        var bar = text.GetVScrollBar();
        double line = text.GetThemeFontSize("normal_font_size") * 1.4, page = text.Size.Y - 2 * line;
        switch (key.Keycode)
        {
            case Key.Up: bar.Value -= 3 * line; return true;
            case Key.Down: bar.Value += 3 * line; return true;
            case Key.Pageup: bar.Value -= page; return true;
            case Key.Pagedown or Key.Space: bar.Value += page; return true;
            case Key.Home: bar.Value = 0; return true;
            case Key.End: bar.Value = bar.MaxValue; return true;
            case Key.Escape or Key.Enter or Key.KpEnter or Key.Backspace: Back(); return true;
        }
        return false;
    }

    static void Back() => G.Shell.Go(G.Shell.Credits, Shell.Dark);
}
