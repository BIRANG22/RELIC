using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class MonsterFlipBeforeActionTests
{
    [Test]
    public void ResolveProjectedDirection_OppositeForcedAttack_MergesFlip()
    {
        BattleDirection result = MonsterFlipBeforeActionPolicy.ResolveProjectedDirection(
            BattleDirection.Right,
            isMoveAction: false,
            moveOffset: Vector2Int.zero,
            hasForcedDirection: true,
            forcedDirection: BattleDirection.Left,
            out bool flipBeforeAction);

        Assert.That(flipBeforeAction, Is.True);
        Assert.That(result, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void ResolveProjectedDirection_SameForcedAttack_DoesNotMergeFlip()
    {
        BattleDirection result = MonsterFlipBeforeActionPolicy.ResolveProjectedDirection(
            BattleDirection.Left,
            isMoveAction: false,
            moveOffset: Vector2Int.zero,
            hasForcedDirection: true,
            forcedDirection: BattleDirection.Left,
            out bool flipBeforeAction);

        Assert.That(flipBeforeAction, Is.False);
        Assert.That(result, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void ResolveProjectedDirection_HorizontalMove_UsesMoveDirectionWithoutMergedFlip()
    {
        BattleDirection result = MonsterFlipBeforeActionPolicy.ResolveProjectedDirection(
            BattleDirection.Right,
            isMoveAction: true,
            moveOffset: Vector2Int.left,
            hasForcedDirection: false,
            forcedDirection: BattleDirection.Right,
            out bool flipBeforeAction);

        Assert.That(flipBeforeAction, Is.False);
        Assert.That(result, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void MonsterCommand_MergedFlip_DoesNotIncreaseTimelineCost()
    {
        var runtime = new MonsterRuntimeData("monster-runtime", null)
        {
            CurrentHP = 1
        };
        var command = new MonsterReservedCommand(runtime, null);
        int originalCost = command.TimelineEntryCost;

        command.SetFlipBeforeAction();

        Assert.That(command.FlipBeforeAction, Is.True);
        Assert.That(command.TimelineEntryCost, Is.EqualTo(originalCost));
    }

    [Test]
    public void MonsterPreviewEntry_MergedFlip_ReportsHighlight()
    {
        var runtime = new MonsterRuntimeData("monster-runtime", null)
        {
            CurrentHP = 1
        };
        var command = new MonsterReservedCommand(runtime, null);
        command.SetFlipBeforeAction();

        BattleTimelinePreviewEntry entry = BattleTimelinePreviewEntry.CreateMonster(0, 0, command);

        Assert.That(entry.HasMergedFacingFlip, Is.True);
    }

    [Test]
    public void MonsterNetworkSnapshot_PreservesMergedFlipField()
    {
        var snapshot = new BattleNetworkMonsterCommandSnapshot
        {
            flipBeforeAction = true
        };

        Assert.That(snapshot.flipBeforeAction, Is.True);
    }
}
