using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleReservationAttackPreviewOriginTests
{
    [Test]
    public void PreviewMoveGridIndex_UsesReservedDestination_WhileExecutionUsesCollisionResult()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Preview_Origin_Test"
        };
        SkillMasterData skill = new()
        {
            SkillId = "Skill_Preview_Origin_Test",
            Category = Category.Move
        };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetSelectionResult(
            BattleDirection.Right,
            9,
            new List<int> { 9 },
            Vector2Int.right);
        command.SetSimulatedMoveResult(
            blocked: true,
            gridIndex: 4,
            moveOffset: Vector2Int.zero);

        MethodInfo previewPolicy = typeof(BattleTimelineController).GetMethod(
            "TryGetCommandPreviewMoveGridIndex",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(previewPolicy, Is.Not.Null);

        object[] arguments = { command, -1 };
        bool hasPreviewGrid = (bool)previewPolicy.Invoke(null, arguments);

        Assert.That(hasPreviewGrid, Is.True);
        Assert.That((int)arguments[1], Is.EqualTo(9));
        Assert.That(command.EffectiveMoveGridIndex, Is.EqualTo(4));
    }
}
