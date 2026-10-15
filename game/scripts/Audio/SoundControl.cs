using Godot;

namespace TypingShip;

// Sound effects and music, with the original's per-sound volumes and 0.5 s music cross-fade.
public partial class SoundControl : Node
{
    public const int Type = 0, Click = 1, Score = 2, Laser = 3, ExplodeS = 4, ExplodeB = 5, Charge = 6, Heal = 7,
        EmpSound = 8, BossCharge = 9, BossFire = 10, Superman = 11;

    const float MaxVolume = 0.5f;
    const float FadeRate = 1f; // volume per second (0.01 per 10 ms in the original)
    const int Voices = 16;

    static readonly string[] SoundNames =
        { "TypeS", "ClickS", "ScoreS", "LaserS", "ExplodeSS", "ExplodeBS", "ChargeS", "HealS", "EmpS", "BChargeS", "BFireS", "SuperS" };
    static readonly float[] Volumes = { 1, 1, 1, 0.3f, 0.4f, 0.3f, 1, 1.5f, 1.3f, 0.75f, 1, 0.5f };

    readonly AudioStream[] sounds = SoundNames.Select(n => GD.Load<AudioStream>($"res://assets/sfx/{n}.wav")).ToArray();
    readonly AudioStream?[] music = new AudioStream?[5];
    readonly List<AudioStreamPlayer> voices = new();
    readonly AudioStreamPlayer musicPlayer = new();
    int musicI, playing = -1, nextVoice;
    float volume;
    bool fading, fadeIn, muteM, muteS;

    public override void _Ready()
    {
        for (int i = 0; i < Voices; i++)
        {
            var p = new AudioStreamPlayer();
            AddChild(p);
            voices.Add(p);
        }
        AddChild(musicPlayer);
        musicPlayer.Finished += () => musicPlayer.Play(); // loop
    }

    public void Sound(int i)
    {
        if (muteS) return;
        var p = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Count;
        p.Stream = sounds[i];
        p.VolumeDb = Mathf.LinearToDb(Volumes[i]);
        p.Play();
    }

    // Music isn't in git (see assets/music/README.md), so a fresh checkout runs without it.
    AudioStream? Music(int i)
    {
        var path = $"res://assets/music/Music{i}.wav";
        return music[i] ??= ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
    }

    /// <summary>Fade the current track out and the given one in (-1 = the last requested track).</summary>
    public void Play(int i = -1)
    {
        if (i == -1) i = musicI;
        musicI = i;
        if (fading)
        {
            fadeIn = musicI == playing;
            return;
        }
        fadeIn = false;
        if (musicPlayer.Playing)
        {
            fading = true;
        }
        else
        {
            MusicSwap();
        }
    }

    public void Stop()
    {
        if (!musicPlayer.Playing) return;
        fadeIn = false;
        fading = true;
    }

    public override void _Process(double delta)
    {
        if (!fading) return;
        float step = FadeRate * (float)delta;
        if (fadeIn)
        {
            volume += step;
            if (volume >= MaxVolume)
            {
                volume = MaxVolume;
                fading = false;
            }
        }
        else
        {
            volume -= step;
            if (volume <= 0)
            {
                MusicSwap();
                return;
            }
        }
        musicPlayer.VolumeDb = Mathf.LinearToDb(volume);
    }

    void MusicSwap()
    {
        musicPlayer.Stop();
        volume = 0;
        fadeIn = true;
        if (muteM)
        {
            fading = false;
            return;
        }
        var stream = Music(musicI);
        if (stream == null)
        {
            fading = false;
            return;
        }
        fading = true;
        playing = musicI;
        musicPlayer.Stream = stream;
        musicPlayer.VolumeDb = Mathf.LinearToDb(0.01f);
        musicPlayer.Play();
    }

    /// <summary>The mute button's music half: stops the music, or brings back the last track.</summary>
    public bool MuteMusic
    {
        get => muteM;
        set
        {
            if (value == muteM) return;
            muteM = value;
            if (value) Stop();
            else Play();
        }
    }

    public bool MuteSound
    {
        get => muteS;
        set => muteS = value;
    }

    public bool Mute
    {
        get => muteS && muteM;
        set
        {
            if (value)
            {
                Stop();
                muteS = muteM = true;
            }
            else
            {
                bool wasMuted = muteM;
                muteS = muteM = false;
                if (wasMuted) Play();
            }
        }
    }
}
