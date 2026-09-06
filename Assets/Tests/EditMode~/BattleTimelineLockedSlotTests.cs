using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleTimelineLockedSlotTests
{
    [Test]
    public void ConfirmPlayerCommand_RejectsLockedPlayerSlot()
    {
        GameObject timelineObject = new("TimelineLockedSlotReject");
        GameObject slotObject = new("LockedSlotReject");

        try
        {
            BattleTimelineController timeline = timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);
            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            timeline.SetPlayerLockedSlot(0);

            PlayerReservedCommand command = CreatePlayerCommand();

            Assert.That(timeline.IsPlayerSlotLocked(0), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, command), Is.False);
            Assert.That(slot.CommandCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void SelectDefaultSlotWhenInputReady_SkipsLockedDefaultSlot()
    {
        GameObject timelineObject = new("TimelineLockedDefaultSkip");
        GameObject slotAObject = new("LockedDefaultSlotA");
        GameObject slotBObject = new("LockedDefaultSlotB");

        try
        {
            BattleTimelineController timeline = timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slotA = slotAObject.AddComponent<ReserveTurnSlotUI>();
            ReserveTurnSlotUI slotB = slotBObject.AddComponent<ReserveTurnSlotUI>();
            slotA.Init(timeline, 0);
            slotB.Init(timeline, 1);
            SetPrivateField(timeline, "reserveSlots", new[] { slotA, slotB });
            SetPrivateField(timeline, "defaultSlotIndex", 0);

            timeline.SetPlayerLockedSlot(0);
            timeline.SelectDefaultSlotWhenInputReady();

            Assert.That(timeline.ActiveSlotIndex, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(slotBObject);
            Object.DestroyImmediate(slotAObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void EliseSlotLockService_ReturnsSlotWhenAliveEliseExists()
    {
        GameObject monsterObject = new("EliseSlotLockMonster");

        try
        {
            MonsterUnit monsterUnit = monsterObject.AddComponent<MonsterUnit>();
            MonsterRuntimeData runtime = new(
                "Runtime_Elise_Lock",
                new MonsterMasterData
                {
                    MonsterId = EliseSlotLockService.EliseMonsterId,
                    HP = 10
                });

            monsterUnit.Initialize(runtime);

            Relic.Gameplay.Battle.BattleRandom.SetSeed(7);
            int lockedSlotIndex = EliseSlotLockService.RollLockedSlotIndex(
                new List<MonsterUnit> { monsterUnit },
                5);

            Assert.That(lockedSlotIndex, Is.InRange(0, 4));
        }
        finally
        {
            Relic.Gameplay.Battle.BattleRandom.ClearSeed();
            Object.DestroyImmediate(monsterObject);
        }
    }

    private static PlayerReservedCommand CreatePlayerCommand()
    {
        CharacterMasterData masterData = new()
        {
            CharacterId = "Char_LockedSlot_Test",
            MaxHP = 10,
            MaxCost = 3
        };
        CharacterRuntimeData runtime = new(masterData);
        SkillMasterData skill = new()
        {
            SkillId = "S_LockedSlot_Test",
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostValue = 0,
            SkillType = SkillType.Attack,
            Category = Category.Ability,
            TimelineNotation = TimelineActionType.Attack,
            RangeType = RangeType.Direction,
            RangeId = "0"
        };

        return new PlayerReservedCommand(runtime, skill);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }
}
