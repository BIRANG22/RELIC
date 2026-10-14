using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class SkillUpgradeRemovalTests
{
    [Test]
    public void RuntimeSkillData_ContainsOnlySequentialLevelOneCoreAndAbilitySkills()
    {
        TextAsset data = Resources.Load<TextAsset>("Data/GameDataRuntime");
        Assert.That(data, Is.Not.Null);

        Dictionary<string, List<Dictionary<string, string>>> workbook =
            ExcelWorkbookReader.Read(data.bytes);
        List<SkillMasterData> skills = SkillCsvLoader.LoadSkills(workbook);

        AssertCategory(skills, Category.Core, "S_Core_", 40);
        AssertCategory(skills, Category.Ability, "S_Ability_", 10);
    }

    [Test]
    public void SkillRarityUtility_DisablesUpgradeAndPairingRules()
    {
        SkillMasterData core = new() { Category = Category.Core, SkillId = "S_Core_02" };

        Assert.That(SkillRarityUtility.CanUpgrade(core), Is.False);
        Assert.That(SkillRarityUtility.IsBaseSkillVariant(core.SkillId), Is.True);
        Assert.That(SkillRarityUtility.IsUpgradeSkillVariant(core.SkillId), Is.False);
        Assert.That(
            SkillRarityUtility.TryGetPairedVariantId(core.SkillId, out string pairedSkillId),
            Is.False);
        Assert.That(pairedSkillId, Is.Null);
    }

    private static void AssertCategory(
        IEnumerable<SkillMasterData> allSkills,
        Category category,
        string idPrefix,
        int expectedCount)
    {
        List<SkillMasterData> skills = allSkills
            .Where(skill => skill.Category == category)
            .OrderBy(skill => skill.SkillId)
            .ToList();

        Assert.That(skills, Has.Count.EqualTo(expectedCount));
        Assert.That(
            skills.Select(skill => skill.SkillId),
            Is.EqualTo(Enumerable.Range(1, expectedCount).Select(index => $"{idPrefix}{index:00}")));
        Assert.That(skills.All(skill => skill.Level == 1), Is.True);
        Assert.That(skills.All(skill => !skill.Name.EndsWith("+")), Is.True);
    }
}
