using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class PlayerMovePathfindingRegressionTests
{
    [Test]
    public void MovePathCandidates_FindsVRouteAroundBlockedCenter()
    {
        GridManager gridManager = new GameObject("GridManagerVRoute").AddComponent<GridManager>();
        int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 2));
        int targetIndex = gridManager.CoordToIndex(new Vector2Int(2, 2));
        int blockedCenterIndex = gridManager.CoordToIndex(new Vector2Int(1, 2));

        HashSet<int> blockedGridIndices = new()
        {
            blockedCenterIndex
        };

        List<List<Vector2Int>> paths =
            PlayerSkillReservationController.GetReservableMovePathCandidates(
                startIndex,
                targetIndex,
                2,
                2,
                gridManager,
                blockedGridIndices
            );

        Assert.That(paths, Is.Not.Empty);
        Assert.That(GetReservationCount(paths[0], 2), Is.EqualTo(2));
        Assert.That(GetTotalOffset(paths[0]), Is.EqualTo(new Vector2Int(2, 0)));
        AssertPathAvoidsBlockedCell(startIndex, paths[0], gridManager, blockedCenterIndex);

        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MovePathCandidates_FindsSplitXAxisRouteWhenAxisEndsAreBlocked()
    {
        GridManager gridManager = new GameObject("GridManagerSplitXAxis").AddComponent<GridManager>();
        int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
        int targetIndex = gridManager.CoordToIndex(new Vector2Int(3, 1));
        int blockedXAxisIndex = gridManager.CoordToIndex(new Vector2Int(2, 1));
        int blockedYAxisFirstIndex = gridManager.CoordToIndex(new Vector2Int(0, 2));

        HashSet<int> blockedGridIndices = new()
        {
            blockedXAxisIndex,
            blockedYAxisFirstIndex
        };

        List<List<Vector2Int>> paths =
            PlayerSkillReservationController.GetReservableMovePathCandidates(
                startIndex,
                targetIndex,
                2,
                3,
                gridManager,
                blockedGridIndices
            );

        Assert.That(paths, Is.Not.Empty);
        Assert.That(GetReservationCount(paths[0], 2), Is.EqualTo(3));
        Assert.That(GetTotalOffset(paths[0]), Is.EqualTo(new Vector2Int(3, 0)));
        Assert.That(paths[0], Does.Contain(Vector2Int.up));
        Assert.That(paths[0], Does.Contain(Vector2Int.down));
        AssertPathAvoidsBlockedCell(startIndex, paths[0], gridManager, blockedXAxisIndex);
        AssertPathAvoidsBlockedCell(startIndex, paths[0], gridManager, blockedYAxisFirstIndex);

        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MovePreview_AvoidsMonsterOccupancyAlongPathButAllowsMonsterTarget()
    {
        GameObject controllerObject = new("ReservationControllerSplitXAxisPreview");
        GameObject gridObject = new("GridManagerSplitXAxisPreview");
        GameObject monsterXAxisObject = new("MonsterXAxisBlocker");
        GameObject monsterYAxisObject = new("MonsterYAxisBlocker");
        GameObject monsterTargetObject = new("MonsterTargetOccupant");

        try
        {
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            MonsterUnit monsterXAxis = monsterXAxisObject.AddComponent<MonsterUnit>();
            MonsterUnit monsterYAxis = monsterYAxisObject.AddComponent<MonsterUnit>();
            MonsterUnit monsterTarget = monsterTargetObject.AddComponent<MonsterUnit>();

            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
            int targetIndex = gridManager.CoordToIndex(new Vector2Int(3, 1));
            int monsterXAxisIndex = gridManager.CoordToIndex(new Vector2Int(2, 1));
            int monsterYAxisIndex = gridManager.CoordToIndex(new Vector2Int(0, 2));

            monsterXAxis.SetOccupiedCells(new List<int> { monsterXAxisIndex });
            monsterYAxis.SetOccupiedCells(new List<int> { monsterYAxisIndex });
            monsterTarget.SetOccupiedCells(new List<int> { targetIndex });

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test",
                CurrentCost = 3,
                MaxCost = 3,
                Direction = BattleDirection.Right
            };
            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_2",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1,
                GridMove = 2
            };

            SetPrivateField(controller, "gridManager", gridManager);
            SetPrivateField(controller, "currentUserRuntime", runtime);
            SetPrivateField(controller, "currentSkillData", moveSkill);
            SetPrivateField(controller, "currentCasterGridIndex", startIndex);
            SetPrivateField(controller, "currentCasterDirection", BattleDirection.Right);

            InvokePrivateMethod(controller, "PreviewMoveSelectableCells");

            Dictionary<int, List<List<Vector2Int>>> pathCandidatesByTarget =
                GetPrivateField<Dictionary<int, List<List<Vector2Int>>>>(
                    controller,
                    "currentMovePathCandidatesByTargetIndex");

            Assert.That(pathCandidatesByTarget.TryGetValue(targetIndex, out List<List<Vector2Int>> paths), Is.True);
            Assert.That(paths, Is.Not.Empty);
            Assert.That(GetReservationCount(paths[0], 2), Is.EqualTo(3));
            Assert.That(GetTotalOffset(paths[0]), Is.EqualTo(new Vector2Int(3, 0)));
            Assert.That(paths[0], Does.Contain(Vector2Int.up));
            Assert.That(paths[0], Does.Contain(Vector2Int.down));
            AssertPathAvoidsBlockedCell(startIndex, paths[0], gridManager, monsterXAxisIndex);
            AssertPathAvoidsBlockedCell(startIndex, paths[0], gridManager, monsterYAxisIndex);
        }
        finally
        {
            Object.DestroyImmediate(monsterTargetObject);
            Object.DestroyImmediate(monsterYAxisObject);
            Object.DestroyImmediate(monsterXAxisObject);
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void MoveSimulation_DoesNotBlockPlayerMoveByMonsterReservedPosition()
    {
        GridManager gridManager = new GameObject("GridManagerSimulationMonsterPosition").AddComponent<GridManager>();

        try
        {
            BattleActionSimulationService simulator = new(gridManager);
            Dictionary<string, List<int>> monsterPositions =
                GetPrivateField<Dictionary<string, List<int>>>(
                    simulator,
                    "monsterPositions");
            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
            int targetIndex = gridManager.CoordToIndex(new Vector2Int(3, 1));
            int monsterReservedIndex = gridManager.CoordToIndex(new Vector2Int(2, 1));

            monsterPositions["Monster_Test"] = new List<int> { monsterReservedIndex };

            MethodInfo method = typeof(BattleActionSimulationService).GetMethod(
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

            Assert.That(method, Is.Not.Null, "TryGetPlayerMoveTargetGridIndex method is missing.");

            object[] args =
            {
                startIndex,
                new Vector2Int(3, 0),
                "P:Char_Test",
                -1
            };

            bool reachedTarget = (bool)method.Invoke(simulator, args);

            Assert.That(reachedTarget, Is.True);
            Assert.That((int)args[3], Is.EqualTo(targetIndex));
        }
        finally
        {
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    [Test]
    public void MovePathCandidates_AllowsDirectStairPathWhenMoveCostFits()
    {
        GridManager gridManager = new GameObject("GridManagerDirectStair").AddComponent<GridManager>();
        int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 2));
        int targetIndex = gridManager.CoordToIndex(new Vector2Int(2, 3));

        List<List<Vector2Int>> paths =
            PlayerSkillReservationController.GetReservableMovePathCandidates(
                startIndex,
                targetIndex,
                2,
                2,
                gridManager
            );

        Assert.That(paths, Is.Not.Empty);
        Assert.That(GetTotalOffset(paths[0]), Is.EqualTo(new Vector2Int(2, 1)));

        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MovePathSelection_UsesReservedMonsterPathWhenItIsNotLonger()
    {
        GridManager gridManager = new GameObject("GridManagerReservedMonsterShorter").AddComponent<GridManager>();
        int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
        int targetIndex = gridManager.CoordToIndex(new Vector2Int(3, 1));
        int currentMonsterIndex = gridManager.CoordToIndex(new Vector2Int(2, 1));

        HashSet<int> currentBlockedGridIndices = new()
        {
            currentMonsterIndex
        };
        HashSet<int> reservedBlockedGridIndices = new();

        List<Vector2Int> path = PlayerSkillReservationController.ChooseReservableMovePath(
            startIndex,
            targetIndex,
            2,
            3,
            gridManager,
            currentBlockedGridIndices,
            reservedBlockedGridIndices,
            true);

        Assert.That(path, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.right,
            Vector2Int.right
        }));

        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MovePathSelection_KeepsCurrentMonsterPathWhenReservedMonsterPathIsLonger()
    {
        GridManager gridManager = new GameObject("GridManagerCurrentMonsterShorter").AddComponent<GridManager>();
        int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
        int targetIndex = gridManager.CoordToIndex(new Vector2Int(3, 1));
        int reservedMonsterIndex = gridManager.CoordToIndex(new Vector2Int(2, 1));

        HashSet<int> currentBlockedGridIndices = new();
        HashSet<int> reservedBlockedGridIndices = new()
        {
            reservedMonsterIndex
        };

        List<Vector2Int> path = PlayerSkillReservationController.ChooseReservableMovePath(
            startIndex,
            targetIndex,
            2,
            3,
            gridManager,
            currentBlockedGridIndices,
            reservedBlockedGridIndices,
            true);

        Assert.That(path, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.right,
            Vector2Int.right
        }));

        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MoveExecution_RefundsHalfOfBlockedMoveCostImmediately()
    {
        GameObject gridObject = new("GridManagerBlockedMoveRefund");
        GameObject characterObject = new("CharacterBlockedMoveRefund");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            BattleCharacter character = characterObject.AddComponent<BattleCharacter>();
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test",
                CurrentCost = 10,
                MaxCost = 10
            };
            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_2",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1
            };
            PlayerReservedCommand command = new(runtime, moveSkill);
            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 0));
            int stoppedIndex = gridManager.CoordToIndex(new Vector2Int(4, 0));

            character.Initialize(runtime);
            command.SetSelectionResult(
                BattleDirection.Right,
                gridManager.CoordToIndex(new Vector2Int(7, 0)),
                new List<int> { gridManager.CoordToIndex(new Vector2Int(7, 0)) },
                new Vector2Int(7, 0));
            command.SetMoveReservationCost(7, 2);

            BattleActionRunner runner = new(gridManager);

            InvokePrivateMethod(
                runner,
                "ConsumePlayerMoveCost",
                command,
                character);

            Assert.That(runtime.CurrentCost, Is.EqualTo(6));

            InvokePrivateMethod(
                runner,
                "RecordPlayerMoveExecutionDistance",
                command,
                startIndex,
                stoppedIndex);

            Assert.That(command.ExecutedMoveDistance, Is.EqualTo(4));
            Assert.That(command.BlockedMoveCostRefundApplied, Is.True);
            Assert.That(runtime.CurrentCost, Is.EqualTo(7));
            Assert.That(command.ApplyBlockedMoveCostRefund(), Is.EqualTo(0));
            Assert.That(runtime.CurrentCost, Is.EqualTo(7));
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void MoveExecution_ReleasesReservedMoveCostWhenConsumed()
    {
        GameObject gridObject = new("GridManagerMoveCostPreview");
        GameObject characterObject = new("CharacterMoveCostPreview");

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            BattleCharacter character = characterObject.AddComponent<BattleCharacter>();
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test",
                CurrentCost = 10,
                MaxCost = 10
            };
            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_2",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1
            };
            PlayerReservedCommand command = new(runtime, moveSkill);

            character.Initialize(runtime);
            command.SetSelectionResult(
                BattleDirection.Right,
                gridManager.CoordToIndex(new Vector2Int(4, 0)),
                new List<int> { gridManager.CoordToIndex(new Vector2Int(4, 0)) },
                new Vector2Int(4, 0));
            command.SetMoveReservationCost(4, 1);
            runtime.AddReservedCost(command.Cost);

            Assert.That(runtime.CurrentCost, Is.EqualTo(10));
            Assert.That(runtime.ReservedCost, Is.EqualTo(4));
            Assert.That(runtime.PreviewCost, Is.EqualTo(6));

            BattleActionRunner runner = new(gridManager);
            InvokePrivateMethod(
                runner,
                "ConsumePlayerMoveCost",
                command,
                character);

            Assert.That(runtime.CurrentCost, Is.EqualTo(6));
            Assert.That(runtime.ReservedCost, Is.EqualTo(0));
            Assert.That(runtime.PreviewCost, Is.EqualTo(6));
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void MoveExecution_DoesNotReleaseConsumedMoveReservationTwice()
    {
        GameObject timelineObject = new("TimelineConsumedMoveCostRelease");
        GameObject gridObject = new("GridManagerConsumedMoveCostRelease");
        GameObject characterObject = new("CharacterConsumedMoveCostRelease");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            BattleCharacter character = characterObject.AddComponent<BattleCharacter>();
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test",
                CurrentCost = 10,
                MaxCost = 10
            };
            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_2",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1
            };
            PlayerReservedCommand executedMove = new(runtime, moveSkill);
            PlayerReservedCommand laterMove = new(runtime, moveSkill);

            character.Initialize(runtime);
            executedMove.SetSelectionResult(
                BattleDirection.Right,
                gridManager.CoordToIndex(new Vector2Int(4, 0)),
                new List<int> { gridManager.CoordToIndex(new Vector2Int(4, 0)) },
                new Vector2Int(4, 0));
            executedMove.SetMoveReservationCost(4, 1);
            laterMove.SetSelectionResult(
                BattleDirection.Right,
                gridManager.CoordToIndex(new Vector2Int(7, 0)),
                new List<int> { gridManager.CoordToIndex(new Vector2Int(7, 0)) },
                new Vector2Int(3, 0));
            laterMove.SetMoveReservationCost(3, 1);

            runtime.AddReservedCost(executedMove.Cost);
            runtime.AddReservedCost(laterMove.Cost);

            BattleActionRunner runner = new(gridManager);
            InvokePrivateMethod(
                runner,
                "ConsumePlayerMoveCost",
                executedMove,
                character);

            Assert.That(runtime.ReservedCost, Is.EqualTo(laterMove.Cost));

            InvokePrivateMethod(
                timeline,
                "RemoveReservedCosts",
                executedMove);

            Assert.That(runtime.ReservedCost, Is.EqualTo(laterMove.Cost));
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void MoveSimulation_ReplansLaterMovePathAroundEarlierPlayerMove()
    {
        GridManager gridManager = new GameObject("GridManagerReplanLaterMove").AddComponent<GridManager>();

        try
        {
            BattleActionSimulationService simulator = new(gridManager);
            Dictionary<string, int> playerPositions =
                GetPrivateField<Dictionary<string, int>>(
                    simulator,
                    "playerPositions");

            int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 1));
            int blockedIndex = gridManager.CoordToIndex(new Vector2Int(1, 1));
            int targetIndex = gridManager.CoordToIndex(new Vector2Int(3, 1));
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Replan"
            };
            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_1",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1
            };
            PlayerReservedCommand command = new(runtime, moveSkill);

            command.SetSelectionResult(
                BattleDirection.Right,
                targetIndex,
                new List<int> { targetIndex },
                new Vector2Int(3, 0));
            command.SetMoveReservationCost(5, 1);
            command.SetVisualMoveResult(
                targetIndex,
                new Vector2Int(3, 0),
                new List<Vector2Int>
                {
                    Vector2Int.right,
                    Vector2Int.right,
                    Vector2Int.right
                });

            playerPositions["Char_Replan"] = startIndex;
            playerPositions["Char_Blocker"] = blockedIndex;

            InvokePrivateMethod(
                simulator,
                "SimulatePlayerMove",
                command,
                startIndex);

            Assert.That(command.IsSimulatedMoveBlocked, Is.False);
            Assert.That(command.SimulatedMoveGridIndex, Is.EqualTo(targetIndex));
            Assert.That(command.VisualMoveSteps, Is.Not.EqualTo(new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.right,
                Vector2Int.right
            }));
            AssertPathAvoidsBlockedCell(
                startIndex,
                command.VisualMoveSteps,
                gridManager,
                blockedIndex);
        }
        finally
        {
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    [Test]
    public void MoveRangeIndices_TreatsDiagonalAsOneMoveCostWhenDistanceFits()
    {
        GridManager gridManager = new GameObject("GridManagerDiagonalRange").AddComponent<GridManager>();
        int startIndex = gridManager.CoordToIndex(new Vector2Int(0, 2));
        int diagonalTargetIndex = gridManager.CoordToIndex(new Vector2Int(1, 3));

        List<int> rangeIndices = PlayerSkillReservationController.GetMoveRangeIndices(
            startIndex,
            1,
            2,
            gridManager);

        Assert.That(rangeIndices, Does.Contain(diagonalTargetIndex));

        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MoveReservationCommands_KeepsVRouteAsOneCommandWithVisualSteps()
    {
        GameObject controllerObject = new("ReservationControllerVRoute");
        PlayerSkillReservationController controller =
            controllerObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerVRouteCommands").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_2",
            RangeType = RangeType.Selection,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostType = ResourceCostType.Fixed,
            ResourceCostValue = 1,
            GridMove = 2
        };

        SetPrivateField(controller, "gridManager", gridManager);
        SetPrivateField(controller, "currentUserRuntime", runtime);
        SetPrivateField(controller, "currentSkillData", moveSkill);
        SetPrivateField(controller, "currentCasterGridIndex", gridManager.CoordToIndex(new Vector2Int(0, 2)));
        SetPrivateField(controller, "currentCasterDirection", BattleDirection.Right);
        SetPrivateField(controller, "currentMoveDistancePerCommand", 2);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "BuildMoveReservationCommands",
            BindingFlags.Instance | BindingFlags.NonPublic);

        List<PlayerReservedCommand> commands =
            (List<PlayerReservedCommand>)method.Invoke(
                controller,
                new object[]
                {
                    gridManager.CoordToIndex(new Vector2Int(2, 2)),
                    new List<Vector2Int>
                    {
                        Vector2Int.up,
                        Vector2Int.right,
                        Vector2Int.right,
                        Vector2Int.down
                    }
                });

        Assert.That(commands, Has.Count.EqualTo(1));
        Assert.That(commands[0].MoveOffset, Is.EqualTo(new Vector2Int(2, 0)));
        Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(gridManager.CoordToIndex(new Vector2Int(2, 2))));
        Assert.That(commands[0].Cost, Is.EqualTo(2));
        Assert.That(commands[0].IsMoveContinuationCommand, Is.False);
        Assert.That(commands[0].VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.right,
            Vector2Int.down
        }));

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            commands[0],
            0,
            false,
            false,
            0);
        Assert.That(commands[0].Cost, Is.EqualTo(2));

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MoveReservationCommands_KeepsVRouteAsOneCommandWhenMoveDistanceCoversDetour()
    {
        GameObject controllerObject = new("ReservationControllerLargeMoveVRoute");
        PlayerSkillReservationController controller =
            controllerObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerLargeMoveVRouteCommands").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_4",
            RangeType = RangeType.Selection,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostType = ResourceCostType.Fixed,
            ResourceCostValue = 1,
            GridMove = 4
        };

        SetPrivateField(controller, "gridManager", gridManager);
        SetPrivateField(controller, "currentUserRuntime", runtime);
        SetPrivateField(controller, "currentSkillData", moveSkill);
        SetPrivateField(controller, "currentCasterGridIndex", gridManager.CoordToIndex(new Vector2Int(0, 2)));
        SetPrivateField(controller, "currentCasterDirection", BattleDirection.Right);
        SetPrivateField(controller, "currentMoveDistancePerCommand", 4);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "BuildMoveReservationCommands",
            BindingFlags.Instance | BindingFlags.NonPublic);

        List<PlayerReservedCommand> commands =
            (List<PlayerReservedCommand>)method.Invoke(
                controller,
                new object[]
                {
                    gridManager.CoordToIndex(new Vector2Int(2, 2)),
                    new List<Vector2Int>
                    {
                        Vector2Int.up,
                        Vector2Int.right,
                        Vector2Int.right,
                        Vector2Int.down
                    }
                });

        Assert.That(commands, Has.Count.EqualTo(1));
        Assert.That(commands[0].MoveOffset, Is.EqualTo(new Vector2Int(2, 0)));
        Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(gridManager.CoordToIndex(new Vector2Int(2, 2))));
        Assert.That(commands[0].Cost, Is.EqualTo(1));
        Assert.That(commands[0].IsMoveContinuationCommand, Is.False);
        Assert.That(commands[0].VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.right,
            Vector2Int.down
        }));

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void MoveReservationCommands_KeepsDirectStairPathAsSingleMoveCommand()
    {
        GameObject controllerObject = new("ReservationControllerDirectStair");
        PlayerSkillReservationController controller =
            controllerObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerDirectStairCommands").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_2",
            RangeType = RangeType.Selection,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostType = ResourceCostType.Fixed,
            ResourceCostValue = 1,
            GridMove = 2
        };

        SetPrivateField(controller, "gridManager", gridManager);
        SetPrivateField(controller, "currentUserRuntime", runtime);
        SetPrivateField(controller, "currentSkillData", moveSkill);
        SetPrivateField(controller, "currentCasterGridIndex", gridManager.CoordToIndex(new Vector2Int(0, 2)));
        SetPrivateField(controller, "currentCasterDirection", BattleDirection.Right);
        SetPrivateField(controller, "currentMoveDistancePerCommand", 2);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "BuildMoveReservationCommands",
            BindingFlags.Instance | BindingFlags.NonPublic);

        List<PlayerReservedCommand> commands =
            (List<PlayerReservedCommand>)method.Invoke(
                controller,
                new object[]
                {
                    gridManager.CoordToIndex(new Vector2Int(2, 3)),
                    new List<Vector2Int>
                    {
                        Vector2Int.right,
                        Vector2Int.right,
                        Vector2Int.up
                    }
                });

        Assert.That(commands, Has.Count.EqualTo(1));
        Assert.That(commands[0].MoveOffset, Is.EqualTo(new Vector2Int(2, 1)));
        Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(gridManager.CoordToIndex(new Vector2Int(2, 3))));
        Assert.That(commands[0].Cost, Is.EqualTo(2));
        Assert.That(commands[0].IsMoveContinuationCommand, Is.False);

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(gridManager.gameObject);
    }

    [Test]
    public void ConfirmPlayerCommand_MergesMoveReservationInSameSlot()
    {
        GameObject timelineObject = new("TimelineMergeMove");
        GameObject slotObject = new("SlotMergeMove");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test",
                CurrentCost = 10,
                MaxCost = 10,
                Direction = BattleDirection.Right
            };

            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_1",
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1,
                GridMove = 1
            };

            PlayerReservedCommand first = CreateMoveCommand(
                runtime,
                moveSkill,
                1,
                Vector2Int.right,
                1,
                new List<Vector2Int> { Vector2Int.right });

            PlayerReservedCommand second = CreateMoveCommand(
                runtime,
                moveSkill,
                2,
                Vector2Int.right,
                1,
                new List<Vector2Int> { Vector2Int.right });

            Assert.That(timeline.ConfirmPlayerCommand(0, first), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, second), Is.True);

            Assert.That(slot.Commands, Has.Count.EqualTo(1));

            PlayerReservedCommand merged = slot.Commands[0];
            Assert.That(merged.ReservedMoveGridIndex, Is.EqualTo(2));
            Assert.That(merged.MoveOffset, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(merged.Cost, Is.EqualTo(2));
            Assert.That(runtime.ReservedCost, Is.EqualTo(2));
            Assert.That(merged.VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.right
            }));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    private static int GetReservationCount(IReadOnlyList<Vector2Int> moveSteps, int moveDistancePerCommand)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return 0;

        if (moveSteps.Count == 1 && moveSteps[0] == Vector2Int.zero)
            return 1;

        int safeDistancePerCommand = Mathf.Max(1, moveDistancePerCommand);
        int totalDistance = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            totalDistance += Mathf.Abs(moveSteps[i].x) + Mathf.Abs(moveSteps[i].y);

        return Mathf.CeilToInt(totalDistance / (float)safeDistancePerCommand);
    }

    private static Vector2Int GetTotalOffset(IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int total = Vector2Int.zero;

        if (moveSteps == null)
            return total;

        for (int i = 0; i < moveSteps.Count; i++)
            total += moveSteps[i];

        return total;
    }

    private static void AssertPathAvoidsBlockedCell(
        int startIndex,
        IReadOnlyList<Vector2Int> moveSteps,
        GridManager gridManager,
        int blockedGridIndex)
    {
        Vector2Int currentCoord = gridManager.IndexToCoord(startIndex);

        for (int i = 0; i < moveSteps.Count; i++)
        {
            Vector2Int moveStep = moveSteps[i];
            Assert.That(Mathf.Abs(moveStep.x) + Mathf.Abs(moveStep.y), Is.EqualTo(1));

            currentCoord += moveStep;
            Assert.That(gridManager.CoordToIndex(currentCoord), Is.Not.EqualTo(blockedGridIndex));
        }
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        return (T)field.GetValue(target);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        method.Invoke(target, null);
    }

    private static object InvokePrivateMethod(
        object target,
        string methodName,
        params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        return method.Invoke(target, args);
    }

    private static PlayerReservedCommand CreateMoveCommand(
        CharacterRuntimeData runtime,
        SkillMasterData skill,
        int selectedGridIndex,
        Vector2Int moveOffset,
        int moveDistancePerCost,
        IReadOnlyList<Vector2Int> visualMoveSteps)
    {
        PlayerReservedCommand command = new(runtime, skill);
        command.SetSelectionResult(
            BattleDirection.Right,
            selectedGridIndex,
            new List<int> { selectedGridIndex },
            moveOffset);
        command.SetMoveReservationCost(
            GetTotalDistance(visualMoveSteps),
            moveDistancePerCost);
        command.SetVisualMoveResult(
            selectedGridIndex,
            moveOffset,
            visualMoveSteps);
        return command;
    }

    private static int GetTotalDistance(IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null)
            return 0;

        int total = 0;

        for (int i = 0; i < moveSteps.Count; i++)
            total += Mathf.Abs(moveSteps[i].x) + Mathf.Abs(moveSteps[i].y);

        return total;
    }
}
