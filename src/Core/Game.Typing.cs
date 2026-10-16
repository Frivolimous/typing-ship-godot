namespace TypingShip.Core;

// In-game typing (the original's KeyboardControl): letters build up the typed text; whenever it ends with a word
// on screen, that word fires. A few secret words do other things.
public sealed partial class Game
{
    public const string MuteWord = "mute", UnmuteWord = "unmute", SupermanWord = "superman", KamikazeWord = "kamikaze",
        ChameleonWord = "chameleon", PauseWord = "pause";

    /// <summary>Whether letters typed now count (the level is on, unpaused, and the player is alive).</summary>
    public bool Typing => Running && CanType;

    public void TypeLetter(char letter)
    {
        AddLetter(letter);
        Events.Sound(GameSound.Type);

        for (int i = 0; i < Objects.Count; i++)
        {
            var o = Objects[i];
            if (TestWord(o))
            {
                if (o.ShieldOn) ShootEmp(Player, o);
                else
                    switch (o.KillBy)
                    {
                        case Weapons.Missile: ShootMissile(Player, o); break;
                        case Weapons.Laser: ShootLaser(Player, o); break;
                        case Weapons.Instant: ShootInstant(Player, o); break;
                        default: throw new InvalidOperationException($"Invalid kill type: {o.KillBy}");
                    }
                return;
            }
            if (o.Turret != null && TestWord(o.Turret))
            {
                ShootLaser(Player, o.Turret);
                return;
            }
        }

        foreach (var overflow in Overflows)
        {
            if (TestWord(overflow.Word))
            {
                overflow.Remove();
                return;
            }
        }

        if (TestWord(Hull))
        {
            Heal();
            return;
        }

        if (Boss?.Scanner is { } scanner && TestWord(scanner))
        {
            scanner.Scan();
            return;
        }

        if (TestWord(UnmuteWord)) Events.SetMute(false);
        else if (TestWord(MuteWord)) Events.SetMute(true);
        else if (TestWord(SupermanWord))
        {
            MakeSuperman();
            if (Save.Achieve[Achievement.Riddler] == 0) Award(Achievement.Riddler, 1); // then kamikaze, then chameleon
        }
        else if (TestWord(KamikazeWord))
        {
            if (Save.Achieve[Achievement.Riddler] == 1) Award(Achievement.Riddler, 2);
            while (Objects.Count > 0) HitTarget(Objects[0], Weapons.Instant);
            PlayerDeath();
        }
        else if (TestWord(ChameleonWord))
        {
            if (Save.Achieve[Achievement.Riddler] == 2) Award(Achievement.Riddler, 3);
            Events.Chameleon();
        }
        else if (TestWord(PauseWord)) TypedPause();
    }

    /// <summary>Pausing by keyboard (typing "pause" or Escape) counts for the silver Explorer medal.</summary>
    public void TypedPause()
    {
        Pause();
        if (Save.Achieve[Achievement.Explorer] == 1) Award(Achievement.Explorer, 2);
    }

    /// <summary>Backspace: takes a letter off the typing bar, for a point.</summary>
    public void Backspace()
    {
        RemoveLetter();
        Events.Sound(GameSound.Type);
    }

    void AddLetter(char c)
    {
        Typed += c;
        if (Typed.Length > Tuning.TypingBar)
        {
            Score--;
            Events.ScorePopup(Vec2.Zero, -1, false);
            Typed = Typed[1..];
        }
    }

    /// <summary>One letter off the typing bar, for a point (backspace, and the score screen's count-down).</summary>
    public void RemoveLetter()
    {
        if (Typed.Length == 0) return;
        Perfect = false;
        Typed = Typed[..^1];
        Score--;
        Events.ScorePopup(Vec2.Zero, -1, false);
    }

    void MakeSuperman()
    {
        Events.Sound(GameSound.Superman);
        var s = new Superman(this);
        s.Init(Rng.Rand() * 400 + 50, 550, -MathF.PI / 2, 1.3f, 1);
        Events.Spawned(s, Layer.Bottom);
        Objects.Add(s);
    }

    bool TestWord(Entity v)
    {
        if (v.Word == null || !TestWord(v.Word)) return false;
        Events.WordTyped(v);
        v.RecycleWord();
        return true;
    }

    /// <summary>Does the typed text end with this word? If so, consume it.</summary>
    bool TestWord(string word)
    {
        if (string.IsNullOrEmpty(word) || !Typed.EndsWith(word, StringComparison.Ordinal)) return false;
        Typed = Typed[..^word.Length];
        return true;
    }
}
