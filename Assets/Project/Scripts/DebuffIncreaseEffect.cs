using System.Collections.Generic;
using Relic.Gameplay.Data;

public class DebuffIncreaseEffect : BattleEffectBase
{
    public override string EffectId => "E_DebuffIncrease";

    protected override void Apply(BattleEffectContext context)
    {
        int amount = BattleEffectUtility.GetRepeatedValue(context);
        if (amount <= 0)
            return;

        if (context?.PlayerTarget != null)
        {
            Increase(context.PlayerTarget.RuntimeData?.StatusEffects, amount);
            BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(context.PlayerTarget);
        }
        else if (context?.MonsterTarget != null)
        {
            Increase(context.MonsterTarget.RuntimeData?.StatusEffects, amount);
            context.MonsterTarget.ShowTemporaryHUDForEffect();
        }
    }

    private static void Increase(List<StatusEffectRuntimeData> statuses, int amount)
    {
        if (statuses == null || DataManager.Instance?.EffectDatabase == null)
            return;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status == null || status.Stack <= 0)
                continue;

            if (DataManager.Instance.EffectDatabase.TryGet(status.EffectId, out EffectMasterData data) &&
                data != null && data.EffectType == EffectType.Harmful)
            {
                status.Stack += amount;
            }
        }
    }
}
