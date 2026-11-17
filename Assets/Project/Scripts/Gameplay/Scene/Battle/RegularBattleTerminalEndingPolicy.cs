using Relic.Gameplay.Data;

public static class RegularBattleTerminalEndingPolicy
{
    public static bool ShouldEnter(
        BattleRuntimeData battleRuntime,
        MapRuntimeData mapRuntime,
        bool isDefeat,
        bool alreadyShown)
    {
        return !alreadyShown && ShouldShow(battleRuntime, mapRuntime, isDefeat);
    }

    public static bool ShouldShow(
        BattleRuntimeData battleRuntime,
        MapRuntimeData mapRuntime,
        bool isDefeat)
    {
        if (battleRuntime == null ||
            battleRuntime.IsDemoBattle ||
            battleRuntime.IsTutorialBattle ||
            isDefeat ||
            mapRuntime == null)
        {
            return false;
        }

        if (!MapRuntimeProgressUtility.IsCurrentNodeCleared(mapRuntime))
            return false;

        return MapRuntimeProgressUtility.FindCurrentNode(mapRuntime) != null &&
               MapRuntimeProgressUtility.CollectSelectableNextNodes(mapRuntime).Count == 0;
    }
}
