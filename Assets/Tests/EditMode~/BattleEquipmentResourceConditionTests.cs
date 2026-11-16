using NUnit.Framework;
using Relic.Gameplay.Data;

public class BattleEquipmentResourceConditionTests
{
    [Test]
    public void UsesUniqueResource_CoreSkillUsingKarma_ReturnsTrue()
    {
        SkillMasterData skill = new SkillMasterData
        {
            Category = Category.Core,
            ReferenceResource = ReferenceResource.UniqueResource
        };

        Assert.That(BattleEquipmentEffectService.UsesUniqueResource(skill), Is.True);
    }

    [Test]
    public void UsesUniqueResource_UniqueSkillUsingMana_ReturnsFalse()
    {
        SkillMasterData skill = new SkillMasterData
        {
            Category = Category.Unique,
            ReferenceResource = ReferenceResource.Cost
        };

        Assert.That(BattleEquipmentEffectService.UsesUniqueResource(skill), Is.False);
    }

    [TestCase(ReferenceResource.HP, 3, 0, 0)]
    [TestCase(ReferenceResource.Cost, 2, 1, 0)]
    [TestCase(ReferenceResource.UniqueResource, 2, 0, 1)]
    public void ApplyResourceCostDelta_IncreasesOnlySelectedResource(
        ReferenceResource resource,
        int expectedHpCost,
        int expectedManaCost,
        int expectedKarmaCost)
    {
        int hpCost = 2;
        int manaCost = 0;
        int karmaCost = 0;

        BattleEquipmentEffectService.ApplyResourceCostDelta(
            resource,
            1,
            ref hpCost,
            ref manaCost,
            ref karmaCost);

        Assert.That(hpCost, Is.EqualTo(expectedHpCost));
        Assert.That(manaCost, Is.EqualTo(expectedManaCost));
        Assert.That(karmaCost, Is.EqualTo(expectedKarmaCost));
    }

    [Test]
    public void SelectAttackResourceCostDelta_NonAttackSkill_ReturnsZero()
    {
        SkillMasterData skill = new SkillMasterData
        {
            SkillType = SkillType.Buff,
            ReferenceResource = ReferenceResource.HP
        };

        Assert.That(
            BattleEquipmentEffectService.SelectAttackResourceCostDelta(skill, 1),
            Is.Zero);
    }

    [Test]
    public void SelectAttackResourceCostDelta_AttackSkill_ReturnsConfiguredDelta()
    {
        SkillMasterData skill = new SkillMasterData
        {
            SkillType = SkillType.Attack,
            ReferenceResource = ReferenceResource.UniqueResource
        };

        Assert.That(
            BattleEquipmentEffectService.SelectAttackResourceCostDelta(skill, 1),
            Is.EqualTo(1));
    }

    [Test]
    public void VariableKarmaAttack_ReservesRelicDeltaBeforeResolvingX()
    {
        CharacterRuntimeData caster = new CharacterRuntimeData
        {
            CharacterId = "Char_03",
            CurrentHP = 10,
            CurrentResource = 3
        };
        SkillMasterData skill = new SkillMasterData
        {
            SkillId = "S_Core_35",
            SkillType = SkillType.Attack,
            ReferenceResource = ReferenceResource.UniqueResource,
            ResourceCostFormula = "X",
            EffectIds = "E_RandomStrike",
            ValueRate = "10",
            CountRate = "X"
        };
        PlayerReservedCommand command = new PlayerReservedCommand(caster, skill);

        command.RecalculateVariableCostForAdditionalDelta(1);
        int hpCost = command.HPCost;
        int manaCost = command.Cost;
        int karmaCost = command.ResourceCost;
        BattleEquipmentEffectService.ApplyResourceCostDelta(
            skill.ReferenceResource,
            1,
            ref hpCost,
            ref manaCost,
            ref karmaCost);

        Assert.That(command.ResolvedX, Is.EqualTo(2));
        Assert.That(command.ResolvedEffectCounts, Is.EqualTo(new[] { 2 }));
        Assert.That(karmaCost, Is.EqualTo(3));
    }
}
