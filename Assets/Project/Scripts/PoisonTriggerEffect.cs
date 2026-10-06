using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTriggerEffect : BattleEffectBase
{
    private readonly string effectId;
    private readonly bool addValueBeforeTrigger;
    public override string EffectId => effectId;
    private const string PoisonEffectId = "E_Poison";

    public PoisonTriggerEffect(string effectId = "E_PoisonTrigger")
    {
        this.effectId = string.IsNullOrWhiteSpace(effectId) ? "E_PoisonTrigger" : effectId.Trim();
        addValueBeforeTrigger = this.effectId == "E_PoisonTrigger";
    }

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        int addValue = addValueBeforeTrigger
            ? BattleEffectUtility.GetRepeatedValue(context)
            : 0;

        if (context.PlayerTarget != null)
        {
            if (addValue > 0)
                BattleEffectUtility.AddStatusToPlayer(context.PlayerTarget, PoisonEffectId, addValue, 1);
            TriggerPlayer(context.PlayerTarget);
            return;
        }

        if (context.MonsterTarget != null)
        {
            if (addValue > 0)
                BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, PoisonEffectId, addValue, 1);
            TriggerMonster(context.MonsterTarget);
        }
    }

    private static void TriggerPlayer(BattleCharacter target)
    {
        if (target?.RuntimeData?.StatusEffects == null || target.RuntimeData.IsDead)
            return;

        StatusEffectRuntimeData poison = FindPoison(target.RuntimeData.StatusEffects);
        if (poison == null || poison.Stack <= 0)
            return;

        int damage = poison.Stack;
        BattleEffectUtility.PoisonDamagePlayer(target, damage);
        DecreasePoison(target.RuntimeData.StatusEffects, poison);
        BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(target);
    }

    private static void TriggerMonster(Relic.Gameplay.Monster.MonsterUnit target)
    {
        if (target?.RuntimeData?.StatusEffects == null || target.RuntimeData.IsDead)
            return;

        StatusEffectRuntimeData poison = FindPoison(target.RuntimeData.StatusEffects);
        if (poison == null || poison.Stack <= 0)
            return;

        int damage = poison.Stack;
        BattleEffectUtility.PoisonDamageMonster(target, damage);
        DecreasePoison(target.RuntimeData.StatusEffects, poison);
        target.ShowTemporaryHUDForEffect();
    }

    private static StatusEffectRuntimeData FindPoison(List<StatusEffectRuntimeData> statuses)
    {
        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];
            if (status != null && status.EffectId == PoisonEffectId && !status.IsPassive)
                return status;
        }
        return null;
    }

    private static void DecreasePoison(List<StatusEffectRuntimeData> statuses, StatusEffectRuntimeData poison)
    {
        poison.Stack = Mathf.Max(0, poison.Stack - 1);
        if (poison.Stack <= 0)
            statuses.Remove(poison);
    }
}
