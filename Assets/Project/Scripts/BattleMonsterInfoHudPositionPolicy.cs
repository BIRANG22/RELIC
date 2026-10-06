public enum BattleHudMonsterInfoPosition
{
    Down,
    Up
}

public static class BattleMonsterInfoHudPositionPolicy
{
    public static BattleHudMonsterInfoPosition Resolve(
        bool monsterInfoOpen,
        bool canAcceptPlayerInput,
        bool introBlocking,
        bool battleEnded)
    {
        if (monsterInfoOpen || !canAcceptPlayerInput || introBlocking || battleEnded)
            return BattleHudMonsterInfoPosition.Down;

        return BattleHudMonsterInfoPosition.Up;
    }
}
