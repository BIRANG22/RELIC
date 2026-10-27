using Relic.Gameplay.Data;

public static class DemoBattleEndingPolicy
{
    public static bool ShouldShow(BattleRuntimeData battleRuntime, MapRuntimeData mapRuntime)
    {
        if (battleRuntime?.IsDemoBattle != true || mapRuntime == null)
            return false;

        if (!MapRuntimeProgressUtility.IsCurrentNodeCleared(mapRuntime))
            return false;

        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);
        if (currentNode == null)
            return false;

        return MapRuntimeProgressUtility.CollectSelectableNextNodes(mapRuntime).Count == 0;
    }
}
