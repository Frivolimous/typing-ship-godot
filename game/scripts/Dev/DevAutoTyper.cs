using Godot;

namespace TypingShip;

// Dev aid (--autotype): the rules' bot (TypingShip.Core.AutoTyper) types at the selected WPM, once per tick after
// the game has moved. See AutoTyper for how it plays.
public partial class DevAutoTyper : Node
{
    public int BotWpm; // 0 = type at the game's WPM
    AutoTyper bot = null!;

    public override void _Ready() => bot = new AutoTyper(G.Model, BotWpm);

    public override void _PhysicsProcess(double delta) => bot.Step();
}
