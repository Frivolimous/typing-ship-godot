namespace TypingShip.Core;

public enum GameSound { Type, Laser, Charge, Heal, Emp, BossCharge, BossFire, Superman }

public enum BeamKind { PlayerLaser, EnemyLaser, Emp }

/// <summary>Which side of a tutorial box its pointer leaves from (Auto: worked out from the target's position).</summary>
public enum PointerDir { Auto, Up, Down, Left, Right }

/// <summary>
/// What the rules tell the presentation, as it happens (synchronously, mid-tick). Everything else the presentation
/// needs it reads from the Game's state after each tick. All methods do nothing by default, so tests and the
/// simulator can ignore what they don't need.
/// </summary>
public interface IGameEvents
{
    /// <summary>A sprite joined the game view (everything but the player, created before any Game exists).</summary>
    void Spawned(Sprite s, Layer layer) { }
    /// <summary>A sprite or the boss scanner left play.</summary>
    void Removed(Entity e) { }
    void ScannerAdded(Scanner s) { }
    /// <summary>The player typed an entity's word; it's about to be released (and the entity may be removed).</summary>
    void WordTyped(Entity e) { }

    void Explosion(Vec2 at, int size) { }
    /// <summary>The player's ship was destroyed: the presentation plays its death (the level ends 100 ticks later).</summary>
    void PlayerDestroyed() { }
    void Beam(Vec2 from, Vec2 to, BeamKind kind) { }
    /// <summary>The score changed by amount: a floating number at a point in the game view, or by the typing bar.</summary>
    void ScorePopup(Vec2 at, int amount, bool inGameView) { }
    void Sound(GameSound sound) { }
    void Music(int track) { }
    void SetMute(bool mute) { }
    /// <summary>The "chameleon" cheat: recolour the player's ship.</summary>
    void Chameleon() { }

    /// <summary>The game paused or resumed (Running changed). Game.Tutorial says whether a tutorial caused it.</summary>
    void Paused(bool paused) { }
    void TutorialShown(int index, Vec2? pointer, PointerDir dir) { }
    void AchievementAwarded(int achievement, int tier) { }

    /// <summary>A level started (after NewGame or NextLevel set it up).</summary>
    void LevelStarted() { }
    void LevelOver(bool cleared) { }
    /// <summary>The level is won and about to end: the last ship is gone (or the boss is dying).</summary>
    void LevelClearing() { }

    // Ticks the presentation keeps in step with: the backdrop scrolls at the start of a tick, and the HUD and
    // sprite animations advance once the ship has moved on.
    void TickStarted() { }
    void TickAdvanced() { }
    /// <summary>A charge (laser or EMP) grew by a tick.</summary>
    void ChargeTicked(Charge c) { }
    /// <summary>A dying boss, each tick of its 200-tick fade (Boss.Alpha).</summary>
    void BossBurning(Boss b) { }
    /// <summary>Boss 3's beam is hitting the unshielded player (every 5 ticks).</summary>
    void BeamHitsPlayer() { }
}

/// <summary>Ignores everything.</summary>
public sealed class NoEvents : IGameEvents
{
    public static readonly NoEvents Instance = new();
}
