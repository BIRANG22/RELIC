using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleActionBatchBuilderMultiCharacterTests
{
    private readonly List<GameObject> objects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }

        objects.Clear();
    }

    [Test]
    public void Build_PreservesEachCharactersReservationOrderAcrossConflictBatches()
    {
        BattleTimelineController timeline = CreateTimeline();
        CharacterRuntimeData first = Runtime("OrderA");
        CharacterRuntimeData second = Runtime("OrderB");
        PlayerReservedCommand firstAttack = Attack(first, 5);
        PlayerReservedCommand blockedSecondAttack = Attack(second, 5);
        PlayerReservedCommand laterSecondAttack = Attack(second, 9);
        Assert.That(timeline.ConfirmPlayerCommandFromNetwork(0, firstAttack), Is.True);
        Assert.That(timeline.ConfirmPlayerCommandFromNetwork(0, blockedSecondAttack), Is.True);
        Assert.That(timeline.ConfirmPlayerCommandFromNetwork(0, laterSecondAttack), Is.True);

        List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

        Assert.That(batches, Has.Count.EqualTo(3));
        Assert.That(batches[0].PlayerCommands, Is.EqualTo(new[] { firstAttack }));
        Assert.That(batches[1].PlayerCommands, Is.EqualTo(new[] { blockedSecondAttack }));
        Assert.That(batches[2].PlayerCommands, Is.EqualTo(new[] { laterSecondAttack }));
    }

    [Test]
    public void Build_SeparatesMoveDestinationFromOtherCharactersEffectTarget()
    {
        BattleTimelineController timeline = CreateTimeline();
        CharacterRuntimeData mover = Runtime("Mover");
        CharacterRuntimeData attacker = Runtime("Attacker");
        PlayerReservedCommand move = Move(mover, 7);
        PlayerReservedCommand attack = Attack(attacker, 7);
        Assert.That(timeline.ConfirmPlayerCommandFromNetwork(0, move), Is.True);
        Assert.That(timeline.ConfirmPlayerCommandFromNetwork(0, attack), Is.True);

        List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

        Assert.That(batches, Has.Count.EqualTo(2));
        Assert.That(batches[0].PlayerCommands, Is.EqualTo(new[] { move }));
        Assert.That(batches[1].PlayerCommands, Is.EqualTo(new[] { attack }));
    }

    private BattleTimelineController CreateTimeline()
    {
        GameObject controllerObject = Track(new GameObject("BatchTimeline"));
        BattleTimelineController controller = controllerObject.AddComponent<BattleTimelineController>();
        GameObject slotObject = Track(new GameObject("BatchSlot"));
        ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
        slot.SetAutoBindButtonsInChildren(false);
        slot.Init(controller, 0);
        SetPrivate(controller, "reserveSlots", new[] { slot });
        SetPrivate(controller, "activeSlotIndex", 0);
        return controller;
    }

    private GameObject Track(GameObject value)
    {
        objects.Add(value);
        return value;
    }

    private static CharacterRuntimeData Runtime(string id) => new()
    {
        CharacterId = id,
        Direction = BattleDirection.Right,
        CurrentHP = 100,
        CurrentCost = 100
    };

    private static PlayerReservedCommand Attack(CharacterRuntimeData runtime, int targetGridIndex)
    {
        SkillMasterData skill = new()
        {
            SkillId = "Attack" + targetGridIndex,
            Category = Category.Core,
            Entry = 1,
            SkillType = SkillType.Attack,
            TimelineNotation = TimelineActionType.Attack
        };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetDirectionResult(
            BattleDirection.Right,
            new List<int> { targetGridIndex },
            new List<int> { targetGridIndex });
        return command;
    }

    private static PlayerReservedCommand Move(CharacterRuntimeData runtime, int targetGridIndex)
    {
        SkillMasterData skill = new()
        {
            SkillId = "Move" + targetGridIndex,
            Category = Category.Move,
            Entry = 1,
            SkillType = SkillType.None,
            TimelineNotation = TimelineActionType.Move
        };
        PlayerReservedCommand command = new(runtime, skill);
        command.SetSelectionResult(
            BattleDirection.Right,
            targetGridIndex,
            new List<int> { targetGridIndex },
            Vector2Int.right);
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
