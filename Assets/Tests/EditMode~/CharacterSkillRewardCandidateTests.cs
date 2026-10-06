using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class CharacterSkillRewardCandidateTests
{
    [Test]
    public void GetCandidates_FiltersByCoreRarityBaseVariantAndPartyCompatibility()
    {
        List<SkillMasterData> skills = new()
        {
            Skill("Core_Char01", "Char_01", Category.Core, SkillRarity.Common),
            Skill("Core_Char02", "Char_02", Category.Core, SkillRarity.Common),
            Skill("Core_All", "ALL", Category.Core, SkillRarity.Common),
            Skill("Core_Empty", "", Category.Core, SkillRarity.Common),
            Skill("Public_All", "ALL", Category.Public, SkillRarity.Common)
        };

        IReadOnlyList<SkillMasterData> result = SkillRewardRoller.GetCandidates(
            skills,
            SkillRarity.Common,
            true,
            new[] { "Char_01" });

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].SkillId, Is.EqualTo("Core_Char01"));
        Assert.That(result[1].SkillId, Is.EqualTo("Core_All"));
    }

    private static SkillMasterData Skill(
        string skillId,
        string characterId,
        Category category,
        SkillRarity rarity)
    {
        return new SkillMasterData
        {
            SkillId = skillId,
            CharacterId = characterId,
            Category = category,
            Rarity = rarity
        };
    }
}
