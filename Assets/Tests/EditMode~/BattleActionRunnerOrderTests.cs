using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleActionRunnerOrderTests
{
    [Test]
    public void BuildActionRoutines_OrdersMonsterCommandsBeforePlayerCommandsInSameBatch()
    {
        BattleActionBatch batch = new();
        batch.PlayerCommands.Add(CreatePlayerCommand("Char_Test", "S_Player_Test"));
        batch.MonsterCommands.Add(CreateMonsterCommand("Monster_Runtime_Test", "S_Monster_Test"));

        BattleActionRunner runner = new(null);

        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "BuildActionRoutines",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "BuildActionRoutines method is missing.");

        object result = method.Invoke(runner, new object[] { batch });
        Assert.That(result, Is.InstanceOf<IList>());

        IList routines = (IList)result;
        Assert.That(routines, Has.Count.EqualTo(2));

        Assert.That(GetActionRoutineLabel(routines[0]), Does.StartWith("Monster:"));
        Assert.That(GetActionRoutineLabel(routines[1]), Does.StartWith("PlayerSkill:"));
    }

    [Test]
    public void BuildActionRoutines_OrdersSwiftPlayerCommandsBeforeMonsterCommandsInSameBatch()
    {
        BattleActionBatch batch = new();
        batch.PlayerCommands.Add(CreatePlayerCommand("Char_Test", "S_Player_Swift_Test", "E_Swift"));
        batch.MonsterCommands.Add(CreateMonsterCommand("Monster_Runtime_Test", "S_Monster_Test"));

        BattleActionRunner runner = new(null);

        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "BuildActionRoutines",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "BuildActionRoutines method is missing.");

        object result = method.Invoke(runner, new object[] { batch });
        IList routines = (IList)result;

        Assert.That(routines, Has.Count.EqualTo(2));
        Assert.That(GetActionRoutineLabel(routines[0]), Does.StartWith("PlayerSkill:"));
        Assert.That(GetActionRoutineLabel(routines[1]), Does.StartWith("Monster:"));
    }

    [Test]
    public void MuckProjectileTargetPosition_UsesRangeOriginInsteadOfHitPlayerGrid()
    {
        GridManager gridManager = CreateGridManagerWithCells("Grid_Muck_Projectile_Target");
        GameObject playerObject = new("Player_Muck_Projectile_Target");

        try
        {
            int originGridIndex = gridManager.CoordToIndex(new Vector2Int(3, 2));
            int hitGridIndex = gridManager.CoordToIndex(new Vector2Int(4, 2));
            BattleCharacter player = playerObject.AddComponent<BattleCharacter>();
            player.Initialize(new CharacterRuntimeData
            {
                CharacterId = "Char_Muck_Projectile_Target",
                MaxHP = 10,
                CurrentHP = 10
            });
            player.SetGridIndex(hitGridIndex);

            MonsterReservedCommand command = CreateMuckProjectileCommand();
            command.SetRangeOriginGridIndex(originGridIndex);
            command.SetRangeResult(
                CreateCrossRange(gridManager, new Vector2Int(3, 2)),
                new List<int> { hitGridIndex });

            bool found = TryGetMonsterProjectileTargetPosition(
                new BattleActionRunner(gridManager),
                command,
                out Vector3 targetPosition);

            Assert.That(found, Is.True);
            Assert.That(targetPosition, Is.EqualTo(gridManager.GetWorldPositionByIndex(originGridIndex)));
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    [Test]
    public void MuckProjectileTargetPosition_UsesRangeOriginWhenNoPlayerIsHit()
    {
        GridManager gridManager = CreateGridManagerWithCells("Grid_Muck_Projectile_Miss");

        try
        {
            int originGridIndex = gridManager.CoordToIndex(new Vector2Int(3, 2));
            MonsterReservedCommand command = CreateMuckProjectileCommand();
            command.SetRangeOriginGridIndex(originGridIndex);
            command.SetRangeResult(
                CreateCrossRange(gridManager, new Vector2Int(3, 2)),
                new List<int>());

            bool found = TryGetMonsterProjectileTargetPosition(
                new BattleActionRunner(gridManager),
                command,
                out Vector3 targetPosition);

            Assert.That(found, Is.True);
            Assert.That(targetPosition, Is.EqualTo(gridManager.GetWorldPositionByIndex(originGridIndex)));
        }
        finally
        {
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    [Test]
    public void MuckDamageHitProjectileVfx_PlaysAtRangeOriginWhenNoPlayerIsHit()
    {
        GridManager gridManager = CreateGridManagerWithCells("Grid_Muck_DamageHit_Projectile_Miss");
        GameObject monsterObject = new("Muck_DamageHit_Projectile_Miss");
        GameObject missilePrefab = new("MuckMissileVfx");
        GameObject impactPrefab = new("MuckImpactVfx");

        try
        {
            int originGridIndex = gridManager.CoordToIndex(new Vector2Int(3, 2));
            MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();
            monster.Initialize(new MonsterRuntimeData(
                "Runtime_Muck_DamageHit_Projectile",
                new MonsterMasterData
                {
                    MonsterId = "Mon_Muck",
                    Name = "Muck",
                    HP = 10
                }));
            monster.SetOccupiedCells(new List<int> { gridManager.CoordToIndex(new Vector2Int(1, 2)) });

            BattleUnitAnimator animator = monsterObject.AddComponent<BattleUnitAnimator>();
            BattleUnitActionPresentation[] presentations = BattleUnitActionPresentation.CreateArray(10);
            presentations[0].projectileVfx = new BattleProjectileVfxEntry
            {
                skillId = "S_Monster_04",
                missilePrefab = missilePrefab,
                impactPrefab = impactPrefab,
                travelDuration = 0f,
                impactLifeTime = 1f
            };
            SetPrivateField(animator, "monsterActionPresentations", presentations);

            MonsterReservedCommand command = CreateMuckProjectileCommand("E_Strike");
            command.SetRangeOriginGridIndex(originGridIndex);
            command.SetRangeResult(
                CreateCrossRange(gridManager, new Vector2Int(3, 2)),
                new List<int>());

            IEnumerator routine = CreateMonsterDamageHitSequence(
                new BattleActionRunner(gridManager),
                monster,
                command,
                animator);

            RunEnumeratorToEnd(routine);

            GameObject impact = GameObject.Find("MuckImpactVfx(Clone)");

            Assert.That(impact, Is.Not.Null);
            Assert.That(impact.transform.position, Is.EqualTo(gridManager.GetWorldPositionByIndex(originGridIndex)));
        }
        finally
        {
            DestroyIfExists("MuckMissileVfx(Clone)");
            DestroyIfExists("MuckImpactVfx(Clone)");
            Object.DestroyImmediate(missilePrefab);
            Object.DestroyImmediate(impactPrefab);
            Object.DestroyImmediate(monsterObject);
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    [Test]
    public void BuildBatches_OrdersSwiftPlayerBatchBeforeMonsterBatchInSameSlot()
    {
        GameObject timelineObject = new("TimelineSwiftBeforeMonsterBatchOrder");
        GameObject slotObject = new("SlotSwiftBeforeMonsterBatchOrder");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot =
                slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            int sharedTargetGridIndex = 12;
            PlayerReservedCommand swiftPlayerCommand = CreatePlayerCommand(
                "Char_Test",
                "S_Player_Swift_Test",
                "E_Swift",
                sharedTargetGridIndex);
            MonsterReservedCommand monsterCommand = CreateMonsterCommand(
                "Monster_Runtime_Test",
                "S_Monster_Test",
                sharedTargetGridIndex);

            Assert.That(slot.AddCommand(swiftPlayerCommand), Is.True);
            AddMonsterCommandDirectly(timeline, 0, monsterCommand);

            List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

            Assert.That(batches, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(batches[0].PlayerCommands, Has.Count.EqualTo(1));
            Assert.That(batches[0].MonsterCommands, Is.Empty);
            Assert.That(batches[1].MonsterCommands, Has.Count.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void BuildBatches_OrdersMonsterBatchBeforeNonSwiftPlayerBatchInSameSlot()
    {
        GameObject timelineObject = new("TimelineMonsterBeforeNormalBatchOrder");
        GameObject slotObject = new("SlotMonsterBeforeNormalBatchOrder");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot =
                slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            int sharedTargetGridIndex = 12;
            PlayerReservedCommand playerCommand = CreatePlayerCommand(
                "Char_Test",
                "S_Player_Normal_Test",
                null,
                sharedTargetGridIndex);
            MonsterReservedCommand monsterCommand = CreateMonsterCommand(
                "Monster_Runtime_Test",
                "S_Monster_Test",
                sharedTargetGridIndex);

            Assert.That(slot.AddCommand(playerCommand), Is.True);
            AddMonsterCommandDirectly(timeline, 0, monsterCommand);

            List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

            Assert.That(batches, Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(batches[0].MonsterCommands, Has.Count.EqualTo(1));
            Assert.That(batches[0].PlayerCommands, Is.Empty);
            Assert.That(batches[1].PlayerCommands, Has.Count.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void BuildBatches_KeepsNonSwiftPlayerAfterAllMonsterBatchesInSameSlot()
    {
        GameObject timelineObject = new("TimelineAllMonstersBeforeNormalPlayer");
        GameObject slotObject = new("SlotAllMonstersBeforeNormalPlayer");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot =
                slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            MonsterReservedCommand monsterMoveCommand = CreateMonsterCommand(
                "Monster_Runtime_Test",
                "S_Monster_Move_Test",
                timelineNotation: TimelineActionType.Move);
            monsterMoveCommand.SetMoveOffset(Vector2Int.right);

            MonsterReservedCommand monsterAttackCommand = CreateMonsterCommand(
                "Monster_Runtime_Test",
                "S_Monster_Attack_Test");

            PlayerReservedCommand playerMoveCommand = CreatePlayerMoveCommand(
                "Char_Test",
                selectedGridIndex: 20,
                moveOffset: Vector2Int.right);

            AddMonsterCommandDirectly(timeline, 0, monsterMoveCommand);
            AddMonsterCommandDirectly(timeline, 0, monsterAttackCommand);
            Assert.That(slot.AddCommand(playerMoveCommand), Is.True);

            List<BattleActionBatch> batches = new BattleActionBatchBuilder(null).Build(timeline);

            Assert.That(batches, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(batches[0].MonsterCommands, Has.Count.EqualTo(1));
            Assert.That(batches[0].PlayerCommands, Is.Empty);
            Assert.That(batches[1].MonsterCommands, Has.Count.EqualTo(1));
            Assert.That(batches[1].PlayerCommands, Is.Empty);
            Assert.That(batches[2].PlayerCommands, Has.Count.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    private static PlayerReservedCommand CreatePlayerCommand(
        string characterId,
        string skillId,
        string effectIds = null,
        int targetGridIndex = -1)
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = characterId
        };

        SkillMasterData skill = new()
        {
            SkillId = skillId,
            Category = Category.Ability,
            EffectIds = effectIds
        };

        PlayerReservedCommand command = new(runtime, skill);

        if (targetGridIndex >= 0)
        {
            command.SetDirectionResult(
                BattleDirection.Right,
                new List<int> { targetGridIndex },
                new List<int> { targetGridIndex });
        }

        return command;
    }

    private static PlayerReservedCommand CreatePlayerMoveCommand(
        string characterId,
        int selectedGridIndex,
        Vector2Int moveOffset)
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = characterId
        };

        SkillMasterData skill = new()
        {
            SkillId = "S_Player_Move_Test",
            Category = Category.Move,
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, skill);
        command.SetSelectionResult(
            BattleDirection.Right,
            selectedGridIndex,
            new List<int> { selectedGridIndex },
            moveOffset);

        return command;
    }

    private static MonsterReservedCommand CreateMonsterCommand(
        string runtimeId,
        string skillId,
        int targetGridIndex = -1,
        TimelineActionType timelineNotation = TimelineActionType.Attack)
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = "Monster_Test",
            Name = "Monster Test",
            HP = 10
        };

        MonsterRuntimeData runtime = new(runtimeId, masterData);
        MonsterSkillData skill = new()
        {
            SkillId = skillId,
            TimelineNotation = timelineNotation
        };

        MonsterReservedCommand command = new(runtime, skill);

        if (targetGridIndex >= 0)
        {
            command.SetRangeResult(
                new List<int> { targetGridIndex },
                new List<int> { targetGridIndex });
        }

        return command;
    }

    private static MonsterReservedCommand CreateMuckProjectileCommand(string effectIds = null)
    {
        MonsterRuntimeData runtime = new(
            "Runtime_Muck_Projectile",
            new MonsterMasterData
            {
                MonsterId = "Mon_Muck",
                Name = "Muck",
                HP = 10
            });

        return new MonsterReservedCommand(
            runtime,
            new MonsterSkillData
            {
                SkillId = "S_Monster_04",
                RangeId = "Range_Muck_Attack",
                Target = TargetType.PlayerParty,
                TimelineNotation = TimelineActionType.Attack,
                EffectIds = effectIds
            });
    }

    private static IEnumerator CreateMonsterDamageHitSequence(
        BattleActionRunner runner,
        MonsterUnit monster,
        MonsterReservedCommand command,
        BattleUnitAnimator animator)
    {
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "ExecuteMonsterDamageHitSequence",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "ExecuteMonsterDamageHitSequence method is missing.");

        return (IEnumerator)method.Invoke(runner, new object[] { monster, command, animator });
    }

    private static bool TryGetMonsterProjectileTargetPosition(
        BattleActionRunner runner,
        MonsterReservedCommand command,
        out Vector3 targetPosition)
    {
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "TryGetMonsterProjectileTargetPosition",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "TryGetMonsterProjectileTargetPosition method is missing.");

        object[] args = { command, Vector3.zero };
        bool result = (bool)method.Invoke(runner, args);
        targetPosition = (Vector3)args[1];
        return result;
    }

    private static GridManager CreateGridManagerWithCells(string name)
    {
        GameObject gridObject = new(name);

        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                GameObject cellObject = new($"Cell_{x}_{y}");
                cellObject.transform.SetParent(gridObject.transform, false);
                cellObject.transform.position = new Vector3(x * 2f, y * 3f, 0f);
                cellObject.AddComponent<MeshRenderer>();
                cellObject.AddComponent<BoxCollider>();
                cellObject.AddComponent<GridCell>();
            }
        }

        return gridObject.AddComponent<GridManager>();
    }

    private static void RunEnumeratorToEnd(IEnumerator routine, int maxSteps = 200)
    {
        Assert.That(routine, Is.Not.Null);

        int steps = 0;

        while (routine.MoveNext())
        {
            steps++;
            Assert.That(steps, Is.LessThanOrEqualTo(maxSteps), "Coroutine did not finish.");

            if (routine.Current is IEnumerator nested)
                RunEnumeratorToEnd(nested, maxSteps);
        }
    }

    private static void DestroyIfExists(string name)
    {
        GameObject found = GameObject.Find(name);

        if (found != null)
            Object.DestroyImmediate(found);
    }

    private static List<int> CreateCrossRange(GridManager gridManager, Vector2Int origin)
    {
        Vector2Int[] coords =
        {
            origin,
            origin + Vector2Int.right,
            origin + Vector2Int.left,
            origin + Vector2Int.up,
            origin + Vector2Int.down
        };

        List<int> result = new();

        for (int i = 0; i < coords.Length; i++)
        {
            if (gridManager.IsValidCoord(coords[i]))
                result.Add(gridManager.CoordToIndex(coords[i]));
        }

        return result;
    }

    private static void AddMonsterCommandDirectly(
        BattleTimelineController timeline,
        int slotIndex,
        MonsterReservedCommand command)
    {
        FieldInfo field = typeof(BattleTimelineController).GetField(
            "monsterCommandsBySlot",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "monsterCommandsBySlot field is missing.");

        List<MonsterReservedCommand>[] commandsBySlot =
            (List<MonsterReservedCommand>[])field.GetValue(timeline);

        Assert.That(commandsBySlot, Is.Not.Null);
        Assert.That(slotIndex, Is.InRange(0, commandsBySlot.Length - 1));

        if (commandsBySlot[slotIndex] == null)
            commandsBySlot[slotIndex] = new List<MonsterReservedCommand>();

        commandsBySlot[slotIndex].Add(command);
    }

    private static string GetActionRoutineLabel(object actionRoutine)
    {
        Assert.That(actionRoutine, Is.Not.Null);

        FieldInfo field = actionRoutine.GetType().GetField(
            "Label",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "ActionRoutine.Label field is missing.");
        return (string)field.GetValue(actionRoutine);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }
}
