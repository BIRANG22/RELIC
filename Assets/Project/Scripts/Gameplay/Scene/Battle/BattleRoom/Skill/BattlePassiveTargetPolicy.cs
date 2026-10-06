using Relic.Gameplay.Data;

public enum BattlePassiveTargetGroup
{
    Owner,
    Players,
    Monsters
}

public static class BattlePassiveTargetPolicy
{
    public static BattlePassiveTargetGroup Resolve(TargetType target)
    {
        return target switch
        {
            TargetType.PlayerParty => BattlePassiveTargetGroup.Players,
            TargetType.EnemyParty => BattlePassiveTargetGroup.Monsters,
            _ => BattlePassiveTargetGroup.Owner
        };
    }
}
