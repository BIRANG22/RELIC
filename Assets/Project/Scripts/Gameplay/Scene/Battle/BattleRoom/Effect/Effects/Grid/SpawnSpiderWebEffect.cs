using UnityEngine;

public class SpawnSpiderWebEffect : BattleEffectBase
{
    private const string SpiderWebGridEffectId = "GR_spider_web";

    public override string EffectId => "E_Spawn_Spider_Web";

    protected override void Apply(BattleEffectContext context)
    {
        int gridIndex = ResolveTargetGridIndex(context);

        if (gridIndex < 0)
            return;

        if (BattleOccupancyService.IsOccupiedByMonster(gridIndex))
            return;

        BattleGridEffectController controller =
            Object.FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (controller == null || !controller.TryPlaceEffect(gridIndex, SpiderWebGridEffectId))
            return;

        if (BattleOccupancyService.TryGetCharacterAtGrid(gridIndex, out BattleCharacter character))
            controller.ApplyToPlayer(gridIndex, character);
    }

    private static int ResolveTargetGridIndex(BattleEffectContext context)
    {
        if (context?.MonsterCommand != null)
            return context.MonsterCommand.RangeOriginGridIndex;

        if (context?.PlayerCommand != null)
            return context.PlayerCommand.SelectedGridIndex;

        return -1;
    }
}
