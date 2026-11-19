using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleReservationTutorialArabellaRegressionTests
{
    [Test]
    public void EquipmentSlotWarnings_HaveStableLocalizationKeysAndKoreanEntries()
    {
        Assert.That(LocalizationKeys.Battle.SkillSlotBlocked, Is.EqualTo("battle.skill_slot_blocked"));
        Assert.That(LocalizationKeys.Battle.SkillSlotLimit, Is.EqualTo("battle.skill_slot_limit"));

        string shared = File.ReadAllText("Assets/Language/Text Shared Data.asset");
        string korean = File.ReadAllText("Assets/Language/Text_ko.asset");
        StringAssert.Contains("m_Key: battle.skill_slot_blocked", shared);
        StringAssert.Contains("m_Key: battle.skill_slot_limit", shared);
        StringAssert.Contains("이 유물의 효과로 해당 타임라인 슬롯을 사용할 수 없습니다.", korean);
        StringAssert.Contains("이 유물의 효과로 행동을 예약할 수 있는 타임라인 슬롯은 최대 {0}개입니다.", korean);
    }

    [TestCase(false, false, false, KeyCode.Alpha1, true)]
    [TestCase(true, false, false, KeyCode.Alpha1, false)]
    [TestCase(true, false, false, KeyCode.Escape, true)]
    [TestCase(false, true, false, KeyCode.Alpha1, false)]
    [TestCase(false, true, true, KeyCode.Alpha1, true)]
    public void TutorialKeyboardPolicy_AllowsOnlyExplicitGuideKeys(
        bool dialogueRunning,
        bool focusGuideRunning,
        bool focusGuideAllowsKey,
        KeyCode key,
        bool expected)
    {
        Assert.That(
            TutorialBattleSequence.ResolveBattleKeyboardInputAllowed(
                dialogueRunning,
                focusGuideRunning,
                focusGuideAllowsKey,
                key),
            Is.EqualTo(expected));
    }

    [Test]
    public void TimelineDirectionInput_ChecksThePressedKeyPolicy()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs");
        StringAssert.Contains(
            "Input.GetKeyDown(KeyCode.RightArrow) &&\n            TutorialBattleSequence.IsBattleKeyboardInputAllowed(KeyCode.RightArrow)",
            source.Replace("\r\n", "\n"));
        StringAssert.Contains(
            "Input.GetKeyDown(KeyCode.LeftArrow) &&\n                 TutorialBattleSequence.IsBattleKeyboardInputAllowed(KeyCode.LeftArrow)",
            source.Replace("\r\n", "\n"));
    }

    [Test]
    public void ConsecutiveMovesInSameSlot_MergeIntoOneCommand()
    {
        GameObject controllerObject = new("TimelineController-MoveMerge-Test");
        GameObject slotObject = new("TimelineSlot-MoveMerge-Test");
        try
        {
            BattleTimelineController controller = controllerObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.SetAutoBindButtonsInChildren(false);
            slot.Init(controller, 0);
            SetPrivate(controller, "reserveSlots", new[] { slot });
            SetPrivate(controller, "activeSlotIndex", 0);

            CharacterRuntimeData runtime = Runtime("MoveMerge");
            PlayerReservedCommand first = Move(runtime, 1, Vector2Int.right);
            PlayerReservedCommand second = Move(runtime, 2, Vector2Int.right);

            Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, first), Is.True);
            Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, second), Is.True);

            Assert.That(slot.Commands, Has.Count.EqualTo(1));
            Assert.That(slot.Commands[0], Is.SameAs(first));
            Assert.That(first.ReservedMoveGridIndex, Is.EqualTo(2));
            Assert.That(first.VisualMoveSteps, Has.Count.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void MoveAfterAnotherAction_DoesNotMergeWithEarlierMove()
    {
        Assert.That(
            BattleTimelineController.ShouldMergeConsecutiveMoveCommands(
                previousIsMove: true,
                previousCharacterId: "Char_01",
                nextIsMove: true,
                nextCharacterId: "Char_01",
                hasExplicitInsertion: false,
                hasInterveningCommand: true),
            Is.False);
    }

    [Test]
    public void TerminalReservationRejection_ClearsPendingInsertion()
    {
        GameObject controllerObject = new("TimelineController-Rejection-Test");
        try
        {
            BattleTimelineController controller = controllerObject.AddComponent<BattleTimelineController>();
            controller.SetPendingPlayerInsertion(0, 2);

            controller.RejectPendingActionSelection(false);

            Assert.That(controller.HasPendingPlayerInsertion, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void TerminalReservationRejection_IsUsedByEquipmentAndMergeFailures()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/BattleTimelineController.cs");
        string normalized = source.Replace("\r\n", "\n");
        StringAssert.Contains(
            "ShowBattleWarning(equipmentBlockReason);\n            if (clearLocalSelectionOnReject)\n                RejectPendingActionSelection();",
            normalized);
        StringAssert.Contains(
            "ShowBattleWarning(blockReason);\n            if (clearLocalSelectionOnReject)\n                RejectPendingActionSelection();\n            return true;",
            normalized);

        string networkSource = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Network/SteamBattleStateSynchronizer.cs");
        StringAssert.Contains("clearLocalSelectionOnReject: false", networkSource);
    }

    [Test]
    public void OverlapFollowupWait_StopsWhenCompletedOrTimedOut()
    {
        Assert.That(BattleActionRunner.ShouldStopOverlapFollowupWait(true, 0.1f, 5f), Is.True);
        Assert.That(BattleActionRunner.ShouldStopOverlapFollowupWait(false, 5f, 5f), Is.True);
        Assert.That(BattleActionRunner.ShouldStopOverlapFollowupWait(false, 0.1f, 5f), Is.False);
    }

    [Test]
    public void OverlapFollowupCompletion_IsGuaranteedByFinally()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Actionrunner/BattleActionRunner.cs");
        int wrapper = source.IndexOf("RunEnumeratorAndComplete", System.StringComparison.Ordinal);
        Assert.That(wrapper, Is.GreaterThanOrEqualTo(0));
        string block = source.Substring(wrapper, System.Math.Min(900, source.Length - wrapper));
        StringAssert.Contains("finally", block);
        StringAssert.Contains("onComplete?.Invoke()", block);
        StringAssert.Contains("Overlap Followup Timeout", source);
    }

    [Test]
    public void ArabellaPresentationCancellation_CompletesOnce()
    {
        int completedCount = 0;
        System.Action handler = () => completedCount++;
        ArabellaPhaseTransition.PhaseTwoPresentationCompleted += handler;
        try
        {
            ArabellaPhaseTransition.NotifyPhaseTwoPresentationStarted();
            ArabellaPhaseTransition.CancelPresentationLock();
            ArabellaPhaseTransition.CancelPresentationLock();
            Assert.That(completedCount, Is.EqualTo(1));
        }
        finally
        {
            ArabellaPhaseTransition.PhaseTwoPresentationCompleted -= handler;
            ArabellaPhaseTransition.CancelPresentationLock();
        }
    }

    private static CharacterRuntimeData Runtime(string id) => new()
    {
        CharacterId = id,
        Direction = BattleDirection.Right,
        CurrentHP = 100,
        CurrentCost = 100,
        MaxCost = 100
    };

    private static PlayerReservedCommand Move(
        CharacterRuntimeData runtime,
        int gridIndex,
        Vector2Int offset)
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Move_1",
            Category = Category.Move,
            Entry = 1,
            TimelineNotation = TimelineActionType.Move
        };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetSelectionResult(
            BattleDirection.Right,
            gridIndex,
            new List<int> { gridIndex },
            offset);
        command.SetVisualMoveResult(gridIndex, offset, new[] { offset });
        command.SetMoveReservationCost(1, 1);
        return command;
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
