using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class EliseBattleEffectTests
{
    [Test]
    public void SpiderWebStatus_DoublesNextMoveReservationCostAndCanBeConsumed()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Webbed",
            CurrentCost = 10,
            MaxCost = 10,
            StatusEffects = new()
            {
                new StatusEffectRuntimeData("E_Spider_Web", 2, 1)
            }
        };
        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_1",
            Category = Category.Move,
            TimelineNotation = TimelineActionType.Move,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostValue = 1
        };
        PlayerReservedCommand command = new(runtime, moveSkill);
        command.SetMoveReservationCost(3, 1);

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            0,
            true,
            false);

        Assert.That(command.Cost, Is.EqualTo(6));
        Assert.That(BattleEquipmentEffectService.TryConsumeSpiderWebMoveCostPenalty(runtime), Is.True);
        Assert.That(runtime.StatusEffects.Exists(status => status.EffectId == "E_Spider_Web"), Is.False);
    }

    [Test]
    public void BarrierStatus_BlocksOneMonsterDamageHit()
    {
        MonsterRuntimeData monsterRuntime = new(
            "Monster_Barrier",
            new MonsterMasterData
            {
                MonsterId = "Mon_Barrier",
                HP = 20
            });
        GameObject monsterObject = new("Monster_Barrier");
        MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();

        try
        {
            monster.Initialize(monsterRuntime);
            monster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData("E_Barrier", 1, 1));

            int dealt = BattleEffectUtility.DamageMonster(monster, 5);

            Assert.That(dealt, Is.Zero);
            Assert.That(monster.RuntimeData.CurrentHP, Is.EqualTo(20));
            Assert.That(monster.RuntimeData.StatusEffects.Exists(status => status.EffectId == "E_Barrier"), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }
}
