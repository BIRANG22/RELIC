using System.Collections.Generic;

public class BattleActionBatchBuilder
{
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

        DebugPrintBatches(batches);
        return batches;
    }

    private void DebugPrintBatches(List<BattleActionBatch> batches)
    {
        if (batches == null)
            return;

        for (int i = 0; i < batches.Count; i++)
        {
            BattleActionBatch batch = batches[i];

            if (batch == null)
                continue;

            for (int p = 0; p < batch.PlayerCommands.Count; p++)
            {
                PlayerReservedCommand command = batch.PlayerCommands[p];

                if (command == null)
                    continue;
            }

            for (int m = 0; m < batch.MonsterCommands.Count; m++)
            {
                MonsterReservedCommand command = batch.MonsterCommands[m];

                if (command == null)
                    continue;
            }
        }
    }

    private void AddPlayerCommand(
        List<BattleActionBatch> batches,
        PlayerReservedCommand command,
        int minBatchIndex)
    {
        if (command == null)
            return;

        EnsureBatchExists(batches, minBatchIndex);

        for (int i = minBatchIndex; i < batches.Count; i++)
        {
            if (CanAddPlayerCommand(batches[i], command))
            {
                batches[i].PlayerCommands.Add(command);
                return;
            }
        }

        BattleActionBatch newBatch = new();
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

        EnsureBatchExists(batches, minBatchIndex);

        for (int i = minBatchIndex; i < batches.Count; i++)
        {
            if (CanAddMonsterCommand(batches[i], command))
            {
                batches[i].MonsterCommands.Add(command);
                return;
            }
        }

        BattleActionBatch newBatch = new();
        newBatch.MonsterCommands.Add(command);
        batches.Add(newBatch);
    }

    private void EnsureBatchExists(List<BattleActionBatch> batches, int index)
    {
        while (batches.Count <= index)
            batches.Add(new BattleActionBatch());
    }

    private bool CanAddPlayerCommand(BattleActionBatch batch, PlayerReservedCommand command)
    {
        if (batch == null || command == null)
            return false;

        for (int i = 0; i < batch.PlayerCommands.Count; i++)
        {
            PlayerReservedCommand existing = batch.PlayerCommands[i];

            if (existing == null)
                continue;

            if (existing.CharacterId == command.CharacterId)
                return false;

            if (IsSameMoveTarget(existing, command))
                return false;
        }

        return true;
    }

    private bool CanAddMonsterCommand(BattleActionBatch batch, MonsterReservedCommand command)
    {
        if (batch == null || command == null)
            return false;

        for (int i = 0; i < batch.MonsterCommands.Count; i++)
        {
            MonsterReservedCommand existing = batch.MonsterCommands[i];

            if (existing == null)
                continue;

            if (existing.RuntimeId == command.RuntimeId)
                return false;
        }

        return true;
    }

    private bool IsSameMoveTarget(PlayerReservedCommand a, PlayerReservedCommand b)
    {
        if (a == null || b == null)
            return false;

        if (a.ReservedMoveGridIndex < 0 || b.ReservedMoveGridIndex < 0)
            return false;

        return a.ReservedMoveGridIndex == b.ReservedMoveGridIndex;
    }
}