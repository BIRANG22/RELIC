using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class CharacterSkillEquipPolicyTests
{
    [TestCase("Char_01", "Char_01", true)]
    [TestCase("Char_01", "Char_02", false)]
    [TestCase("ALL", "Char_02", true)]
    public void RewardEquipSelection_UsesCharacterOwnership(
        string ownerId,
        string characterId,
        bool expected)
    {
        SkillMasterData skill = new()
        {
            SkillId = "RewardSkill",
            CharacterId = ownerId,
            Category = Category.Core,
            Rarity = SkillRarity.Common
        };

        Assert.That(
            BattleRewardEquipSelectionPolicy.CanEquipRewardSkill(skill, characterId),
            Is.EqualTo(expected));
    }

    [Test]
    public void EquipInventorySkillToSlot_RejectsIncompatibleSkillWithoutChangingState()
    {
        CharacterRuntimeStore store = new();
        CharacterRuntimeData character = new()
        {
            CharacterId = "Char_01",
            EquippedSkillIds = new[] { "", "", "OldSkill", "" }
        };
        store.AddOrUpdate(character);
        List<string> inventory = new() { "Char02Skill" };
        Dictionary<string, SkillMasterData> skills = new()
        {
            ["OldSkill"] = new SkillMasterData
            {
                SkillId = "OldSkill", CharacterId = "ALL", Category = Category.Core,
                Rarity = SkillRarity.Common
            },
            ["Char02Skill"] = new SkillMasterData
            {
                SkillId = "Char02Skill", CharacterId = "Char_02", Category = Category.Core,
                Rarity = SkillRarity.Common
            }
        };
        SkillInventoryEquipService service = new(
            store,
            inventory,
            id => skills.TryGetValue(id, out SkillMasterData skill) ? skill : null);

        bool equipped = service.EquipInventorySkillToSlot("Char_01", 2, "Char02Skill");

        Assert.That(equipped, Is.False);
        Assert.That(character.EquippedSkillIds[2], Is.EqualTo("OldSkill"));
        Assert.That(inventory, Is.EqualTo(new[] { "Char02Skill" }));
    }

    [Test]
    public void FindFirstCompatibleCharacterIndex_SelectsRewardOwnerImmediately()
    {
        SkillMasterData skill = new()
        {
            SkillId = "RewardSkill",
            CharacterId = "Char_02",
            Category = Category.Core,
            Rarity = SkillRarity.Common
        };

        int index = BattleRewardEquipSelectionPolicy.FindFirstCompatibleCharacterIndex(
            skill,
            new[] { "Char_01", "Char_02", "Char_03" });

        Assert.That(index, Is.EqualTo(1));
    }

    [TestCase("Char_01", false)]
    [TestCase("Char_02", true)]
    [TestCase("Char_03", false)]
    public void RewardCharacterSelection_DisablesNonOwners(string characterId, bool expected)
    {
        SkillMasterData skill = new()
        {
            SkillId = "RewardSkill",
            CharacterId = "Char_02",
            Category = Category.Core,
            Rarity = SkillRarity.Common
        };

        Assert.That(
            BattleRewardEquipSelectionPolicy.CanSelectCharacter(skill, characterId),
            Is.EqualTo(expected));
    }
}
