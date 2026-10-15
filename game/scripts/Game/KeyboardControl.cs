using Godot;

namespace TypingShip;

// In-game keys: letters and Backspace go to the rules (G.Model.TypeLetter), plus Escape to pause and the keys
// that close a tutorial pop-up.
public partial class KeyboardControl : Node
{
    static Game M => G.Model;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key || !G.Shell.InGame) return;

        if (key.Keycode == Key.Backspace)
        {
            if (M.Typing) M.Backspace();
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
        if (key.Keycode == Key.Escape && M.CanType && !M.LevelEnded)
        {
            if (M.Running) M.TypedPause();
            else M.Resume();
            GetViewport().SetInputAsHandled();
            return;
        }

        char c = char.ToLowerInvariant((char)key.Unicode);
        if (c < 'a' || c > 'z') return;
        GetViewport().SetInputAsHandled();
        if (M.Typing) M.TypeLetter(c);
        else if (G.Shell.Pause.Visible) G.Shell.Pause.Nav.Type(c); // the pause screen's MENU, YES, NO
    }
}
