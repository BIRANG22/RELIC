public class BurnEffect : BattleEffectBase
{
    public override string EffectId => "E_Burn";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null && context.PlayerTarget.RuntimeData != null)
        {
            AddOrStackStatus(
                context.PlayerTarget.RuntimeData.StatusEffects,
                EffectId,
                context.Value
            );

            return;
        }

        if (context.MonsterTarget != null && context.MonsterTarget.RuntimeData != null)
        {
            AddOrStackStatus(
                context.MonsterTarget.RuntimeData.StatusEffects,
                EffectId,
                context.Value
            );

            context.MonsterTarget.ShowAndRefreshHUD();
        }
    }

    private void AddOrStackStatus(
        System.Collections.Generic.List<Relic.Gameplay.Data.StatusEffectRuntimeData> statusEffects,
        string effectId,
        int stack)
    {
        if (statusEffects == null)
            return;

        stack = UnityEngine.Mathf.Max(1, stack);

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            if (statusEffects[i].EffectId != effectId)
                continue;

            statusEffects[i].Stack += stack;
            return;
        }

        statusEffects.Add(new Relic.Gameplay.Data.StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack
        });
    }
}
