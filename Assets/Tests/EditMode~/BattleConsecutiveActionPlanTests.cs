using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleConsecutiveActionPlanTests
{
    [Test]
    public void Build_GroupsMatchingPlayerActionsAcrossTimelineSlots()
    {
        PlayerReservedCommand first = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            11,
            12);
        PlayerReservedCommand second = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            12,
            11);

        BattleActionBatch firstBatch = CreateBatch(0, first);
        BattleActionBatch secondBatch = CreateBatch(1, second);

        BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(
            new[] { firstBatch, secondBatch },
            1.5f);

        BattleConsecutiveActionInfo firstInfo = plan.GetInfo(first);
        BattleConsecutiveActionInfo secondInfo = plan.GetInfo(second);

        Assert.That(firstInfo.IsGrouped, Is.True);
        Assert.That(firstInfo.GroupSize, Is.EqualTo(2));
        Assert.That(firstInfo.GroupIndex, Is.EqualTo(0));
        Assert.That(firstInfo.IsGroupStart, Is.True);
        Assert.That(firstInfo.IsGroupEnd, Is.False);
        Assert.That(firstInfo.ShouldEnterCamera, Is.True);
        Assert.That(firstInfo.ShouldPlayExternalImpact, Is.False);
        Assert.That(firstInfo.ShouldReturnCamera, Is.False);

        Assert.That(secondInfo.IsGrouped, Is.True);
        Assert.That(secondInfo.GroupSize, Is.EqualTo(2));
        Assert.That(secondInfo.GroupIndex, Is.EqualTo(1));
        Assert.That(secondInfo.IsGroupStart, Is.False);
        Assert.That(secondInfo.IsGroupEnd, Is.True);
        Assert.That(secondInfo.ShouldEnterCamera, Is.False);
        Assert.That(secondInfo.ShouldPlayExternalImpact, Is.True);
        Assert.That(secondInfo.ShouldReturnCamera, Is.True);
        Assert.That(secondInfo.SpeedMultiplier, Is.EqualTo(1.5f));
    }

    [TestCase("Char_B", "Skill_A", BattleDirection.Right, 11, TestName = "Build_DifferentActorBreaksGroup")]
    [TestCase("Char_A", "Skill_B", BattleDirection.Right, 11, TestName = "Build_DifferentSkillBreaksGroup")]
    [TestCase("Char_A", "Skill_A", BattleDirection.Left, 11, TestName = "Build_DifferentDirectionBreaksGroup")]
    [TestCase("Char_A", "Skill_A", BattleDirection.Right, 13, TestName = "Build_DifferentTargetBreaksGroup")]
    public void Build_DifferentSignatureFieldBreaksGroup(
        string secondActor,
        string secondSkill,
        BattleDirection secondDirection,
        int secondTarget)
    {
        PlayerReservedCommand first = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            11);
        PlayerReservedCommand second = CreatePlayerSkill(
            secondActor,
            secondSkill,
            secondDirection,
            secondTarget);

        BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(
            new[] { CreateBatch(0, first), CreateBatch(1, second) },
            2f);

        Assert.That(plan.GetInfo(first).IsGrouped, Is.False);
        Assert.That(plan.GetInfo(second).IsGrouped, Is.False);
        Assert.That(plan.GetInfo(first).SpeedMultiplier, Is.EqualTo(1f));
        Assert.That(plan.GetInfo(second).SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void Build_MoveBetweenMatchingActionsBreaksGroupAndKeepsMoveUngrouped()
    {
        PlayerReservedCommand first = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            11);
        PlayerReservedCommand move = CreatePlayerMove("Char_A", 15);
        PlayerReservedCommand third = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            11);

        BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(
            new[]
            {
                CreateBatch(0, first),
                CreateBatch(1, move),
                CreateBatch(2, third)
            },
            1.5f);

        Assert.That(plan.GetInfo(first).IsGrouped, Is.False);
        Assert.That(plan.GetInfo(move).IsGrouped, Is.False);
        Assert.That(plan.GetInfo(third).IsGrouped, Is.False);
    }

    [Test]
    public void Build_GroupsMatchingMonsterActionsByRuntimeIdSkillTargetsAndDirection()
    {
        MonsterReservedCommand first = CreateMonsterSkill(
            "Runtime_A",
            "MonsterSkill_A",
            BattleDirection.Left,
            4,
            5);
        MonsterReservedCommand second = CreateMonsterSkill(
            "Runtime_A",
            "MonsterSkill_A",
            BattleDirection.Left,
            5,
            4);

        BattleActionBatch firstBatch = new();
        firstBatch.SetTimelineSlotIndexIfNeeded(0);
        firstBatch.MonsterCommands.Add(first);

        BattleActionBatch secondBatch = new();
        secondBatch.SetTimelineSlotIndexIfNeeded(1);
        secondBatch.MonsterCommands.Add(second);

        BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(
            new[] { firstBatch, secondBatch },
            1.75f);

        Assert.That(plan.GetInfo(first).IsGroupStart, Is.True);
        Assert.That(plan.GetInfo(second).IsGroupEnd, Is.True);
        Assert.That(plan.GetInfo(first).SpeedMultiplier, Is.EqualTo(1.75f));
    }

    [Test]
    public void Build_ClampsGroupedSpeedToAtLeastOne()
    {
        PlayerReservedCommand first = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            11);
        PlayerReservedCommand second = CreatePlayerSkill(
            "Char_A",
            "Skill_A",
            BattleDirection.Right,
            11);

        BattleConsecutiveActionPlan plan = BattleConsecutiveActionPlan.Build(
            new[] { CreateBatch(0, first), CreateBatch(1, second) },
            0.25f);

        Assert.That(plan.GetInfo(first).SpeedMultiplier, Is.EqualTo(1f));
        Assert.That(plan.GetInfo(second).SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void PresentationContext_AllowsStatusPulseOnlyAtGroupEnd()
    {
        BattleConsecutiveActionInfo first = new(
            groupId: 7,
            groupIndex: 0,
            groupSize: 2,
            speedMultiplier: 1.5f);
        BattleConsecutiveActionInfo last = new(
            groupId: 7,
            groupIndex: 1,
            groupSize: 2,
            speedMultiplier: 1.5f);

        try
        {
            BattleConsecutiveActionPresentationContext.BeginAction(first);
            BattleHitImpactFeedback feedback =
                Object.FindFirstObjectByType<BattleHitImpactFeedback>(
                    FindObjectsInactive.Include);

            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.enabled, Is.False);
            Assert.That(
                BattleConsecutiveActionPresentationContext.ShouldPlayStatusPulse,
                Is.False);

            BattleConsecutiveActionPresentationContext.CompleteAction(
                first,
                completedNormally: true);
            BattleConsecutiveActionPresentationContext.BeginAction(last);

            Assert.That(feedback.enabled, Is.True);
            Assert.That(
                BattleConsecutiveActionPresentationContext.ShouldPlayStatusPulse,
                Is.True);
        }
        finally
        {
            BattleConsecutiveActionPresentationContext.EndGroup();

            BattleHitImpactFeedback feedback =
                Object.FindFirstObjectByType<BattleHitImpactFeedback>(
                    FindObjectsInactive.Include);

            if (feedback != null)
                Object.DestroyImmediate(feedback.gameObject);
        }
    }

    [Test]
    public void PresentationContext_ScalesProjectileDeltaTimeForGroupedAction()
    {
        BattleConsecutiveActionInfo info = new(
            groupId: 3,
            groupIndex: 0,
            groupSize: 2,
            speedMultiplier: 1.5f);

        try
        {
            BattleConsecutiveActionPresentationContext.BeginAction(info);

            Assert.That(
                BattleConsecutiveActionPresentationContext.ScaleDeltaTime(0.2f),
                Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(
                BattleConsecutiveActionPresentationContext.ScaleDuration(0.3f),
                Is.EqualTo(0.2f).Within(0.0001f));
        }
        finally
        {
            BattleConsecutiveActionPresentationContext.EndGroup();
        }
    }

    [Test]
    public void PresentationContext_KeepsGroupedActionBeatAtMinimumDuration()
    {
        BattleConsecutiveActionInfo info = new(
            groupId: 4,
            groupIndex: 0,
            groupSize: 3,
            speedMultiplier: 2f);

        try
        {
            BattleConsecutiveActionPresentationContext.BeginAction(info);

            Assert.That(
                BattleConsecutiveActionPresentationContext.ScaleActionBeatDuration(
                    0.03f,
                    0.12f),
                Is.EqualTo(0.12f).Within(0.0001f));
        }
        finally
        {
            BattleConsecutiveActionPresentationContext.EndGroup();
        }
    }

    [Test]
    public void PresentationContext_LeavesSingleActionBeatUnclamped()
    {
        BattleConsecutiveActionPresentationContext.BeginAction(
            BattleConsecutiveActionInfo.Single);

        Assert.That(
            BattleConsecutiveActionPresentationContext.ScaleActionBeatDuration(
                0.03f,
                0.12f),
            Is.EqualTo(0.03f).Within(0.0001f));
    }

    private static BattleActionBatch CreateBatch(
        int timelineSlotIndex,
        PlayerReservedCommand command)
    {
        BattleActionBatch batch = new();
        batch.SetTimelineSlotIndexIfNeeded(timelineSlotIndex);
        batch.PlayerCommands.Add(command);
        return batch;
    }

    private static PlayerReservedCommand CreatePlayerSkill(
        string characterId,
        string skillId,
        BattleDirection direction,
        params int[] targetGridIndices)
    {
        PlayerReservedCommand command = new(
            new CharacterRuntimeData { CharacterId = characterId },
            new SkillMasterData
            {
                SkillId = skillId,
                Category = Category.Ability,
                TimelineNotation = TimelineActionType.Attack
            });

        List<int> targets = new(targetGridIndices);
        command.SetDirectionResult(direction, targets, targets);
        return command;
    }

    private static PlayerReservedCommand CreatePlayerMove(
        string characterId,
        int targetGridIndex)
    {
        PlayerReservedCommand command = new(
            new CharacterRuntimeData { CharacterId = characterId },
            new SkillMasterData
            {
                SkillId = "S_Move_Test",
                Category = Category.Move,
                TimelineNotation = TimelineActionType.Move,
                RangeType = RangeType.Selection
            });

        command.SetSelectionResult(
            BattleDirection.Right,
            targetGridIndex,
            new List<int> { targetGridIndex },
            Vector2Int.right);
        return command;
    }

    private static MonsterReservedCommand CreateMonsterSkill(
        string runtimeId,
        string skillId,
        BattleDirection direction,
        params int[] targetGridIndices)
    {
        MonsterRuntimeData runtime = new(
            runtimeId,
            new MonsterMasterData
            {
                MonsterId = "Monster_Test",
                Name = "Monster Test",
                HP = 10
            });

        MonsterReservedCommand command = new(
            runtime,
            new MonsterSkillData
            {
                SkillId = skillId,
                TimelineNotation = TimelineActionType.Attack,
                Target = TargetType.PlayerParty
            });

        command.SetForcedDirection(direction);
        command.SetExplicitRangeResult(
            new List<int>(targetGridIndices),
            new List<int>(targetGridIndices));
        return command;
    }
}
