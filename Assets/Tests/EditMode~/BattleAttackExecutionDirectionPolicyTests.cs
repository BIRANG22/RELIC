using NUnit.Framework;
using Relic.Gameplay.Data;
using System.Collections.Generic;

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

    [TestCase(MonsterAttackSpatialPolicy.LockedTargetGrid, true)]
    [TestCase(MonsterAttackSpatialPolicy.ActorRelative, false)]
    public void ShouldPreserveMonsterTargetGrid_OnlyPreservesLockedTargetGrid(
        MonsterAttackSpatialPolicy spatialPolicy,
        bool expected)
    {
        Assert.That(
            BattleAttackExecutionDirectionPolicy.ShouldPreserveMonsterTargetGrid(
                spatialPolicy),
            Is.EqualTo(expected));
    }

    [TestCase("Range_m_01", MonsterAttackSpatialPolicy.LockedTargetGrid)]
    [TestCase("Range_m_02", MonsterAttackSpatialPolicy.LockedTargetGrid)]
    [TestCase("Range_10", MonsterAttackSpatialPolicy.ActorRelative)]
    [TestCase("Range_All", MonsterAttackSpatialPolicy.ActorRelative)]
    public void ResolveMonsterAttackSpatialPolicy_UsesRangeSemantics(
        string rangeId,
        MonsterAttackSpatialPolicy expected)
    {
        Assert.That(MonsterAttackSpatialPolicyResolver.Resolve(rangeId), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveSavedMonsterAttackSpatialPolicy_OldSaveKeepsGridTargetRangeLocked()
    {
        Assert.That(
            MonsterAttackSpatialPolicyResolver.ResolveSaved(
                (int)MonsterAttackSpatialPolicy.ActorRelative,
                "Range_m_01"),
            Is.EqualTo(MonsterAttackSpatialPolicy.LockedTargetGrid));
    }

    [TestCase(MonsterAttackSpatialPolicy.ActorRelative, 42)]
    [TestCase(MonsterAttackSpatialPolicy.LockedTargetGrid, 17)]
    public void ResolveMonsterAttackOrigin_UsesLiveOriginUnlessTargetGridIsLocked(
        MonsterAttackSpatialPolicy spatialPolicy,
        int expected)
    {
        Assert.That(
            BattleAttackExecutionDirectionPolicy.ResolveExecutionOriginGridIndex(
                spatialPolicy,
                liveOriginGridIndex: 42,
                reservedOriginGridIndex: 17),
            Is.EqualTo(expected));
    }

    [Test]
    public void RestoreMonsterRangeResult_DirectionalRangeRemainsCasterRelative()
    {
        MonsterReservedCommand command = new(null, null);

        BattleMonsterCommandRangeSnapshotPolicy.Restore(
            command,
            hasExplicitRangeResult: false,
            MonsterAttackSpatialPolicy.ActorRelative,
            new List<int> { 11, 12 },
            new List<int> { 12 });

        Assert.That(command.HasExplicitRangeResult, Is.False);
        Assert.That(command.AttackSpatialPolicy, Is.EqualTo(MonsterAttackSpatialPolicy.ActorRelative));
        Assert.That(command.RangeGridIndices, Is.EqualTo(new[] { 11, 12 }));
        Assert.That(command.TargetGridIndices, Is.EqualTo(new[] { 12 }));
    }

    [Test]
    public void RestoreMonsterRangeResult_ExplicitGridRemainsAbsolute()
    {
        MonsterReservedCommand command = new(null, null);

        BattleMonsterCommandRangeSnapshotPolicy.Restore(
            command,
            hasExplicitRangeResult: true,
            MonsterAttackSpatialPolicy.LockedTargetGrid,
            new List<int> { 27 },
            new List<int> { 27 });

        Assert.That(command.HasExplicitRangeResult, Is.True);
        Assert.That(command.AttackSpatialPolicy, Is.EqualTo(MonsterAttackSpatialPolicy.LockedTargetGrid));
        Assert.That(command.RangeGridIndices, Is.EqualTo(new[] { 27 }));
        Assert.That(command.TargetGridIndices, Is.EqualTo(new[] { 27 }));
    }

    [Test]
    public void ProtocolVersion_LegacyRangeSchemaIsRejected()
    {
        bool accepted = BattleNetworkSerialization.IsSupportedProtocolVersion(2);

        Assert.That(accepted, Is.False);
    }
}
