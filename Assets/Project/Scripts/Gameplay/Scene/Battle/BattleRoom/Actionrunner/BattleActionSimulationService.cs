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
        BattleDirection direction = GetDirectionAfterMove(
            GetPlayerDirection(command),
            command.MoveOffset
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
            range = BattleRangeCalculator.GetSelectionRangeIndices(
                casterGrid,
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

        List<int> movedCells = new();

        for (int i = 0; i < currentCells.Count; i++)
        {
            Vector2Int currentCoord = gridManager.IndexToCoord(currentCells[i]);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
            {
                command.SetSimulatedMoveResult(true, Vector2Int.zero);
                return;
            }

            int targetGrid = gridManager.CoordToIndex(targetCoord);

            if (IsOccupied(targetGrid, "M:" + command.RuntimeId))
            {
                command.SetSimulatedMoveResult(true, Vector2Int.zero);
                return;
            }

            movedCells.Add(targetGrid);
        }

        monsterPositions[command.RuntimeId] = movedCells;
        command.SetSimulatedMoveResult(false, moveOffset);
    }

    private void SimulateMonsterSkillRange(
        MonsterReservedCommand command,
        List<int> currentCells)
    {
        if (currentCells == null || currentCells.Count <= 0)
            return;

        int casterGrid = currentCells[0];

        BattleDirection direction = GetMonsterDirection(command.RuntimeId);

        List<int> range = BattleRangeCalculator.GetDirectionRangeIndices(
            casterGrid,
            command.SkillData.RangeId,
            direction,
            DataManager.Instance.RangeDatabase,
            gridManager
        );

        command.SetRangeResult(range, range);
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
