using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleActionRegressionTests
{
    [Test]
    public void PartyRuntimeStore_AllowsFullBattleGridCurrentIndex()
    {
        PartyRuntimeStore store = new();

        Assert.That(store.SetCharacter(0, "Char_Test"), Is.True);
        Assert.That(store.SetCurrentGridIndex(0, 34), Is.True);
        Assert.That(store.GetCurrentGridIndex(0), Is.EqualTo(34));
    }

    [Test]
    public void PlayerMoveConflictInfo_UsesSimulatedMoveResult()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            CurrentMoveLevel = 3
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, moveSkill);
        command.SetSelectionResult(BattleDirection.Right, 10, new List<int> { 10 }, Vector2Int.right);
        command.SetSimulatedMoveResult(false, 12, new Vector2Int(2, 0));

        BattleActionBatchBuilder builder = new(null);
        MethodInfo method = typeof(BattleActionBatchBuilder).GetMethod(
            "CreatePlayerActionInfo",
            BindingFlags.Instance | BindingFlags.NonPublic);

        object actionInfo = method.Invoke(builder, new object[] { command });
        FieldInfo moveTargetCellsField = actionInfo.GetType().GetField("MoveTargetCells");
        List<int> moveTargetCells = (List<int>)moveTargetCellsField.GetValue(actionInfo);

        Assert.That(moveTargetCells, Is.EquivalentTo(new[] { 12 }));
    }

    [Test]
    public void SelfMoveSelection_FlipsCurrentDirection()
    {
        GameObject gameObject = new("ReservationController");
        PlayerSkillReservationController controller =
            gameObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManager").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        typeof(PlayerSkillReservationController)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, gridManager);

        typeof(PlayerSkillReservationController)
            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, runtime);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "GetDirectionFromMove",
            BindingFlags.Instance | BindingFlags.NonPublic);

        BattleDirection direction =
            (BattleDirection)method.Invoke(controller, new object[] { 10, 10 });

        Object.DestroyImmediate(gameObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(direction, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void SelfMoveSelection_FlipsPreviewDirectionWithoutMutatingRuntimeDirection()
    {
        GameObject gameObject = new("ReservationControllerPreview");
        PlayerSkillReservationController controller =
            gameObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerPreview").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        typeof(PlayerSkillReservationController)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, gridManager);

        typeof(PlayerSkillReservationController)
            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, runtime);

        FieldInfo previewDirectionField = typeof(PlayerSkillReservationController)
            .GetField("currentCasterDirection", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(previewDirectionField, Is.Not.Null);
        previewDirectionField.SetValue(controller, BattleDirection.Left);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "GetDirectionFromMove",
            BindingFlags.Instance | BindingFlags.NonPublic);

        BattleDirection direction =
            (BattleDirection)method.Invoke(controller, new object[] { 10, 10 });

        Object.DestroyImmediate(gameObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(direction, Is.EqualTo(BattleDirection.Right));
        Assert.That(runtime.Direction, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void PlayerReservedCommand_SetMoveDirection_UpdatesDirectionOnly()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, moveSkill);
        command.SetSelectionResult(BattleDirection.Right, 10, new List<int> { 10 }, Vector2Int.zero);

        MethodInfo method = typeof(PlayerReservedCommand).GetMethod(
            "SetMoveDirection",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);
        method.Invoke(command, new object[] { BattleDirection.Left });

        Assert.That(command.Direction, Is.EqualTo(BattleDirection.Left));
        Assert.That(command.MoveOffset, Is.EqualTo(Vector2Int.zero));
        Assert.That(command.ReservedMoveGridIndex, Is.EqualTo(10));
    }

    [Test]
    public void TimelinePreviewDirection_RecomputesAfterSelfFlipIsRemoved()
    {
        GameObject timelineObject = new("TimelinePreviewDirection");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("Slot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot1 = new GameObject("Slot1").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0, slot1 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand selfFlip = new(runtime, moveSkill);
        selfFlip.SetSelectionResult(
            BattleDirection.Left,
            10,
            new List<int> { 10 },
            Vector2Int.zero
        );

        Assert.That(slot0.AddCommand(selfFlip), Is.True);
        Assert.That(timeline.GetPreviewDirection(runtime, 1), Is.EqualTo(BattleDirection.Left));

        Assert.That(slot0.RemoveCommandAt(0, out _), Is.True);

        PlayerReservedCommand verticalMove = new(runtime, moveSkill);
        verticalMove.SetSelectionResult(
            BattleDirection.Left,
            15,
            new List<int> { 15 },
            Vector2Int.up
        );

        Assert.That(slot0.AddCommand(verticalMove), Is.True);
        Assert.That(timeline.GetPreviewDirection(runtime, 1), Is.EqualTo(BattleDirection.Right));

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);
        Object.DestroyImmediate(slot1.gameObject);
    }

    [Test]
    public void MoveRangePreview_UsesRemainingSlotCapacityAsManhattanDistance()
    {
        GridManager gridManager = new GameObject("GridManagerMoveRange").AddComponent<GridManager>();

        List<int> range = PlayerSkillReservationController.GetMoveRangeIndices(
            12,
            3,
            gridManager
        );

        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(range, Does.Contain(12));
        Assert.That(range, Does.Contain(27));
        Assert.That(range, Does.Contain(15));
        Assert.That(range, Has.No.Member(28));
    }

    [Test]
    public void MoveReservationCount_TreatsDiagonalAsHorizontalPlusVertical()
    {
        int count = PlayerSkillReservationController.GetRequiredMoveReservationCount(
            new Vector2Int(1, 1),
            1
        );

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void MoveReservationOffsets_SplitsDiagonalIntoCardinalSteps()
    {
        List<Vector2Int> offsets = PlayerSkillReservationController.BuildMoveReservationOffsets(
            new Vector2Int(1, 1),
            1
        );

        Assert.That(offsets, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.up
        }));
    }

    [Test]
    public void MoveReservationOffsets_UsesMoveDistanceForStraightSteps()
    {
        List<Vector2Int> offsets = PlayerSkillReservationController.BuildMoveReservationOffsets(
            new Vector2Int(2, 0),
            2
        );

        Assert.That(offsets, Is.EqualTo(new List<Vector2Int>
        {
            new Vector2Int(2, 0)
        }));
    }

    [Test]
    public void MoveRangePreview_LevelTwoMoveAllowsStraightTwoButNotDiagonalInOneReservation()
    {
        GridManager gridManager = new GameObject("GridManagerLevelTwoMoveRange").AddComponent<GridManager>();

        List<int> range = PlayerSkillReservationController.GetMoveRangeIndices(
            12,
            1,
            2,
            gridManager
        );

        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(range, Does.Contain(22));
        Assert.That(range, Does.Contain(14));
        Assert.That(range, Has.No.Member(18));
    }

    [Test]
    public void TimelineRemainingPlayerCommandCapacity_SubtractsExistingCommands()
    {
        GameObject timelineObject = new("TimelineCapacity");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("SlotCapacity0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        SkillMasterData skill = new()
        {
            SkillId = "S_Test"
        };

        Assert.That(slot0.AddCommand(new PlayerReservedCommand(runtime, skill)), Is.True);

        int remainingCapacity = timeline.GetRemainingPlayerCommandCapacity(0);

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);

        Assert.That(remainingCapacity, Is.EqualTo(2));
    }

    [Test]
    public void MoveReservationPathCandidates_UsesOpenAxisWhenDiagonalFirstAxisIsBlocked()
    {
        GridManager gridManager = new GameObject("GridManagerBlockedDiagonalPath").AddComponent<GridManager>();
        HashSet<int> blockedGridIndices = new()
        {
            17
        };

        List<List<Vector2Int>> paths =
            PlayerSkillReservationController.GetReservableMovePathCandidates(
                12,
                16,
                1,
                2,
                gridManager,
                blockedGridIndices
            );

        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(paths, Has.Count.EqualTo(1));
        Assert.That(paths[0], Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.down,
            Vector2Int.right
        }));
    }

    [Test]
    public void MoveReservationCommands_AddsDiagonalVisualWithoutChangingDataSteps()
    {
        GameObject controllerObject = new("ReservationControllerNoDiagonalJump");
        PlayerSkillReservationController controller =
            controllerObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerNoDiagonalJump").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        typeof(PlayerSkillReservationController)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, gridManager);

        typeof(PlayerSkillReservationController)
            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, runtime);

        typeof(PlayerSkillReservationController)
            .GetField("currentSkillData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, moveSkill);

        typeof(PlayerSkillReservationController)
            .GetField("currentCasterGridIndex", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, 12);

        typeof(PlayerSkillReservationController)
            .GetField("currentCasterDirection", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, BattleDirection.Right);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "BuildMoveReservationCommands",
            BindingFlags.Instance | BindingFlags.NonPublic);

        List<PlayerReservedCommand> commands =
            (List<PlayerReservedCommand>)method.Invoke(
                controller,
                new object[]
                {
                    new List<Vector2Int>
                    {
                        Vector2Int.right,
                        Vector2Int.down
                    }
                });

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(commands, Has.Count.EqualTo(2));
        Assert.That(commands[0].MoveOffset, Is.EqualTo(Vector2Int.right));
        Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(17));
        Assert.That(commands[0].EffectiveVisualMoveOffset, Is.EqualTo(Vector2Int.right + Vector2Int.down));
        Assert.That(commands[0].EffectiveVisualMoveGridIndex, Is.EqualTo(16));
        Assert.That(commands[0].VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.down
        }));
        Assert.That(commands[0].SkipMoveVisual, Is.False);

        Assert.That(commands[1].MoveOffset, Is.EqualTo(Vector2Int.down));
        Assert.That(commands[1].ReservedMoveGridIndex, Is.EqualTo(16));
        Assert.That(commands[1].SkipMoveVisual, Is.True);
    }

    [Test]
    public void TimelinePreviewGridIndexAtSlotEnd_IgnoresFutureSlots()
    {
        GameObject timelineObject = new("TimelineSlotEndPreview");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("SlotEnd0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot1 = new GameObject("SlotEnd1").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot2 = new GameObject("SlotEnd2").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0, slot1, slot2 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        BattleCharacter character =
            new GameObject("TimelineSlotEndCharacter").AddComponent<BattleCharacter>();
        character.Initialize(runtime);
        character.SetGridIndex(12);

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand move1 = new(runtime, moveSkill);
        move1.SetSelectionResult(BattleDirection.Right, 17, new List<int> { 17 }, Vector2Int.right);
        PlayerReservedCommand move2 = new(runtime, moveSkill);
        move2.SetSelectionResult(BattleDirection.Right, 22, new List<int> { 22 }, Vector2Int.right);
        PlayerReservedCommand move3 = new(runtime, moveSkill);
        move3.SetSelectionResult(BattleDirection.Right, 27, new List<int> { 27 }, Vector2Int.right);

        PlayerReservedCommand futureMove = new(runtime, moveSkill);
        futureMove.SetSelectionResult(BattleDirection.Left, 22, new List<int> { 22 }, Vector2Int.left);

        Assert.That(slot0.AddCommand(move1), Is.True);
        Assert.That(slot0.AddCommand(move2), Is.True);
        Assert.That(slot0.AddCommand(move3), Is.True);
        Assert.That(slot2.AddCommand(futureMove), Is.True);

        int slot1StartGrid = timeline.GetPreviewGridIndexAtSlotEnd(runtime, 1);

        Object.DestroyImmediate(character.gameObject);
        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);
        Object.DestroyImmediate(slot1.gameObject);
        Object.DestroyImmediate(slot2.gameObject);

        Assert.That(slot1StartGrid, Is.EqualTo(27));
    }

    [Test]
    public void TimelineLastMoveGhostPreviewResult_UsesSlotOrderNotReservationOrder()
    {
        GameObject timelineObject = new("TimelineGhostOrder");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("GhostSlot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot1 = new GameObject("GhostSlot1").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot2 = new GameObject("GhostSlot2").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0, slot1, slot2 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand slot0Move = new(runtime, moveSkill);
        slot0Move.SetSelectionResult(BattleDirection.Right, 17, new List<int> { 17 }, Vector2Int.right);

        PlayerReservedCommand slot2Move = new(runtime, moveSkill);
        slot2Move.SetSelectionResult(BattleDirection.Right, 22, new List<int> { 22 }, Vector2Int.right);

        PlayerReservedCommand slot1Move = new(runtime, moveSkill);
        slot1Move.SetSelectionResult(BattleDirection.Left, 12, new List<int> { 12 }, Vector2Int.left);

        Assert.That(slot0.AddCommand(slot0Move), Is.True);
        Assert.That(slot2.AddCommand(slot2Move), Is.True);
        Assert.That(slot1.AddCommand(slot1Move), Is.True);

        bool found = timeline.TryGetLastMoveGhostPreviewResult(
            runtime,
            out int ghostGridIndex,
            out BattleDirection ghostDirection
        );

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);
        Object.DestroyImmediate(slot1.gameObject);
        Object.DestroyImmediate(slot2.gameObject);

        Assert.That(found, Is.True);
        Assert.That(ghostGridIndex, Is.EqualTo(22));
        Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void VisualSkipMove_IsConsumedOnlyAtReservedTarget()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, moveSkill);
        command.SetSelectionResult(
            BattleDirection.Right,
            16,
            new List<int> { 16 },
            Vector2Int.right
        );
        command.SetSkipMoveVisual(true);
        command.SetSimulatedMoveResult(true, 11, Vector2Int.zero);

        Assert.That(command.EffectiveMoveGridIndex, Is.EqualTo(11));
        Assert.That(command.IsVisualSkipConsumedAtGrid(11), Is.False);
        Assert.That(command.IsVisualSkipConsumedAtGrid(16), Is.True);
        Assert.That(command.ExecutionMoveOffset, Is.EqualTo(Vector2Int.right));
        Assert.That(command.PreviewMoveGridIndex, Is.EqualTo(16));
    }

    [Test]
    public void TimelineLastMoveGhostPreviewResult_UsesReservedTargetForVisualSkipMove()
    {
        GameObject timelineObject = new("TimelineGhostVisualSkip");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("GhostVisualSkipSlot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand skippedAxisMove = new(runtime, moveSkill);
        skippedAxisMove.SetSelectionResult(
            BattleDirection.Right,
            16,
            new List<int> { 16 },
            Vector2Int.right
        );
        skippedAxisMove.SetSkipMoveVisual(true);
        skippedAxisMove.SetSimulatedMoveResult(true, 11, Vector2Int.zero);

        Assert.That(slot0.AddCommand(skippedAxisMove), Is.True);

        bool found = timeline.TryGetLastMoveGhostPreviewResult(
            runtime,
            out int ghostGridIndex,
            out BattleDirection ghostDirection
        );

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);

        Assert.That(found, Is.True);
        Assert.That(ghostGridIndex, Is.EqualTo(16));
        Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void BattleOccupancyService_IgnoresDeadMonsters()
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "DeadMonster",
            Health = 1
        };

        MonsterRuntimeData runtime = new("M_Dead", masterData);
        runtime.TakeDamage(1);

        MonsterUnit monster =
            new GameObject("DeadMonster").AddComponent<MonsterUnit>();
        monster.Initialize(runtime);
        monster.SetOccupiedCells(new List<int> { 16 });

        bool occupied = BattleOccupancyService.IsOccupiedByAnyUnit(16);

        Object.DestroyImmediate(monster.gameObject);

        Assert.That(runtime.IsDead, Is.True);
        Assert.That(occupied, Is.False);
    }

    [Test]
    public void BattleActionSimulation_IgnoresMonsterFutureMoveTargetForPlayerReservation()
    {
        GameObject timelineObject = new("TimelineFutureMonsterMove");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();
        ReserveTurnSlotUI slot0 = new GameObject("FutureMonsterMoveSlot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        MethodInfo initializeMonsterSlots = typeof(BattleTimelineController).GetMethod(
            "InitializeMonsterCommandSlots",
            BindingFlags.Instance | BindingFlags.NonPublic);
        initializeMonsterSlots.Invoke(timeline, null);

        CharacterRuntimeData playerRuntime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        BattleCharacter player =
            new GameObject("FutureMonsterMovePlayer").AddComponent<BattleCharacter>();
        player.Initialize(playerRuntime);
        player.SetGridIndex(10);

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand playerMove = new(playerRuntime, moveSkill);
        playerMove.SetSelectionResult(
            BattleDirection.Right,
            20,
            new List<int> { 20 },
            new Vector2Int(2, 0)
        );
        Assert.That(slot0.AddCommand(playerMove), Is.True);

        MonsterMasterData monsterMasterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "MovingMonster",
            Health = 10
        };
        MonsterRuntimeData monsterRuntime = new("M_Move", monsterMasterData);
        MonsterUnit monster =
            new GameObject("FutureMonsterMoveMonster").AddComponent<MonsterUnit>();
        monster.Initialize(monsterRuntime);
        monster.SetOccupiedCells(new List<int> { 21 });

        MonsterSkillData monsterMoveSkill = new()
        {
            SkillId = "S_Monster_Move_Test",
            TimelineNotation = TimelineActionType.Move
        };
        MonsterReservedCommand monsterMove = new(monsterRuntime, monsterMoveSkill);
        monsterMove.SetMoveOffset(Vector2Int.down);

        List<MonsterReservedCommand>[] monsterCommandsBySlot =
            (List<MonsterReservedCommand>[])typeof(BattleTimelineController)
                .GetField("monsterCommandsBySlot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(timeline);
        monsterCommandsBySlot[0].Add(monsterMove);

        GridManager gridManager =
            new GameObject("FutureMonsterMoveGrid").AddComponent<GridManager>();
        BattleActionSimulationService simulator = new(gridManager);
        simulator.Simulate(timeline);

        Object.DestroyImmediate(player.gameObject);
        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(gridManager.gameObject);
        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);

        Assert.That(monsterMove.IsSimulatedMoveBlocked, Is.False);
        Assert.That(playerMove.IsSimulatedMoveBlocked, Is.False);
        Assert.That(playerMove.EffectiveMoveGridIndex, Is.EqualTo(20));
        Assert.That(playerMove.EffectiveMoveOffset, Is.EqualTo(new Vector2Int(2, 0)));
    }

    [Test]
    public void BattleActionRunner_PlayerMoveTargetStopsBeforeRuntimeBlocker()
    {
        GridManager gridManager =
            new GameObject("RuntimeBlockGrid").AddComponent<GridManager>();

        MonsterMasterData monsterMasterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "RuntimeBlockMonster",
            Health = 10
        };
        MonsterRuntimeData monsterRuntime = new("M_Block", monsterMasterData);
        MonsterUnit monster =
            new GameObject("RuntimeBlockMonster").AddComponent<MonsterUnit>();
        monster.Initialize(monsterRuntime);
        monster.SetOccupiedCells(new List<int> { 20 });

        BattleActionRunner runner = new(gridManager);
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "TryGetPlayerMoveTargetGridIndex",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(int),
                typeof(Vector2Int),
                typeof(string),
                typeof(int).MakeByRefType()
            },
            null);

        object[] args =
        {
            10,
            new Vector2Int(2, 0),
            "Char_Test",
            0
        };

        bool found = (bool)method.Invoke(runner, args);
        int targetGridIndex = (int)args[3];

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(found, Is.True);
        Assert.That(targetGridIndex, Is.EqualTo(15));
    }

    [Test]
    public void BattleActionRunner_UsesFastMovementDuration()
    {
        Assert.That(BattleActionRunner.MoveAnimationDuration, Is.GreaterThan(0f));
        Assert.That(BattleActionRunner.MoveAnimationDuration, Is.LessThan(0.25f));
    }
}
