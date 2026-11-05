using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class PlayerReservedMovePathPriorityTests
{
    [Test]
    public void BuildMoveExecutionSegments_AxisChangeStartsNextSegment()
    {
        List<Vector2Int> path = new()
        {
            Vector2Int.right,
            Vector2Int.down
        };

        List<List<Vector2Int>> segments =
            PlayerReservedCommand.BuildMoveExecutionSegments(path);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0], Is.EqualTo(new[] { Vector2Int.right }));
        Assert.That(segments[1], Is.EqualTo(new[] { Vector2Int.down }));
    }

    [Test]
    public void BuildMoveExecutionSegments_SameDirectionRemainsSingleCollisionSegment()
    {
        List<Vector2Int> path = new()
        {
            Vector2Int.right,
            Vector2Int.right
        };

        List<List<Vector2Int>> segments =
            PlayerReservedCommand.BuildMoveExecutionSegments(path);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0], Is.EqualTo(new[]
        {
            Vector2Int.right,
            Vector2Int.right
        }));
    }

    [Test]
    public void BuildMoveExecutionSegments_DirectionReversalStartsNextSegment()
    {
        List<Vector2Int> path = new()
        {
            Vector2Int.right,
            Vector2Int.left
        };

        List<List<Vector2Int>> segments =
            PlayerReservedCommand.BuildMoveExecutionSegments(path);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0], Is.EqualTo(new[] { Vector2Int.right }));
        Assert.That(segments[1], Is.EqualTo(new[] { Vector2Int.left }));
    }

    [Test]
    public void BlockedMove_DoesNotRecalculateReservedCost()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Move_Test",
            CurrentCost = 10,
            MaxCost = 10
        };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_Move_Test",
            Category = Category.Move
        };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetMoveReservationCost(2, 1);
        command.MarkMoveCostConsumed();
        command.SetExecutedMoveDistance(0);

        Assert.That(command.GetBlockedMoveCostRefund(), Is.Zero);
        Assert.That(command.ApplyBlockedMoveCostRefund(), Is.Zero);
        Assert.That(runtime.CurrentCost, Is.EqualTo(10));
    }
}
