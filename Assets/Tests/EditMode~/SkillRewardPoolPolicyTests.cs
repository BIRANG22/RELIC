using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEditor;
using UnityEngine;

public class SkillRewardPoolPolicyTests
{
    [Test]
    public void FilterCandidates_WhenRestrictionDisabled_PreservesCandidates()
    {
        List<SkillMasterData> candidates = new()
        {
            Skill("S_Core_01"),
            Skill("S_Core_02")
        };

        List<SkillMasterData> result = SkillRewardPoolPolicy.FilterCandidates(
            candidates,
            false,
            new[] { "S_Core_02" });

        Assert.That(result, Is.EqualTo(candidates));
    }

    [Test]
    public void FilterCandidates_WhenRestrictionEnabled_KeepsOnlyAllowedIds()
    {
        List<SkillMasterData> candidates = new()
        {
            Skill("S_Core_01"),
            Skill("S_Core_02"),
            Skill("S_Core_03")
        };

        List<SkillMasterData> result = SkillRewardPoolPolicy.FilterCandidates(
            candidates,
            true,
            new[] { "S_Core_03", "S_Core_01" });

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].SkillId, Is.EqualTo("S_Core_01"));
        Assert.That(result[1].SkillId, Is.EqualTo("S_Core_03"));
    }

    [Test]
    public void FilterCandidates_NormalizesWhitespaceAndDuplicateAllowedIds()
    {
        List<SkillMasterData> candidates = new()
        {
            Skill("S_Core_01"),
            Skill("S_Core_02")
        };

        List<SkillMasterData> result = SkillRewardPoolPolicy.FilterCandidates(
            candidates,
            true,
            new[] { " S_Core_02 ", "S_Core_02", "", null });

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].SkillId, Is.EqualTo("S_Core_02"));
    }

    [Test]
    public void FilterCandidates_WhenEnabledWithEmptyAllowedIds_ReturnsNoCandidates()
    {
        List<SkillMasterData> result = SkillRewardPoolPolicy.FilterCandidates(
            new[] { Skill("S_Core_01") },
            true,
            new string[0]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void IsAllowed_WhenDatabaseIsMissing_AllowsExistingBehavior()
    {
        Assert.That(SkillRewardPoolPolicy.IsAllowed("S_Core_01", null), Is.True);
    }

    [Test]
    public void FilterCandidates_WhenRestrictionDisabled_IgnoresEmptyAllowedIds()
    {
        List<SkillMasterData> result = SkillRewardPoolPolicy.FilterCandidates(
            new[] { Skill("S_Core_01") },
            false,
            new string[0]);

        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public void IsAllowed_WhenRestrictionEnabled_UsesNormalizedWhitelist()
    {
        SkillRewardPoolDatabase database = CreateDatabase(true, " S_Core_01 ", "S_Core_01");

        try
        {
            Assert.That(SkillRewardPoolPolicy.IsAllowed("S_Core_01", database), Is.True);
            Assert.That(SkillRewardPoolPolicy.IsAllowed(" S_Core_01 ", database), Is.True);
            Assert.That(SkillRewardPoolPolicy.IsAllowed("S_Core_02", database), Is.False);
            Assert.That(SkillRewardPoolPolicy.IsAllowed("", database), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }

    [Test]
    public void IsAllowed_WhenRestrictionEnabledWithEmptyList_ReturnsFalse()
    {
        SkillRewardPoolDatabase database = CreateDatabase(true);

        try
        {
            Assert.That(SkillRewardPoolPolicy.IsAllowed("S_Core_01", database), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(database);
        }
    }

    private static SkillRewardPoolDatabase CreateDatabase(bool restrictRewards, params string[] ids)
    {
        SkillRewardPoolDatabase database = ScriptableObject.CreateInstance<SkillRewardPoolDatabase>();
        SerializedObject serialized = new(database);
        serialized.FindProperty("restrictRewards").boolValue = restrictRewards;

        SerializedProperty allowedIds = serialized.FindProperty("allowedSkillIds");
        allowedIds.arraySize = ids?.Length ?? 0;
        for (int i = 0; i < allowedIds.arraySize; i++)
            allowedIds.GetArrayElementAtIndex(i).stringValue = ids[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();
        return database;
    }

    private static SkillMasterData Skill(string skillId)
    {
        return new SkillMasterData { SkillId = skillId };
    }
}
