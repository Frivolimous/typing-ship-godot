using Godot;

namespace TypingShip;

// Shows the rules (G.Model) on screen: it hears what happens as it happens (TypingShip.Core.IGameEvents) and
// keeps a node for each sprite in play, copying their state after every tick.
public sealed class Presenter : IGameEvents
{
    readonly Dictionary<Entity, SpriteView> views = new();
    readonly Dictionary<Scanner, ScannerView> scanners = new();
    public PlayerView Player = null!;

    /// <summary>Called when a level is won (true) or lost (false).</summary>
    public event Action<bool>? LevelFinished;

    public IEnumerable<SpriteView> Views => views.Values;

    public SpriteView? ViewOf(Entity e) => views.GetValueOrDefault(e);

    /// <summary>Makes the player's view (the player exists before any level, so there's no Spawned for it).</summary>
    public void Init()
    {
        Player = new PlayerView(G.Model.Player);
        // New ships take the new art as they spawn; the player's ship swaps at once.
        Sprites.ArtChanged += Player.Reskin;
    }

    /// <summary>Copies every sprite's state into its node (once per tick).</summary>
    public void Sync()
    {
        // The death sequence takes the ship away itself, at its big blast.
        if (G.Model.Player.Destroyed && !playerDying) Player.GetParent()?.RemoveChild(Player);
        Player.Sync();
        foreach (var v in views.Values) v.Sync();
        foreach (var s in scanners.Values) s.Sync();
    }

    /// <summary>Pausing hides the words, so they can't be read ahead.</summary>
    public void ShowAll(bool b)
    {
        foreach (var v in views.Values) v.ShowWord(b);
        G.UI.Hull.ShowWord(b);
        foreach (var s in scanners.Values) s.Word.ShowWord(b);
    }

    // ---- IGameEvents ----

    public void Spawned(Sprite s, Layer layer)
    {
        SpriteView view = s switch
        {
            EnemyShip e => new EnemyShipView(e),
            Missile m => new SpriteView(m, m.FromPlayer ? Sprites.PMissile : Sprites.EMissile, Sprites.MissileScale),
            Superman => new SpriteView(s, new[] { Sprites.Superman }, 0.2f),
            Boss b => new BossView(b),
            _ => new SpriteView(s, null, 0.2f), // boss 3's counter-words: just a word
        };
        views[s] = view;
        var gameView = G.UI.GameView;
        switch (layer)
        {
            case Layer.Front: gameView.AddChild(view); break;
            case Layer.Back: G.UI.AddToGameViewBack(view); break;
            case Layer.Bottom: gameView.AddChild(view); gameView.MoveChild(view, 0); break;
            case Layer.Boss: G.UI.BossView.AddChild(view); break;
        }
        view.ResetPhysicsInterpolation();
        if (s is EnemyShip { Hull: "xm" } && view is EnemyShipView ev) new StealthEffect(ev);
        if (s is Boss) new Banner("WARNING", "BOSS APPROACHING", new Color(1, 0.3f, 0.25f), 1.8f);
    }

    public void Removed(Entity e)
    {
        if (e is Scanner s)
        {
            if (scanners.Remove(s, out var sv))
            {
                sv.GetParent()?.RemoveChild(sv);
                sv.QueueFree();
            }
            return;
        }
        if (views.Remove(e, out var view)) view.Detach();
    }

    public void ScannerAdded(Scanner s)
    {
        var view = new ScannerView(s);
        scanners[s] = view;
        G.UI.GameView.AddChild(view);
        view.ResetPhysicsInterpolation();
    }

    public void WordTyped(Entity e)
    {
        var label = e switch
        {
            Turret t => (views.GetValueOrDefault(t.Ship) as EnemyShipView)?.TurretWord,
            Sprite sprite => views.GetValueOrDefault(sprite)?.Word,
            Scanner scanner => scanners.GetValueOrDefault(scanner)?.Word,
            _ => null,
        };
        if (label != null) label.Pop();
        else if (e == G.Model.Hull) G.UI.Hull.PopWord();
    }

    bool playerDying;

    public void PlayerDestroyed()
    {
        playerDying = true;
        G.Ctrl.AddTicker(new DeathSequence(Player, () =>
        {
            playerDying = false;
            Player.View.Modulate = Colors.White;
            Player.GetParent()?.RemoveChild(Player);
        }));
    }

    public void Explosion(Vec2 at, int size) => G.Starfield.Explosion(at.X, at.Y, size);

    public void Beam(Vec2 from, Vec2 to, BeamKind kind) => BeamFx.Make(Muzzle(from), to, kind);

    /// <summary>
    /// Where a shot from a ship at <paramref name="at"/> leaves it: the gun on its nose (its charge point in
    /// data/mounts.txt), or its centre if it has none.
    /// </summary>
    Vector2 Muzzle(Vec2 at)
    {
        ChargeGlow? gun = G.Model.Player.P == at ? Player.Glow
            : views.Values.OfType<EnemyShipView>().FirstOrDefault(v => v.Model.P == at)?.Glow;
        return gun != null && gun.IsInsideTree() ? G.UI.GameView.ToLocal(gun.GlobalPosition) : G.V(at);
    }

    public void ScorePopup(Vec2 at, int amount, bool inGameView)
    {
        if (inGameView) new PopScore(G.V(at), amount);
        else new PopScore(G.UI.TypedPosition + new Vector2(5, -5), amount, false);
    }

    public void Sound(GameSound sound) => G.Sound.Sound(sound switch
    {
        GameSound.Type => SoundControl.Type,
        GameSound.Laser => SoundControl.Laser,
        GameSound.Charge => SoundControl.Charge,
        GameSound.Heal => SoundControl.Heal,
        GameSound.Emp => SoundControl.EmpSound,
        GameSound.BossCharge => SoundControl.BossCharge,
        GameSound.BossFire => SoundControl.BossFire,
        _ => SoundControl.Superman,
    });

    public void Music(int track) => G.Sound.Play(track);

    public void SetMute(bool mute) => G.Sound.Mute = mute;

    public void Chameleon() => Sprites.Recolour(Player.View);

    public void Paused(bool paused)
    {
        ShowAll(!paused);
        G.Shell.PauseB.SetPaused(paused);
        if (paused)
        {
            if (G.Model.Tutorial == null) G.Shell.Pause.Show(true);
            else if (G.Model.Tutorial == 8) G.UI.Hull.ShowWord(true); // the tutorial is about the hull's word
        }
        else
        {
            G.UI.TutBox?.Remove();
            G.Shell.Pause.Show(false);
        }
    }

    public void TutorialShown(int index, Vec2? pointer, PointerDir dir) =>
        new PopBox(index, pointer is { } p ? G.V(p) : null, dir);

    public void AchievementAwarded(int achievement, int tier)
    {
        new PopAchieve(achievement, tier);
        G.Platform.Unlock(achievement, tier);
    }

    public void WaveStarting(int wave) =>
        new Banner($"WAVE {wave}", "", Cockpit.Rim, hold: (float)G.Model.Tuning.EndlessAnnounce - Banner.FadeIn);

    public void LevelStarted()
    {
        G.UI.SnapScore();
        new Banner(G.Model.Endless != null ? "ENDLESS" : Banner.LevelName(G.Model.Level), Difficulty.Names[Difficulty.Of(G.Model.Cps) - 1].ToUpperInvariant(),
            Cockpit.Rim);
        G.Starfield.SetBack(G.Model.Level);
        var gameView = G.UI.GameView;
        if (Player.GetParent() == null) gameView.AddChild(Player);
        else gameView.MoveChild(Player, -1);
        Player.Sync();
        Player.ResetPhysicsInterpolation();
    }

    public void LevelClearing()
    {
        if (G.Model.Boss != null) new Banner("BOSS DESTROYED", "MISSION COMPLETE", Kit.Good, 2.2f, delay: 1.6f);
        else new Banner("MISSION COMPLETE", "ALL TARGETS DOWN", Kit.Good, 1.6f);
    }

    public void LevelOver(bool cleared) => LevelFinished?.Invoke(cleared);

    public void TickStarted() => G.Starfield.Update();

    public void TickAdvanced()
    {
        G.UI.OnTick();
        SpriteTick();
    }

    int spriteCount;

    /// <summary>SpriteSheets.onTick: animations advance every other tick.</summary>
    void SpriteTick()
    {
        if (++spriteCount <= 1) return;
        spriteCount = 0;
        Player.AdvanceFrame();
        foreach (var o in G.Model.Objects)
            if (views.TryGetValue(o, out var v)) v.AdvanceFrame();
    }

    public void ChargeTicked(Charge c)
    {
        var glow = c.Origin == G.Model.Player ? Player.Glow : (views.GetValueOrDefault(c.Origin) as EnemyShipView)?.Glow;
        glow?.SetColor(ChargeGlow.NextColor());
    }

    Boss? burning;
    int burnTick;

    /// <summary>
    /// A dying boss, each tick of its 200-tick fade: explosions come faster and bigger, then at tick 100 one huge blast
    /// whites out the screen and the boss is gone (the rules keep fading it out unseen until the level ends).
    /// </summary>
    public void BossBurning(Boss b)
    {
        if (b != burning)
        {
            burning = b;
            burnTick = 0;
        }
        int n = burnTick++;
        if (n == 100)
        {
            var c = b.Position;
            G.Starfield.Explosion(c.X, c.Y, 300);
            for (int i = 0; i < 6; i++)
            {
                var p = b.RandomPoint(G.Rng);
                G.Starfield.Explosion(p.X, p.Y, 60);
            }
            G.Starfield.Shockwave(G.V(c), 420, 18);
            G.Flash.Flash(new Color(1, 0.97f, 0.9f), 1, 0.9f, edgesOnly: false);
            G.Camera.Shake(1);
            (views.GetValueOrDefault(b) as BossView)?.Vanish();
            return;
        }
        if (n < 100 && G.Rand() < 0.35f + 0.5f * n / 100)
        {
            var p = b.RandomPoint(G.Rng);
            int size = 30 + n / 3;
            G.Starfield.Explosion(p.X, p.Y, size);
            G.Camera.Shake(0.06f + size / 900f);
        }
    }

    public void BeamHitsPlayer()
    {
        var player = G.Model.Player;
        var size = Player.View.Texture.GetSize() * Player.View.Scale;
        G.Starfield.Explosion(player.Position.X - size.X / 2 + G.Rand() * size.X,
            player.Position.Y - size.Y / 2 + G.Rand() * size.Y);
    }
}
