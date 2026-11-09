using System.Collections.Generic;
using Relic.Gameplay.Data;

public class DebuffDoubleEffect : BattleEffectBase
{
    public override string EffectId => "E_DebuffDouble";

    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerTarget != null)
        {
            MultiplyHighest(context.PlayerTarget.RuntimeData?.StatusEffects, context.Value);
            BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(context.PlayerTarget);
        }
        else if (context?.MonsterTarget != null)
        {
            MultiplyHighest(context.MonsterTarget.RuntimeData?.StatusEffects, context.Value);
            context.MonsterTarget.ShowTemporaryHUDForEffect();
        }
    }

    private static void MultiplyHighest(List<StatusEffectRuntimeData> statuses, int multiplier)
    {
        if (statuses == null || multiplier <= 0 || DataManager.Instance?.EffectDatabase == null)
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
            highest.Stack = (int)System.Math.Min(int.MaxValue, (long)highest.Stack * multiplier);
    }
}
