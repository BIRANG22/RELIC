using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleActionSimulationService
{
    private readonly GridManager gridManager;

    private readonly Dictionary<string, int> playerPositions = new();
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
        Vector2Int currentCoord = gridManager.IndexToCoord(currentGrid);
        Vector2Int targetCoord = currentCoord + command.MoveOffset;

        if (!gridManager.IsValidCoord(targetCoord))
        {
            command.SetSimulatedMoveResult(true, currentGrid, Vector2Int.zero);
            return;
        }

        int targetGrid = gridManager.CoordToIndex(targetCoord);

        if (IsOccupied(targetGrid, "P:" + command.CharacterId))
        {
            command.SetSimulatedMoveResult(true, currentGrid, Vector2Int.zero);
            return;
        }

        playerPositions[command.CharacterId] = targetGrid;
        command.SetSimulatedMoveResult(false, targetGrid, command.MoveOffset);
    }

    private void SimulatePlayerSkillRange(PlayerReservedCommand command, int casterGrid)
    {
        List<int> range = new();

        if (command.SkillData.RangeType == RangeType.Direction)
        {
            range = BattleRangeCalculator.GetDirectionRangeIndices(
                casterGrid,
                command.SkillData.RangeId,
                command.Direction,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }
        else if (command.SkillData.RangeType == RangeType.Selection)
        {
            range = BattleRangeCalculator.GetSelectionRangeIndices(
                casterGrid,
                command.SkillData.RangeId,
                DataManager.Instance.RangeDatabase,
                gridManager
            );
        }

        command.SetSimulatedRangeResult(range, range);
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

    private void CaptureCurrentPositions()
    {
        playerPositions.Clear();
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

            monsterPositions[monsters[i].RuntimeData.RuntimeId] =
                new List<int>(monsters[i].OccupiedGridIndices);
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