using Godot;

namespace TypingShip;

// Dev aid (--autotype): types like a player at the selected WPM, so levels can be played through unattended.
// Picks the most urgent word: incoming missiles, then a badly damaged hull, then the ship closest to the player.
public partial class DevAutoTyper : Node
{
    public int BotWpm; // 0 = type at the game's WPM
    float charge;
    string? word;
    GameObject? owner;
    int typed;

    public override void _PhysicsProcess(double delta)
    {
        var m = G.Model;
        if (!m.Running || !m.CanType) return;
        charge += (BotWpm > 0 ? BotWpm : m.Wpm) * 5f / 60f / G.TickRate;
        while (charge >= 1)
        {
            charge -= 1;
            if (word == null || owner is GameSprite { Removed: true } || owner?.TextObject?.Text != word || typed >= word.Length) Pick();
            if (word == null) return;
            GetParent().GetNode<KeyboardControl>("Keyboard").TypeLetter(word[typed++].ToString());
        }
    }

    void Pick()
    {
        var m = G.Model;
        var player = m.PShip.Position;
        GameObject? best = null;
        float bestScore = float.MaxValue;
        foreach (var o in m.GObjects)
        {
            if (o.TextObject == null || o.Modulate.A < 0.5f) continue;
            if (o.Position.X < 25 || o.Position.X > 575 || o.Position.Y < 25) continue; // still off screen
            float score = o.Position.DistanceTo(player) - (o.Priority >= 3 ? 1000 : 0);
            if (score < bestScore)
            {
                bestScore = score;
                best = o;
            }
        }
        if (m.Hull.TextObject != null && m.PShip.Health <= 2 && bestScore > -500) best = m.Hull;
        owner = best;
        word = best?.TextObject?.Text;
        typed = 0;
    }
}
