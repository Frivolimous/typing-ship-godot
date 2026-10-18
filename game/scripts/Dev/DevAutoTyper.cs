using Godot;

namespace TypingShip;

// Dev aid (--autotype, or F11 with the editing keys): the rules' bot (TypingShip.Core.AutoTyper) types at the
// selected speed, once per tick after the game has moved. See AutoTyper for how it plays. F12 switches it between the
// perfect typist and the realistic one (the pacing report's: 80% of the speed, 3.5% mistakes).
public partial class DevAutoTyper : Node
{
    public double BotCps; // characters per second; 0 = type at the game's speed
    /// <summary>Off: the bot stays put (F11 switches it), and you type yourself.</summary>
    public bool On = true;
    /// <summary>Which bot types: the perfect one, or the realistic one. Kept across bots, so F12 works before F11.</summary>
    public static bool Realistic;
    AutoTyper bot = null!;

    public DevAutoTyper() => Name = "AutoTyper";

    public override void _Ready() => bot = new AutoTyper(G.Model, BotCps, errorSeed: (ulong)Random.Shared.NextInt64());

    public override void _PhysicsProcess(double delta)
    {
        if (!On) return;
        // The realistic bot follows the difficulty (it can change between levels); the perfect one keeps --botwpm.
        bot.BotCps = Realistic ? G.Model.Cps * Pacing.RealisticSpeed : BotCps;
        bot.ErrorRate = Realistic ? Pacing.RealisticErrors : 0;
        bot.Step();
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

    /// <summary>"perfect" or "realistic, 80% speed, 3.5% mistakes", for the editor's banner and messages.</summary>
    public static string Mode => Realistic
        ? $"realistic, {Pacing.RealisticSpeed * 100:0}% speed, {Pacing.RealisticErrors * 100:0.#}% mistakes"
        : "perfect";
}
