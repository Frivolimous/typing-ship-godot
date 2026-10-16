using Godot;

namespace TypingShip;

// Dev aid (--autotype, or F11 with the editing keys): the rules' bot (TypingShip.Core.AutoTyper) types at the
// selected speed, once per tick after the game has moved. See AutoTyper for how it plays.
public partial class DevAutoTyper : Node
{
    public double BotCps; // characters per second; 0 = type at the game's speed
    /// <summary>Off: the bot stays put (F11 switches it), and you type yourself.</summary>
    public bool On = true;
    AutoTyper bot = null!;

    public DevAutoTyper() => Name = "AutoTyper";

    public override void _Ready() => bot = new AutoTyper(G.Model, BotCps);

    public override void _PhysicsProcess(double delta)
    {
        if (On) bot.Step();
    }

    /// <summary>Switches the bot on or off, adding it (at the game's speed) the first time. Returns whether it's on.</summary>
    public static bool Toggle(Node main)
    {
        if (main.GetNodeOrNull<DevAutoTyper>("AutoTyper") is { } existing) return existing.On = !existing.On;
        main.AddChild(new DevAutoTyper());
        G.Model.TutOff(); // tutorial pop-ups would wait for Space
        return true;
    }

    public static bool IsOn(Node main) => main.GetNodeOrNull<DevAutoTyper>("AutoTyper") is { On: true };
}
