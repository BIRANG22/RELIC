using NUnit.Framework;

public class BattleRearAttackActionSnapshotTests
{
    [Test]
    public void SameAction_KeepsFirstRearAttackQualificationAcrossHits()
    {
        BattleRearAttackActionSnapshot snapshot = new();

        Assert.That(snapshot.GetOrCapture("Monster_A", true), Is.True);
        Assert.That(snapshot.GetOrCapture("Monster_A", false), Is.True);
    }

    [Test]
    public void NextAction_UsesFreshRearAttackQualification()
    {
        BattleRearAttackActionSnapshot firstAction = new();
        BattleRearAttackActionSnapshot nextAction = new();

        Assert.That(firstAction.GetOrCapture("Monster_A", true), Is.True);
        Assert.That(nextAction.GetOrCapture("Monster_A", false), Is.False);
    }

    [Test]
    public void SameAction_KeepsQualificationsSeparatedByTargetRuntimeId()
    {
        BattleRearAttackActionSnapshot snapshot = new();

        Assert.That(snapshot.GetOrCapture("Monster_A", true), Is.True);
        Assert.That(snapshot.GetOrCapture("Monster_B", false), Is.False);
    }

    [Test]
    public void DamageModifier_UsesActionQualificationBeforeChangedLiveFacing()
    {
        Assert.That(
            BattleDamageModifierUtility.ResolveRearAttackQualification(true, false),
            Is.True);
        Assert.That(
            BattleDamageModifierUtility.ResolveRearAttackQualification(false, true),
            Is.False);
        Assert.That(
            BattleDamageModifierUtility.ResolveRearAttackQualification(null, true),
            Is.True);
    }
}
