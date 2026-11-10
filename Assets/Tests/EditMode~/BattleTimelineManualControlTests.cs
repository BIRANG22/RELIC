using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleTimelineManualControlTests
{
    private GameObject controllerObject;
    private GameObject[] slotObjects;

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

    private static PlayerReservedCommand Attack(CharacterRuntimeData runtime)
    {
        SkillMasterData skill = new()
        {
            SkillId = "AttackSkill",
            Category = Category.Core,
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

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(target, null);
    }

}
