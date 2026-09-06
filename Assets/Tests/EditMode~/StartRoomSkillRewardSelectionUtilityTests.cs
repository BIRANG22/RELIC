using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class StartRoomSkillRewardSelectionUtilityTests
{
    [Test]
    public void DefaultChoices_ReturnAttackBuffDebuffInStartRoomOrder()
    {
        IReadOnlyList<StartRoomSkillRewardChoice> choices =
            StartRoomSkillRewardSelectionUtility.DefaultChoices;

        Assert.That(choices, Has.Count.EqualTo(3));
        Assert.That(choices[0].SkillType, Is.EqualTo(SkillType.Attack));
        Assert.That(choices[1].SkillType, Is.EqualTo(SkillType.Buff));
        Assert.That(choices[2].SkillType, Is.EqualTo(SkillType.Debuff));
    }

    [Test]
    public void CollectAvailableCoreSkillRewards_FiltersByTypeCoreBaseAndUnavailableIds()
    {
        List<SkillMasterData> skills = new()
        {
            Skill("S_Core_01", Category.Core, SkillType.Attack),
            Skill("S_Core_02", Category.Core, SkillType.Attack),
            Skill("S_Core_03", Category.Core, SkillType.Attack),
            Skill("S_Core_05", Category.Core, SkillType.Buff),
            Skill("S_Public_01", Category.Public, SkillType.Attack),
            Skill("S_Core_07", Category.Core, SkillType.Debuff)
        };
        HashSet<string> unavailable = new() { "S_Core_03" };

        List<SkillMasterData> result =
            StartRoomSkillRewardSelectionUtility.CollectAvailableCoreSkillRewards(
                skills,
                SkillType.Attack,
                unavailable);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].SkillId, Is.EqualTo("S_Core_01"));
    }

    [Test]
    public void CollectUnavailableSkillIds_IncludesInventoryEquippedAndPairedVariants()
    {
        BattleRuntimeData runtime = new()
        {
            SkillInventoryIds = new List<string> { "S_Core_01" }
        };
        Dictionary<string, CharacterRuntimeData> characters = new()
        {
            ["C001"] = new CharacterRuntimeData
            {
                CharacterId = "C001",
                AbilitySkillId = "S_Core_05",
                EquippedSkillIds = new[] { "S_Move_1", "S_Core_03", string.Empty, null }
            }
        };

        HashSet<string> result =
            StartRoomSkillRewardSelectionUtility.CollectUnavailableSkillIds(runtime, characters);

        Assert.That(result, Does.Contain("S_Core_01"));
        Assert.That(result, Does.Contain("S_Core_02"));
        Assert.That(result, Does.Contain("S_Core_03"));
        Assert.That(result, Does.Contain("S_Core_04"));
        Assert.That(result, Does.Contain("S_Core_05"));
        Assert.That(result, Does.Contain("S_Core_06"));
    }

    private static SkillMasterData Skill(string id, Category category, SkillType skillType)
    {
        return new SkillMasterData
        {
            SkillId = id,
            Category = category,
            SkillType = skillType
        };
    }
}
