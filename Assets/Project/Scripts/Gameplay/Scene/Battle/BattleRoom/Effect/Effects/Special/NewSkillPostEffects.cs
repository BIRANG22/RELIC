using UnityEngine;

public sealed class MissSelfDamageEffect : BattleEffectBase
{
    public override string EffectId => "E_MissSelfDamage";
    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerCaster == null || (context.ExecutionResult?.SuccessfulHitCount ?? 0) > 0) return;
        BattleEffectUtility.DamagePlayer(context.PlayerCaster, Mathf.Max(0, context.Value));
    }
}

public sealed class RestoreManaPerHitTargetEffect : BattleEffectBase
{
    public override string EffectId => "E_RestoreManaPerHitTarget";
    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerCaster?.RuntimeData == null || context.ExecutionResult == null) return;
        int amount = NewSkillEffectRules.ResolveManaRestore(context.ExecutionResult.SuccessfulHitCount, context.Value);
        context.PlayerCaster.RuntimeData.CurrentCost = Mathf.Min(
            context.PlayerCaster.RuntimeData.MaxCost,
            context.PlayerCaster.RuntimeData.CurrentCost + amount);
    }
}

public sealed class ValueUpOnKillEffect : BattleEffectBase
{
    public override string EffectId => "E_ValueUpOnKill";
    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerCaster?.RuntimeData == null || context.ExecutionResult == null || DataManager.Instance == null) return;
        int amount = NewSkillEffectRules.ResolvePermanentValueBonus(context.ExecutionResult.KillCount, context.Value);
        DataManager.Instance.SkillRuntimeStore.AddPermanentValueBonus(
            context.PlayerCaster.RuntimeData.CharacterId, context.PlayerSkillData?.SkillId, amount);
    }
}

public sealed class ValueUpOnHitEffect : BattleEffectBase
{
    public override string EffectId => "E_ValueUpOnHit";
    protected override void Apply(BattleEffectContext context)
    {
        if (context?.PlayerCaster?.RuntimeData == null || context.ExecutionResult == null || DataManager.Instance == null) return;
        int amount = NewSkillEffectRules.ResolveValueUpOnHit(
            context.ExecutionResult.SuccessfulHitCount,
            context.Value);
        DataManager.Instance.SkillRuntimeStore.AddBattleValueBonus(
            context.PlayerCaster.RuntimeData.CharacterId,
            context.PlayerSkillData?.SkillId,
            amount);
    }
}

public sealed class DamageUpIfBleedingEffect : BattleEffectBase
{
    public override string EffectId => "E_DamageUpIfBleeding";
    protected override void Apply(BattleEffectContext context) =>
        new StrikeEffect().Execute(context);
}

public sealed class StrikeCountByBuffEffect : BattleEffectBase
{
    public override string EffectId => "E_StrikeCountByBuff";
    protected override void Apply(BattleEffectContext context) =>
        new StrikeEffect().Execute(context);
}
