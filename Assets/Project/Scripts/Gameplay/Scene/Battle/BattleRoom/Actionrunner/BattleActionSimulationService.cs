using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleActionSimulationService
{
    private readonly GridManager gridManager;

    private readonly Dictionary<string, int> playerPositions = new();
    private readonly Dictionary<string, BattleDirection> playerDirections = new();
    private readonly Dictionary<string, List<int>> monsterPositions = new();
    private BattleGridEffectController gridEffectController;

    public BattleActionSimulationService(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public void Simulate(BattleTimelineController timelineController)
    {
        if (timelineController == null || gridManager == null)
            return;

        CaptureCurrentPositions();

        for (int slotIndex = 0; slotIndex < timelineController.SlotCount; slotIndex++)
        {
            SimulateSlot(timelineController, slotIndex);
        }
    }

    public HashSet<int> GetProjectedMonsterOccupiedGridIndices(
        BattleTimelineController timelineController,
        int targetSlotIndex,
        bool includeTargetSlotMonsterCommands)
    {
        HashSet<int> result = new();

        if (gridManager == null)
            return result;

        CaptureCurrentPositions();

        if (timelineController != null && targetSlotIndex >= 0)
        {
            int lastSlotIndex = Mathf.Min(targetSlotIndex, timelineController.SlotCount - 1);

            for (int slotIndex = 0; slotIndex <= lastSlotIndex; slotIndex++)
            {
                bool isTargetSlot = slotIndex == lastSlotIndex;
                IReadOnlyList<PlayerReservedCommand> playerCommands =
                    timelineController.GetPlayerCommands(slotIndex);
                IReadOnlyList<MonsterReservedCommand> monsterCommands =
                    timelineController.GetMonsterCommands(slotIndex);

                if (playerCommands != null)
                {
                    for (int i = 0; i < playerCommands.Count; i++)
                    {
                        if (BattleActionOrderUtility.HasSwift(playerCommands[i]))
                            SimulatePlayerCommand(playerCommands[i]);
                    }
                }

                if ((!isTargetSlot || includeTargetSlotMonsterCommands) &&
                    monsterCommands != null)
                {
                    for (int i = 0; i < monsterCommands.Count; i++)
                        SimulateMonsterCommand(monsterCommands[i]);
                }

                if (!isTargetSlot && playerCommands != null)
                {
                    for (int i = 0; i < playerCommands.Count; i++)
                    {
                        if (!BattleActionOrderUtility.HasSwift(playerCommands[i]))
                            SimulatePlayerCommand(playerCommands[i]);
                    }
                }
            }
        }

        foreach (var pair in monsterPositions)
        {
            if (pair.Value == null)
                continue;

            for (int i = 0; i < pair.Value.Count; i++)
                result.Add(pair.Value[i]);
        }

        return result;
    }

    private void SimulateSlot(BattleTimelineController timelineController, int slotIndex)
    {
        IReadOnlyList<PlayerReservedCommand> playerCommands =
            timelineController.GetPlayerCommands(slotIndex);

        IReadOnlyList<MonsterReservedCommand> monsterCommands =
            timelineController.GetMonsterCommands(slotIndex);

        // 1. Swift 플레이어
        if (playerCommands != null)
        {
            for (int i = 0; i < playerCommands.Count; i++)
            {
                if (BattleActionOrderUtility.HasSwift(playerCommands[i]))
                    SimulatePlayerCommand(playerCommands[i]);
            }
        }

        // 2. 몬스터
        if (monsterCommands != null)
        {
            for (int i = 0; i < monsterCommands.Count; i++)
                SimulateMonsterCommand(monsterCommands[i]);
        }

        // 3. 일반 플레이어
        if (playerCommands != null)
        {
            for (int i = 0; i < playerCommands.Count; i++)
            {
                if (!BattleActionOrderUtility.HasSwift(playerCommands[i]))
                    SimulatePlayerCommand(playerCommands[i]);
            }
        }
    }

    private void SimulatePlayerCommand(PlayerReservedCommand command)
    {
        if (command == null || command.UserRuntime == null || command.SkillData == null)
            return;

        if (command.UserRuntime.IsDead)
            return;

        if (!playerPositions.TryGetValue(command.CharacterId, out int currentGrid))
            return;

        if (command.ReservedMoveGridIndex >= 0)
        {
            SimulatePlayerMove(command, currentGrid);
            return;
        }

        SimulatePlayerSkillRange(command, currentGrid);
    }

    private void SimulatePlayerMove(PlayerReservedCommand command, int currentGrid)
    {
        ReplanPlayerMovePath(command, currentGrid);

        BattleDirection direction = GetDirectionAfterMoveSteps(
            GetPlayerDirection(command),
            command.VisualMoveSteps != null && command.VisualMoveSteps.Count > 0
                ? command.VisualMoveSteps
                : new List<Vector2Int> { command.MoveOffset }
        );

        playerDirections[command.CharacterId] = direction;
        command.SetMoveDirection(direction);

        bool reachedTarget = TryGetPlayerMoveTargetGridIndex(
            currentGrid,
            command,
            "P:" + command.CharacterId,
            out int targetGrid);

        Vector2Int startCoord = gridManager.IndexToCoord(currentGrid);
        Vector2Int targetCoord = gridManager.IndexToCoord(targetGrid);
        Vector2Int actualMoveOffset = targetCoord - startCoord;

        if (!reachedTarget)
        {
            playerPositions[command.CharacterId] = targetGrid;
            command.SetSimulatedMoveResult(true, targetGrid, actualMoveOffset);
            return;
        }

        playerPositions[command.CharacterId] = targetGrid;
        command.SetSimulatedMoveResult(false, targetGrid, actualMoveOffset);
    }

    private void ReplanPlayerMovePath(PlayerReservedCommand command, int currentGrid)
    {
        if (command == null || gridManager == null)
            return;

        if (command.ReservedMoveGridIndex < 0)
            return;

        int targetGridIndex = command.ReservedMoveGridIndex;
        int moveDistancePerCommand = Mathf.Max(1, command.MoveDistancePerCost);
        int reservationCapacity = Mathf.Max(
            command.Cost,
            PlayerReservedCommand.CalculateMoveCost(
                command.PlannedMoveDistance,
                moveDistancePerCommand));

        if (reservationCapacity <= 0)
            return;

        HashSet<int> blockedGridIndices =
            BuildPlayerMoveBlockedGridIndices(
                "P:" + command.CharacterId,
                targetGridIndex);

        if (command.VisualMoveSteps != null &&
            command.VisualMoveSteps.Count > 0 &&
            IsMovePathClear(currentGrid, command.VisualMoveSteps, blockedGridIndices))
        {
            return;
        }

        List<Vector2Int> path = PlayerSkillReservationController.ChooseReservableMovePath(
            currentGrid,
            targetGridIndex,
            moveDistancePerCommand,
            reservationCapacity,
            gridManager,
            blockedGridIndices,
            blockedGridIndices);

        if (path == null || path.Count <= 0)
            return;

        List<Vector2Int> visualPath = BuildReplannedVisualMoveSteps(command, path);
        Vector2Int totalMoveOffset = GetTotalMoveOffset(visualPath);

        if (command.VisualMoveSteps != null &&
            IsSamePath(command.VisualMoveSteps, visualPath) &&
            command.MoveOffset == totalMoveOffset)
        {
            return;
        }

        BattleDirection direction = GetDirectionAfterMoveSteps(
            GetPlayerDirection(command),
            visualPath);

        command.SetSelectionResult(
            direction,
            targetGridIndex,
            new List<int> { targetGridIndex },
            totalMoveOffset);
        command.SetVisualMoveResult(
            targetGridIndex,
            totalMoveOffset,
            visualPath);
    }

    private List<Vector2Int> BuildReplannedVisualMoveSteps(
        PlayerReservedCommand command,
        IReadOnlyList<Vector2Int> replannedPath)
    {
        List<Vector2Int> visualPath = replannedPath != null
            ? new List<Vector2Int>(replannedPath)
            : new List<Vector2Int>();

        if (HasTerminalSelfFlipStep(command))
            visualPath.Add(Vector2Int.zero);

        return visualPath;
    }

    private bool HasTerminalSelfFlipStep(PlayerReservedCommand command)
    {
        if (command == null || command.VisualMoveSteps == null)
            return false;

        int lastIndex = command.VisualMoveSteps.Count - 1;
        return lastIndex >= 0 && command.VisualMoveSteps[lastIndex] == Vector2Int.zero;
    }

    private HashSet<int> BuildPlayerMoveBlockedGridIndices(
        string selfKey,
        int targetGridIndex)
    {
        HashSet<int> blockedGridIndices = new();

        foreach (var pair in playerPositions)
        {
            if ("P:" + pair.Key == selfKey)
                continue;

            blockedGridIndices.Add(pair.Value);
        }

        foreach (var pair in monsterPositions)
        {
            if (pair.Value == null)
                continue;

            for (int i = 0; i < pair.Value.Count; i++)
            {
                int gridIndex = pair.Value[i];

                if (gridIndex == targetGridIndex)
                    continue;

                blockedGridIndices.Add(gridIndex);
            }
        }

        AddBlockedGridEffectIndices(blockedGridIndices);

        return blockedGridIndices;
    }

    private bool IsMovePathClear(
        int currentGrid,
        IReadOnlyList<Vector2Int> moveSteps,
        ISet<int> blockedGridIndices)
    {
        if (gridManager == null || moveSteps == null || moveSteps.Count <= 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGrid);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        for (int i = 0; i < moveSteps.Count; i++)
        {
            Vector2Int moveOffset = moveSteps[i];

            if (!IsMoveOffsetClear(ref currentCoord, moveOffset.x, true, blockedGridIndices))
                return false;

            if (!IsMoveOffsetClear(ref currentCoord, moveOffset.y, false, blockedGridIndices))
                return false;
        }

        return true;
    }

    private bool IsMoveOffsetClear(
        ref Vector2Int currentCoord,
        int amount,
        bool horizontal,
        ISet<int> blockedGridIndices)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            Vector2Int nextCoord = currentCoord + (horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step));

            if (!gridManager.IsValidCoord(nextCoord))
                return false;

            int gridIndex = gridManager.CoordToIndex(nextCoord);

            if (blockedGridIndices != null && blockedGridIndices.Contains(gridIndex))
                return false;

            currentCoord = nextCoord;
            remaining -= step;
        }

        return true;
    }

    private Vector2Int GetTotalMoveOffset(IReadOnlyList<Vector2Int> moveSteps)
    {
        Vector2Int total = Vector2Int.zero;

        if (moveSteps == null)
            return total;

        for (int i = 0; i < moveSteps.Count; i++)
            total += moveSteps[i];

        return total;
    }

    private bool IsSamePath(IReadOnlyList<Vector2Int> a, IReadOnlyList<Vector2Int> b)
    {
        if (a == null || b == null)
            return false;

        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGrid,
        PlayerReservedCommand command,
        string selfKey,
        out int targetGrid)
    {
        targetGrid = currentGrid;

        if (command == null)
            return false;

        if (command.VisualMoveSteps != null && command.VisualMoveSteps.Count > 0)
        {
            return TryGetPlayerMoveTargetGridIndex(
                currentGrid,
                command.VisualMoveSteps,
                selfKey,
                out targetGrid
            );
        }

        return TryGetPlayerMoveTargetGridIndex(
            currentGrid,
            command.MoveOffset,
            selfKey,
            out targetGrid
        );
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGrid,
        Vector2Int moveOffset,
        string selfKey,
        out int targetGrid)
    {
        targetGrid = currentGrid;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGrid);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        if (moveOffset == Vector2Int.zero)
            return true;

        bool reachedTarget = true;

        if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, selfKey))
            reachedTarget = false;

        if (reachedTarget &&
            !TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, selfKey))
        {
            reachedTarget = false;
        }

        targetGrid = gridManager.CoordToIndex(currentCoord);
        return reachedTarget;
    }

    private bool TryGetPlayerMoveTargetGridIndex(
        int currentGrid,
        IReadOnlyList<Vector2Int> moveSteps,
        string selfKey,
        out int targetGrid)
    {
        targetGrid = currentGrid;

        if (moveSteps == null || moveSteps.Count <= 0)
            return false;

        Vector2Int currentCoord = gridManager.IndexToCoord(currentGrid);

        if (!gridManager.IsValidCoord(currentCoord))
            return false;

        bool reachedTarget = true;

        for (int i = 0; i < moveSteps.Count; i++)
        {
            Vector2Int moveOffset = moveSteps[i];

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.x, true, selfKey))
            {
                reachedTarget = false;
                break;
            }

            if (!TryApplyPlayerMoveAxisStep(ref currentCoord, moveOffset.y, false, selfKey))
            {
                reachedTarget = false;
                break;
            }
        }

        targetGrid = gridManager.CoordToIndex(currentCoord);
        return reachedTarget;
    }

    private bool TryApplyPlayerMoveAxisStep(
        ref Vector2Int currentCoord,
        int amount,
        bool horizontal,
        string selfKey)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            Vector2Int nextCoord = currentCoord + (horizontal
                ? new Vector2Int(step, 0)
                : new Vector2Int(0, step));

            if (!gridManager.IsValidCoord(nextCoord))
                return false;

            int gridIndex = gridManager.CoordToIndex(nextCoord);

            if (IsOccupiedForPlayerMove(gridIndex, selfKey))
                return false;

            if (IsGridEffectBlocked(gridIndex))
                return false;

            currentCoord = nextCoord;
            remaining -= step;
        }

        return true;
    }

    private void SimulatePlayerSkillRange(PlayerReservedCommand command, int casterGrid)
    {
        List<int> range = new();
        BattleDirection direction = GetPlayerDirection(command);
        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(
            command.UserRuntime,
            command.SkillData);

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            range = BattleRangeCalculator.GetDirectionRangeIndices(
                casterGrid,
                rangeId,
                direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );

            command.SetDirectionResult(direction, range, range);
            return;
        }
        else if (command.SkillData.RangeType == RangeType.Selection)
        {
            int selectionCenter = command.SelectedGridIndex >= 0
                ? command.SelectedGridIndex
                : casterGrid;

            range = BattleRangeCalculator.GetSelectionRangeIndices(
                selectionCenter,
                rangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }

        command.SetSimulatedRangeResult(range, range);
    }

    private BattleDirection GetPlayerDirection(PlayerReservedCommand command)
    {
        if (command == null)
            return BattleDirection.Right;

        if (playerDirections.TryGetValue(command.CharacterId, out BattleDirection direction))
            return direction;

        return command.Direction;
    }

    private BattleDirection GetDirectionAfterMove(
        BattleDirection currentDirection,
        Vector2Int moveOffset)
    {
        if (moveOffset.x < 0)
            return BattleDirection.Left;

        if (moveOffset.x > 0)
            return BattleDirection.Right;

        if (moveOffset == Vector2Int.zero)
            return GetOppositeDirection(currentDirection);

        return currentDirection;
    }

    private BattleDirection GetDirectionAfterMoveSteps(
        BattleDirection currentDirection,
        IReadOnlyList<Vector2Int> moveSteps)
    {
        if (moveSteps == null || moveSteps.Count <= 0)
            return currentDirection;

        BattleDirection direction = currentDirection;

        for (int i = 0; i < moveSteps.Count; i++)
            direction = GetDirectionAfterMove(direction, moveSteps[i]);

        return direction;
    }

    private BattleDirection GetOppositeDirection(BattleDirection direction)
    {
        return direction == BattleDirection.Right
            ? BattleDirection.Left
            : BattleDirection.Right;
    }

    private void SimulateMonsterCommand(MonsterReservedCommand command)
    {
        if (command == null || command.UserRuntime == null || command.SkillData == null)
            return;

        if (!monsterPositions.TryGetValue(command.RuntimeId, out List<int> currentCells))
            return;

        bool isMove = command.SkillData.TimelineNotation == TimelineActionType.Move;

        if (isMove)
        {
            SimulateMonsterMove(command, currentCells);
            return;
        }

        SimulateMonsterSkillRange(command, currentCells);
    }

    private void SimulateMonsterMove(MonsterReservedCommand command, List<int> currentCells)
    {
        Vector2Int moveOffset = command.MoveOffset;

        if (moveOffset == Vector2Int.zero)
        {
            command.SetSimulatedMoveResult(true, Vector2Int.zero);
            return;
        }

        if (IsNocturnPortalMove(command))
        {
            if (!TryGetSimulatedPortalDestinationCells(
                    currentCells,
                    moveOffset,
                    "M:" + command.RuntimeId,
                    out List<int> portalCells))
            {
                command.SetSimulatedMoveResult(true, Vector2Int.zero);
                return;
            }

            monsterPositions[command.RuntimeId] = portalCells;
            command.SetSimulatedMoveResult(false, moveOffset);
            return;
        }

        SimulatedMonsterMoveResolution moveResolution = ResolveSimulatedMonsterMove(
            currentCells,
            moveOffset,
            "M:" + command.RuntimeId);

        if (moveResolution.ActualOffset != Vector2Int.zero &&
            moveResolution.MovedCells != null &&
            moveResolution.MovedCells.Count > 0)
        {
            monsterPositions[command.RuntimeId] = moveResolution.MovedCells;
        }

        command.SetSimulatedMoveResult(
            moveResolution.WasBlocked,
            moveResolution.ActualOffset);
    }

    private sealed class SimulatedMonsterMoveResolution
    {
        public Vector2Int ActualOffset;
        public bool WasBlocked;
        public List<int> MovedCells = new();
    }

    private SimulatedMonsterMoveResolution ResolveSimulatedMonsterMove(
        IReadOnlyList<int> currentCells,
        Vector2Int requestedOffset,
        string selfKey)
    {
        if (currentCells == null || currentCells.Count <= 0 || requestedOffset == Vector2Int.zero)
            return new SimulatedMonsterMoveResolution { WasBlocked = true };

        if (requestedOffset.x != 0 && requestedOffset.y != 0)
        {
            SimulatedMonsterMoveResolution horizontalFirst =
                ResolveSimulatedMonsterMoveAxisOrder(currentCells, requestedOffset, selfKey, true);
            SimulatedMonsterMoveResolution verticalFirst =
                ResolveSimulatedMonsterMoveAxisOrder(currentCells, requestedOffset, selfKey, false);

            int horizontalDistance =
                Mathf.Abs(horizontalFirst.ActualOffset.x) + Mathf.Abs(horizontalFirst.ActualOffset.y);
            int verticalDistance =
                Mathf.Abs(verticalFirst.ActualOffset.x) + Mathf.Abs(verticalFirst.ActualOffset.y);

            return verticalDistance > horizontalDistance
                ? verticalFirst
                : horizontalFirst;
        }

        return ResolveSimulatedMonsterMoveAxisOrder(
            currentCells,
            requestedOffset,
            selfKey,
            requestedOffset.x != 0);
    }

    private SimulatedMonsterMoveResolution ResolveSimulatedMonsterMoveAxisOrder(
        IReadOnlyList<int> currentCells,
        Vector2Int requestedOffset,
        string selfKey,
        bool horizontalFirst)
    {
        SimulatedMonsterMoveResolution result = new();
        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < currentCells.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(currentCells[i]));

        Vector2Int startMainCoord = currentCoords[0];
        bool completed;

        if (horizontalFirst)
        {
            completed = TryApplySimulatedMonsterMoveAxisSteps(
                currentCoords,
                requestedOffset.x,
                true,
                selfKey);

            if (completed)
            {
                completed = TryApplySimulatedMonsterMoveAxisSteps(
                    currentCoords,
                    requestedOffset.y,
                    false,
                    selfKey);
            }
        }
        else
        {
            completed = TryApplySimulatedMonsterMoveAxisSteps(
                currentCoords,
                requestedOffset.y,
                false,
                selfKey);

            if (completed)
            {
                completed = TryApplySimulatedMonsterMoveAxisSteps(
                    currentCoords,
                    requestedOffset.x,
                    true,
                    selfKey);
            }
        }

        result.ActualOffset = currentCoords[0] - startMainCoord;
        result.WasBlocked = !completed || result.ActualOffset != requestedOffset;

        for (int i = 0; i < currentCoords.Count; i++)
            result.MovedCells.Add(gridManager.CoordToIndex(currentCoords[i]));

        return result;
    }


    private static bool IsNocturnPortalMove(MonsterReservedCommand command)
    {
        // 포탈 여부는 몬스터/스킬 ID로 추측하지 않고 AI가 예약한 플래그만 사용합니다.
        // 따라서 S_Monster_18은 중간 경로가 막혀 있어도 일반 이동 경로 검사를 타지 않습니다.
        return command != null && command.IsPortalMove;
    }

    private bool TryGetSimulatedPortalDestinationCells(
        IReadOnlyList<int> currentCells,
        Vector2Int moveOffset,
        string selfKey,
        out List<int> movedCells)
    {
        movedCells = new List<int>();

        if (currentCells == null || currentCells.Count <= 0 || gridManager == null)
            return false;

        for (int i = 0; i < currentCells.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(currentCells[i]);
            Vector2Int destinationCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(destinationCoord))
                return false;

            int destinationGridIndex = gridManager.CoordToIndex(destinationCoord);

            if (IsOccupied(destinationGridIndex, selfKey))
                return false;

            movedCells.Add(destinationGridIndex);
        }

        return true;
    }

    private bool TryGetSimulatedMonsterMoveCells(
        IReadOnlyList<int> currentCells,
        Vector2Int moveOffset,
        string selfKey,
        out List<int> movedCells)
    {
        movedCells = null;

        if (currentCells == null || currentCells.Count <= 0)
            return false;

        if (moveOffset.x != 0 && moveOffset.y != 0)
        {
            return TryGetSimulatedMonsterMoveCellsInAxisOrder(
                       currentCells,
                       moveOffset,
                       selfKey,
                       true,
                       out movedCells) ||
                   TryGetSimulatedMonsterMoveCellsInAxisOrder(
                       currentCells,
                       moveOffset,
                       selfKey,
                       false,
                       out movedCells);
        }

        return TryGetSimulatedMonsterMoveCellsInAxisOrder(
            currentCells,
            moveOffset,
            selfKey,
            moveOffset.x != 0,
            out movedCells);
    }

    private bool TryGetSimulatedMonsterMoveCellsInAxisOrder(
        IReadOnlyList<int> currentCells,
        Vector2Int moveOffset,
        string selfKey,
        bool horizontalFirst,
        out List<int> movedCells)
    {
        List<Vector2Int> currentCoords = new();

        for (int i = 0; i < currentCells.Count; i++)
            currentCoords.Add(gridManager.IndexToCoord(currentCells[i]));

        bool canMove = horizontalFirst
            ? TryApplySimulatedMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, selfKey) &&
              TryApplySimulatedMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, selfKey)
            : TryApplySimulatedMonsterMoveAxisSteps(currentCoords, moveOffset.y, false, selfKey) &&
              TryApplySimulatedMonsterMoveAxisSteps(currentCoords, moveOffset.x, true, selfKey);

        movedCells = new List<int>();

        if (!canMove)
            return false;

        for (int i = 0; i < currentCoords.Count; i++)
            movedCells.Add(gridManager.CoordToIndex(currentCoords[i]));

        return true;
    }

    private bool TryApplySimulatedMonsterMoveAxisSteps(
        List<Vector2Int> currentCoords,
        int amount,
        bool horizontal,
        string selfKey)
    {
        int remaining = amount;

        while (remaining != 0)
        {
            int step = remaining > 0 ? 1 : -1;
            List<Vector2Int> nextCoords = new();

            for (int i = 0; i < currentCoords.Count; i++)
            {
                Vector2Int nextCoord = currentCoords[i] + (horizontal
                    ? new Vector2Int(step, 0)
                    : new Vector2Int(0, step));

                if (!gridManager.IsValidCoord(nextCoord))
                    return false;

                int targetGrid = gridManager.CoordToIndex(nextCoord);

                if (IsOccupied(targetGrid, selfKey))
                    return false;

                if (IsGridEffectBlocked(targetGrid))
                    return false;

                nextCoords.Add(nextCoord);
            }

            currentCoords.Clear();
            currentCoords.AddRange(nextCoords);
            remaining -= step;
        }

        return true;
    }

    private void SimulateMonsterSkillRange(
        MonsterReservedCommand command,
        List<int> currentCells)
    {
        if (command != null && command.HasExplicitRangeResult)
            return;

        if (currentCells == null || currentCells.Count <= 0)
            return;

        int casterGrid = currentCells[0];
        int rangeOriginGrid = command.RangeOriginGridIndex >= 0
            ? command.RangeOriginGridIndex
            : casterGrid;

        BattleDirection direction = command.HasForcedDirection
            ? command.ForcedDirection
            : command.RangeOriginGridIndex >= 0
                ? GetDirectionToNearestSimulatedPlayer(rangeOriginGrid)
                : GetMonsterDirection(command.RuntimeId);

        List<int> range = BattleRangeCalculator.GetDirectionRangeIndices(
            rangeOriginGrid,
            command.SkillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        command.SetRangeResult(range, range);
    }

    private BattleDirection GetDirectionToNearestSimulatedPlayer(int originGridIndex)
    {
        if (gridManager == null || originGridIndex < 0 || playerPositions.Count <= 0)
            return BattleDirection.Left;

        Vector2Int originCoord = gridManager.IndexToCoord(originGridIndex);
        int nearestDistance = int.MaxValue;
        int nearestGridIndex = -1;

        foreach (var pair in playerPositions)
        {
            int playerGridIndex = pair.Value;

            if (playerGridIndex < 0)
                continue;

            Vector2Int playerCoord = gridManager.IndexToCoord(playerGridIndex);
            int distance =
                Mathf.Abs(playerCoord.x - originCoord.x) +
                Mathf.Abs(playerCoord.y - originCoord.y);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGridIndex = playerGridIndex;
            }
        }

        if (nearestGridIndex < 0)
            return BattleDirection.Left;

        return gridManager.IndexToCoord(nearestGridIndex).x >= originCoord.x
            ? BattleDirection.Right
            : BattleDirection.Left;
    }

    private BattleDirection GetMonsterDirection(string runtimeId)
    {
        MonsterUnit monster = FindMonster(runtimeId);

        if (monster == null)
            return BattleDirection.Left;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();

        if (facing == null)
            return BattleDirection.Left;

        return facing.IsFacingRight ? BattleDirection.Right : BattleDirection.Left;
    }

    private bool IsOccupied(int gridIndex, string selfKey)
    {
        foreach (var pair in playerPositions)
        {
            if ("P:" + pair.Key == selfKey)
                continue;

            if (pair.Value == gridIndex)
                return true;
        }

        foreach (var pair in monsterPositions)
        {
            if ("M:" + pair.Key == selfKey)
                continue;

            if (pair.Value != null && pair.Value.Contains(gridIndex))
                return true;
        }

        return false;
    }

    private bool IsOccupiedForPlayerMove(int gridIndex, string selfKey)
    {
        foreach (var pair in playerPositions)
        {
            if ("P:" + pair.Key == selfKey)
                continue;

            if (pair.Value == gridIndex)
                return true;
        }

        return false;
    }

    private void AddBlockedGridEffectIndices(HashSet<int> blockedGridIndices)
    {
        if (blockedGridIndices == null)
            return;

        BattleGridEffectController controller = ResolveGridEffectController();

        if (controller == null || controller.State == null)
            return;

        IReadOnlyList<Relic.Gameplay.Battle.BattleGridEffectPlacement> placements =
            controller.State.GetPlacements();

        for (int i = 0; i < placements.Count; i++)
        {
            int gridIndex = placements[i].GridIndex;

            if (gridIndex >= 0 && controller.IsBlocked(gridIndex))
                blockedGridIndices.Add(gridIndex);
        }
    }

    private bool IsGridEffectBlocked(int gridIndex)
    {
        BattleGridEffectController controller = ResolveGridEffectController();
        return controller != null && controller.IsBlocked(gridIndex);
    }

    private BattleGridEffectController ResolveGridEffectController()
    {
        if (gridEffectController != null)
            return gridEffectController;

        gridEffectController = Object.FindFirstObjectByType<BattleGridEffectController>(
            FindObjectsInactive.Include
        );

        return gridEffectController;
    }

    private void CaptureCurrentPositions()
    {
        playerPositions.Clear();
        playerDirections.Clear();
        monsterPositions.Clear();

        BattleCharacter[] players = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || players[i].RuntimeData == null)
                continue;

            if (players[i].CurrentGridIndex < 0)
                continue;

            playerPositions[players[i].CharacterId] = players[i].CurrentGridIndex;
            playerDirections[players[i].CharacterId] = players[i].RuntimeData.Direction;
        }

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.IsDead)
                continue;

            List<int> occupiedGridIndices = new(monsters[i].OccupiedGridIndices);

            monsterPositions[monsters[i].RuntimeData.RuntimeId] =
                new List<int>(occupiedGridIndices);
        }
    }

    private MonsterUnit FindMonster(string runtimeId)
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            if (monsters[i].RuntimeData.RuntimeId == runtimeId)
                return monsters[i];
        }

        return null;
    }
}
