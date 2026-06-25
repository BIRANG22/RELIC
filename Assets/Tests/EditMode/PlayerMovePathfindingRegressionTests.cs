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
    public void MoveExecutionStepGroups_CombinesMonotonicStairPathIntoSingleSegment()
    {
        List<List<Vector2Int>> groups = BuildPlayerMoveExecutionStepGroups(
            new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.right,
                Vector2Int.up
            });

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(GetTotalOffset(groups[0]), Is.EqualTo(new Vector2Int(2, 1)));
    }

    [Test]
    public void MoveExecutionStepGroups_SplitsWhenPathReversesAxis()
    {
        List<List<Vector2Int>> groups = BuildPlayerMoveExecutionStepGroups(
            new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down
            });

        Assert.That(groups, Has.Count.EqualTo(2));
        Assert.That(GetTotalOffset(groups[0]), Is.EqualTo(new Vector2Int(3, 1)));
        Assert.That(GetTotalOffset(groups[1]), Is.EqualTo(Vector2Int.down));
    }

    [Test]
    public void MoveExecutionStepGroups_PreservesTerminalSelfFlip()
    {
        List<List<Vector2Int>> groups = BuildPlayerMoveExecutionStepGroups(
            new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.zero
            });

        Assert.That(groups, Has.Count.EqualTo(2));
        Assert.That(GetTotalOffset(groups[0]), Is.EqualTo(Vector2Int.right));
        Assert.That(groups[1], Is.EqualTo(new List<Vector2Int> { Vector2Int.zero }));
    }

    [Test]
    public void ConfirmPlayerCommand_AddsSecondMoveReservationInSameSlot()
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

            Assert.That(slot.Commands, Has.Count.EqualTo(2));

            Assert.That(slot.Commands[0].ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(slot.Commands[0].MoveOffset, Is.EqualTo(Vector2Int.right));
            Assert.That(slot.Commands[0].Cost, Is.EqualTo(1));
            Assert.That(slot.Commands[1].ReservedMoveGridIndex, Is.EqualTo(2));
            Assert.That(slot.Commands[1].MoveOffset, Is.EqualTo(Vector2Int.right));
            Assert.That(slot.Commands[1].Cost, Is.EqualTo(1));
            Assert.That(runtime.ReservedCost, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void ConfirmPlayerCommand_SecondMoveReservationKeepsItsOwnDirection()
    {
        GameObject timelineObject = new("TimelineMergeMoveDirection");
        GameObject slotObject = new("SlotMergeMoveDirection");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Direction",
                CurrentCost = 10,
                MaxCost = 10,
                Direction = BattleDirection.Left
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
                0,
                Vector2Int.left,
                1,
                new List<Vector2Int> { Vector2Int.left });
            second.SetMoveDirection(BattleDirection.Left);

            Assert.That(timeline.ConfirmPlayerCommand(0, first), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, second), Is.True);

            Assert.That(slot.Commands, Has.Count.EqualTo(2));
            Assert.That(slot.Commands[0].MoveOffset, Is.EqualTo(Vector2Int.right));
            Assert.That(slot.Commands[0].ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(slot.Commands[1].MoveOffset, Is.EqualTo(Vector2Int.left));
            Assert.That(slot.Commands[1].ReservedMoveGridIndex, Is.EqualTo(0));
            Assert.That(slot.Commands[1].Direction, Is.EqualTo(BattleDirection.Left));
            Assert.That(timeline.GetPreviewDirection(runtime, 0), Is.EqualTo(BattleDirection.Left));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void StartReservation_UsesExistingMoveGhostPositionForSelfFlip()
    {
        GameObject timelineObject = new("TimelineSelfFlipGhostPosition");
        GameObject slotObject = new("SlotSelfFlipGhostPosition");
        GameObject controllerObject = new("ControllerSelfFlipGhostPosition");
        GameObject gridObject = new("GridSelfFlipGhostPosition");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();

            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });
            SetPrivateField(controller, "timelineController", timeline);
            SetPrivateField(controller, "gridManager", gridManager);

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_SelfFlip_Ghost",
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

            PlayerReservedCommand existingMove = CreateMoveCommand(
                runtime,
                moveSkill,
                1,
                Vector2Int.right,
                1,
                new List<Vector2Int> { Vector2Int.right });

            Assert.That(slot.AddCommand(existingMove), Is.True);

            controller.StartReservation(
                runtime,
                moveSkill,
                0,
                0,
                BattleDirection.Right);

            Assert.That(
                GetPrivateField<int>(controller, "currentCasterGridIndex"),
                Is.EqualTo(1));
            Assert.That(
                GetPrivateField<BattleDirection>(controller, "currentCasterDirection"),
                Is.EqualTo(BattleDirection.Right));

            InvokePrivateMethod(
                controller,
                "AddCurrentCasterSelfFlipCandidate",
                new HashSet<int>());

            List<int> selectableIndices =
                GetPrivateField<List<int>>(
                    controller,
                    "currentMoveSelectableIndices");
            Dictionary<int, List<List<Vector2Int>>> candidatesByTarget =
                GetPrivateField<Dictionary<int, List<List<Vector2Int>>>>(
                    controller,
                    "currentMovePathCandidatesByTargetIndex");

            Assert.That(selectableIndices, Does.Contain(1));
            Assert.That(
                candidatesByTarget.TryGetValue(1, out List<List<Vector2Int>> selfFlipCandidates),
                Is.True);
            Assert.That(selfFlipCandidates, Has.Count.EqualTo(1));
            Assert.That(selfFlipCandidates[0], Is.EqualTo(new List<Vector2Int> { Vector2Int.zero }));

            List<PlayerReservedCommand> commands =
                (List<PlayerReservedCommand>)InvokePrivateMethod(
                    controller,
                    "BuildMoveReservationCommands",
                    1,
                    new List<Vector2Int> { Vector2Int.zero });

            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].MoveOffset, Is.EqualTo(Vector2Int.zero));
            Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(commands[0].Direction, Is.EqualTo(BattleDirection.Left));
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void StartReservation_ClickingReservedCurrentPositionMergesSelfFlip()
    {
        GameObject dataManagerObject = new("DataManagerReservedCurrentSelfFlipClick");
        GameObject timelineObject = new("TimelineReservedCurrentSelfFlipClick");
        GameObject slotObject = new("SlotReservedCurrentSelfFlipClick");
        GameObject controllerObject = new("ControllerReservedCurrentSelfFlipClick");
        GameObject gridObject = new("GridReservedCurrentSelfFlipClick");
        GameObject cellObject = new("CellReservedCurrentSelfFlipClick");

        try
        {
            DataManager dataManager = dataManagerObject.AddComponent<DataManager>();
            InvokePrivateMethod(dataManager, "Awake");

            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell currentPreviewCell = cellObject.AddComponent<GridCell>();

            slot.Init(timeline, 0);
            currentPreviewCell.Initialize(gridManager, 0, 1, 1);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });
            SetPrivateField(controller, "timelineController", timeline);
            SetPrivateField(controller, "gridManager", gridManager);

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Reserved_Current_SelfFlip_Click",
                CurrentCost = 1,
                MaxCost = 1,
                Direction = BattleDirection.Right
            };

            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_1",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1,
                GridMove = 1
            };

            PlayerReservedCommand firstMove = CreateMoveCommand(
                runtime,
                moveSkill,
                1,
                Vector2Int.right,
                1,
                new List<Vector2Int> { Vector2Int.right });

            Assert.That(timeline.ConfirmPlayerCommand(0, firstMove), Is.True);

            controller.StartReservation(
                runtime,
                moveSkill,
                0,
                0,
                BattleDirection.Right);

            InvokePrivateMethod(controller, "HandleCellClicked", currentPreviewCell);

            Assert.That(slot.Commands, Has.Count.EqualTo(1));
            Assert.That(slot.Commands[0].MoveOffset, Is.EqualTo(Vector2Int.right));
            Assert.That(slot.Commands[0].ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(slot.Commands[0].Direction, Is.EqualTo(BattleDirection.Left));
            Assert.That(timeline.GetPreviewDirection(runtime, 0), Is.EqualTo(BattleDirection.Left));
        }
        finally
        {
            Object.DestroyImmediate(cellObject);
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
            Object.DestroyImmediate(dataManagerObject);
        }
    }

    [Test]
    public void CharacterRuntimeData_DoesNotExposeReservedMoveCost()
    {
        Assert.That(
            typeof(CharacterRuntimeData).GetField("ReservedMoveCost"),
            Is.Null);
    }

    [Test]
    public void LegacyMovePointReference_ReservesCostInsteadOfMoveCost()
    {
        GameObject timelineObject = new("TimelineLegacyMovePointCost");
        GameObject slotObject = new("SlotLegacyMovePointCost");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Legacy_MovePoint",
                CurrentCost = 3,
                MaxCost = 3,
                CurrentMoveLevel = 0
            };

            SkillMasterData legacyMovePointSkill = new()
            {
                SkillId = "S_Legacy_MovePoint",
                ReferenceResource = ReferenceResource.MovePoint,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 2
            };

            PlayerReservedCommand command = new(runtime, legacyMovePointSkill);

            Assert.That(command.Cost, Is.EqualTo(2));
            Assert.That(timeline.ConfirmPlayerCommand(0, command), Is.True);
            Assert.That(runtime.ReservedCost, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void PreviewMoveSelectableCells_KeepsGhostSelfFlipWhenNoRemainingCostForMergedMove()
    {
        GameObject dataManagerObject = new("DataManagerSelfFlipNoCost");
        GameObject timelineObject = new("TimelineSelfFlipNoCost");
        GameObject slotObject = new("SlotSelfFlipNoCost");
        GameObject controllerObject = new("ControllerSelfFlipNoCost");
        GameObject gridObject = new("GridSelfFlipNoCost");

        try
        {
            DataManager dataManager = dataManagerObject.AddComponent<DataManager>();
            InvokePrivateMethod(dataManager, "Awake");

            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();

            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });
            SetPrivateField(controller, "timelineController", timeline);
            SetPrivateField(controller, "gridManager", gridManager);

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_SelfFlip_NoCost",
                CurrentCost = 1,
                MaxCost = 1,
                ReservedCost = 1,
                Direction = BattleDirection.Right
            };

            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_1",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1,
                GridMove = 1
            };

            PlayerReservedCommand existingMove = CreateMoveCommand(
                runtime,
                moveSkill,
                1,
                Vector2Int.right,
                1,
                new List<Vector2Int> { Vector2Int.right });

            Assert.That(slot.AddCommand(existingMove), Is.True);

            SetPrivateField(controller, "currentUserRuntime", runtime);
            SetPrivateField(controller, "currentSkillData", moveSkill);
            SetPrivateField(controller, "currentCasterGridIndex", 0);
            SetPrivateField(controller, "currentCasterDirection", BattleDirection.Right);
            SetPrivateField(controller, "currentSlotIndex", 0);

            InvokePrivateMethod(controller, "PreviewMoveSelectableCells");

            List<int> selectableIndices =
                GetPrivateField<List<int>>(
                    controller,
                    "currentMoveSelectableIndices");
            Dictionary<int, List<List<Vector2Int>>> candidatesByTarget =
                GetPrivateField<Dictionary<int, List<List<Vector2Int>>>>(
                    controller,
                    "currentMovePathCandidatesByTargetIndex");

            Assert.That(selectableIndices, Does.Contain(1));
            Assert.That(
                candidatesByTarget.TryGetValue(1, out List<List<Vector2Int>> selfFlipCandidates),
                Is.True);
            Assert.That(selfFlipCandidates, Has.Count.EqualTo(1));
            Assert.That(selfFlipCandidates[0], Is.EqualTo(new List<Vector2Int> { Vector2Int.zero }));
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
            Object.DestroyImmediate(dataManagerObject);
        }
    }

    [Test]
    public void ConfirmMoveReservation_MergesGhostSelfFlipWhenNoRemainingCost()
    {
        GameObject dataManagerObject = new("DataManagerConfirmSelfFlipNoCost");
        GameObject timelineObject = new("TimelineConfirmSelfFlipNoCost");
        GameObject slotObject = new("SlotConfirmSelfFlipNoCost");
        GameObject controllerObject = new("ControllerConfirmSelfFlipNoCost");
        GameObject gridObject = new("GridConfirmSelfFlipNoCost");

        try
        {
            DataManager dataManager = dataManagerObject.AddComponent<DataManager>();
            InvokePrivateMethod(dataManager, "Awake");

            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();

            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });
            SetPrivateField(controller, "timelineController", timeline);
            SetPrivateField(controller, "gridManager", gridManager);

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Confirm_SelfFlip_NoCost",
                CurrentCost = 1,
                MaxCost = 1,
                ReservedCost = 1,
                Direction = BattleDirection.Right
            };

            SkillMasterData moveSkill = new()
            {
                SkillId = "S_Move_1",
                Category = Category.Move,
                RangeType = RangeType.Selection,
                ReferenceResource = ReferenceResource.Cost,
                ResourceCostType = ResourceCostType.Fixed,
                ResourceCostValue = 1,
                GridMove = 1
            };

            PlayerReservedCommand existingMove = CreateMoveCommand(
                runtime,
                moveSkill,
                1,
                Vector2Int.right,
                1,
                new List<Vector2Int> { Vector2Int.right });

            Assert.That(slot.AddCommand(existingMove), Is.True);

            SetPrivateField(controller, "currentUserRuntime", runtime);
            SetPrivateField(controller, "currentSkillData", moveSkill);
            SetPrivateField(controller, "currentCasterGridIndex", 1);
            SetPrivateField(controller, "currentCasterDirection", BattleDirection.Right);
            SetPrivateField(controller, "currentSlotIndex", 0);
            SetPrivateField(controller, "currentMoveDistancePerCommand", 1);
            SetPrivateField(controller, "currentMoveReservationCapacity", 0);

            InvokePrivateMethod(controller, "ConfirmMoveReservation", 1);

            Assert.That(slot.Commands, Has.Count.EqualTo(1));
            Assert.That(slot.Commands[0].MoveOffset, Is.EqualTo(Vector2Int.right));
            Assert.That(slot.Commands[0].ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(slot.Commands[0].Direction, Is.EqualTo(BattleDirection.Left));
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
            Object.DestroyImmediate(dataManagerObject);
        }
    }

    [Test]
    public void ConfirmPlayerCommand_MergedSelfFlipUpdatesMoveAndGhostDirection()
    {
        GameObject timelineObject = new("TimelineMergeSelfFlipDirection");
        GameObject slotObject = new("SlotMergeSelfFlipDirection");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Merge_SelfFlip",
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

            PlayerReservedCommand selfFlip = new(runtime, moveSkill);
            selfFlip.SetSelectionResult(
                BattleDirection.Left,
                1,
                new List<int> { 1 },
                Vector2Int.zero);
            selfFlip.SetMoveReservationCost(1, 1);
            selfFlip.SetVisualMoveResult(
                1,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero });

            Assert.That(timeline.ConfirmPlayerCommand(0, first), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, selfFlip), Is.True);

            Assert.That(slot.Commands, Has.Count.EqualTo(1));

            PlayerReservedCommand merged = slot.Commands[0];
            Assert.That(merged.MoveOffset, Is.EqualTo(Vector2Int.right));
            Assert.That(merged.ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(merged.Direction, Is.EqualTo(BattleDirection.Left));
            Assert.That(merged.PreviewMoveDirection, Is.EqualTo(BattleDirection.Left));
            Assert.That(timeline.GetPreviewDirection(runtime, 0), Is.EqualTo(BattleDirection.Left));

            bool found = timeline.TryGetLastMoveGhostPreviewResult(
                runtime,
                out int ghostGridIndex,
                out BattleDirection ghostDirection);

            Assert.That(found, Is.True);
            Assert.That(ghostGridIndex, Is.EqualTo(1));
            Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Left));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void ConfirmPlayerCommand_SelfFlipMergesIntoLastMoveWhenSlotHasMultipleMoves()
    {
        GameObject timelineObject = new("TimelineMergeSelfFlipIntoLastMove");
        GameObject slotObject = new("SlotMergeSelfFlipIntoLastMove");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Merge_SelfFlip_Last",
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

            PlayerReservedCommand selfFlip = new(runtime, moveSkill);
            selfFlip.SetSelectionResult(
                BattleDirection.Left,
                2,
                new List<int> { 2 },
                Vector2Int.zero);
            selfFlip.SetMoveReservationCost(0, 1);
            selfFlip.SetVisualMoveResult(
                2,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero });

            Assert.That(timeline.ConfirmPlayerCommand(0, first), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, second), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, selfFlip), Is.True);

            Assert.That(slot.Commands, Has.Count.EqualTo(2));
            Assert.That(slot.Commands[0].ReservedMoveGridIndex, Is.EqualTo(1));
            Assert.That(slot.Commands[0].Direction, Is.EqualTo(BattleDirection.Right));
            Assert.That(slot.Commands[1].ReservedMoveGridIndex, Is.EqualTo(2));
            Assert.That(slot.Commands[1].Direction, Is.EqualTo(BattleDirection.Left));
            Assert.That(slot.Commands[1].VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.zero
            }));
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void ConfirmPlayerCommand_MergedSelfFlipKeepsDirectionAfterSimulation()
    {
        GameObject timelineObject = new("TimelineMergeSelfFlipSimulationDirection");
        GameObject slotObject = new("SlotMergeSelfFlipSimulationDirection");
        GameObject gridObject = new("GridMergeSelfFlipSimulationDirection");
        GameObject characterObject = new("CharacterMergeSelfFlipSimulationDirection");

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            BattleCharacter character = characterObject.AddComponent<BattleCharacter>();
            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });
            SetPrivateField(timeline, "gridManager", gridManager);

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Merge_SelfFlip_Sim",
                CurrentCost = 10,
                MaxCost = 10,
                Direction = BattleDirection.Right
            };

            character.Initialize(runtime);
            character.SetGridIndex(0);

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

            PlayerReservedCommand selfFlip = new(runtime, moveSkill);
            selfFlip.SetSelectionResult(
                BattleDirection.Left,
                1,
                new List<int> { 1 },
                Vector2Int.zero);
            selfFlip.SetMoveReservationCost(0, 1);
            selfFlip.SetVisualMoveResult(
                1,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero });

            Assert.That(timeline.ConfirmPlayerCommand(0, first), Is.True);
            Assert.That(timeline.ConfirmPlayerCommand(0, selfFlip), Is.True);

            Assert.That(slot.Commands, Has.Count.EqualTo(1));

            PlayerReservedCommand merged = slot.Commands[0];
            Assert.That(merged.VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.zero
            }));
            Assert.That(merged.Direction, Is.EqualTo(BattleDirection.Left));
            Assert.That(timeline.GetPreviewDirection(runtime, 0), Is.EqualTo(BattleDirection.Left));
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);
        }
    }

    [Test]
    public void RefreshMoveGhostPreview_UpdatesExistingGhostDirectionAfterMergedMove()
    {
        GameObject timelineObject = new("TimelineMergeMoveGhostDirection");
        GameObject slotObject = new("SlotMergeMoveGhostDirection");
        GameObject gridObject = new("GridMergeMoveGhostDirection");
        GameObject previewObject = new("MoveGhostPreviewMergeDirection");
        GameObject prefabObject = new("MoveGhostPrefabMergeDirection");
        GameObject characterObject = new("BattleCharacterMergeDirection");
        MoveGhostPreview moveGhostPreview = null;
        Texture2D texture = null;
        Sprite sprite = null;

        try
        {
            BattleTimelineController timeline =
                timelineObject.AddComponent<BattleTimelineController>();
            ReserveTurnSlotUI slot = slotObject.AddComponent<ReserveTurnSlotUI>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            moveGhostPreview = previewObject.AddComponent<MoveGhostPreview>();
            SpriteRenderer ghostPrefab = prefabObject.AddComponent<SpriteRenderer>();
            BattleCharacter character = characterObject.AddComponent<BattleCharacter>();
            SpriteRenderer characterRenderer = characterObject.AddComponent<SpriteRenderer>();

            slot.Init(timeline, 0);

            SetPrivateField(timeline, "reserveSlots", new[] { slot });
            SetPrivateField(timeline, "moveGhostPreview", moveGhostPreview);
            SetPrivateField(moveGhostPreview, "gridManager", gridManager);
            SetPrivateField(moveGhostPreview, "ghostPrefab", ghostPrefab);

            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            characterRenderer.sprite = sprite;

            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Test_Ghost_Direction",
                CurrentCost = 10,
                MaxCost = 10,
                Direction = BattleDirection.Right
            };
            character.Initialize(runtime);
            character.SetGridIndex(0);

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
                0,
                Vector2Int.left,
                1,
                new List<Vector2Int> { Vector2Int.left });

            Assert.That(timeline.ConfirmPlayerCommand(0, first), Is.True);

            Dictionary<string, SpriteRenderer> ghosts =
                GetPrivateField<Dictionary<string, SpriteRenderer>>(
                    moveGhostPreview,
                    "ghostsByCharacterId");
            SpriteRenderer firstGhost = ghosts[runtime.CharacterId];
            Assert.That(firstGhost.flipX, Is.False);

            Assert.That(timeline.ConfirmPlayerCommand(0, second), Is.True);

            SpriteRenderer updatedGhost = ghosts[runtime.CharacterId];
            Assert.That(updatedGhost, Is.SameAs(firstGhost));
            Assert.That(updatedGhost.flipX, Is.True);

            PlayerReservedCommand merged = slot.Commands[0];
            merged.SetMoveDirection(BattleDirection.Right);

            InvokePrivateMethod(timeline, "RefreshMoveGhostPreview");

            Assert.That(merged.PreviewMoveDirection, Is.EqualTo(BattleDirection.Right));
            Assert.That(updatedGhost.flipX, Is.False);
        }
        finally
        {
            if (moveGhostPreview != null)
                moveGhostPreview.ClearAll();

            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(previewObject);
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(slotObject);
            Object.DestroyImmediate(timelineObject);

            if (sprite != null)
                Object.DestroyImmediate(sprite);

            if (texture != null)
                Object.DestroyImmediate(texture);
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

    private static List<List<Vector2Int>> BuildPlayerMoveExecutionStepGroups(
        IReadOnlyList<Vector2Int> moveSteps)
    {
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "BuildPlayerMoveExecutionStepGroups",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "BuildPlayerMoveExecutionStepGroups method is missing.");

        return (List<List<Vector2Int>>)method.Invoke(
            null,
            new object[] { moveSteps });
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
