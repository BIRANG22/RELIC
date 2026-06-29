using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class MonsterRangedAttackAITests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void MonsterCsvLoader_LoadsAttackRangeIdFromKoreanAttackRangeColumn()
    {
        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
        {
            ["Monster"] = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["MonsterId"] = "Mon_Muck",
                    ["HP"] = "10",
                    ["공격범위"] = "Range_Muck_Remote"
                }
            }
        };

        List<MonsterMasterData> monsters = MonsterCsvLoader.Load(workbook);

        Assert.That(monsters, Has.Count.EqualTo(1));
        Assert.That(monsters[0].AttackRangeId, Is.EqualTo("Range_Muck_Remote"));
    }

    [Test]
    public void MonsterRuntimeData_CopiesAttackRangeIdFromMaster()
    {
        MonsterRuntimeData runtime = new(
            "Runtime_Muck",
            new MonsterMasterData
            {
                MonsterId = "Mon_Muck",
                HP = 10,
                AttackRangeId = "Range_Muck_Remote"
            });

        Assert.That(runtime.AttackRangeId, Is.EqualTo("Range_Muck_Remote"));
    }

    [Test]
    public void MonsterReservedCommand_StoresRangeOriginGridIndex()
    {
        MonsterReservedCommand command = new(
            new MonsterRuntimeData("Runtime_Muck", new MonsterMasterData { HP = 10 }),
            new MonsterSkillData { SkillId = "S_Monster_04" });

        command.SetRangeOriginGridIndex(17);

        Assert.That(command.RangeOriginGridIndex, Is.EqualTo(17));

        command.SetRangeOriginGridIndex(-3);

        Assert.That(command.RangeOriginGridIndex, Is.EqualTo(-1));
    }

    [Test]
    public void MonsterSkillRangeService_UsesExplicitRangeOriginGridIndex()
    {
        GameObject gridObject = CreateObject("Grid_Monster_Ranged_Origin");
        GameObject monsterObject = CreateObject("Monster_Ranged_Origin");
        GridManager gridManager = gridObject.AddComponent<GridManager>();
        MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();
        monster.Initialize(new MonsterRuntimeData("Runtime_Muck", new MonsterMasterData { HP = 10 }));
        monster.SetOccupiedCells(new List<int> { gridManager.CoordToIndex(new Vector2Int(1, 2)) });

        RangeDatabase rangeDatabase = new();
        rangeDatabase.Initialize(new[]
        {
            new SkillRangeData
            {
                RangeId = "Range_Single",
                Positions = new List<Vector2Int> { Vector2Int.zero }
            }
        });

        int explicitOrigin = gridManager.CoordToIndex(new Vector2Int(4, 2));

        List<int> range = MonsterSkillRangeService.BuildRangeGridIndices(
            monster,
            new MonsterSkillData { SkillId = "S_Monster_04", RangeId = "Range_Single" },
            gridManager,
            true,
            explicitOrigin,
            rangeDatabase);

        Assert.That(range, Is.EqualTo(new[] { explicitOrigin }));
    }

    [Test]
    public void MuckAI_AttackStoresRangedOriginThatCanHitPlayer()
    {
        GameObject dataObject = CreateObject("DataManager_Muck_Ranged");
        DataManager dataManager = dataObject.AddComponent<DataManager>();
        dataManager.RangeDatabase.Initialize(new[]
        {
            new SkillRangeData
            {
                RangeId = "Range_Muck_Remote",
                Positions = new List<Vector2Int>
                {
                    new(1, 0),
                    new(2, 0)
                }
            },
            new SkillRangeData
            {
                RangeId = "Range_Muck_Attack",
                Positions = new List<Vector2Int>
                {
                    new(1, 0)
                }
            }
        });
        dataManager.MonsterSkillDatabase.Initialize(new[]
        {
            new MonsterSkillData
            {
                SkillId = "S_Monster_04",
                RangeId = "Range_Muck_Attack",
                Target = TargetType.PlayerParty,
                TimelineNotation = TimelineActionType.Attack
            }
        });

        GameObject gridObject = CreateObject("Grid_Muck_Ranged");
        GridManager gridManager = gridObject.AddComponent<GridManager>();

        GameObject monsterObject = CreateObject("Muck_Ranged");
        MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();
        MonsterRuntimeData runtime = new(
            "Runtime_Muck",
            new MonsterMasterData
            {
                MonsterId = "Mon_Muck",
                HP = 10,
                AttackRangeId = "Range_Muck_Remote"
            });
        monster.Initialize(runtime);
        monster.SetOccupiedCells(new List<int> { gridManager.CoordToIndex(new Vector2Int(1, 2)) });

        GameObject playerObject = CreateObject("Player_Muck_Target");
        BattleCharacter player = playerObject.AddComponent<BattleCharacter>();
        player.Initialize(new CharacterRuntimeData
        {
            CharacterId = "Char_Target",
            MaxHP = 10,
            CurrentHP = 10
        });
        player.SetGridIndex(gridManager.CoordToIndex(new Vector2Int(4, 2)));

        MonsterAIPlan plan = new MuckAI().CreatePlan(monster, new BattleContext(), gridManager);
        MonsterAIAction attack = plan.Actions.Find(action => action.SkillId == "S_Monster_04");

        Assert.That(attack, Is.Not.Null);
        Assert.That(attack.RangeOriginGridIndex, Is.EqualTo(gridManager.CoordToIndex(new Vector2Int(3, 2))));
    }

    private GameObject CreateObject(string objectName)
    {
        GameObject go = new(objectName);
        createdObjects.Add(go);
        return go;
    }
}
