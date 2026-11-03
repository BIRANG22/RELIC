using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleConsecutiveActionCameraContinuityTests
{
    [Test]
    public void ShouldKeepCameraAcrossBatchBoundary_PreservesConsecutiveGroupAcrossSlots()
    {
        GameObject owner = new GameObject("ConsecutiveCameraTest");

        try
        {
            BattleTurnExecutor executor = owner.AddComponent<BattleTurnExecutor>();
            BattleActionRunner runner = new BattleActionRunner(null);
            List<BattleActionBatch> batches = CreateBatches("S_Test", "S_Test");
            BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(batches, 1.2f);

            Assert.That(
                executor.ShouldKeepCameraAcrossBatchBoundary(
                    batches[0], batches, 1, runner, plan),
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void ShouldKeepCameraAcrossBatchBoundary_ReturnsForDifferentActionAcrossSlots()
    {
        GameObject owner = new GameObject("ConsecutiveCameraTest");

        try
        {
            BattleTurnExecutor executor = owner.AddComponent<BattleTurnExecutor>();
            BattleActionRunner runner = new BattleActionRunner(null);
            List<BattleActionBatch> batches = CreateBatches("S_Test_A", "S_Test_B");
            BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(batches, 1.2f);

            Assert.That(
                executor.ShouldKeepCameraAcrossBatchBoundary(
                    batches[0], batches, 1, runner, plan),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static List<BattleActionBatch> CreateBatches(string firstSkillId, string secondSkillId)
    {
        CharacterRuntimeData actor = new CharacterRuntimeData { CharacterId = "C_Test" };
        BattleActionBatch first = CreateBatch(actor, firstSkillId, 0);
        BattleActionBatch second = CreateBatch(actor, secondSkillId, 1);
        return new List<BattleActionBatch> { first, second };
    }

    private static BattleActionBatch CreateBatch(
        CharacterRuntimeData actor,
        string skillId,
        int slotIndex)
    {
        SkillMasterData skill = new SkillMasterData
        {
            SkillId = skillId,
            Category = Category.Ability,
            TimelineNotation = TimelineActionType.Attack
        };
        PlayerReservedCommand command = new PlayerReservedCommand(actor, skill);
        command.SetDirectionResult(
            BattleDirection.Right,
            new List<int> { 4 },
            new List<int> { 4 });

        BattleActionBatch batch = new BattleActionBatch();
        batch.SetTimelineSlotIndexIfNeeded(slotIndex);
        batch.PlayerCommands.Add(command);
        return batch;
    }
}
