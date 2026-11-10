using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class MonsterSkillDataAuthorityTests
{
    [Test]
    public void MonsterSkillData_ExposesExistingGridMoveColumn()
    {
        MonsterSkillData skill = DataRowMapper.Map<MonsterSkillData>(
            new Dictionary<string, string>
            {
                ["SkillId"] = "S_Monster_Test",
                ["GridMove"] = "6"
            });

        Assert.That(skill.GridMove, Is.EqualTo(6));
    }

    [Test]
    public void BuildRangeOffsets_UsesPositionsFromSkillRangeData()
    {
        RangeDatabase ranges = BuildRanges(
            "Range_Test_Move",
            new Vector2Int(0, 2),
            new Vector2Int(-1, 0));
        MonsterSkillData skill = new MonsterSkillData { RangeId = "Range_Test_Move" };

        List<Vector2Int> offsets = MonsterSkillRangeService.BuildRangeOffsets(
            skill, true, ranges);

        Assert.That(offsets, Is.EqualTo(new[]
        {
            new Vector2Int(0, 2),
            new Vector2Int(-1, 0)
        }));
    }

    [Test]
    public void BuildRangeOffsets_MirrorsHorizontalAxisWhenFacingLeft()
    {
        RangeDatabase ranges = BuildRanges(
            "Range_Test_Directional",
            new Vector2Int(2, 1),
            new Vector2Int(-1, -1));
        MonsterSkillData skill = new MonsterSkillData { RangeId = "Range_Test_Directional" };

        List<Vector2Int> offsets = MonsterSkillRangeService.BuildRangeOffsets(
            skill, false, ranges);

        Assert.That(offsets, Is.EqualTo(new[]
        {
            new Vector2Int(-2, 1),
            new Vector2Int(1, -1)
        }));
    }

    [Test]
    public void BuildRangeOffsets_ExcludesOriginAndReturnsEmptyForMissingData()
    {
        RangeDatabase ranges = BuildRanges(
            "Range_Test_Origin",
            Vector2Int.zero,
            Vector2Int.right);

        List<Vector2Int> offsets = MonsterSkillRangeService.BuildRangeOffsets(
            new MonsterSkillData { RangeId = "Range_Test_Origin" }, true, ranges);
        List<Vector2Int> missing = MonsterSkillRangeService.BuildRangeOffsets(
            new MonsterSkillData { RangeId = "Range_Missing" }, true, ranges);

        Assert.That(offsets, Is.EqualTo(new[] { Vector2Int.right }));
        Assert.That(missing, Is.Empty);
    }

    private static RangeDatabase BuildRanges(string id, params Vector2Int[] positions)
    {
        RangeDatabase ranges = new RangeDatabase();
        ranges.Initialize(new[]
        {
            new SkillRangeData
            {
                RangeId = id,
                Positions = new List<Vector2Int>(positions)
            }
        });
        return ranges;
    }
}
