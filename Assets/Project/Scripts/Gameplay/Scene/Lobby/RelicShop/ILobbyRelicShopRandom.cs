using System;

public interface ILobbyRelicShopRandom
{
    int NextIndex(int exclusiveMax);
}

public sealed class SeededLobbyRelicShopRandom : ILobbyRelicShopRandom
{
    private readonly Random random;

    public SeededLobbyRelicShopRandom(int seed)
    {
        random = new Random(seed);
    }

    public int NextIndex(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));

        return random.Next(exclusiveMax);
    }
}
