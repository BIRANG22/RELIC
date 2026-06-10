using System.Collections.Generic;

public class BattleActionBatch
{
    public readonly List<PlayerReservedCommand> PlayerCommands = new();
    public readonly List<MonsterReservedCommand> MonsterCommands = new();
}