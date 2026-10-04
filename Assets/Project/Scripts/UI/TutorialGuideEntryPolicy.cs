using Relic.Gameplay.Data;

public static class TutorialGuideEntryPolicy
{
    public static bool ShouldSpawn(BattleRuntimeData runtime) => runtime?.IsDemoBattle == true;
}
