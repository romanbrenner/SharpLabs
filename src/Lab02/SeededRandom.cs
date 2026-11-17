using Microsoft.Extensions.Options;

namespace Lab02;

public sealed class SeededRandom : Random
{
    public int Seed { get; }

    public SeededRandom(IOptions<Lab02Options> options)
        : this(options.Value.Seed ?? Random.Shared.Next())
    {
    }

    private SeededRandom(int seed) : base(seed) => Seed = seed;
}
