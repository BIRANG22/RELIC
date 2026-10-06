using NUnit.Framework;

public class SkillExecutionResultTests
{
    [Test]
    public void RecordImpact_AggregatesHitsDistinctTargetsAndKills()
    {
        SkillExecutionResult result = new();
        result.RecordImpact(new SkillImpactResult
        {
            TargetRuntimeId = "M1", Attempted = true, Hit = true, DamageAmount = 3
        });
        result.RecordImpact(new SkillImpactResult
        {
            TargetRuntimeId = "M1", Attempted = true, Hit = true, DamageAmount = 4, Killed = true
        });
        result.RecordImpact(new SkillImpactResult
        {
            TargetRuntimeId = "M2", Attempted = true, Hit = false
        });

        Assert.That(result.AttemptedHitCount, Is.EqualTo(3));
        Assert.That(result.SuccessfulHitCount, Is.EqualTo(2));
        Assert.That(result.DistinctHitTargetCount, Is.EqualTo(1));
        Assert.That(result.KillCount, Is.EqualTo(1));
    }
}
