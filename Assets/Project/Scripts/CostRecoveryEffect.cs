using UnityEngine;

public class CostRecoveryEffect : BattleEffectBase
{
    public override string EffectId => "E_CostRecovery";
    protected override void Apply(BattleEffectContext context)
    {
        BattleCharacter target = context?.PlayerTarget != null ? context.PlayerTarget : context?.PlayerCaster;
        if (target?.RuntimeData == null) return;
        int value = BattleEffectUtility.GetRepeatedValue(context);
        target.RuntimeData.CurrentCost = Mathf.Min(target.RuntimeData.MaxCost, target.RuntimeData.CurrentCost + value);
    }
}
