using System.Reflection;

namespace TypingShip;

// Which build this is, for playtesters to quote: the version and the commit it was built from (the .NET SDK puts
// the commit in the assembly's informational version, "0.1.0+6a4c804...").
public static class BuildInfo
{
    /// <summary>The version and commit as built, e.g. "0.1.0+6a4c804...".</summary>
    public static readonly string Full =
        typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

    /// <summary>For the title screen: "V0.1.0 · 6A4C804".</summary>
    public static string Short
    {
        get
        {
            var parts = Full.Split('+', 2);
            return "V" + parts[0] + (parts.Length > 1 ? " · " + parts[1][..Math.Min(7, parts[1].Length)] : "");
        }
    }
}
