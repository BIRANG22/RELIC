using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleExplorationElapsedTimeTests
{
    [Test]
    public void Advance_AddsOnlyActiveExplorationTime()
    {
        Assert.That(BattleExplorationElapsedTime.Advance(10f, 0.5f, false, false, false), Is.EqualTo(10.5f));
        Assert.That(BattleExplorationElapsedTime.Advance(10f, 0.5f, true, false, false), Is.EqualTo(10f));
        Assert.That(BattleExplorationElapsedTime.Advance(10f, 0.5f, false, true, false), Is.EqualTo(10f));
        Assert.That(BattleExplorationElapsedTime.Advance(10f, 0.5f, false, false, true), Is.EqualTo(10f));
    }

    [Test]
    public void Advance_ClampsNegativeValues()
    {
        Assert.That(BattleExplorationElapsedTime.Advance(-5f, -1f, false, false, false), Is.Zero);
    }

    [TestCase(0f, "00:00")]
    [TestCase(159f, "02:39")]
    [TestCase(3759f, "1:02:39")]
    public void Format_UsesClockNotation(float seconds, string expected)
    {
        Assert.That(BattleExplorationElapsedTime.Format(seconds), Is.EqualTo(expected));
    }

    [Test]
    public void RuntimeData_KeepsExplorationElapsedSeconds()
    {
        BattleRuntimeData runtime = new() { ExplorationElapsedSeconds = 12.5f };

        Assert.That(runtime.ExplorationElapsedSeconds, Is.EqualTo(12.5f));
    }

    [Test]
    public void CharacterStatisticsTooltip_ContainsEveryRequestedValue()
    {
        BattleRunCharacterStatisticsData statistics = new()
        {
            KillCount = 13,
            DamageDealt = 402,
            DamageTaken = 152,
            BuffApplied = 24,
            DeathCount = 1
        };

        string tooltip = ExplorationResultCharacterRowUI.BuildStatisticsTooltip(statistics);

        StringAssert.Contains("몬스터 처치  13", tooltip);
        StringAssert.Contains("입힌 피해량  402", tooltip);
        StringAssert.Contains("받은 피해량  152", tooltip);
        StringAssert.Contains("버프 부여량  24", tooltip);
        StringAssert.Contains("전투불능 횟수  1", tooltip);
    }
}
