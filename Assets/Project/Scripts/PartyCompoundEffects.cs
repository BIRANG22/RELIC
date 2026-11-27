using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class RemoveFirstDebuffEffect : BattleEffectBase
{
    public override string EffectId => "E_RemoveFirstDebuff";
    public override bool CanApply(BattleEffectContext context)
    {
        if (context == null || BattleEffectUtility.GetRepeatedValue(context) <= 0) return false;
        var player = context.PlayerTarget ?? context.PlayerCaster;
        var monster = context.MonsterTarget ?? context.MonsterCaster;
        var statuses = player != null ? player.RuntimeData?.StatusEffects : monster?.RuntimeData?.StatusEffects;
        if (statuses == null) return false;
        foreach (var status in statuses)
        {
            var data = status == null ? null : DataManager.Instance?.EffectDatabase?.Get(status.EffectId);
            if (data != null && data.EffectType == EffectType.Harmful && !status.IsPassive) return true;
        }
        return false;
    }
    protected override void Apply(BattleEffectContext context)
    {
        var player = context.PlayerTarget ?? context.PlayerCaster;
        var monster = context.MonsterTarget ?? context.MonsterCaster;
        var statuses = player != null ? player.RuntimeData?.StatusEffects : monster?.RuntimeData?.StatusEffects;
        if (statuses == null) return;
        int remaining = BattleEffectUtility.GetRepeatedValue(context);
        for (int i = 0; i < statuses.Count && remaining > 0;)
        {
            var status = statuses[i];
            var data = status == null ? null : DataManager.Instance?.EffectDatabase?.Get(status.EffectId);
            if (data != null && data.EffectType == EffectType.Harmful && !status.IsPassive)
            { statuses.RemoveAt(i); remaining--; }
            else i++;
        }
        if (player != null) BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(player);
    }
}

public sealed class ReduceErosionEffect : BattleEffectBase
{
    public override string EffectId => "E_ReduceErosion";
    public override bool IsPartyShared => true;
    public override bool CanApply(BattleEffectContext context) =>
        context != null && BattleEffectUtility.GetRepeatedValue(context) > 0 &&
        BattleErosionRuntimeService.CurrentValue > BattleErosionRuntimeService.MinValue;
    protected override void Apply(BattleEffectContext context)
    {
        BattleErosionRuntimeService.Add(-BattleEffectUtility.GetRepeatedValue(context), EffectId);
    }
}

public sealed class ReviveEffect : BattleEffectBase
{
    public override string EffectId => "E_Revive";
    public override bool AcceptsDeadTarget => true;
    public override bool CanApply(BattleEffectContext context) =>
        context?.PlayerTarget?.RuntimeData?.IsDead == true && BattleEffectUtility.GetRepeatedValue(context) > 0;
    protected override void Apply(BattleEffectContext context)
    {
        var unit = context.PlayerTarget;
        var runtime = unit.RuntimeData;
        if (runtime.MaxHP <= 0) return;
        runtime.CurrentHP = Mathf.Clamp(Mathf.CeilToInt(runtime.MaxHP * (BattleEffectUtility.GetRepeatedValue(context) / 100f)), 1, runtime.MaxHP);
        unit.GetComponent<BattleUnitAnimator>()?.PlayRevived();
        BattleEffectUtility.OnPlayerHudRefreshRequested?.Invoke(unit);
    }
}
