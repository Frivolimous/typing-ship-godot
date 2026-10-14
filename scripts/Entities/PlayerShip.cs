using Godot;

namespace TypingShip;

public partial class PlayerShip : GameSprite
{
    public const int MaxHealth = 5;

    public bool Injured;
    public readonly ShieldView ShieldV = new();
    public readonly Charge Charge = new(0, -20, 5, ActionControl.Emp);

    public void Setup()
    {
        SpriteScale = 1;
        ViewSource = Sprites.Player;
        Init(G.ViewWidth / 2, G.ViewHeight - 100, -Mathf.Pi / 2, 0, 0);
        ShieldV.SetSize(100, 100);
    }

    public void AddShield()
    {
        ShieldV.Add(this);
        ShieldOn = true;
    }

    public void RemoveShield()
    {
        if (!ShieldOn) return;
        ShieldV.Remove();
        ShieldOn = false;
    }
}
