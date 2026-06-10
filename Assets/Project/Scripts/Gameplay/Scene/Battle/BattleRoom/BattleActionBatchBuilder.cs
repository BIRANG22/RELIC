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
            var playerCommands = timelineController.GetPlayerCommands(slotIndex);

            if (playerCommands != null)
            {
                for (int i = 0; i < playerCommands.Count; i++)
                    AddPlayerCommand(batches, playerCommands[i]);
            }

            var monsterCommands = timelineController.GetMonsterCommands(slotIndex);

            if (monsterCommands != null)
            {
                for (int i = 0; i < monsterCommands.Count; i++)
                    AddMonsterCommand(batches, monsterCommands[i]);
            }
        }

        return batches;
    }

    private void AddPlayerCommand(List<BattleActionBatch> batches, PlayerReservedCommand command)
    {
        if (command == null)
            return;

        for (int i = 0; i < batches.Count; i++)
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

    private void AddMonsterCommand(List<BattleActionBatch> batches, MonsterReservedCommand command)
    {
        if (command == null)
            return;

        if (batches.Count == 0)
            batches.Add(new BattleActionBatch());

        batches[0].MonsterCommands.Add(command);
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

            if (existing.ReservedMoveGridIndex >= 0 &&
                command.ReservedMoveGridIndex >= 0 &&
                existing.ReservedMoveGridIndex == command.ReservedMoveGridIndex)
            {
                return false;
            }
        }

        return true;
    }
}