using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public sealed class SkillVfxDatabaseIntegrityTests
{
    private const string DatabasePath = "Assets/DB/SkillVfxDatabase.asset";

    [Test]
    public void ValidateText_ReportsEmptyAndDuplicateSkillIds()
    {
        const string yaml =
            "  - SkillId: S_Test_01\n" +
            "  - SkillId: \n" +
            "  - SkillId: S_Test_01\n";

        SkillVfxDatabaseIntegrityIssue[] issues =
            SkillVfxDatabaseIntegrityValidator.ValidateText(yaml, _ => true);

        Assert.That(issues.Any(issue => issue.Code == "empty-skill-id"), Is.True);
        Assert.That(
            issues.Any(issue => issue.Code == "duplicate-skill-id" && issue.SkillId == "S_Test_01"),
            Is.True);
    }

    [Test]
    public void ValidateText_ReportsEveryReferenceWhoseGuidDoesNotExist()
    {
        const string missingGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string existingGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        string yaml =
            "  - SkillId: S_Test_01\n" +
            $"      prefab: {{fileID: 1, guid: {missingGuid}, type: 3}}\n" +
            $"      impactPrefab: {{fileID: 2, guid: {existingGuid}, type: 3}}\n";

        SkillVfxDatabaseIntegrityIssue[] issues =
            SkillVfxDatabaseIntegrityValidator.ValidateText(yaml, guid => guid == existingGuid);

        Assert.That(issues.Count(issue => issue.Code == "missing-guid"), Is.EqualTo(1));
        Assert.That(issues.Single(issue => issue.Code == "missing-guid").Guid, Is.EqualTo(missingGuid));
        Assert.That(issues.Single(issue => issue.Code == "missing-guid").SkillId, Is.EqualTo("S_Test_01"));
    }

    [Test]
    public void ProjectDatabase_HasNoIntegrityIssues()
    {
        SkillVfxDatabaseIntegrityIssue[] issues =
            SkillVfxDatabaseIntegrityValidator.ValidateAsset(DatabasePath);

        Assert.That(
            issues,
            Is.Empty,
            string.Join(Environment.NewLine, issues.Select(issue => issue.ToString())));
        Assert.That(AssetDatabase.LoadMainAssetAtPath(DatabasePath), Is.Not.Null);
    }
}
