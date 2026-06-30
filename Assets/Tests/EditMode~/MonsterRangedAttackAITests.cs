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

    [Test]
    public void MuckAI_AlwaysMovesWhenCurrentPositionCannotAttackPlayer()
    {
        CreateMuckDataManager("DataManager_Muck_Move_When_No_Attack");
        GridManager gridManager = CreateObject("Grid_Muck_Move_When_No_Attack").AddComponent<GridManager>();
        MonsterUnit muck = CreateMuck(gridManager, new Vector2Int(0, 2));
        CreatePlayer("Player_Muck_Move_Target", gridManager, new Vector2Int(4, 2));

        for (int seed = 0; seed < 20; seed++)
        {
            Random.InitState(seed);

            MonsterAIPlan plan = new MuckAI().CreatePlan(muck, new BattleContext(), gridManager);

            Assert.That(plan.Actions, Is.Not.Empty, $"seed {seed} produced no Muck action.");
            Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_01"), $"seed {seed} skipped movement.");
            Assert.That(plan.Actions[0].MoveOffset, Is.EqualTo(Vector2Int.right), $"seed {seed} chose the wrong movement.");
        }
    }

    [Test]
    public void BlobAI_AttacksOnlyWhenPlayerIsInsideFrontTwo()
    {
        CreateBlobDataManager("DataManager_Blob_FrontTwo");
        GridManager gridManager = CreateObject("Grid_Blob_FrontTwo").AddComponent<GridManager>();
        MonsterUnit blob = CreateBlob(gridManager, new Vector2Int(1, 2));
        CreatePlayer("Player_Blob_InRange", gridManager, new Vector2Int(3, 2));

        MonsterAIPlan plan = new BlobAI().CreatePlan(blob, new BattleContext(), gridManager);

        Assert.That(plan.Actions, Has.Count.EqualTo(1));
        Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_05"));
        Assert.That(plan.Actions[0].RangeOriginGridIndex, Is.EqualTo(blob.MainGridIndex));
        Assert.That(plan.Actions[0].HasForcedDirection, Is.True);
        Assert.That(plan.Actions[0].ForcedDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void BlobAI_AttacksPlayerOnLeftWithinTwo()
    {
        CreateBlobDataManager("DataManager_Blob_LeftTwo");
        GridManager gridManager = CreateObject("Grid_Blob_LeftTwo").AddComponent<GridManager>();
        MonsterUnit blob = CreateBlob(gridManager, new Vector2Int(3, 2));
        CreatePlayer("Player_Blob_LeftRange", gridManager, new Vector2Int(1, 2));

        MonsterAIPlan plan = new BlobAI().CreatePlan(blob, new BattleContext(), gridManager);

        Assert.That(plan.Actions, Has.Count.EqualTo(1));
        Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_05"));
        Assert.That(plan.Actions[0].RangeOriginGridIndex, Is.EqualTo(blob.MainGridIndex));
        Assert.That(plan.Actions[0].HasForcedDirection, Is.True);
        Assert.That(plan.Actions[0].ForcedDirection, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void BlobAI_MovesTowardVerticalLineWithoutAttackingWhenFrontTwoIsEmpty()
    {
        CreateBlobDataManager("DataManager_Blob_Move_Line");
        GridManager gridManager = CreateObject("Grid_Blob_Move_Line").AddComponent<GridManager>();
        MonsterUnit blob = CreateBlob(gridManager, new Vector2Int(1, 1));
        CreatePlayer("Player_Blob_OffLine", gridManager, new Vector2Int(4, 2));

        MonsterAIPlan plan = new BlobAI().CreatePlan(blob, new BattleContext(), gridManager);

        Assert.That(plan.Actions, Has.Count.EqualTo(1));
        Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_01"));
        Assert.That(plan.Actions[0].MoveOffset, Is.EqualTo(Vector2Int.up));
    }

    [Test]
    public void BlobAI_MovesThenAttacksWhenMovePlacesPlayerInFrontTwo()
    {
        CreateBlobDataManager("DataManager_Blob_Move_Attack");
        GridManager gridManager = CreateObject("Grid_Blob_Move_Attack").AddComponent<GridManager>();
        MonsterUnit blob = CreateBlob(gridManager, new Vector2Int(1, 1));
        CreatePlayer("Player_Blob_AfterMove", gridManager, new Vector2Int(3, 2));

        MonsterAIPlan plan = new BlobAI().CreatePlan(blob, new BattleContext(), gridManager);

        Assert.That(plan.Actions, Has.Count.EqualTo(2));
        Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_01"));
        Assert.That(plan.Actions[0].MoveOffset, Is.EqualTo(Vector2Int.up));
        Assert.That(plan.Actions[1].SkillId, Is.EqualTo("S_Monster_05"));
        Assert.That(plan.Actions[1].RangeOriginGridIndex, Is.EqualTo(gridManager.CoordToIndex(new Vector2Int(1, 2))));
        Assert.That(plan.Actions[1].HasForcedDirection, Is.True);
        Assert.That(plan.Actions[1].ForcedDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void VespaAI_PrefersVerticalMoveTowardPlayerBeforeDiagonalWhenClear()
    {
        CreateVespaDataManager("DataManager_Vespa_Vertical_Clear");
        GridManager gridManager = CreateObject("Grid_Vespa_Vertical_Clear").AddComponent<GridManager>();
        MonsterUnit vespa = CreateMonster(
            "Mon_04",
            "Vespa",
            "Vespa_Vertical_Clear",
            gridManager,
            new Vector2Int(2, 2));
        CreatePlayer("Player_Vespa_UpperRight", gridManager, new Vector2Int(4, 3));

        MonsterAIPlan plan = new VespaAI().CreatePlan(vespa, new BattleContext(), gridManager);

        Assert.That(plan.Actions, Has.Count.EqualTo(2));
        Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_01"));
        Assert.That(plan.Actions[0].MoveOffset, Is.EqualTo(Vector2Int.up));
        Assert.That(plan.Actions[1].HasForcedDirection, Is.True);
        Assert.That(plan.Actions[1].ForcedDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void VespaAI_UsesDiagonalTowardPlayerWhenVerticalMoveIsBlocked()
    {
        CreateVespaDataManager("DataManager_Vespa_Vertical_Blocked");
        GridManager gridManager = CreateObject("Grid_Vespa_Vertical_Blocked").AddComponent<GridManager>();
        MonsterUnit vespa = CreateMonster(
            "Mon_04",
            "Vespa",
            "Vespa_Vertical_Blocked",
            gridManager,
            new Vector2Int(2, 2));
        CreateMonster(
            "Mon_Blocker",
            "Blocker",
            "Vespa_Vertical_Blocker",
            gridManager,
            new Vector2Int(2, 3));
        CreatePlayer("Player_Vespa_UpperRight_Blocked", gridManager, new Vector2Int(4, 3));

        MonsterAIPlan plan = new VespaAI().CreatePlan(vespa, new BattleContext(), gridManager);

        Assert.That(plan.Actions, Has.Count.EqualTo(2));
        Assert.That(plan.Actions[0].SkillId, Is.EqualTo("S_Monster_01"));
        Assert.That(plan.Actions[0].MoveOffset, Is.EqualTo(new Vector2Int(1, 1)));
        Assert.That(plan.Actions[1].HasForcedDirection, Is.True);
        Assert.That(plan.Actions[1].ForcedDirection, Is.EqualTo(BattleDirection.Right));
    }

    private GameObject CreateObject(string objectName)
    {
        GameObject go = new(objectName);
        createdObjects.Add(go);
        return go;
    }

    private DataManager CreateBlobDataManager(string objectName)
    {
        GameObject dataObject = CreateObject(objectName);
        DataManager dataManager = dataObject.AddComponent<DataManager>();
        dataManager.RangeDatabase.Initialize(new[]
        {
            new SkillRangeData
            {
                RangeId = "Range_10",
                Positions = new List<Vector2Int>
                {
                    Vector2Int.zero,
                    new(1, 0),
                    new(2, 0)
                }
            }
        });
        dataManager.MonsterSkillDatabase.Initialize(new[]
        {
            new MonsterSkillData
            {
                SkillId = "S_Monster_05",
                RangeId = "Range_10",
                Target = TargetType.PlayerParty,
                TimelineNotation = TimelineActionType.Attack
            }
        });

        return dataManager;
    }

    private DataManager CreateMuckDataManager(string objectName)
    {
        GameObject dataObject = CreateObject(objectName);
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

        return dataManager;
    }

    private DataManager CreateVespaDataManager(string objectName)
    {
        GameObject dataObject = CreateObject(objectName);
        DataManager dataManager = dataObject.AddComponent<DataManager>();
        dataManager.RangeDatabase.Initialize(new[]
        {
            new SkillRangeData
            {
                RangeId = "Range_Vespa_Move",
                Positions = new List<Vector2Int>
                {
                    Vector2Int.up,
                    Vector2Int.down,
                    Vector2Int.left,
                    Vector2Int.right,
                    new(1, 1),
                    new(-1, 1),
                    new(1, -1),
                    new(-1, -1)
                }
            }
        });
        dataManager.MonsterSkillDatabase.Initialize(new[]
        {
            new MonsterSkillData
            {
                SkillId = "S_Monster_01",
                RangeId = "Range_Vespa_Move",
                Target = TargetType.Self,
                TimelineNotation = TimelineActionType.Move
            },
            new MonsterSkillData
            {
                SkillId = "S_Monster_07",
                RangeId = "Range_10",
                Target = TargetType.PlayerParty,
                TimelineNotation = TimelineActionType.Attack
            }
        });

        return dataManager;
    }

    private MonsterUnit CreateBlob(GridManager gridManager, Vector2Int coord)
    {
        return CreateMonster("Mon_02", "Blob", $"Blob_{coord.x}_{coord.y}", gridManager, coord);
    }

    private MonsterUnit CreateMuck(GridManager gridManager, Vector2Int coord)
    {
        return CreateMonster(
            "Mon_Muck",
            "Muck",
            $"Muck_{coord.x}_{coord.y}",
            gridManager,
            coord,
            "Range_Muck_Remote");
    }

    private MonsterUnit CreateMonster(
        string monsterId,
        string monsterName,
        string runtimeSuffix,
        GridManager gridManager,
        Vector2Int coord,
        string attackRangeId = null)
    {
        GameObject monsterObject = CreateObject($"{monsterName}_{coord.x}_{coord.y}");
        MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();
        MonsterRuntimeData runtime = new(
            $"Runtime_{runtimeSuffix}",
            new MonsterMasterData
            {
                MonsterId = monsterId,
                Name = monsterName,
                HP = 10,
                AttackRangeId = attackRangeId
            });

        monster.Initialize(runtime);
        monster.SetOccupiedCells(new List<int> { gridManager.CoordToIndex(coord) });
        return monster;
    }

    private BattleCharacter CreatePlayer(string objectName, GridManager gridManager, Vector2Int coord)
    {
        GameObject playerObject = CreateObject(objectName);
        BattleCharacter player = playerObject.AddComponent<BattleCharacter>();
        player.Initialize(new CharacterRuntimeData
        {
            CharacterId = objectName,
            MaxHP = 10,
            CurrentHP = 10
        });
        player.SetGridIndex(gridManager.CoordToIndex(coord));
        return player;
    }
}
