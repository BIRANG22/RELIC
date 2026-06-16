using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleActionBatchBuilder
{
    private readonly GridManager gridManager;

    private class ActionInfo
    {
        public string ActorKey;
        public List<int> CurrentCells = new();
        public List<int> MoveTargetCells = new();
        public List<int> EffectTargetCells = new();
        public bool IsMove;
        public bool IsEffect;
    }

    public BattleActionBatchBuilder(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

    public List<BattleActionBatch> Build(BattleTimelineController timelineController)
    {
        List<BattleActionBatch> batches = new();

        if (timelineController == null)
            return batches;

        for (int slotIndex = 0; slotIndex < timelineController.SlotCount; slotIndex++)
        {
            EnsureBatchExists(batches, slotIndex);

            var playerCommands = timelineController.GetPlayerCommands(slotIndex);

            if (playerCommands != null)
            {
                for (int i = 0; i < playerCommands.Count; i++)
                    AddPlayerCommand(batches, playerCommands[i], slotIndex);
            }

            var monsterCommands = timelineController.GetMonsterCommands(slotIndex);

            if (monsterCommands != null)
            {
                for (int i = 0; i < monsterCommands.Count; i++)
                    AddMonsterCommand(batches, monsterCommands[i], slotIndex);
            }
        }

        RemoveEmptyTailBatches(batches);
        return batches;
    }

    private void AddPlayerCommand(
        List<BattleActionBatch> batches,
        PlayerReservedCommand command,
        int minBatchIndex)
    {
        if (command == null)
            return;

        ActionInfo next = CreatePlayerActionInfo(command);

        EnsureBatchExists(batches, minBatchIndex);

        for (int i = minBatchIndex; i < batches.Count; i++)
        {
            if (!CanUseBatchForTimelineSlot(batches[i], minBatchIndex))
                continue;

            if (CanAddAction(batches[i], next))
            {
                batches[i].SetTimelineSlotIndexIfNeeded(minBatchIndex);
                batches[i].PlayerCommands.Add(command);
                return;
            }
        }

        BattleActionBatch newBatch = new();
        newBatch.SetTimelineSlotIndexIfNeeded(minBatchIndex);
        newBatch.PlayerCommands.Add(command);
        batches.Add(newBatch);
    }

    private void AddMonsterCommand(
        List<BattleActionBatch> batches,
        MonsterReservedCommand command,
        int minBatchIndex)
    {
        if (command == null)
            return;

        ActionInfo next = CreateMonsterActionInfo(command);

        EnsureBatchExists(batches, minBatchIndex);

        for (int i = minBatchIndex; i < batches.Count; i++)
        {
            if (!CanUseBatchForTimelineSlot(batches[i], minBatchIndex))
                continue;

            if (CanAddAction(batches[i], next))
            {
                batches[i].SetTimelineSlotIndexIfNeeded(minBatchIndex);
                batches[i].MonsterCommands.Add(command);
                return;
            }
        }

        BattleActionBatch newBatch = new();
        newBatch.SetTimelineSlotIndexIfNeeded(minBatchIndex);
        newBatch.MonsterCommands.Add(command);
        batches.Add(newBatch);
    }


    private bool CanUseBatchForTimelineSlot(BattleActionBatch batch, int timelineSlotIndex)
    {
        if (batch == null)
            return false;

        return batch.CanAcceptTimelineSlot(timelineSlotIndex);
    }

    private bool CanAddAction(BattleActionBatch batch, ActionInfo next)
    {
        if (batch == null || next == null)
            return false;

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            if (HasConflict(CreatePlayerActionInfo(batch.PlayerCommands[i]), next))
                return false;
        }

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            if (HasConflict(CreateMonsterActionInfo(batch.MonsterCommands[i]), next))
                return false;
        }

        return true;
    }

    private bool HasConflict(ActionInfo a, ActionInfo b)
    {
        if (a == null || b == null)
            return false;

        if (!string.IsNullOrWhiteSpace(a.ActorKey) &&
            a.ActorKey == b.ActorKey)
            return true;

        return HasMoveConflict(a, b) || HasEffectConflict(a, b);
    }

    private bool HasMoveConflict(ActionInfo a, ActionInfo b)
    {
        if (a.IsMove)
        {
            if (Intersects(a.MoveTargetCells, b.CurrentCells))
                return true;

            if (Intersects(a.MoveTargetCells, b.MoveTargetCells))
                return true;

            if (Intersects(a.CurrentCells, b.MoveTargetCells))
                return true;
        }

        if (b.IsMove)
        {
            if (Intersects(b.MoveTargetCells, a.CurrentCells))
                return true;

            if (Intersects(b.MoveTargetCells, a.MoveTargetCells))
                return true;

            if (Intersects(b.CurrentCells, a.MoveTargetCells))
                return true;
        }

        return false;
    }

    private bool HasEffectConflict(ActionInfo a, ActionInfo b)
    {
        if (a.IsEffect)
        {
            if (Intersects(a.EffectTargetCells, b.CurrentCells))
                return true;

            if (Intersects(a.EffectTargetCells, b.EffectTargetCells))
                return true;
        }

        if (b.IsEffect)
        {
            if (Intersects(b.EffectTargetCells, a.CurrentCells))
                return true;

            if (Intersects(b.EffectTargetCells, a.EffectTargetCells))
                return true;
        }

        return false;
    }

    private ActionInfo CreatePlayerActionInfo(PlayerReservedCommand command)
    {
        ActionInfo info = new();

        if (command == null)
            return info;

        info.ActorKey = "P:" + command.CharacterId;

        BattleCharacter character = FindPlayer(command.CharacterId);

        if (character != null && character.CurrentGridIndex >= 0)
            info.CurrentCells.Add(character.CurrentGridIndex);

        if (command.ReservedMoveGridIndex >= 0)
        {
            info.IsMove = true;
            AddUnique(info.MoveTargetCells, command.ReservedMoveGridIndex);
        }
        else
        {
            info.IsEffect = command.TargetGridIndices != null &&
                            command.TargetGridIndices.Count > 0;

            AddUnique(info.EffectTargetCells, command.TargetGridIndices);
        }

        return info;
    }

    private ActionInfo CreateMonsterActionInfo(MonsterReservedCommand command)
    {
        ActionInfo info = new();

        if (command == null)
            return info;

        info.ActorKey = "M:" + command.RuntimeId;

        MonsterUnit monster = FindMonster(command.RuntimeId);

        if (monster != null)
            AddUnique(info.CurrentCells, monster.OccupiedGridIndices);

        bool isTimelineMove =
            command.SkillData != null &&
            command.SkillData.TimelineNotation == TimelineActionType.Move;

        bool isDashAttack =
            command.SkillData != null &&
            command.SkillData.SkillId == "S_Monster_07";

        if (isTimelineMove)
        {
            info.IsMove = true;
            AddMonsterMoveTargetCells(info, monster, command.MoveOffset);
        }
        else if (isDashAttack)
        {
            info.IsMove = true;
            info.IsEffect = true;

            AddDashMovePathCells(info, monster, command);
            AddUnique(info.EffectTargetCells, command.TargetGridIndices);
        }
        else
        {
            info.IsEffect = command.TargetGridIndices != null &&
                            command.TargetGridIndices.Count > 0;

            AddUnique(info.EffectTargetCells, command.TargetGridIndices);
        }

        return info;
    }

    private void AddMonsterMoveTargetCells(
        ActionInfo info,
        MonsterUnit monster,
        Vector2Int moveOffset)
    {
        if (info == null || monster == null || gridManager == null)
            return;

        if (moveOffset == Vector2Int.zero)
            return;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int currentIndex = monster.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
            Vector2Int targetCoord = currentCoord + moveOffset;

            if (!gridManager.IsValidCoord(targetCoord))
                continue;

            AddUnique(info.MoveTargetCells, gridManager.CoordToIndex(targetCoord));
        }
    }

    private void AddDashMovePathCells(
        ActionInfo info,
        MonsterUnit monster,
        MonsterReservedCommand command)
    {
        if (info == null || monster == null || command == null || command.SkillData == null || gridManager == null)
            return;

        BattleUnitFacing facing = monster.GetComponent<BattleUnitFacing>();
        bool facingRight = facing == null || facing.IsFacingRight;

        int dirX = facingRight ? 1 : -1;
        int maxMove = Mathf.Max(1, command.SkillData.GridMove);

        for (int step = 1; step <= maxMove; step++)
        {
            Vector2Int offset = new Vector2Int(dirX * step, 0);

            if (!CanDashStepForBatch(monster, offset))
                break;

            for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
            {
                int currentIndex = monster.OccupiedGridIndices[i];
                Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
                Vector2Int targetCoord = currentCoord + offset;

                if (!gridManager.IsValidCoord(targetCoord))
                    continue;

                AddUnique(info.MoveTargetCells, gridManager.CoordToIndex(targetCoord));
            }
        }
    }

    private bool CanDashStepForBatch(MonsterUnit monster, Vector2Int offset)
    {
        if (monster == null || gridManager == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            int currentIndex = monster.OccupiedGridIndices[i];
            Vector2Int currentCoord = gridManager.IndexToCoord(currentIndex);
            Vector2Int targetCoord = currentCoord + offset;

            if (!gridManager.IsValidCoord(targetCoord))
                return false;

            int targetIndex = gridManager.CoordToIndex(targetCoord);

            if (IsPlayerAtGrid(targetIndex))
                return false;

            if (BattleOccupancyService.IsOccupiedByAnyUnit(targetIndex, null, monster))
                return false;
        }

        return true;
    }

    private bool IsPlayerAtGrid(int gridIndex)
    {
        BattleCharacter[] players = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || players[i].RuntimeData == null)
                continue;

            if (players[i].CurrentGridIndex == gridIndex)
                return true;
        }

        return false;
    }

    private BattleCharacter FindPlayer(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        BattleCharacter[] players = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            if (players[i].CharacterId == characterId)
                return players[i];
        }

        return null;
    }

    private MonsterUnit FindMonster(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            return null;

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

    private bool Intersects(List<int> a, List<int> b)
    {
        if (a == null || b == null)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (b.Contains(a[i]))
                return true;
        }

        return false;
    }

    private void AddUnique(List<int> target, IReadOnlyList<int> source)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
            AddUnique(target, source[i]);
    }

    private void AddUnique(List<int> target, int value)
    {
        if (target == null)
            return;

        if (!target.Contains(value))
            target.Add(value);
    }

    private void EnsureBatchExists(List<BattleActionBatch> batches, int index)
    {
        while (batches.Count <= index)
            batches.Add(new BattleActionBatch());
    }

    private void RemoveEmptyTailBatches(List<BattleActionBatch> batches)
    {
        if (batches == null)
            return;

        for (int i = batches.Count - 1; i >= 0; i--)
        {
            BattleActionBatch batch = batches[i];

            if (batch == null)
            {
                batches.RemoveAt(i);
                continue;
            }

            bool empty =
                batch.PlayerCommands.Count <= 0 &&
                batch.MonsterCommands.Count <= 0;

            if (empty)
                batches.RemoveAt(i);
            else
                break;
        }
    }
}