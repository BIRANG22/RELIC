using UnityEngine;

public class ManaEffect : BattleEffectBase
{
    public override string EffectId => "E_Mana";

    protected override void Apply(BattleEffectContext context)
    {
        BattleCharacter target = BattleEffectUtility.GetPlayerTargetOrCaster(context);
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        int amount = BattleEffectUtility.GetRepeatedValue(context);
        target.RuntimeData.CurrentCost = Mathf.Min(
            target.RuntimeData.MaxCost,
            Mathf.Max(0, target.RuntimeData.CurrentCost) + amount);

        BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(target);
    }
}
