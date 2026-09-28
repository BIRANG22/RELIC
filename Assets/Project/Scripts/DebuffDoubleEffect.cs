using System.Collections.Generic;
using Relic.Gameplay.Data;

public class DebuffDoubleEffect : BattleEffectBase
{
    public override string EffectId => "E_DebuffDouble";

    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerTarget != null)
        {
            DoubleHighest(context.PlayerTarget.RuntimeData?.StatusEffects);
            BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(context.PlayerTarget);
        }
        else if (context?.MonsterTarget != null)
        {
            DoubleHighest(context.MonsterTarget.RuntimeData?.StatusEffects);
            context.MonsterTarget.ShowTemporaryHUDForEffect();
        }
    }

    private static void DoubleHighest(List<StatusEffectRuntimeData> statuses)
    {
        if (statuses == null || DataManager.Instance?.EffectDatabase == null)
            return;

        StatusEffectRuntimeData highest = null;
        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status == null || status.Stack <= 0)
                continue;

            if (!DataManager.Instance.EffectDatabase.TryGet(status.EffectId, out EffectMasterData data) ||
                data == null || data.EffectType != EffectType.Harmful)
                continue;

            if (highest == null || status.Stack > highest.Stack)
                highest = status;
        }

        if (highest != null)
            highest.Stack *= 2;
    }
}
