using Godot;

namespace TypingShip;

// In-game typing: letters build up the typed text; whenever it ends with a word on screen, that word fires.
public partial class KeyboardControl : Node
{
    public const string MuteWord = "mute", UnmuteWord = "unmute", SupermanWord = "superman", KamikazeWord = "kamikaze",
        ChameleonWord = "chameleon", PauseWord = "pause", UnpauseWord = "unpause";

    GameModel M => G.Model;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key) return;

        if (key.Keycode == Key.Backspace)
        {
            if (M.Running && M.CanType)
            {
                G.UI.RemoveLetter();
                G.Sound.Sound(SoundControl.Type);
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Echo) return;

        // Escape stands in for the old on-screen pause button.
        if (key.Keycode == Key.Escape && M.CanType && !G.Ctrl.LevelEnded)
        {
            G.Ctrl.PauseGame(M.Running);
            GetViewport().SetInputAsHandled();
            return;
        }

        char c = char.ToLowerInvariant((char)key.Unicode);
        if (c < 'a' || c > 'z') return;
        GetViewport().SetInputAsHandled();
        if (M.Running && M.CanType) TypeLetter(c.ToString());
    }

    public void TypeLetter(string letter)
    {
        G.UI.AddLetter(letter);
        G.Sound.Sound(SoundControl.Type);

        {
            for (int i = 0; i < M.GObjects.Count; i++)
            {
                var o = M.GObjects[i];
                if (TestWord(o))
                {
                    if (o.ShieldOn) G.Action.ShootEmp(M.PShip, o);
                    else
                        switch (o.KillBy)
                        {
                            case ActionControl.Missile: G.Action.ShootMissile(M.PShip, o); break;
                            case ActionControl.Laser: G.Action.ShootLaser(M.PShip, o); break;
                            default: throw new InvalidOperationException($"Invalid kill type: {o.KillBy}");
                        }
                    return;
                }
                if (o.Turret != null && TestWord(o.Turret))
                {
                    G.Action.ShootLaser(M.PShip, o.Turret);
                    return;
                }
            }

            foreach (var overflow in M.GOverflow)
            {
                if (TestWord(overflow.Word))
                {
                    overflow.Remove();
                    return;
                }
            }

            if (TestWord(M.Hull))
            {
                G.Action.Heal();
                return;
            }
        }

        if (TestWord(UnmuteWord)) G.Sound.Mute = false;
        else if (TestWord(MuteWord)) G.Sound.Mute = true;
        else if (TestWord(PauseWord)) G.Ctrl.PauseGame(true);
        else if (TestWord(KamikazeWord))
        {
            while (M.GObjects.Count > 0) G.Action.HitTarget(M.GObjects[0], ActionControl.Instant);
            G.Ctrl.PlayerDeath();
        }
    }

    bool TestWord(GameObject v)
    {
        if (v.TextObject == null || !TestWord(v.TextObject.Text)) return false;
        v.RecycleWord();
        return true;
    }

    /// <summary>Does the typed text end with this word? If so, consume it.</summary>
    static bool TestWord(string word)
    {
        if (string.IsNullOrEmpty(word) || !G.UI.Typed.EndsWith(word, StringComparison.Ordinal)) return false;
        G.UI.RemoveLetters(word.Length);
        return true;
    }
}
