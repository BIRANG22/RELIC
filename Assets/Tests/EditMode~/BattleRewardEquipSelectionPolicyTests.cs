using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class BattleRewardEquipSelectionPolicyTests
{
    [Test]
    public void TryFindSkillViewIndex_PrefersFirstEmptyFreeSlot()
    {
        CharacterRuntimeData character = CreateCharacter();
        character.EquippedSkillIds[2] = "S_Core_01";

        bool found = BattleRewardEquipSelectionPolicy.TryFindSkillViewIndex(
            character,
            _ => CreateSkill(Category.Core),
            out int viewIndex);

        Assert.That(found, Is.True);
        Assert.That(viewIndex, Is.EqualTo(2));
    }

    [Test]
    public void TryFindSkillViewIndex_WhenFull_SelectsFirstReplaceableFreeSlot()
    {
        CharacterRuntimeData character = CreateCharacter();
        character.EquippedSkillIds[2] = "S_Core_01";
        character.EquippedSkillIds[3] = "S_Public_01";
        Dictionary<string, SkillMasterData> skills = new()
        {
            ["S_Core_01"] = CreateSkill(Category.Core),
            ["S_Public_01"] = CreateSkill(Category.Public)
        };

        bool found = BattleRewardEquipSelectionPolicy.TryFindSkillViewIndex(
            character,
            id => skills.TryGetValue(id, out SkillMasterData skill) ? skill : null,
            out int viewIndex);

        Assert.That(found, Is.True);
        Assert.That(viewIndex, Is.EqualTo(1));
    }

    [Test]
    public void TryFindSkillViewIndex_WhenAllFreeSlotsContainLockedSkills_ReturnsFalse()
    {
        CharacterRuntimeData character = CreateCharacter();
        character.EquippedSkillIds[2] = "S_Ability_01";
        character.EquippedSkillIds[3] = "S_Ability_02";

        bool found = BattleRewardEquipSelectionPolicy.TryFindSkillViewIndex(
            character,
            _ => CreateSkill(Category.Ability),
            out int viewIndex);

        Assert.That(found, Is.False);
        Assert.That(viewIndex, Is.EqualTo(-1));
    }

    private static CharacterRuntimeData CreateCharacter()
    {
        return new CharacterRuntimeData { EquippedSkillIds = new string[4] };
    }

    private static SkillMasterData CreateSkill(Category category)
    {
        return new SkillMasterData { Category = category };
    }
}
