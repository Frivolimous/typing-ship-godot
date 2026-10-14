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
        if (e is not InputEventKey { Pressed: true } key || !G.Shell.InGame) return;

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

        // A tutorial pop-up: Space continues, Escape continues with the tutorial switched off.
        if (G.UI.TutBox is { } tut)
        {
            if (key.Keycode is Key.Space or Key.Escape)
            {
                tut.Remove();
                if (key.Keycode == Key.Escape) M.TutOff();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // Escape pauses and resumes, as well as the pause button. It counts as pausing "without clicking".
        if (key.Keycode == Key.Escape && M.CanType && !G.Ctrl.LevelEnded)
        {
            if (M.Running) TypedPause();
            else G.Ctrl.PauseGame(false);
            GetViewport().SetInputAsHandled();
            return;
        }

        char c = char.ToLowerInvariant((char)key.Unicode);
        if (c < 'a' || c > 'z') return;
        GetViewport().SetInputAsHandled();
        if (M.Running && M.CanType) TypeLetter(c.ToString());
        else if (G.Shell.Pause.Visible) G.Shell.Pause.Nav.Type(c); // the pause screen's MENU, YES, NO
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
                            case ActionControl.Instant: G.Action.ShootInstant(M.PShip, o); break;
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

            if (M.Boss?.Scanner is { } scanner && TestWord(scanner))
            {
                scanner.Scan();
                return;
            }
        }

        if (TestWord(UnmuteWord)) G.Sound.Mute = false;
        else if (TestWord(MuteWord)) G.Sound.Mute = true;
        else if (TestWord(SupermanWord))
        {
            MakeSuperman();
            if (M.Achieve[Strings.Riddler] == 0) Achievements.Award(Strings.Riddler, 1); // then kamikaze, then chameleon
        }
        else if (TestWord(KamikazeWord))
        {
            if (M.Achieve[Strings.Riddler] == 1) Achievements.Award(Strings.Riddler, 2);
            while (M.GObjects.Count > 0) G.Action.HitTarget(M.GObjects[0], ActionControl.Instant);
            G.Ctrl.PlayerDeath();
        }
        else if (TestWord(ChameleonWord))
        {
            if (M.Achieve[Strings.Riddler] == 2) Achievements.Award(Strings.Riddler, 3);
            Sprites.Recolour(M.PShip);
        }
        else if (TestWord(PauseWord)) TypedPause();
    }

    void TypedPause()
    {
        G.Ctrl.PauseGame(true);
        if (M.Achieve[Strings.Explorer] == 1) Achievements.Award(Strings.Explorer, 2);
    }

    static void MakeSuperman()
    {
        G.Sound.Sound(SoundControl.Superman);
        var s = new Superman();
        s.Init(G.Rand() * 400 + 50, 550, -Mathf.Pi / 2, 1.3f, 1);
        G.UI.GameView.MoveChild(s, 0);
        G.Model.GObjects.Add(s);
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
