using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class ExplorationResearchOneToOneConversionTests
{
    [Test]
    public void CreatePending_ConvertsRedDustiumOneToOneAndIgnoresMultiplier()
    {
        ExplorationResultData result = new ExplorationResultData
        {
            Remnant = 11
        };
        result.RelicIds.Add("Relic_Test");
        result.NewSkillIds.Add("Skill_Test");

        PendingResearchResultData pending = ExplorationResearchService.CreatePending(
            result,
            dataManager: null,
            rewardMultiplier: 0.25f);

        Assert.That(pending.RemnantBlue, Is.EqualTo(11));
        Assert.That(pending.RelicBlue, Is.Zero);
        Assert.That(pending.SkillBlue, Is.Zero);
        Assert.That(pending.TotalBlue, Is.EqualTo(11));
    }

    [TestCase(-3, 0)]
    [TestCase(0, 0)]
    [TestCase(11, 11)]
    public void CalculateOneToOneBlueDustium_ClampsNegativeAndPreservesAmount(
        int redDustium,
        int expected)
    {
        Assert.That(
            ExplorationResearchService.CalculateOneToOneBlueDustium(redDustium),
            Is.EqualTo(expected));
    }
}
