using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattleTimelineManualControlTests
{
    private GameObject controllerObject;
    private GameObject[] slotObjects;
    private GameObject characterPanelObject;

    [Test]
    public void RedoModifier_UsesEitherAltKey()
    {
        Assert.That(BattleTimelineController.IsRedoModifierPressed(true, false), Is.True);
        Assert.That(BattleTimelineController.IsRedoModifierPressed(false, true), Is.True);
        Assert.That(BattleTimelineController.IsRedoModifierPressed(false, false), Is.False);
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void ReservationHistoryButtons_RespectMenuAndTutorialInputLocks(
        bool menuOpen,
        bool tutorialPresentationRunning,
        bool expected)
    {
        Assert.That(
            BattleCharacterPanelUI.CanUseReservationHistoryButtons(menuOpen, tutorialPresentationRunning),
            Is.EqualTo(expected));
    }

    [TestCase(100, -1, false)]
    [TestCase(100, 101, false)]
    [TestCase(101, 101, true)]
    [TestCase(102, 101, true)]
    public void SkillEffectTooltip_WaitsUntilBaseTooltipLayoutReadyFrame(
        int currentFrame,
        int readyFrame,
        bool expected)
    {
        Assert.That(
            BattleCharacterPanelUI.IsSkillEffectTooltipLayoutReady(currentFrame, readyFrame),
            Is.EqualTo(expected));
    }

    [TearDown]
    public void TearDown()
    {
        if (slotObjects != null)
        {
            foreach (GameObject slotObject in slotObjects)
                if (slotObject != null) Object.DestroyImmediate(slotObject);
        }

        if (controllerObject != null)
            Object.DestroyImmediate(controllerObject);

        if (characterPanelObject != null)
            Object.DestroyImmediate(characterPanelObject);
    }

    [Test]
    public void SelectCharacter_DoesNotChangeActiveSlotWhenCurrentSlotBelongsToAnotherCharacter()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData first = Runtime("First", BattleDirection.Right);
        CharacterRuntimeData second = Runtime("Second", BattleDirection.Right);
        Assert.That(Slot(0).AddCommand(Attack(first)), Is.True);

        controller.SelectCharacter(second);

        Assert.That(controller.ActiveSlotIndex, Is.Zero);
    }

    [Test]
    public void FullSlot_DoesNotAdvanceActiveSlot()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("Full", BattleDirection.Right);
        for (int i = 0; i < ReserveTurnSlotUI.MaxCommandCount - 1; i++)
            Assert.That(Slot(0).AddCommand(Command(runtime, Category.Move)), Is.True);

        Assert.That(controller.ConfirmPlayerCommand(0, Command(runtime, Category.Move)), Is.True);

        Assert.That(controller.ActiveSlotIndex, Is.Zero);
    }

    [Test]
    public void RemoveMove_KeepsLaterMoveAndAttackAndRestoresRuntimeDirection()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("Sequence", BattleDirection.Right);
        PlayerReservedCommand firstMove = Move(runtime, BattleDirection.Left, 2, Vector2Int.left);
        PlayerReservedCommand laterMove = Move(runtime, BattleDirection.Right, 3, Vector2Int.right);
        PlayerReservedCommand attack = Attack(runtime);
        Assert.That(Slot(0).AddCommand(firstMove), Is.True);
        Assert.That(Slot(0).AddCommand(laterMove), Is.True);
        Assert.That(Slot(0).AddCommand(attack), Is.True);

        Assert.That(controller.RemoveCommandFromNetwork(0, 0), Is.True);

        Assert.That(Slot(0).Commands, Has.Count.EqualTo(2));
        Assert.That(Slot(0).Commands[0], Is.SameAs(laterMove));
        Assert.That(Slot(0).Commands[1], Is.SameAs(attack));

        Assert.That(controller.RemoveCommandFromNetwork(0, 0), Is.True);
        Assert.That(controller.GetPreviewDirection(runtime, 0), Is.EqualTo(BattleDirection.Right));
        Assert.That(Slot(0).Commands, Has.Count.EqualTo(1));
        Assert.That(Slot(0).Commands[0], Is.SameAs(attack));
    }

    [Test]
    public void ConfirmEarlierMove_KeepsAlreadyReservedLaterAttack()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("Insert", BattleDirection.Right);
        PlayerReservedCommand attack = Attack(runtime);
        Assert.That(Slot(1).AddCommand(attack), Is.True);

        PlayerReservedCommand move = Move(runtime, BattleDirection.Left, 2, Vector2Int.left);
        Assert.That(controller.ConfirmPlayerCommand(0, move), Is.True);

        Assert.That(Slot(1).Commands, Has.Count.EqualTo(1));
        Assert.That(Slot(1).Commands[0], Is.SameAs(attack));
    }

    [Test]
    public void RemoveCommand_DoesNotSelectRemovedCommandsSlot()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 1);
        CharacterRuntimeData runtime = Runtime("Remove", BattleDirection.Right);
        Assert.That(Slot(0).AddCommand(Attack(runtime)), Is.True);

        Assert.That(controller.RemoveCommandFromNetwork(0, 0), Is.True);

        Assert.That(controller.ActiveSlotIndex, Is.EqualTo(1));
    }

    [Test]
    public void UndoLastReservation_DoesNotSelectRemovedCommandsSlot()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("Undo", BattleDirection.Right);
        Assert.That(controller.ConfirmPlayerCommand(0, Attack(runtime)), Is.True);
        Assert.That(controller.SelectTimelineSlotFromNetwork(1, false, false), Is.True);

        InvokePrivate(controller, "HandleKeyboardUndoReservationInput");

        Assert.That(controller.ActiveSlotIndex, Is.EqualTo(1));
        Assert.That(Slot(0).Commands, Is.Empty);
    }

    [Test]
    public void RedoLastReservation_RestoresCommandAtOriginalOrder()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("RedoOrder", BattleDirection.Right);
        PlayerReservedCommand before = Attack(runtime);
        PlayerReservedCommand restored = Attack(runtime);
        PlayerReservedCommand after = Attack(runtime);
        Assert.That(Slot(0).AddCommand(before), Is.True);
        Assert.That(Slot(0).AddCommand(after), Is.True);
        controller.SetPendingPlayerInsertion(0, 1);
        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, restored), Is.True);

        controller.UndoLastPlayerReservation();
        Assert.That(controller.RedoLastPlayerReservation(), Is.True);

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { before, restored, after }));
    }

    [Test]
    public void CancelAndRestoreButtons_InvokeReservationUndoAndRedo()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("ButtonHistory", BattleDirection.Right);
        PlayerReservedCommand command = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, command), Is.True);

        characterPanelObject = new GameObject("BattleCharacterPanel", typeof(RectTransform));
        characterPanelObject.SetActive(false);
        Button cancelButton = CreateButton(characterPanelObject.transform, "CancelButton");
        Button restoreButton = CreateButton(characterPanelObject.transform, "RestoreButton");
        BattleCharacterPanelUI panel = characterPanelObject.AddComponent<BattleCharacterPanelUI>();
        SetPrivate(panel, "battleTimelineController", controller);
        InvokePrivate(panel, "Awake");

        cancelButton.onClick.Invoke();
        Assert.That(Slot(0).Commands, Is.Empty);

        restoreButton.onClick.Invoke();
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { command }));
    }

    [Test]
    public void RedoLastReservation_AfterMultipleUndos_RestoresInForwardOrder()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("RedoSequence", BattleDirection.Right);
        PlayerReservedCommand first = Attack(runtime);
        PlayerReservedCommand second = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, first), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, second), Is.True);

        controller.UndoLastPlayerReservation();
        controller.UndoLastPlayerReservation();

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { first }));
        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void RedoLastReservation_RestoresOriginalFirstSkillCostContext()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("RedoFirstSkillCost", BattleDirection.Right);
        runtime.CurrentCost = 5;
        runtime.AppliedBattleEquipmentEffectIds.Add("State_Rune_Next_Turn_First_Skill_Free");
        PlayerReservedCommand first = Attack(runtime, 10);
        PlayerReservedCommand second = Attack(runtime, 1);
        Assert.That(controller.ConfirmPlayerCommand(0, first), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, second), Is.True);
        Assert.That(first.Cost, Is.Zero);
        Assert.That(second.Cost, Is.EqualTo(1));
        Assert.That(controller.TryMovePlayerReservation(first, 0, 0), Is.True);
        controller.UndoLastPlayerReservation();

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { first, second }));
        Assert.That(first.Cost, Is.Zero);
        Assert.That(second.Cost, Is.EqualTo(1));
        Assert.That(runtime.ReservedCost, Is.EqualTo(1));
    }

    [Test]
    public void NewReservation_AfterUndo_InvalidatesRedoHistory()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("RedoBranch", BattleDirection.Right);
        PlayerReservedCommand undone = Attack(runtime);
        PlayerReservedCommand replacement = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, undone), Is.True);
        controller.UndoLastPlayerReservation();

        Assert.That(controller.ConfirmPlayerCommand(0, replacement), Is.True);

        Assert.That(controller.RedoLastPlayerReservation(), Is.False);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { replacement }));
    }

    [Test]
    public void DirectReservationRemoval_AfterUndo_RedoRestoresClickedCommand()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("RedoDeleteBranch", BattleDirection.Right);
        PlayerReservedCommand retained = Attack(runtime);
        PlayerReservedCommand undone = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, retained), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, undone), Is.True);
        controller.UndoLastPlayerReservation();

        controller.RemoveCommand(0, retained);

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { retained }));
    }

    [Test]
    public void RedoLastReservation_WhenOriginalSlotIsUnavailable_LeavesStateUnchanged()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("RedoMissingSlot", BattleDirection.Right);
        PlayerReservedCommand command = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(1, command), Is.True);
        controller.UndoLastPlayerReservation();
        Object.DestroyImmediate(slotObjects[1]);
        slotObjects[1] = null;

        Assert.That(controller.RedoLastPlayerReservation(), Is.False);
        Assert.That(Slot(0).Commands, Is.Empty);
    }

    [Test]
    public void RedoAdd_WhenCostBecomesUnaffordable_ClearsRolledBackReservedCost()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("RedoCostRollback", BattleDirection.Right);
        runtime.CurrentCost = 5;
        PlayerReservedCommand command = Attack(runtime, 5);
        Assert.That(controller.ConfirmPlayerCommand(0, command), Is.True);
        controller.UndoLastPlayerReservation();
        runtime.CurrentCost = 3;

        Assert.That(controller.RedoLastPlayerReservation(), Is.False);

        Assert.That(Slot(0).Commands, Is.Empty);
        Assert.That(runtime.ReservedCost, Is.Zero);
        runtime.CurrentCost = 5;
        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(runtime.ReservedCost, Is.EqualTo(5));
    }

    [Test]
    public void RemoveCommandByReference_StaleRepeatedRequestDoesNotRemoveNextCommand()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("StableIdentity", BattleDirection.Right);
        PlayerReservedCommand first = Attack(runtime);
        PlayerReservedCommand second = Attack(runtime);
        PlayerReservedCommand third = Attack(runtime);
        Assert.That(Slot(0).AddCommand(first), Is.True);
        Assert.That(Slot(0).AddCommand(second), Is.True);
        Assert.That(Slot(0).AddCommand(third), Is.True);

        controller.RemoveCommand(0, first);
        controller.RemoveCommand(0, first);

        Assert.That(Slot(0).Commands, Has.Count.EqualTo(2));
        Assert.That(Slot(0).Commands[0], Is.SameAs(second));
        Assert.That(Slot(0).Commands[1], Is.SameAs(third));
    }

    [Test]
    public void MoveReservation_BetweenSlots_InsertsWithoutChangingMovePath()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("DragMove", BattleDirection.Right);
        PlayerReservedCommand moving = Move(runtime, BattleDirection.Left, 2, Vector2Int.left);
        PlayerReservedCommand before = Attack(runtime);
        PlayerReservedCommand after = Attack(runtime);
        Assert.That(Slot(0).AddCommand(moving), Is.True);
        Assert.That(Slot(1).AddCommand(before), Is.True);
        Assert.That(Slot(1).AddCommand(after), Is.True);

        Assert.That(controller.TryMovePlayerReservation(moving, 1, 1), Is.True);

        Assert.That(Slot(0).Commands, Is.Empty);
        Assert.That(Slot(1).Commands, Is.EqualTo(new[] { before, moving, after }));
        Assert.That(moving.VisualMoveSteps, Is.EqualTo(new[] { Vector2Int.left }));
    }

    [Test]
    public void MoveReservation_UndoRestoresOriginalSlotInsteadOfRemovingCommand()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("MoveUndo", BattleDirection.Right);
        PlayerReservedCommand command = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, command), Is.True);
        Assert.That(controller.TryMovePlayerReservation(command, 1, 0), Is.True);

        controller.UndoLastPlayerReservation();

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { command }));
        Assert.That(Slot(1).Commands, Is.Empty);
    }

    [Test]
    public void MoveReservation_RedoRestoresDestinationSlot()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData runtime = Runtime("MoveRedo", BattleDirection.Right);
        PlayerReservedCommand command = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, command), Is.True);
        Assert.That(controller.TryMovePlayerReservation(command, 1, 0), Is.True);
        controller.UndoLastPlayerReservation();

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);

        Assert.That(Slot(0).Commands, Is.Empty);
        Assert.That(Slot(1).Commands, Is.EqualTo(new[] { command }));
    }

    [Test]
    public void ReorderReservation_InSameSlot_UndoAndRedoRestoreExactOrder()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("ReorderUndo", BattleDirection.Right);
        PlayerReservedCommand first = Attack(runtime);
        PlayerReservedCommand second = Attack(runtime);
        PlayerReservedCommand third = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, first), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, second), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, third), Is.True);
        Assert.That(controller.TryMovePlayerReservation(first, 0, 3), Is.True);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { second, third, first }));

        controller.UndoLastPlayerReservation();
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { first, second, third }));

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { second, third, first }));
    }

    [Test]
    public void MoveReservation_MultipleMoves_UndoAndRedoEachStep()
    {
        BattleTimelineController controller = CreateControllerWithSlots(3, 0);
        CharacterRuntimeData runtime = Runtime("MoveSteps", BattleDirection.Right);
        PlayerReservedCommand command = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, command), Is.True);
        Assert.That(controller.TryMovePlayerReservation(command, 1, 0), Is.True);
        Assert.That(controller.TryMovePlayerReservation(command, 2, 0), Is.True);

        controller.UndoLastPlayerReservation();
        Assert.That(Slot(1).Commands, Is.EqualTo(new[] { command }));
        controller.UndoLastPlayerReservation();
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { command }));

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(1).Commands, Is.EqualTo(new[] { command }));
        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(2).Commands, Is.EqualTo(new[] { command }));
    }

    [Test]
    public void MoveReservationUndo_WhenOriginalSlotHasOtherCharacter_LeavesMoveUnchanged()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData movingRuntime = Runtime("MoveBlocked", BattleDirection.Right);
        CharacterRuntimeData blockerRuntime = Runtime("MoveBlocker", BattleDirection.Right);
        PlayerReservedCommand moving = Attack(movingRuntime);
        PlayerReservedCommand blocker = Attack(blockerRuntime);
        Assert.That(controller.ConfirmPlayerCommand(0, moving), Is.True);
        Assert.That(controller.TryMovePlayerReservation(moving, 1, 0), Is.True);
        Assert.That(Slot(0).AddCommand(blocker), Is.True);

        controller.UndoLastPlayerReservation();

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { blocker }));
        Assert.That(Slot(1).Commands, Is.EqualTo(new[] { moving }));
    }

    [Test]
    public void MoveReservationUndo_AfterAnotherCommandDeletion_DoesNotUndoOriginalAdd()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("MoveIndexShift", BattleDirection.Right);
        PlayerReservedCommand first = Attack(runtime);
        PlayerReservedCommand second = Attack(runtime);
        PlayerReservedCommand moving = Attack(runtime);
        Assert.That(controller.ConfirmPlayerCommand(0, first), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, second), Is.True);
        Assert.That(controller.ConfirmPlayerCommand(0, moving), Is.True);
        Assert.That(controller.TryMovePlayerReservation(moving, 0, 1), Is.True);
        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { first, moving, second }));
        Assert.That(controller.RemoveCommandFromNetwork(0, 0), Is.True);

        controller.UndoLastPlayerReservation();

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { second, moving }));
    }

    [Test]
    public void MoveReservation_InvalidCharacterDestination_LeavesBothSlotsUnchanged()
    {
        BattleTimelineController controller = CreateControllerWithSlots(2, 0);
        CharacterRuntimeData first = Runtime("FirstDrag", BattleDirection.Right);
        CharacterRuntimeData second = Runtime("SecondDrag", BattleDirection.Right);
        PlayerReservedCommand moving = Attack(first);
        PlayerReservedCommand occupied = Attack(second);
        Assert.That(Slot(0).AddCommand(moving), Is.True);
        Assert.That(Slot(1).AddCommand(occupied), Is.True);

        Assert.That(controller.TryMovePlayerReservation(moving, 1, 0), Is.False);

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { moving }));
        Assert.That(Slot(1).Commands, Is.EqualTo(new[] { occupied }));
    }

    [Test]
    public void PendingInsertion_ConfirmCommand_InsertsBetweenExistingCommands()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("DropSkill", BattleDirection.Right);
        PlayerReservedCommand before = Attack(runtime);
        PlayerReservedCommand inserted = Attack(runtime);
        PlayerReservedCommand after = Attack(runtime);
        Assert.That(Slot(0).AddCommand(before), Is.True);
        Assert.That(Slot(0).AddCommand(after), Is.True);

        controller.SetPendingPlayerInsertion(0, 1);
        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, inserted), Is.True);

        Assert.That(Slot(0).Commands, Is.EqualTo(new[] { before, inserted, after }));
    }

    [Test]
    public void FacingFlipReservation_UsesPendingInsertionIndex()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("FlipDrop", BattleDirection.Right);
        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_1",
            Category = Category.Move,
            Entry = 1
        };
        PlayerReservedCommand before = Attack(runtime);
        PlayerReservedCommand after = Attack(runtime);
        before.SetSelectionResult(
            BattleDirection.Left,
            3,
            new System.Collections.Generic.List<int> { 3 },
            Vector2Int.zero);
        after.SetSelectionResult(
            BattleDirection.Right,
            3,
            new System.Collections.Generic.List<int> { 3 },
            Vector2Int.zero);
        Assert.That(Slot(0).AddCommand(before), Is.True);
        Assert.That(Slot(0).AddCommand(after), Is.True);

        controller.SetPendingPlayerInsertion(0, 1);
        Assert.That(controller.TryReserveFacingFlip(runtime, moveSkill), Is.True);

        Assert.That(Slot(0).Commands[0], Is.SameAs(before));
        Assert.That(Slot(0).Commands[1].IsFacingFlipCommand, Is.True);
        Assert.That(Slot(0).Commands[1].Direction, Is.EqualTo(BattleDirection.Right));
        Assert.That(Slot(0).Commands[2], Is.SameAs(after));
    }

    [Test]
    public void FacingFlipFollowedByAttack_MergesIntoAttackAndUndoRemovesAttack()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("FlipMerge", BattleDirection.Right);
        PlayerReservedCommand flip = Move(runtime, BattleDirection.Left, 3, Vector2Int.zero);
        flip.SetFacingFlipCommand();
        PlayerReservedCommand attack = Attack(runtime);
        attack.SetDirectionResult(BattleDirection.Left, new System.Collections.Generic.List<int> { 3 }, new System.Collections.Generic.List<int> { 3 });

        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, flip), Is.True);
        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, attack), Is.True);

        Assert.That(Slot(0).Commands, Has.Count.EqualTo(1));
        Assert.That(Slot(0).Commands[0], Is.SameAs(attack));
        Assert.That(attack.FlipBeforeAction, Is.True);
        Assert.That(controller.GetPreviewDirection(runtime, 0), Is.EqualTo(BattleDirection.Left));

        controller.UndoLastPlayerReservation();
        Assert.That(Slot(0).Commands, Is.Empty);

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(0).Commands, Has.Count.EqualTo(1));
        Assert.That(Slot(0).Commands[0], Is.SameAs(attack));
        Assert.That(attack.FlipBeforeAction, Is.True);
    }

    [Test]
    public void FacingFlip_CanBeReservedWhenTimelineCapacityIsFull()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("FullSlotFlip", BattleDirection.Right);

        for (int i = 0; i < ReserveTurnSlotUI.MaxCommandCount; i++)
            Assert.That(Slot(0).AddCommand(Attack(runtime)), Is.True);

        PlayerReservedCommand flip = Move(runtime, BattleDirection.Left, 3, Vector2Int.zero);
        flip.SetFacingFlipCommand();

        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, flip), Is.True);
        Assert.That(Slot(0).Commands, Has.Count.EqualTo(ReserveTurnSlotUI.MaxCommandCount + 1));
        Assert.That(Slot(0).Commands[ReserveTurnSlotUI.MaxCommandCount], Is.SameAs(flip));
    }

    [Test]
    public void FacingFlipFollowedByMove_MergesIntoMoveAndUndoRemovesMove()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("FlipMoveMerge", BattleDirection.Right);
        PlayerReservedCommand flip = Move(runtime, BattleDirection.Left, 3, Vector2Int.zero);
        flip.SetFacingFlipCommand();
        PlayerReservedCommand move = Move(runtime, BattleDirection.Left, 2, Vector2Int.left);

        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, flip), Is.True);
        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, move), Is.True);

        Assert.That(Slot(0).Commands, Has.Count.EqualTo(1));
        Assert.That(Slot(0).Commands[0], Is.SameAs(move));
        Assert.That(move.FlipBeforeAction, Is.True);

        controller.UndoLastPlayerReservation();
        Assert.That(Slot(0).Commands, Is.Empty);

        Assert.That(controller.RedoLastPlayerReservation(), Is.True);
        Assert.That(Slot(0).Commands, Has.Count.EqualTo(1));
        Assert.That(Slot(0).Commands[0], Is.SameAs(move));
        Assert.That(move.FlipBeforeAction, Is.True);
    }

    [Test]
    public void FacingFlipFollowedByZeroOffsetMove_DoesNotMergeIntoEarlierMove()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        CharacterRuntimeData runtime = Runtime("FlipZeroMoveMerge", BattleDirection.Right);
        PlayerReservedCommand earlierMove = Move(runtime, BattleDirection.Right, 4, Vector2Int.right);
        PlayerReservedCommand flip = Move(runtime, BattleDirection.Left, 4, Vector2Int.zero);
        flip.SetFacingFlipCommand();
        PlayerReservedCommand nextMove = Move(runtime, BattleDirection.Left, 4, Vector2Int.zero);

        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, earlierMove), Is.True);
        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, flip), Is.True);
        Assert.That(controller.ConfirmPlayerCommandFromNetwork(0, nextMove), Is.True);

        Assert.That(Slot(0).Commands, Has.Count.EqualTo(2));
        Assert.That(Slot(0).Commands[0], Is.SameAs(earlierMove));
        Assert.That(Slot(0).Commands[1], Is.SameAs(nextMove));
        Assert.That(nextMove.FlipBeforeAction, Is.True);
    }

    [Test]
    public void CancelPendingSelection_ClearsPendingDragInsertion()
    {
        BattleTimelineController controller = CreateControllerWithSlots(1, 0);
        controller.SetPendingPlayerInsertion(0, 0);

        controller.CancelPendingActionSelection(false);

        Assert.That(controller.HasPendingPlayerInsertion, Is.False);
    }

    private BattleTimelineController CreateControllerWithSlots(int count, int activeSlotIndex)
    {
        controllerObject = new GameObject("TimelineControllerTest");
        BattleTimelineController controller = controllerObject.AddComponent<BattleTimelineController>();
        slotObjects = new GameObject[count];
        ReserveTurnSlotUI[] slots = new ReserveTurnSlotUI[count];
        for (int i = 0; i < count; i++)
        {
            slotObjects[i] = new GameObject("Slot" + i);
            slots[i] = slotObjects[i].AddComponent<ReserveTurnSlotUI>();
            slots[i].SetAutoBindButtonsInChildren(false);
            slots[i].Init(controller, i);
        }

        SetPrivate(controller, "reserveSlots", slots);
        SetPrivate(controller, "activeSlotIndex", activeSlotIndex);
        return controller;
    }

    private ReserveTurnSlotUI Slot(int index) => slotObjects[index].GetComponent<ReserveTurnSlotUI>();

    private static CharacterRuntimeData Runtime(string id, BattleDirection direction) => new()
    {
        CharacterId = id,
        Direction = direction,
        CurrentHP = 100,
        CurrentCost = 100
    };

    private static PlayerReservedCommand Command(CharacterRuntimeData runtime, Category category)
    {
        SkillMasterData skill = new()
        {
            SkillId = category + "Skill",
            Category = category,
            Entry = 1,
            SkillType = SkillType.None
        };
        return new PlayerReservedCommand(runtime, skill);
    }

    private static PlayerReservedCommand Attack(CharacterRuntimeData runtime, int cost = 0)
    {
        SkillMasterData skill = new()
        {
            SkillId = "AttackSkill",
            Category = Category.Core,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostValue = cost,
            Entry = 1,
            SkillType = SkillType.Attack,
            TimelineNotation = TimelineActionType.Attack
        };
        return new PlayerReservedCommand(runtime, skill);
    }

    private static PlayerReservedCommand Move(
        CharacterRuntimeData runtime,
        BattleDirection direction,
        int gridIndex,
        Vector2Int offset)
    {
        PlayerReservedCommand command = Command(runtime, Category.Move);
        command.SetSelectionResult(direction, gridIndex, new System.Collections.Generic.List<int> { gridIndex }, offset);
        command.SetVisualMoveResult(gridIndex, offset, new[] { offset });
        return command;
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static Button CreateButton(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(target, null);
    }

}
