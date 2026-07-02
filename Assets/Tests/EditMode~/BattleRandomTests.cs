using NUnit.Framework;
using Relic.Gameplay.Battle;

public class BattleRandomTests
{
    [Test]
    public void SetSeed_ReplaysSameIntegerSequence()
    {
        BattleRandom.SetSeed(1234);
        int firstA = BattleRandom.Range(0, 100);
        int secondA = BattleRandom.Range(0, 100);

        BattleRandom.SetSeed(1234);
        int firstB = BattleRandom.Range(0, 100);
        int secondB = BattleRandom.Range(0, 100);

        BattleRandom.ClearSeed();

        Assert.That(firstB, Is.EqualTo(firstA));
        Assert.That(secondB, Is.EqualTo(secondA));
    }
}
