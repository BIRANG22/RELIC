using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class DebugBattleSkillCastUtilityTests
{
    [Test]
    public void TryCreatePlayerSkillCommand_SelectionSkillUsesPreferredTargetAndBuildsEffectRange()
    {
        GameObject gridObject = new("Grid");
        GridManager gridManager = gridObject.AddComponent<GridManager>();
        CharacterRuntimeData runtime = CreateRuntime("Char_03");
        SkillMasterData skill = CreateSelectionAttackSkill();
        RangeDatabase rangeDatabase = CreateRangeDatabase();

        try
        {
            bool created = DebugBattleSkillCastUtility.TryCreatePlayerSkillCommand(
                runtime,
                skill,
                gridManager,
                rangeDatabase,
                casterGridIndex: 12,
                casterDirection: BattleDirection.Right,
                preferredTargetGridIndex: 17,
                forcePreferredTargetGrid: false,
                out PlayerReservedCommand command,
                out string error);

            Assert.That(created, Is.True, error);
            Assert.That(command, Is.Not.Null);
            Assert.That(command.UserRuntime, Is.SameAs(runtime));
            Assert.That(command.SkillData, Is.SameAs(skill));
            Assert.That(command.SelectedGridIndex, Is.EqualTo(17));
            Assert.That(command.TargetGridIndices, Is.EqualTo(new[] { 17, 18, 22, 16, 12 }));
            Assert.That(command.RangeGridIndices, Is.EqualTo(new[] { 17, 18, 22, 16, 12 }));
        }
        finally
        {
            DestroyObject(gridObject);
        }
    }

    [Test]
    public void TryCreatePlayerSkillCommand_ForcedSelectionCanUseDebugTargetOutsideSelectableRange()
    {
        GameObject gridObject = new("Grid");
        GridManager gridManager = gridObject.AddComponent<GridManager>();
        CharacterRuntimeData runtime = CreateRuntime("Char_03");
        SkillMasterData skill = CreateSelectionAttackSkill();
        RangeDatabase rangeDatabase = CreateRangeDatabase();

        try
        {
            bool created = DebugBattleSkillCastUtility.TryCreatePlayerSkillCommand(
                runtime,
                skill,
                gridManager,
                rangeDatabase,
                casterGridIndex: 12,
                casterDirection: BattleDirection.Right,
                preferredTargetGridIndex: 34,
                forcePreferredTargetGrid: true,
                out PlayerReservedCommand command,
                out string error);

            Assert.That(created, Is.True, error);
            Assert.That(command.SelectedGridIndex, Is.EqualTo(34));
            Assert.That(command.TargetGridIndices, Is.EqualTo(new[] { 34, 33, 29 }));
        }
        finally
        {
            DestroyObject(gridObject);
        }
    }

    private static CharacterRuntimeData CreateRuntime(string characterId)
    {
        return new CharacterRuntimeData
        {
            CharacterId = characterId,
            MaxHP = 100,
            CurrentHP = 100,
            MaxCost = 10,
            CurrentCost = 10,
            Direction = BattleDirection.Right
        };
    }

    private static SkillMasterData CreateSelectionAttackSkill()
    {
        return new SkillMasterData
        {
            SkillId = "S_Ability_11",
            Category = Category.Ability,
            SkillType = SkillType.Attack,
            TimelineNotation = TimelineActionType.Attack,
            ReferenceResource = ReferenceResource.Cost,
            Target = TargetType.EnemyParty,
            ResourceCostValue = 3,
            RangeType = RangeType.Selection,
            RangeId = "Range_17"
        };
    }

    private static RangeDatabase CreateRangeDatabase()
    {
        RangeDatabase rangeDatabase = new();
        rangeDatabase.Initialize(new[]
        {
            new SkillRangeData
            {
                RangeId = "Range_24",
                Positions = new List<Vector2Int>
                {
                    new(0, 0),
                    new(1, 0)
                }
            },
            new SkillRangeData
            {
                RangeId = "Range_17",
                Positions = new List<Vector2Int>
                {
                    new(0, 0),
                    new(0, 1),
                    new(1, 0),
                    new(0, -1),
                    new(-1, 0)
                }
            }
        });

        return rangeDatabase;
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}
