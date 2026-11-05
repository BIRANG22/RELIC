using NUnit.Framework;
using Relic.Gameplay.Data;

public sealed class BattleAttackExecutionDirectionPolicyTests
{
    [TestCase(BattleDirection.Right, BattleDirection.Left, BattleDirection.Left)]
    [TestCase(BattleDirection.Left, BattleDirection.Right, BattleDirection.Right)]
    public void ResolveExecutionDirection_UsesLiveFacingInsteadOfReservedDirection(
        BattleDirection reservedDirection,
        BattleDirection liveDirection,
        BattleDirection expected)
    {
        Assert.That(
            BattleAttackExecutionDirectionPolicy.ResolveExecutionDirection(
                reservedDirection,
                liveDirection),
            Is.EqualTo(expected));
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void ShouldPreserveMonsterTargetGrid_OnlyPreservesExplicitRange(
        bool hasExplicitRangeResult,
        bool expected)
    {
        Assert.That(
            BattleAttackExecutionDirectionPolicy.ShouldPreserveMonsterTargetGrid(
                hasExplicitRangeResult),
            Is.EqualTo(expected));
    }
}
