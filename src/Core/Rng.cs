namespace TypingShip.Core;

/// <summary>
/// The game's random numbers: PCG32, bit for bit the same as Godot's RandomNumberGenerator, so a seed gives the
/// same run in the game and in tests. Rand() is the original's Math.random(): a float in [0, 1).
/// </summary>
public sealed class Rng
{
    const ulong Multiplier = 6364136223846793005UL;
    const ulong DefaultInc = 1442695040888963407UL; // PCG_DEFAULT_INC_64, Godot's stream

    ulong state, inc;

    public Rng(ulong seed) => Seed(seed);

    /// <summary>Seeded from the clock (an unseeded game).</summary>
    public Rng() : this((ulong)DateTime.UtcNow.Ticks) { }

    public void Seed(ulong seed)
    {
        // pcg32_srandom_r
        state = 0;
        inc = (DefaultInc << 1) | 1;
        Next();
        state += seed;
        Next();
    }

    uint Next()
    {
        ulong old = state;
        state = old * Multiplier + (inc | 1);
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << (-rot & 31));
    }

    /// <summary>RandomPCG::randf: a float in [0, 1], with full precision near 0.</summary>
    public float Randf()
    {
        uint protoExpOffset = Next();
        if (protoExpOffset == 0) return 0;
        return MathF.ScaleB(Next() | 0x80000001u, -32 - System.Numerics.BitOperations.LeadingZeroCount(protoExpOffset));
    }

    /// <summary>Math.random(): [0, 1).</summary>
    public float Rand()
    {
        float r = Randf();
        return r >= 1f ? 0.99999994f : r;
    }
}
