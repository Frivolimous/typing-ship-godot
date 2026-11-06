using Godot;

namespace TypingShip;

// Onboarding: the first time a new player presses START (SaveData.NeedsSpeedQuestion), before the star map, they're
// asked how fast they type. A speed band (the five difficulties as WPM ranges, the typing test's own cut-offs) shows
// the difficulty it fits, saved as SaveData.StatedSpeed so the difficulty box points it out; TAKE THE TEST goes to the
// typing test instead, whose result does the same. Either way the star map follows. Buttons work with the mouse or by
// typing their word (the band's first number, "under", "test", "continue"); Space or Enter continues from the
// recommendation, Escape goes back to the question, or from the question to the title.
public partial class SpeedQuestionUI : Screen
{
    const float ButtonW = 240;
    readonly Control ask = new(), answer = new();
    readonly Label pick, band;
    int chosen = -1;

    public SpeedQuestionUI()
    {
        Kit.Label(this, Strings.SpeedWelcome, Kit.Title(22), 0, 26, G.StageSize, HorizontalAlignment.Center);

        // The question: a button per difficulty band, then the test.
        foreach (var c in new[] { ask, answer })
        {
            c.Size = Size;
            c.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(c);
        }
        Kit.Label(ask, Strings.SpeedQuestion, Kit.Body(16), 0, 84, G.StageSize, HorizontalAlignment.Center);
        for (int i = 0; i < Difficulty.Wpm.Length; i++)
        {
            int band = i;
            var b = new TextButton(Strings.SpeedBand(i), () => Choose(band), 12, word: Strings.SpeedBandWord(i), boxed: true,
                colour: Kit.DiffColours[i], width: ButtonW)
            {
                Position = new Vector2((G.StageSize - ButtonW) / 2, 152 + 38 * i),
            };
            ask.AddChild(b);
            Nav.Add(b.Word, b.Press, () => ask.Visible);
        }
        Kit.Label(ask, Strings.SpeedOr, Kit.Caption(8), 0, 352, G.StageSize, HorizontalAlignment.Center);
        var test = new TextButton(Strings.SpeedTest, TakeTest, 13, word: "test", boxed: true, colour: Kit.Gold, width: 280,
            bold: true)
        {
            Position = new Vector2((G.StageSize - 280) / 2, 374),
        };
        ask.AddChild(test);
        Nav.Add(test.Word, test.Press, () => ask.Visible);

        // The answer: the difficulty that speed fits, and on to the map.
        Kit.Label(answer, Strings.SpeedRecommended, Kit.Caption(9), 0, 150, G.StageSize, HorizontalAlignment.Center);
        pick = Kit.Label(answer, "", Kit.Title(30), 0, 170, G.StageSize, HorizontalAlignment.Center);
        band = Kit.Label(answer, "", Kit.Body(14), 0, 226, G.StageSize, HorizontalAlignment.Center); // the speed they picked
        var go = new TextButton(Strings.SpeedContinue, Continue, 14, word: "continue", boxed: true, colour: Kit.Gold, width: 200,
            bold: true)
        {
            Position = new Vector2((G.StageSize - 200) / 2, 296),
        };
        answer.AddChild(go);
        Nav.Add(go.Word, go.Press, () => answer.Visible);
        var back = new TextButton(Strings.SpeedBack, Ask, 10, word: "back", boxed: true, width: 110)
        {
            Position = new Vector2((G.StageSize - 110) / 2, 344),
        };
        answer.AddChild(back);
        Nav.Add(back.Word, back.Press, () => answer.Visible);

    }

    public override void NavIn() => Ask();

    public override void _Draw() => Kit.Rule(this, new Vector2(150, 62), new Vector2(400, 62));

    void Ask()
    {
        chosen = -1;
        ask.Visible = true;
        answer.Visible = false;
        Nav.Clear();
    }

    void Choose(int band)
    {
        G.Sound.Sound(SoundControl.Click);
        chosen = band;
        pick.Text = Strings.Diffs[band];
        pick.LabelSettings = Kit.Title(30, Kit.DiffColours[band]);
        this.band.Text = Strings.SpeedBand(band);
        this.band.LabelSettings = Kit.Body(14, Kit.DiffColours[band].Lerp(Colors.White, 0.35f));
        ask.Visible = false;
        answer.Visible = true;
        Nav.Clear();
    }

    /// <summary>Saves the stated speed and goes on to the star map.</summary>
    void Continue()
    {
        if (chosen < 0 || G.Shell.Transitioning) return;
        G.Sound.Sound(SoundControl.Click);
        G.Model.Save.StatedSpeed = chosen;
        G.Model.Save.Save();
        G.Shell.Menu.ShowMapNow();
        G.Shell.Go(G.Shell.Menu, Shell.Dark);
    }

    /// <summary>The typing test, which comes back to the star map when it's done.</summary>
    void TakeTest()
    {
        if (G.Shell.Transitioning) return;
        G.Sound.Sound(SoundControl.Click);
        G.Shell.Menu.ShowMapNow();
        G.Shell.Go(G.Shell.Test, Shell.Dark);
    }

    public override bool OnKey(InputEventKey key)
    {
        if (answer.Visible && key.Keycode is Key.Space or Key.Enter or Key.KpEnter)
        {
            Continue();
            return true;
        }
        if (answer.Visible && key.Keycode == Key.Escape)
        {
            Ask();
            return true;
        }
        if (key.Keycode == Key.Escape && !G.Shell.Transitioning)
        {
            G.Shell.Go(G.Shell.Menu, Shell.Dark); // back to the title; START asks again
            return true;
        }
        return false;
    }
}
