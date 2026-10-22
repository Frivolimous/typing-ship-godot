using System.Globalization;
using System.Text;
using TypingShip.Core.Data;

namespace TypingShip.Core;

public sealed partial class Game
{
    /// <summary>Dev aid (--trace): one line of game state per tick, to compare runs exactly.</summary>
    public TextWriter? Trace;

    void WriteTrace()
    {
        static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.Append($"{Time.ToString("R", CultureInfo.InvariantCulture)} s{Score} h{Player.Health} [{Typed}] hull:{Hull.Word}");
        foreach (var o in Objects)
        {
            sb.Append($" | {o.TraceName} {F(o.Position.X)},{F(o.Position.Y)} {F(o.Rotation)} {o.Word} p{o.Priority}");
            if (o.Turret != null) sb.Append($" t:{o.Turret.Word}");
            if (o is EnemyShip { Charge: { } c }) sb.Append($" c{c.Remaining}");
            if (o.Alpha < 1) sb.Append($" a{F(o.Alpha)}");
        }
        foreach (var o in Overflows) sb.Append($" | of {o.Word}");
        if (Boss is { } b)
            sb.Append($" | boss {F(b.Position.X)},{F(b.Position.Y)} h{b.Health} sc:{b.Scanner?.Word} " +
                (b.Scanner != null ? F(b.Scanner.Position.X) + "," + F(b.Scanner.Position.Y) : ""));
        Trace!.WriteLine(sb.ToString());
    }

    /// <summary>The one-line summary printed when a level ends (headless runs, the simulator).</summary>
    public string ResultLine(bool cleared) => Endless != null
        ? $"Endless run over at {(int)Time / 60}:{(int)Time % 60:00}: score {Score}, wave {Endless.Wave}, ships {TotalShips}, ticks {Ticks}" :
        $"Level {Level} {(cleared ? "cleared" : "lost")}: score {Score}, ships {TotalShips}, missed {MissedShips}, " +
        $"health {Player.Health}, ticks {Ticks}";
}

/// <summary>Plays a level with the bot, without the game: what --autotype --quit-on-end does, much faster.</summary>
public static class Simulator
{
    public sealed record Result(bool Cleared, string Line, Game Game);

    public static Result Run(GameData data, ArtLibrary art, int level, double cps, ulong seed, double botCps = 0,
        TextWriter? trace = null, int maxTicks = 200_000, Action<Game>? onTick = null,
        double errorRate = 0, bool endless = false)
    {
        var game = new Game(data, new Rng(seed), art) { Cps = cps, Trace = trace };
        game.TutOff(); // tutorial pop-ups would wait for Space
        bool? cleared = null;
        string line = "";
        var events = new EndWatcher(c =>
        {
            cleared ??= c;
            line = game.ResultLine(c);
        });
        game.Events = events;
        var bot = new AutoTyper(game, botCps, errorRate, errorSeed: seed);
        if (endless) game.NewEndless(seed);
        else game.NewGame(level);
        while (cleared == null && game.Ticks < maxTicks)
        {
            game.Step();
            onTick?.Invoke(game);
            if (cleared == null) bot.Step();
        }
        trace?.Flush();
        return new Result(cleared == true, cleared == null ? $"Level {level} timed out after {maxTicks} ticks" : line, game);
    }

    sealed class EndWatcher(Action<bool> onEnd) : IGameEvents
    {
        public void LevelOver(bool cleared) => onEnd(cleared);
    }
}
