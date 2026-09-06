using Relic.Gameplay.Data;

public static class BattleRunStatisticsRecorder
{
    public static void RecordDamageTaken(string characterId, int damage, bool died)
    {
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null || string.IsNullOrWhiteSpace(characterId))
            return;

        BattleRunStatisticsService.RecordDamageTaken(runtime, characterId, damage);
        if (died)
            BattleRunStatisticsService.RecordDeath(runtime, characterId);
    }

    public static void RecordDamageDealt(string characterId, int damage, bool killed)
    {
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null || string.IsNullOrWhiteSpace(characterId))
            return;

        BattleRunStatisticsService.RecordDamageDealt(runtime, characterId, damage);
        if (killed)
            BattleRunStatisticsService.RecordKill(runtime, characterId);
    }

    public static void RecordBuffApplied(string characterId, int value)
    {
        BattleRuntimeData runtime = DataManager.Instance?.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null || string.IsNullOrWhiteSpace(characterId))
            return;

        BattleRunStatisticsService.RecordBuffApplied(runtime, characterId, value);
    }
}
